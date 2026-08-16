using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Network.Entities;
using Astrolabe.Domain.Features.Network.Repositories;
using Astrolabe.Infrastructure.Features.Network;
using Astrolabe.Infrastructure.Persistence;
using Astrolabe.Infrastructure.Persistence.Repositories;
using Astrolabe.Infrastructure.Persistence.Repositories.Identity;
using Astrolabe.Infrastructure.Persistence.Repositories.Network;
using Astrolabe.Infrastructure.Tests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Astrolabe.Infrastructure.Tests.Features.Network;

/// <summary>
/// Covers BR-NET-006, BR-NET-007, BR-NET-010 and BR-NET-011 at the seam every other domain consumes.
/// </summary>
[TestFixture]
public sealed class LibraryScopeProviderTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Midtown = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Harlem = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Loop = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private AstrolabeDbContext _context = null!;
    private ILibraryAssignmentRepository _assignments = null!;

    private static CancellationToken Ct => TestContext.CurrentContext.CancellationToken;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContext.Create();
        _assignments = new LibraryAssignmentRepository(_context);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private static ICurrentUser AnonymousUser()
    {
        var mock = new Mock<ICurrentUser>();
        mock.SetupGet(u => u.IsAuthenticated).Returns(false);
        mock.SetupGet(u => u.UserId).Returns((Guid?)null);
        mock.SetupGet(u => u.Role).Returns((UserRole?)null);
        return mock.Object;
    }

    private static ICurrentUser SignedInUser(Guid userId, UserRole role)
    {
        var mock = new Mock<ICurrentUser>();
        mock.SetupGet(u => u.IsAuthenticated).Returns(true);
        mock.SetupGet(u => u.UserId).Returns(userId);
        mock.SetupGet(u => u.Role).Returns(role);
        return mock.Object;
    }

    private async Task GrantAsync(Guid userId, params Guid[] libraryIds)
    {
        foreach (var libraryId in libraryIds)
        {
            await _assignments.AddAsync(
                LibraryAssignment.Grant(Guid.NewGuid(), userId, libraryId, Guid.NewGuid(), Now), Ct);
        }

        await _context.SaveChangesAsync(Ct);
    }

    private LibraryScopeProvider CreateProvider(ICurrentUser user) => new(user, _assignments);

    // ---------- BR-NET-007: super administrator ----------

    [Test]
    public async Task SuperAdmin_GetsAnUnrestrictedScope_WithoutAnyAssignment()
    {
        var provider = CreateProvider(SignedInUser(Guid.NewGuid(), UserRole.SuperAdmin));

        var scope = await provider.GetCurrentScopeAsync(Ct);

        scope.IsUnrestricted.Should().BeTrue();
        scope.Covers(Loop).Should().BeTrue();
    }

    // ---------- BR-NET-006: administrator ----------

    [Test]
    public async Task Admin_GetsExactlyTheirAssignedLibraries()
    {
        var admin = Guid.NewGuid();
        await GrantAsync(admin, Midtown, Harlem);
        var provider = CreateProvider(SignedInUser(admin, UserRole.Admin));

        var scope = await provider.GetCurrentScopeAsync(Ct);

        scope.IsUnrestricted.Should().BeFalse();
        scope.Covers(Midtown).Should().BeTrue();
        scope.Covers(Harlem).Should().BeTrue();
        scope.Covers(Loop).Should().BeFalse("Chicago was never assigned to this administrator");
    }

    [Test]
    public async Task Admin_DoesNotInheritAnotherAdministratorsAssignments()
    {
        var mine = Guid.NewGuid();
        var theirs = Guid.NewGuid();
        await GrantAsync(mine, Midtown);
        await GrantAsync(theirs, Loop);

        var scope = await CreateProvider(SignedInUser(mine, UserRole.Admin)).GetCurrentScopeAsync(Ct);

        scope.Covers(Loop).Should().BeFalse();
    }

    // ---------- BR-NET-010: administrator with nothing assigned ----------

    [Test]
    public async Task Admin_WithNoAssignments_GetsAnEmptyScopeRatherThanAnError()
    {
        var provider = CreateProvider(SignedInUser(Guid.NewGuid(), UserRole.Admin));

        var scope = await provider.GetCurrentScopeAsync(Ct);

        scope.IsEmpty.Should().BeTrue();
        scope.IsUnrestricted.Should().BeFalse();
    }

    // ---------- BR-NET-011: revocation takes effect immediately ----------

    [Test]
    public async Task RevokedAssignment_IsGoneFromTheNextRequestsScope()
    {
        var admin = Guid.NewGuid();
        await GrantAsync(admin, Midtown, Harlem);

        // First request sees both libraries.
        var before = await CreateProvider(SignedInUser(admin, UserRole.Admin)).GetCurrentScopeAsync(Ct);
        before.Covers(Harlem).Should().BeTrue();

        var assignment = await _assignments.GetActiveAsync(admin, Harlem, Ct);
        assignment!.Revoke(Guid.NewGuid(), Now.AddHours(1));
        await _context.SaveChangesAsync(Ct);

        // A new provider stands in for the next request: scoped registration means a fresh instance.
        var after = await CreateProvider(SignedInUser(admin, UserRole.Admin)).GetCurrentScopeAsync(Ct);

        after.Covers(Harlem).Should().BeFalse("a revoked assignment must not survive into the next request");
        after.Covers(Midtown).Should().BeTrue("the remaining assignment is untouched");
    }

    [Test]
    public async Task ScopeIsMemoisedWithinOneRequest_ButNeverBeyondIt()
    {
        var admin = Guid.NewGuid();
        await GrantAsync(admin, Midtown);
        var provider = CreateProvider(SignedInUser(admin, UserRole.Admin));

        var first = await provider.GetCurrentScopeAsync(Ct);
        var second = await provider.GetCurrentScopeAsync(Ct);

        second.Should().BeSameAs(first, "resolving twice in one request must not query twice");
    }

    // ---------- Members and anonymous callers ----------

    [Test]
    public async Task Member_GetsAnEmptyScope()
    {
        const UserRole role = UserRole.Member;

        var member = Guid.NewGuid();
        // Even with a stray assignment row, a member must hold no staff authority.
        await GrantAsync(member, Midtown);

        var scope = await CreateProvider(SignedInUser(member, role)).GetCurrentScopeAsync(Ct);

        scope.IsEmpty.Should().BeTrue();
        scope.Covers(Midtown).Should().BeFalse();
    }

    [Test]
    public async Task AnonymousCaller_GetsAnEmptyScope()
    {
        var scope = await CreateProvider(AnonymousUser()).GetCurrentScopeAsync(Ct);

        scope.IsEmpty.Should().BeTrue();
    }

    [Test]
    public async Task GetScopeForAsync_ResolvesAnotherUsersScope()
    {
        // Used when a super administrator reviews what an administrator can reach.
        var other = Guid.NewGuid();
        await GrantAsync(other, Loop);
        var provider = CreateProvider(SignedInUser(Guid.NewGuid(), UserRole.SuperAdmin));

        var scope = await provider.GetScopeForAsync(other, UserRole.Admin, Ct);

        scope.Covers(Loop).Should().BeTrue();
        scope.Covers(Midtown).Should().BeFalse();
    }

    [Test]
    public async Task GetScopeForAsync_DoesNotDisturbTheCallersOwnScope()
    {
        var admin = Guid.NewGuid();
        var other = Guid.NewGuid();
        await GrantAsync(admin, Midtown);
        await GrantAsync(other, Loop);
        var provider = CreateProvider(SignedInUser(admin, UserRole.Admin));

        await provider.GetScopeForAsync(other, UserRole.Admin, Ct);
        var own = await provider.GetCurrentScopeAsync(Ct);

        own.Covers(Midtown).Should().BeTrue();
        own.Covers(Loop).Should().BeFalse();
    }
}
