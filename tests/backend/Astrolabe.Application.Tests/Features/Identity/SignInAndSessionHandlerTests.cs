using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Features.Identity.Commands.RevokeSessions;
using Astrolabe.Application.Features.Identity.Commands.SignIn;
using Astrolabe.Application.Features.Identity.Queries.GetMySessions;
using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.Events;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Astrolabe.Application.Tests.TestSupport;
using Astrolabe.Domain.Abstractions;
using FluentAssertions;
using Moq;

namespace Astrolabe.Application.Tests.Features.Identity;

/// <summary>
/// Covers sign-in and session management: BR-IDN-011, BR-IDN-020, BR-IDN-023 to BR-IDN-026 and
/// BR-IDN-028.
/// </summary>
[TestFixture]
public sealed class SignInAndSessionHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);

    private IdentityUnitOfWorkMock _identity = null!;
    private Mock<IUserRepository> _users = null!;
    private Mock<IUserSessionRepository> _sessions = null!;
    private Mock<IPasswordHasher> _passwordHasher = null!;
    private Mock<ITokenGenerator> _tokenGenerator = null!;
    private Mock<IDeviceParser> _deviceParser = null!;
    private Mock<ICurrentUser> _currentUser = null!;

    private static CancellationToken Ct => TestContext.CurrentContext.CancellationToken;

    private sealed class FixedClock(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    [SetUp]
    public void SetUp()
    {
        _identity = new IdentityUnitOfWorkMock();
        _users = _identity.Users;
        _sessions = _identity.Sessions;
        _passwordHasher = new Mock<IPasswordHasher>();
        _tokenGenerator = new Mock<ITokenGenerator>();
        _deviceParser = new Mock<IDeviceParser>();
        _currentUser = new Mock<ICurrentUser>();

        _tokenGenerator.SetupGet(t => t.AccessTokenLifetime).Returns(TimeSpan.FromMinutes(15));
        _tokenGenerator.SetupGet(t => t.RefreshTokenLifetime).Returns(TimeSpan.FromDays(30));
        _tokenGenerator.Setup(t => t.CreateRefreshToken()).Returns(() => Guid.NewGuid().ToString());
        _tokenGenerator.Setup(t => t.CreateAccessToken(It.IsAny<User>(), It.IsAny<Guid>()))
            .Returns("access-token");
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>()))
            .Returns(PasswordHash.FromHashedValue("hash"));
        _deviceParser.Setup(d => d.Parse(It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(DeviceDescriptor.Create("Chrome on macOS", DeviceType.Web));
    }

    private SignInCommandHandler SignInHandler() => new(
        _identity.Object, _passwordHasher.Object, _tokenGenerator.Object,
        _deviceParser.Object, new FixedClock(Now));

    private RevokeSessionsCommandHandler RevokeHandler() => new(
        _identity.Object, _currentUser.Object, new FixedClock(Now));

    private GetMySessionsQueryHandler SessionsQueryHandler() =>
        new(_identity.Object, _currentUser.Object, new FixedClock(Now));

    private static User AnActiveUser()
    {
        var user = User.Register(
            Email.Create("ada@example.com").Value, PasswordHash.FromHashedValue("hash"),
            "Ada Lovelace", Guid.NewGuid(), Guid.NewGuid(), UserRole.Plus, Now).Value;
        user.Verify(Now);
        return user;
    }

    private UserSession ASession(Guid userId, string device = "Chrome on macOS") =>
        UserSession.Start(
            userId, DeviceDescriptor.Create(device, DeviceType.Web), "203.0.113.10",
            SecretHash.FromPlaintext(Guid.NewGuid().ToString()), Now, TimeSpan.FromDays(30));

    private void SignedInAs(Guid userId, Guid sessionId)
    {
        _currentUser.SetupGet(u => u.UserId).Returns(userId);
        _currentUser.SetupGet(u => u.SessionId).Returns(sessionId);
    }

    // ---------- Sign-in ----------

    [Test]
    public async Task SignIn_WithCorrectCredentials_OpensASessionAndReturnsAPair()
    {
        var user = AnActiveUser();
        _users.Setup(u => u.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<PasswordHash>())).Returns(true);

        var result = await SignInHandler().Handle(
            new SignInCommand("ada@example.com", "correct-horse-battery", "UA", "dev", "203.0.113.10"), Ct);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        _sessions.Verify(s => s.AddAsync(It.IsAny<UserSession>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SignIn_WithAnUnknownAddress_StillHashesThePassword()
    {
        // Skipping the hash would make an unknown address answer measurably faster, leaking account
        // existence by timing and undoing BR-IDN-028.
        _users.Setup(u => u.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await SignInHandler().Handle(
            new SignInCommand("nobody@example.com", "whatever-long-password", null, null, null), Ct);

        result.Error.Should().Be(IdentityErrors.InvalidCredentials);
        _passwordHasher.Verify(h => h.Hash(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task SignIn_WithAWrongPassword_CountsTowardsLockout()
    {
        var user = AnActiveUser();
        _users.Setup(u => u.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<PasswordHash>())).Returns(false);

        var result = await SignInHandler().Handle(
            new SignInCommand("ada@example.com", "wrong-password-here", null, null, null), Ct);

        result.Error.Should().Be(IdentityErrors.InvalidCredentials);
        user.FailedSignInAttempts.Should().Be(1);
    }

    [Test]
    public async Task SignIn_ByAnUnverifiedAccount_IsRefusedWithoutCheckingThePassword()
    {
        var user = User.Register(
            Email.Create("ada@example.com").Value, PasswordHash.FromHashedValue("hash"),
            "Ada", Guid.NewGuid(), Guid.NewGuid(), UserRole.Plus, Now).Value;

        _users.Setup(u => u.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await SignInHandler().Handle(
            new SignInCommand("ada@example.com", "correct-horse-battery", null, null, null), Ct);

        result.Error.Should().Be(IdentityErrors.InvalidCredentials);
        _passwordHasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<PasswordHash>()), Times.Never);
    }

    [Test]
    public async Task SignIn_WithAMalformedAddress_ReportsTheSameError()
    {
        var result = await SignInHandler().Handle(
            new SignInCommand("not-an-address", "some-long-password", null, null, null), Ct);

        result.Error.Should().Be(IdentityErrors.InvalidCredentials);
    }

    [Test]
    public async Task SignIn_ClearsAnyPreviousFailedAttempts()
    {
        var user = AnActiveUser();
        user.RecordFailedSignIn(Now);
        _users.Setup(u => u.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<PasswordHash>())).Returns(true);

        await SignInHandler().Handle(
            new SignInCommand("ada@example.com", "correct-horse-battery", null, null, null), Ct);

        user.FailedSignInAttempts.Should().Be(0);
    }

    // ---------- Revoking sessions, BR-IDN-024 and BR-IDN-025 ----------

    private IReadOnlyList<UserSession> SetUpThreeSessions(Guid userId)
    {
        var live = new[] { ASession(userId, "Chrome"), ASession(userId, "Safari"), ASession(userId, "Edge") };

        _sessions.Setup(s => s.GetActiveByUserAsync(
                userId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(live);

        return live;
    }

    [Test]
    public async Task RevokeAll_EndsEverySessionIncludingTheCurrentOne()
    {
        var userId = Guid.NewGuid();
        var live = SetUpThreeSessions(userId);
        SignedInAs(userId, live[0].Id);

        var result = await RevokeHandler().Handle(new RevokeSessionsCommand(RevocationScope.All), Ct);

        result.Value.Should().Be(3);
        live.Should().OnlyContain(s => s.IsRevoked);
    }

    [Test]
    public async Task RevokeAllOthers_LeavesExactlyOneLiveSession()
    {
        // AC-IDN-009.
        var userId = Guid.NewGuid();
        var live = SetUpThreeSessions(userId);
        SignedInAs(userId, live[1].Id);

        var result = await RevokeHandler().Handle(
            new RevokeSessionsCommand(RevocationScope.AllOthers), Ct);

        result.Value.Should().Be(2);
        live[1].IsRevoked.Should().BeFalse("the caller's own session is spared");
        live[0].IsRevoked.Should().BeTrue();
        live[2].IsRevoked.Should().BeTrue();
    }

    [Test]
    public async Task RevokeSpecified_EndsOnlyTheNamedSessions()
    {
        var userId = Guid.NewGuid();
        var live = SetUpThreeSessions(userId);
        SignedInAs(userId, live[0].Id);

        var result = await RevokeHandler().Handle(
            new RevokeSessionsCommand(RevocationScope.Specified, [live[2].Id]), Ct);

        result.Value.Should().Be(1);
        live[2].IsRevoked.Should().BeTrue();
        live[0].IsRevoked.Should().BeFalse();
    }

    [Test]
    public async Task RevokeSpecified_CannotReachAnotherMembersSession()
    {
        // BR-IDN-025 holds structurally: the candidate set is only ever the caller's own sessions,
        // so a foreign identifier simply matches nothing.
        var userId = Guid.NewGuid();
        var live = SetUpThreeSessions(userId);
        SignedInAs(userId, live[0].Id);

        var result = await RevokeHandler().Handle(
            new RevokeSessionsCommand(RevocationScope.Specified, [Guid.NewGuid()]), Ct);

        result.Error.Should().Be(IdentityErrors.SessionNotFound);
        live.Should().OnlyContain(s => !s.IsRevoked);
    }

    [Test]
    public async Task Revoking_RaisesTheEventThatEvictsFromTheRevocationCache()
    {
        // BR-IDN-023. The handler no longer evicts directly: every Revoke raises SessionRevoked and
        // the event handler evicts, so no caller can forget it.
        var userId = Guid.NewGuid();
        var live = SetUpThreeSessions(userId);
        SignedInAs(userId, live[0].Id);

        await RevokeHandler().Handle(new RevokeSessionsCommand(RevocationScope.All), Ct);

        foreach (var session in live)
        {
            session.DomainEvents.Should().ContainSingle(e => e is SessionRevoked);
        }
    }

    [Test]
    public async Task Revoking_WhenNotSignedIn_Fails()
    {
        _currentUser.SetupGet(u => u.UserId).Returns((Guid?)null);

        var result = await RevokeHandler().Handle(new RevokeSessionsCommand(RevocationScope.All), Ct);

        result.Error.Should().Be(IdentityErrors.InvalidCredentials);
    }

    // ---------- Listing sessions, BR-IDN-026 ----------

    [Test]
    public async Task GetMySessions_MarksTheCallersOwnSession()
    {
        var userId = Guid.NewGuid();
        var live = SetUpThreeSessions(userId);
        SignedInAs(userId, live[1].Id);

        var result = await SessionsQueryHandler().Handle(new GetMySessionsQuery(), Ct);

        result.Value.Should().HaveCount(3);
        result.Value.Single(s => s.IsCurrent).Id.Should().Be(live[1].Id);
    }

    [Test]
    public async Task GetMySessions_CarriesNoTokenMaterial()
    {
        // The devices screen identifies a device; it must never be able to act as one.
        var userId = Guid.NewGuid();
        var live = SetUpThreeSessions(userId);
        SignedInAs(userId, live[0].Id);

        var result = await SessionsQueryHandler().Handle(new GetMySessionsQuery(), Ct);

        typeof(Application.Contracts.Identity.SessionDto)
            .GetProperties()
            .Select(p => p.Name)
            .Should().NotContain(name =>
                name.Contains("Token", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Hash", StringComparison.OrdinalIgnoreCase));

        result.Value.Should().NotBeEmpty();
    }

    [Test]
    public async Task GetMySessions_WhenNotSignedIn_Fails()
    {
        _currentUser.SetupGet(u => u.UserId).Returns((Guid?)null);

        var result = await SessionsQueryHandler().Handle(new GetMySessionsQuery(), Ct);

        result.Error.Should().Be(IdentityErrors.InvalidCredentials);
    }
}
