using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Mail;
using Astrolabe.Application.Contracts.Mail;
using Astrolabe.Application.Features.Network.Commands.GrantSuperAdmin;
using Astrolabe.Application.Features.Network.Commands.ResendInvitation;
using Astrolabe.Application.Shared.Mail;
using Astrolabe.Application.Tests.TestSupport;
using Astrolabe.Domain.Features.Audit.Entities;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Network.Entities;
using Astrolabe.Domain.Features.Network.Errors;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace Astrolabe.Application.Tests.Features.Network;

/// <summary>
/// Covers the two halves of BR-NET-008 that Stage 6 left unbuilt: resending an invitation
/// (BR-NET-015) and granting extended powers.
/// </summary>
[TestFixture]
public sealed class AdminTeamHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid SuperId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Midtown = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private IdentityUnitOfWorkMock _identity = null!;
    private NetworkUnitOfWorkMock _network = null!;
    private AuditUnitOfWorkMock _audit = null!;
    private Mock<ICurrentUser> _currentUser = null!;
    private Mock<ITokenGenerator> _tokens = null!;
    private Mock<IEmailSender> _mail = null!;

    private static CancellationToken Ct => TestContext.CurrentContext.CancellationToken;

    [SetUp]
    public void SetUp()
    {
        _identity = new IdentityUnitOfWorkMock();
        _network = new NetworkUnitOfWorkMock();
        _audit = new AuditUnitOfWorkMock();

        _currentUser = new Mock<ICurrentUser>();
        _currentUser.SetupGet(u => u.UserId).Returns(SuperId);
        _currentUser.SetupGet(u => u.Role).Returns(UserRole.SuperAdmin);

        _tokens = new Mock<ITokenGenerator>();
        _tokens.Setup(t => t.CreateRefreshToken()).Returns("a-fresh-secret");

        _mail = new Mock<IEmailSender>();
        _mail.Setup(m => m.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmailDeliveryResult.Success("queued"));
    }

    private static User AnInvitedAdmin() =>
        User.Invite(Email.Create("dana@astrolabe.co").Value, "Dana", UserRole.Admin, Now).Value;

    private static User AnActiveAdmin()
    {
        var user = AnInvitedAdmin();
        user.AcceptInvitation(PasswordHash.FromHashedValue("AQAAAAIAAYag..."), Now);
        user.ClearDomainEvents();
        return user;
    }

    private static AdminInvitation AnInvitation(Guid userId) =>
        AdminInvitation.Create(
            Guid.NewGuid(), userId, UserRole.Admin, [Midtown], [1, 2, 3],
            SuperId, Now, TimeSpan.FromDays(7)).Value;

    private ResendInvitationCommandHandler ResendHandler() =>
        new(_identity.Object, _network.Object, _audit.Object, _currentUser.Object,
            _tokens.Object, _mail.Object,
            new NetworkMailTemplates(Options.Create(new MailOptions
            {
                FrontendBaseUrl = "https://astrolabe.test",
            })),
            new FixedClock(Now));

    private GrantSuperAdminCommandHandler GrantHandler() =>
        new(_identity.Object, _audit.Object, _currentUser.Object, new FixedClock(Now));

    private void TheUserIs(User user) =>
        _identity.Users.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

    private void OutstandingInvitations(Guid userId, params AdminInvitation[] invitations) =>
        _network.Invitations
            .Setup(r => r.GetPendingByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitations);

    // ---------- Resending, BR-NET-015 ----------

    [Test]
    public async Task Resending_RevokesEveryOutstandingInvitation()
    {
        // Every one, not merely the newest. An account invited twice by mistake would otherwise
        // leave a live link behind, and BR-NET-015 says the previous one must stop working.
        var user = AnInvitedAdmin();
        var first = AnInvitation(user.Id);
        var second = AnInvitation(user.Id);
        TheUserIs(user);
        OutstandingInvitations(user.Id, first, second);

        var result = await ResendHandler().Handle(new ResendInvitationCommand(user.Id), Ct);

        result.IsSuccess.Should().BeTrue();
        first.IsPending.Should().BeFalse();
        second.IsPending.Should().BeFalse();
    }

    [Test]
    public async Task Resending_CarriesTheRoleAndLibrariesForward()
    {
        // A resend repeats an offer; it does not quietly change it. Altering the libraries is
        // AssignLibrariesCommand's job, and that leaves its own trail.
        var user = AnInvitedAdmin();
        TheUserIs(user);
        OutstandingInvitations(user.Id, AnInvitation(user.Id));

        await ResendHandler().Handle(new ResendInvitationCommand(user.Id), Ct);

        _network.Invitations.Verify(r => r.AddAsync(
            It.Is<AdminInvitation>(i =>
                i.Role == UserRole.Admin && i.LibraryIds.Count == 1 && i.LibraryIds[0] == Midtown),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Resending_SendsTheEmailAfterTheCommit()
    {
        var user = AnInvitedAdmin();
        TheUserIs(user);
        OutstandingInvitations(user.Id, AnInvitation(user.Id));

        await ResendHandler().Handle(new ResendInvitationCommand(user.Id), Ct);

        _network.Saved.Should().Be(1);
        _mail.Verify(m => m.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Resending_ToAnAcceptedAccount_IsRefused()
    {
        // It would email a live link to an account somebody is already signed into, which is a way
        // to lose control of it rather than a convenience.
        var user = AnActiveAdmin();
        TheUserIs(user);

        var result = await ResendHandler().Handle(new ResendInvitationCommand(user.Id), Ct);

        result.Error.Should().Be(NetworkErrors.InvitationNotPending);
        _network.Saved.Should().Be(0);
        _mail.Verify(m => m.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Resending_WithNothingOutstanding_IsRefused()
    {
        var user = AnInvitedAdmin();
        TheUserIs(user);
        OutstandingInvitations(user.Id);

        var result = await ResendHandler().Handle(new ResendInvitationCommand(user.Id), Ct);

        result.Error.Should().Be(NetworkErrors.InvitationNotFound);
    }

    [Test]
    public async Task Resending_ByAnAdmin_IsRefused()
    {
        // BR-NET-008. Otherwise resending would be a way around the rule rather than a repeat of it.
        _currentUser.SetupGet(u => u.Role).Returns(UserRole.Admin);

        var result = await ResendHandler().Handle(new ResendInvitationCommand(Guid.NewGuid()), Ct);

        result.Error.Should().Be(NetworkErrors.SuperAdminRequired);
    }

    // ---------- Granting extended powers, BR-NET-008 ----------

    [Test]
    public async Task Granting_RaisesAnActiveAdministrator()
    {
        var user = AnActiveAdmin();
        TheUserIs(user);

        var result = await GrantHandler().Handle(new GrantSuperAdminCommand(user.Id), Ct);

        result.IsSuccess.Should().BeTrue();
        user.Role.Should().Be(UserRole.SuperAdmin);
        _identity.Saved.Should().Be(1);
    }

    [Test]
    public async Task Granting_WritesTheTrailEntryAnAuditorLooksForFirst()
    {
        var user = AnActiveAdmin();
        TheUserIs(user);

        await GrantHandler().Handle(new GrantSuperAdminCommand(user.Id), Ct);

        _audit.Entries.Verify(r => r.AddAsync(
            It.Is<AuditEntry>(e =>
                e.Action == "network.super_admin_granted"
                && e.ActorUserId == SuperId
                && e.SubjectUserId == user.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Granting_ToSomebodyWhoAlreadyHasIt_SaysSo()
    {
        // The prototype's own wording: "already has full powers". Distinct from a refusal, because
        // nothing is wrong — there is simply nothing to do.
        var user = AnActiveAdmin();
        user.ChangeRole(UserRole.SuperAdmin);
        TheUserIs(user);

        var result = await GrantHandler().Handle(new GrantSuperAdminCommand(user.Id), Ct);

        result.Error.Should().Be(NetworkErrors.AlreadyASuperAdmin);
        _identity.Saved.Should().Be(0);
    }

    [Test]
    public async Task Granting_ToAnInvitedAdministrator_IsRefused()
    {
        // They have not proved they own the address yet. Elevating first would hand the network's
        // widest authority to whoever reads that inbox.
        var user = AnInvitedAdmin();
        TheUserIs(user);

        var result = await GrantHandler().Handle(new GrantSuperAdminCommand(user.Id), Ct);

        result.Error.Should().Be(NetworkErrors.NotAnAdministrator);
        user.Role.Should().Be(UserRole.Admin);
    }

    [Test]
    public async Task Granting_ToAMember_IsRefused()
    {
        // A member must not be elevated straight past the invitation flow BR-NET-013 exists for.
        var member = User.Register(
            Email.Create("ada@example.com").Value,
            PasswordHash.FromHashedValue("AQAAAAIAAYag..."),
            "Ada", Guid.NewGuid(), Guid.NewGuid(), PlanTier.Basic, Now).Value;
        member.Verify(Now);
        member.ClearDomainEvents();
        TheUserIs(member);

        var result = await GrantHandler().Handle(new GrantSuperAdminCommand(member.Id), Ct);

        result.Error.Should().Be(NetworkErrors.NotAnAdministrator);
        member.Role.Should().Be(UserRole.Member);
    }

    [Test]
    public async Task Granting_ByAnAdmin_IsRefused()
    {
        _currentUser.SetupGet(u => u.Role).Returns(UserRole.Admin);

        var result = await GrantHandler().Handle(new GrantSuperAdminCommand(Guid.NewGuid()), Ct);

        result.Error.Should().Be(NetworkErrors.SuperAdminRequired);
    }

    [Test]
    public async Task Granting_ToAnUnknownAccount_IsNotFound()
    {
        _identity.Users.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await GrantHandler().Handle(new GrantSuperAdminCommand(Guid.NewGuid()), Ct);

        result.Error.Should().Be(IdentityErrors.AccountNotFound);
    }
}
