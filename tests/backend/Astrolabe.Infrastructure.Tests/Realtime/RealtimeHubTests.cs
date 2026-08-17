using System.Security.Claims;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Infrastructure.Realtime;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Astrolabe.Infrastructure.Tests.Realtime;

/// <summary>
/// Which groups a connection is put into.
/// </summary>
/// <remarks>
/// The security-critical half of the hub. Every push is addressed to a group, so a connection in the
/// wrong group is a member reading somebody else's fines arrive in real time — and nothing would
/// error, log or look wrong while it happened. These tests exist because that failure is silent.
/// </remarks>
[TestFixture]
public sealed class RealtimeHubTests
{
    private static readonly Guid MemberId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherMemberId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private Mock<IGroupManager> _groups = null!;
    private Mock<HubCallerContext> _context = null!;
    private RealtimeHub _hub = null!;

    [SetUp]
    public void SetUp()
    {
        _groups = new Mock<IGroupManager>();
        _context = new Mock<HubCallerContext>();
        _context.SetupGet(c => c.ConnectionId).Returns("connection-1");

        _hub = new RealtimeHub(NullLogger<RealtimeHub>.Instance)
        {
            Groups = _groups.Object,
            Context = _context.Object
        };
    }

    [TearDown]
    public void TearDown() => _hub.Dispose();

    private void SignedInAs(Guid? userId, string? role)
    {
        var claims = new List<Claim>();

        if (userId.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        }

        if (role is not null)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        _context.SetupGet(c => c.User)
            .Returns(new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")));
    }

    // ---------- Members ----------

    [Test]
    public async Task AMemberJoinsOnlyTheirOwnGroup()
    {
        SignedInAs(MemberId, nameof(UserRole.Member));

        await _hub.OnConnectedAsync();

        _groups.Verify(
            g => g.AddToGroupAsync("connection-1", RealtimeGroups.ForMember(MemberId), default),
            Times.Once);

        _groups.Verify(
            g => g.AddToGroupAsync(
                It.IsAny<string>(), RealtimeGroups.ForMember(OtherMemberId), default),
            Times.Never);

        // The one that would matter most: a member in the staff group would receive every desk
        // payment code in the network.
        _groups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), RealtimeGroups.Staff, default),
            Times.Never);
    }

    [Test]
    public async Task TwoMembersGetDifferentGroups()
    {
        RealtimeGroups.ForMember(MemberId).Should().NotBe(RealtimeGroups.ForMember(OtherMemberId));
    }

    // ---------- Staff ----------

    [TestCase(nameof(UserRole.Admin))]
    [TestCase(nameof(UserRole.SuperAdmin))]
    public async Task StaffJoinTheStaffGroupAndNoMemberGroup(string role)
    {
        SignedInAs(MemberId, role);

        await _hub.OnConnectedAsync();

        _groups.Verify(
            g => g.AddToGroupAsync("connection-1", RealtimeGroups.Staff, default), Times.Once);

        // Staff hold no plan, no loans and no fines, so a member group would only ever deliver
        // events about things they do not have.
        _groups.Verify(
            g => g.AddToGroupAsync(
                It.IsAny<string>(), RealtimeGroups.ForMember(MemberId), default),
            Times.Never);
    }

    [Test]
    public async Task AnUnknownRoleIsTreatedAsAMemberRatherThanAsStaff()
    {
        // Fails closed. A role this build does not recognise must not be handed the staff feed on
        // the strength of not matching "Member".
        SignedInAs(MemberId, "SomethingAddedLater");

        await _hub.OnConnectedAsync();

        _groups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), RealtimeGroups.Staff, default), Times.Never);
    }

    // ---------- Malformed identities ----------

    [Test]
    public async Task AConnectionWithNoSubjectClaimIsAbortedAndJoinsNothing()
    {
        SignedInAs(null, nameof(UserRole.Member));

        await _hub.OnConnectedAsync();

        _context.Verify(c => c.Abort(), Times.Once);
        _groups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Test]
    public async Task AConnectionWithAnUnparseableSubjectIsAborted()
    {
        _context.SetupGet(c => c.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "not-a-guid")], "Test")));

        await _hub.OnConnectedAsync();

        _context.Verify(c => c.Abort(), Times.Once);
        _groups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }
}
