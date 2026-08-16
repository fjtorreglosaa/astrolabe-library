using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Features.Identity.Commands.RefreshToken;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.Events;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Astrolabe.Application.Tests.TestSupport;
using FluentAssertions;
using Moq;

namespace Astrolabe.Application.Tests.Features.Identity;

/// <summary>
/// Covers the refresh flow: BR-IDN-017, BR-IDN-018, BR-IDN-019 and BR-IDN-023.
/// This is the highest-risk handler in the system — it is where a stolen token is caught.
/// </summary>
[TestFixture]
public sealed class RefreshTokenCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);

    private IdentityUnitOfWorkMock _identity = null!;
    private Mock<IUserSessionRepository> _sessions = null!;
    private Mock<IUserRepository> _users = null!;
    private Mock<ITokenGenerator> _tokenGenerator = null!;
    private Mock<IAuditRepository> _audit = null!;

    private static CancellationToken Ct => TestContext.CurrentContext.CancellationToken;

    [SetUp]
    public void SetUp()
    {
        _identity = new IdentityUnitOfWorkMock();
        _sessions = _identity.Sessions;
        _users = _identity.Users;
        _audit = _identity.Audit;
        _tokenGenerator = new Mock<ITokenGenerator>();

        _tokenGenerator.SetupGet(t => t.AccessTokenLifetime).Returns(TimeSpan.FromMinutes(15));
        _tokenGenerator.SetupGet(t => t.RefreshTokenLifetime).Returns(TimeSpan.FromDays(30));
        _tokenGenerator.Setup(t => t.CreateRefreshToken()).Returns(() => Guid.NewGuid().ToString());
        _tokenGenerator.Setup(t => t.CreateAccessToken(It.IsAny<User>(), It.IsAny<Guid>()))
            .Returns("access-token");
    }

    private RefreshTokenCommandHandler CreateHandler() => new(
        _identity.Object, _tokenGenerator.Object, new FixedClock(Now));

    private sealed class FixedClock(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    private static User AnActiveUser()
    {
        var user = User.Register(
            Email.Create("ada@example.com").Value,
            PasswordHash.FromHashedValue("hash"),
            "Ada Lovelace", Guid.NewGuid(), Guid.NewGuid(), UserRole.Plus, Now).Value;

        user.Verify(Now);
        return user;
    }

    private UserSession ASessionFor(User user, string refreshToken)
    {
        var session = UserSession.Start(
            user.Id,
            DeviceDescriptor.Create("Chrome on macOS", DeviceType.Web),
            "203.0.113.10",
            SecretHash.FromPlaintext(refreshToken),
            Now,
            TimeSpan.FromDays(30));

        _sessions.Setup(s => s.GetByRefreshTokenHashAsync(
                It.IsAny<SecretHash>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        return session;
    }

    // ---------- The happy path, BR-IDN-017 ----------

    [Test]
    public async Task Refresh_WithTheLiveToken_IssuesANewPair()
    {
        var user = AnActiveUser();
        ASessionFor(user, "live-token");

        var result = await CreateHandler().Handle(new RefreshTokenCommand("live-token", null), Ct);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Refresh_DoesNotReturnThePresentedTokenAgain()
    {
        var user = AnActiveUser();
        ASessionFor(user, "live-token");

        var result = await CreateHandler().Handle(new RefreshTokenCommand("live-token", null), Ct);

        result.Value.RefreshToken.Should().NotBe("live-token", "rotation must replace the token");
    }

    [Test]
    public async Task Refresh_RotatesThePresentedTokenInTheChain()
    {
        var user = AnActiveUser();
        var session = ASessionFor(user, "live-token");

        await CreateHandler().Handle(new RefreshTokenCommand("live-token", null), Ct);

        session.Tokens.Should().HaveCount(2);
        session.Tokens[0].IsRotated.Should().BeTrue();
    }

    // ---------- Reuse detection, BR-IDN-018 ----------

    [Test]
    public async Task Refresh_WithAnAlreadyRotatedToken_RevokesTheSession()
    {
        var user = AnActiveUser();
        var session = ASessionFor(user, "first-token");
        session.Rotate(
            SecretHash.FromPlaintext("first-token"),
            SecretHash.FromPlaintext("second-token"),
            Now.AddMinutes(1));

        var result = await CreateHandler().Handle(new RefreshTokenCommand("first-token", null), Ct);

        result.IsFailure.Should().BeTrue();
        session.IsRevoked.Should().BeTrue();
        session.RevokedReason.Should().Be(SessionRevocationReason.TokenReuseDetected);
    }

    [Test]
    public async Task Refresh_OnReuse_RaisesTheEventThatEvictsFromTheRevocationCache()
    {
        // BR-IDN-023. Eviction is driven by SessionRevoked rather than by this handler, so no
        // caller can forget it. Without eviction the stolen access token would keep working for up
        // to fifteen minutes after the theft was detected.
        var user = AnActiveUser();
        var session = ASessionFor(user, "first-token");
        session.Rotate(
            SecretHash.FromPlaintext("first-token"),
            SecretHash.FromPlaintext("second-token"),
            Now.AddMinutes(1));
        session.ClearDomainEvents();

        await CreateHandler().Handle(new RefreshTokenCommand("first-token", null), Ct);

        session.DomainEvents.Should().Contain(e => e is SessionRevoked);
    }

    [Test]
    public async Task Refresh_OnReuse_AuditsTheSecurityEvent()
    {
        var user = AnActiveUser();
        var session = ASessionFor(user, "first-token");
        session.Rotate(
            SecretHash.FromPlaintext("first-token"),
            SecretHash.FromPlaintext("second-token"),
            Now.AddMinutes(1));

        await CreateHandler().Handle(new RefreshTokenCommand("first-token", null), Ct);

        _audit.Verify(a => a.AddAsync(It.IsAny<AuditEntry>(), It.IsAny<CancellationToken>()), Times.Once);
        _identity.Saved.Should().Be(1);
    }

    [Test]
    public async Task Refresh_OnReuse_ReportsTheSameErrorAsAnUnknownToken()
    {
        // BR-IDN-019. Saying "reuse detected" would confirm to an attacker that the token was real.
        var user = AnActiveUser();
        var session = ASessionFor(user, "first-token");
        session.Rotate(
            SecretHash.FromPlaintext("first-token"),
            SecretHash.FromPlaintext("second-token"),
            Now.AddMinutes(1));

        var reuse = await CreateHandler().Handle(new RefreshTokenCommand("first-token", null), Ct);

        reuse.Error.Should().Be(IdentityErrors.InvalidRefreshToken);
    }

    // ---------- Other failures ----------

    [Test]
    public async Task Refresh_WithAnUnknownToken_Fails()
    {
        _sessions.Setup(s => s.GetByRefreshTokenHashAsync(
                It.IsAny<SecretHash>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSession?)null);

        var result = await CreateHandler().Handle(new RefreshTokenCommand("nope", null), Ct);

        result.Error.Should().Be(IdentityErrors.InvalidRefreshToken);
    }

    [TestCase("")]
    [TestCase("   ")]
    public async Task Refresh_WithABlankToken_FailsWithoutTouchingTheDatabase(string token)
    {
        var result = await CreateHandler().Handle(new RefreshTokenCommand(token, null), Ct);

        result.Error.Should().Be(IdentityErrors.InvalidRefreshToken);
        _sessions.Verify(s => s.GetByRefreshTokenHashAsync(
            It.IsAny<SecretHash>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Refresh_WhenTheAccountWasBlockedMeanwhile_EndsTheSession()
    {
        // A live session must not outlive the right to sign in.
        var user = AnActiveUser();
        var session = ASessionFor(user, "live-token");
        user.Block(Now);

        var result = await CreateHandler().Handle(new RefreshTokenCommand("live-token", null), Ct);

        result.Error.Should().Be(IdentityErrors.InvalidRefreshToken);
        session.IsRevoked.Should().BeTrue();
        session.DomainEvents.Should().Contain(e => e is SessionRevoked);
    }

    [Test]
    public async Task Refresh_DoesNotExtendTheSessionExpiry()
    {
        var user = AnActiveUser();
        var session = ASessionFor(user, "live-token");
        var originalExpiry = session.ExpiresAt;

        var result = await CreateHandler().Handle(new RefreshTokenCommand("live-token", null), Ct);

        result.Value.RefreshTokenExpiresAt.Should().Be(originalExpiry);
    }
}
