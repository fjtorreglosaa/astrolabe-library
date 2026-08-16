using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Features.Identity.Commands.AdministerUser;
using Astrolabe.Application.Features.Identity.Queries.SearchUsers;
using Astrolabe.Application.Shared.Catalog;
using Astrolabe.Application.Tests.TestSupport;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Features.Network.ValueObjects;
using Astrolabe.Domain.Primitives;
using FluentAssertions;
using Moq;

namespace Astrolabe.Application.Tests.Features.Identity;

/// <summary>
/// Covers the staff user directory, and above all **the scope matrix** — the testing PLAN-001 asks
/// for by name at Stage 6: an administrator of Midtown and Harlem sees and touches nothing in
/// Chicago.
///
/// <para>
/// Scoped by city rather than by library, because a member belongs to a city of residence and
/// reaches a branch through it — there is no assignment tying a member to one library. BR-NET-010
/// is what makes the scoping compulsory: an administrator with no assignments must see no
/// administrative data at all.
/// </para>
/// </summary>
[TestFixture]
public sealed class UserDirectoryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid AdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid NewYork = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Chicago = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Midtown = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Harlem = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid Loop = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private IdentityUnitOfWorkMock _identity = null!;
    private AuditUnitOfWorkMock _audit = null!;
    private MembershipUnitOfWorkMock _membership = null!;
    private Mock<ICurrentUser> _currentUser = null!;
    private Mock<ILibraryScopeProvider> _scope = null!;
    private Mock<ILibraryLocationProvider> _locations = null!;

    private static CancellationToken Ct => TestContext.CurrentContext.CancellationToken;

    [SetUp]
    public void SetUp()
    {
        _identity = new IdentityUnitOfWorkMock();
        _audit = new AuditUnitOfWorkMock();
        _membership = new MembershipUnitOfWorkMock();

        _currentUser = new Mock<ICurrentUser>();
        _currentUser.SetupGet(u => u.UserId).Returns(AdminId);
        _currentUser.SetupGet(u => u.Role).Returns(UserRole.Admin);

        _scope = new Mock<ILibraryScopeProvider>();
        // The demo administrator: Midtown and Harlem, both in New York. Loop is Chicago's.
        _scope.Setup(s => s.GetCurrentScopeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(LibraryScope.Of([Midtown, Harlem]));

        _locations = new Mock<ILibraryLocationProvider>();
        _locations.Setup(l => l.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, BookProjection.LibraryLocation>
            {
                [Midtown] = new(Midtown, "Midtown", NewYork, "New York", IsActive: true),
                [Harlem] = new(Harlem, "Harlem", NewYork, "New York", IsActive: true),
                [Loop] = new(Loop, "Loop", Chicago, "Chicago", IsActive: true),
            });
        _locations.Setup(l => l.GetHomeLibraryByCityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, Guid> { [NewYork] = Midtown, [Chicago] = Loop });

        _membership.Subscriptions
            .Setup(r => r.GetActivePlansForAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, PlanTier>());
    }

    private void SignInAs(UserRole role, LibraryScope reach)
    {
        _currentUser.SetupGet(u => u.Role).Returns(role);
        _scope.Setup(s => s.GetCurrentScopeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(reach);
    }

    private static User AMember(Guid cityId, string name = "Ada Lovelace")
    {
        var user = User.Register(
            Email.Create($"{Guid.NewGuid():N}@example.com").Value,
            PasswordHash.FromHashedValue("AQAAAAIAAYag..."),
            name, Guid.NewGuid(), cityId, PlanTier.Plus, Now).Value;

        user.Verify(Now);
        user.ClearDomainEvents();

        return user;
    }

    private SearchUsersQueryHandler SearchHandler() =>
        new(_identity.Object, _membership.Object, _scope.Object, _locations.Object,
            _currentUser.Object);

    private AdministerUserCommandHandler AdministerHandler() =>
        new(_identity.Object, _audit.Object, _scope.Object, _locations.Object,
            _currentUser.Object, new FixedClock(Now));

    private void DirectoryReturns(params User[] users) =>
        _identity.Users
            .Setup(r => r.SearchAsync(
                It.IsAny<string>(), It.IsAny<UserStatus?>(), It.IsAny<UserRole?>(),
                It.IsAny<IReadOnlyCollection<Guid>?>(), It.IsAny<bool>(),
                It.IsAny<UserSortKey>(), It.IsAny<SortDirection>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PagedResult<User>.Create(users, 1, 20, users.Length));

    private static SearchUsersQuery AQuery() =>
        new(null, null, null, false, UserSortKey.CreatedAt, SortDirection.Descending, 1, 20);

    // ---------- The scope matrix: listing ----------

    [Test]
    public async Task AnAdmin_AsksTheDirectoryOnlyForTheCitiesTheyAdminister()
    {
        IReadOnlyCollection<Guid>? asked = null;
        _identity.Users
            .Setup(r => r.SearchAsync(
                It.IsAny<string>(), It.IsAny<UserStatus?>(), It.IsAny<UserRole?>(),
                It.IsAny<IReadOnlyCollection<Guid>?>(), It.IsAny<bool>(),
                It.IsAny<UserSortKey>(), It.IsAny<SortDirection>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback((string _, UserStatus? _, UserRole? _, IReadOnlyCollection<Guid>? cities,
                bool _, UserSortKey _, SortDirection _, int _, int _, CancellationToken _) =>
                asked = cities)
            .ReturnsAsync(PagedResult<User>.Empty(1, 20));

        await SearchHandler().Handle(AQuery(), Ct);

        // Midtown and Harlem are both New York, so one city — and Chicago is nowhere in the filter.
        asked.Should().BeEquivalentTo([NewYork]);
    }

    [Test]
    public async Task ASuperAdmin_AsksForEverything()
    {
        // Null is "unrestricted" and an empty list is "nothing". Conflating them is the one mistake
        // here that turns BR-NET-010 into its opposite, so it is asserted directly.
        IReadOnlyCollection<Guid>? asked = new List<Guid>();
        SignInAs(UserRole.SuperAdmin, LibraryScope.Unrestricted());
        _identity.Users
            .Setup(r => r.SearchAsync(
                It.IsAny<string>(), It.IsAny<UserStatus?>(), It.IsAny<UserRole?>(),
                It.IsAny<IReadOnlyCollection<Guid>?>(), It.IsAny<bool>(),
                It.IsAny<UserSortKey>(), It.IsAny<SortDirection>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback((string _, UserStatus? _, UserRole? _, IReadOnlyCollection<Guid>? cities,
                bool _, UserSortKey _, SortDirection _, int _, int _, CancellationToken _) =>
                asked = cities)
            .ReturnsAsync(PagedResult<User>.Empty(1, 20));

        await SearchHandler().Handle(AQuery(), Ct);

        asked.Should().BeNull();
    }

    [Test]
    public async Task AnAdminWithNoAssignments_AsksForNothingRatherThanEverything()
    {
        // BR-NET-010 exactly. The dangerous failure is an empty scope reading as "no filter".
        IReadOnlyCollection<Guid>? asked = null;
        SignInAs(UserRole.Admin, LibraryScope.Empty());
        _identity.Users
            .Setup(r => r.SearchAsync(
                It.IsAny<string>(), It.IsAny<UserStatus?>(), It.IsAny<UserRole?>(),
                It.IsAny<IReadOnlyCollection<Guid>?>(), It.IsAny<bool>(),
                It.IsAny<UserSortKey>(), It.IsAny<SortDirection>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback((string _, UserStatus? _, UserRole? _, IReadOnlyCollection<Guid>? cities,
                bool _, UserSortKey _, SortDirection _, int _, int _, CancellationToken _) =>
                asked = cities)
            .ReturnsAsync(PagedResult<User>.Empty(1, 20));

        await SearchHandler().Handle(AQuery(), Ct);

        asked.Should().NotBeNull().And.BeEmpty();
    }

    [Test]
    public async Task AMember_IsRefusedTheDirectoryEntirely()
    {
        SignInAs(UserRole.Member, LibraryScope.Empty());

        var result = await SearchHandler().Handle(AQuery(), Ct);

        result.Error.Should().Be(NetworkErrors.StaffRequired);
    }

    [Test]
    public async Task TheListingTellsEachRowWhetherItCanBeActedOn()
    {
        // Decided server-side so the screen cannot offer a button the API would refuse.
        var member = AMember(NewYork);
        DirectoryReturns(member);

        var result = await SearchHandler().Handle(AQuery(), Ct);

        result.Value.Items.Should().ContainSingle()
            .Which.CanAdminister.Should().BeTrue();
    }

    // ---------- The scope matrix: acting ----------

    [Test]
    public async Task AnAdmin_MayBlockAMemberInTheirOwnCity()
    {
        var member = AMember(NewYork);
        _identity.Users.Setup(r => r.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var result = await AdministerHandler().Handle(
            new AdministerUserCommand(member.Id, UserAdministrationAction.Block), Ct);

        result.IsSuccess.Should().BeTrue();
        member.Status.Should().Be(UserStatus.Blocked);
        _identity.Saved.Should().Be(1);
    }

    [Test]
    public async Task AnAdmin_MayNotTouchAMemberInAnotherCity()
    {
        // PLAN-001's Stage 6 acceptance, in one test: Midtown and Harlem yes, Chicago no.
        var member = AMember(Chicago);
        _identity.Users.Setup(r => r.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var result = await AdministerHandler().Handle(
            new AdministerUserCommand(member.Id, UserAdministrationAction.Block), Ct);

        result.Error.Should().Be(IdentityErrors.AccountOutOfScope);
        member.Status.Should().Be(UserStatus.Active, "nothing may change");
        _identity.Saved.Should().Be(0);
    }

    [Test]
    public async Task AnAdminWithNoAssignments_MayTouchNobody()
    {
        SignInAs(UserRole.Admin, LibraryScope.Empty());
        var member = AMember(NewYork);
        _identity.Users.Setup(r => r.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var result = await AdministerHandler().Handle(
            new AdministerUserCommand(member.Id, UserAdministrationAction.Block), Ct);

        result.Error.Should().Be(IdentityErrors.AccountOutOfScope);
        _identity.Saved.Should().Be(0);
    }

    [Test]
    public async Task ASuperAdmin_MayActAnywhere()
    {
        SignInAs(UserRole.SuperAdmin, LibraryScope.Unrestricted());
        var member = AMember(Chicago);
        _identity.Users.Setup(r => r.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var result = await AdministerHandler().Handle(
            new AdministerUserCommand(member.Id, UserAdministrationAction.Block), Ct);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task AuthorityIsCheckedBeforeReach()
    {
        // An administrator refused another administrator must be told to ask a super administrator,
        // not that the account is in the wrong city. The second reads as "try a colleague in
        // Chicago", which would send them somewhere that cannot help either.
        var otherAdmin = User.Invite(
            Email.Create("dana@astrolabe.co").Value, "Dana", UserRole.Admin, Now).Value;
        _identity.Users.Setup(r => r.GetByIdAsync(otherAdmin.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherAdmin);

        var result = await AdministerHandler().Handle(
            new AdministerUserCommand(otherAdmin.Id, UserAdministrationAction.Block), Ct);

        result.Error.Should().Be(IdentityErrors.SuperAdminRequiredForStaff);
    }

    // ---------- Audit completeness, the other Stage 6 requirement ----------

    [TestCase(UserAdministrationAction.Block, "identity.blocked")]
    [TestCase(UserAdministrationAction.Delete, "identity.deleted")]
    public async Task EveryAdministrativeActionWritesItsOwnTrailEntry(
        UserAdministrationAction action, string expected)
    {
        var member = AMember(NewYork);
        _identity.Users.Setup(r => r.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        await AdministerHandler().Handle(new AdministerUserCommand(member.Id, action), Ct);

        _audit.Entries.Verify(r => r.AddAsync(
            It.Is<Domain.Features.Audit.Entities.AuditEntry>(e =>
                e.Action == expected && e.ActorUserId == AdminId && e.SubjectUserId == member.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UnblockingAndRestoringAreDistinguishableInTheTrail()
    {
        // Both land on Active, so an auditor reading only the outcome could not tell a lifted block
        // from a reinstated deletion. The trail records what was intended.
        var blocked = AMember(NewYork);
        blocked.Block(Now);
        blocked.ClearDomainEvents();
        _identity.Users.Setup(r => r.GetByIdAsync(blocked.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blocked);

        await AdministerHandler().Handle(
            new AdministerUserCommand(blocked.Id, UserAdministrationAction.Unblock), Ct);

        _audit.Entries.Verify(r => r.AddAsync(
            It.Is<Domain.Features.Audit.Entities.AuditEntry>(e => e.Action == "identity.unblocked"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ARefusedActionWritesNoTrailEntry()
    {
        // A trail that records attempts as though they happened is worse than none.
        var member = AMember(Chicago);
        _identity.Users.Setup(r => r.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        await AdministerHandler().Handle(
            new AdministerUserCommand(member.Id, UserAdministrationAction.Block), Ct);

        _audit.Entries.Verify(r => r.AddAsync(
            It.IsAny<Domain.Features.Audit.Entities.AuditEntry>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task BlockingRaisesTheEventThatEndsEverySession()
    {
        // BR-IDN-007. The handler never touches sessions; the aggregate raises and the dispatcher
        // acts after the commit, so no caller can forget it.
        var member = AMember(NewYork);
        _identity.Users.Setup(r => r.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        await AdministerHandler().Handle(
            new AdministerUserCommand(member.Id, UserAdministrationAction.Block), Ct);

        member.DomainEvents.Should().ContainSingle(e =>
            e is Domain.Features.Identity.Events.UserAccessRevoked);
    }
}
