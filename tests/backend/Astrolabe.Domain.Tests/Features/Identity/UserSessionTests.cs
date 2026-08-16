using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.Events;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using FluentAssertions;

namespace Astrolabe.Domain.Tests.Features.Identity;

/// <summary>
/// Covers the session aggregate, and above all <see cref="UserSession.Rotate"/> — the highest-risk
/// unit in the identity domain. Implements the tests demanded by AC-IDN-006 and AC-IDN-007.
/// </summary>
[TestFixture]
public sealed class UserSessionTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    private static SecretHash Hash(string plaintext) => SecretHash.FromPlaintext(plaintext);

    private static UserSession AStartedSession(string firstToken = "token-1") =>
        UserSession.Start(
            Guid.NewGuid(),
            DeviceDescriptor.Create("Chrome on macOS", DeviceType.Web, "device-abc"),
            "203.0.113.10",
            Hash(firstToken),
            Now,
            Lifetime);

    // ---------- Start, BR-IDN-020 and BR-IDN-021 ----------

    [Test]
    public void Start_RecordsTheDeviceAndOpensTheChain()
    {
        var session = AStartedSession();

        session.IsActive(Now).Should().BeTrue();
        session.Device.Name.Should().Be("Chrome on macOS");
        session.Device.Type.Should().Be(DeviceType.Web);
        session.Device.ClientDeviceId.Should().Be("device-abc");
        session.IpAddress.Should().Be("203.0.113.10");
        session.CreatedAt.Should().Be(Now);
        session.LastSeenAt.Should().Be(Now);
        session.ExpiresAt.Should().Be(Now.Add(Lifetime));
        session.Tokens.Should().ContainSingle();
    }

    [Test]
    public void Start_LeavesApproximateLocationUnset()
    {
        // GeoIP needs a licensed database, so the MVP never fills this in.
        AStartedSession().ApproximateLocation.Should().BeNull();
    }

    // ---------- Rotation, BR-IDN-017 ----------

    [Test]
    public void Rotate_ExchangesTheLiveTokenForANewOne()
    {
        var session = AStartedSession("token-1");

        var result = session.Rotate(Hash("token-1"), Hash("token-2"), Now.AddMinutes(10));

        result.IsSuccess.Should().BeTrue();
        session.Tokens.Should().HaveCount(2);
        session.Tokens[0].IsRotated.Should().BeTrue("the presented token is spent");
        session.Tokens[1].IsUsable(Now.AddMinutes(10)).Should().BeTrue();
        session.Tokens[0].ReplacedByTokenId.Should().Be(session.Tokens[1].Id);
    }

    [Test]
    public void Rotate_AdvancesLastSeen()
    {
        var session = AStartedSession();

        session.Rotate(Hash("token-1"), Hash("token-2"), Now.AddHours(3));

        session.LastSeenAt.Should().Be(Now.AddHours(3));
    }

    [Test]
    public void Rotate_DoesNotExtendTheSessionLifetime()
    {
        // A session lives 30 days from sign-in. If refreshing extended it, an active device would
        // hold an unbounded lease and BR-IDN-015 would mean nothing.
        var session = AStartedSession();
        var originalExpiry = session.ExpiresAt;

        session.Rotate(Hash("token-1"), Hash("token-2"), Now.AddDays(20));

        session.ExpiresAt.Should().Be(originalExpiry);
        session.Tokens[1].ExpiresAt.Should().Be(originalExpiry);
    }

    [Test]
    public void Rotate_ChainsRepeatedly()
    {
        var session = AStartedSession("t1");

        session.Rotate(Hash("t1"), Hash("t2"), Now.AddMinutes(10)).IsSuccess.Should().BeTrue();
        session.Rotate(Hash("t2"), Hash("t3"), Now.AddMinutes(20)).IsSuccess.Should().BeTrue();

        session.Tokens.Should().HaveCount(3);
        session.Tokens[2].IsUsable(Now.AddMinutes(20)).Should().BeTrue();
    }

    // ---------- Reuse detection, BR-IDN-018 ----------

    [Test]
    public void Rotate_WithAnAlreadyRotatedToken_RevokesTheEntireSession()
    {
        // AC-IDN-007, and the single most important test in this domain. A rotated token resurfacing
        // means a copy exists somewhere it should not.
        var session = AStartedSession("t1");
        session.Rotate(Hash("t1"), Hash("t2"), Now.AddMinutes(10));

        var result = session.Rotate(Hash("t1"), Hash("t3"), Now.AddMinutes(11));

        result.IsFailure.Should().BeTrue();
        session.IsRevoked.Should().BeTrue();
        session.RevokedReason.Should().Be(SessionRevocationReason.TokenReuseDetected);
    }

    [Test]
    public void Rotate_OnReuse_RaisesTheSecurityEvent()
    {
        var session = AStartedSession("t1");
        session.Rotate(Hash("t1"), Hash("t2"), Now.AddMinutes(10));
        session.ClearDomainEvents();

        session.Rotate(Hash("t1"), Hash("t3"), Now.AddMinutes(11));

        session.DomainEvents.Should().ContainSingle(e => e is RefreshTokenReuseDetected);
    }

    [Test]
    public void Rotate_OnReuse_AlsoKillsTheTokenTheHonestDeviceHolds()
    {
        // The legitimate device loses access too. That is deliberate: with a thief in possession,
        // the only safe move is to force everyone on this session to re-authenticate.
        var session = AStartedSession("t1");
        session.Rotate(Hash("t1"), Hash("t2"), Now.AddMinutes(10));

        session.Rotate(Hash("t1"), Hash("t3"), Now.AddMinutes(11));

        session.Rotate(Hash("t2"), Hash("t4"), Now.AddMinutes(12))
            .IsFailure.Should().BeTrue("the whole chain died with the session");
    }

    [Test]
    public void Rotate_OnReuse_ReportsTheSameErrorAsAnyOtherFailure()
    {
        // BR-IDN-019: unknown, expired, rotated and revoked must be indistinguishable. Telling the
        // caller "reuse detected" would confirm to an attacker that their stolen token was real.
        var session = AStartedSession("t1");
        session.Rotate(Hash("t1"), Hash("t2"), Now.AddMinutes(10));

        var reuse = session.Rotate(Hash("t1"), Hash("t3"), Now.AddMinutes(11));
        var unknown = AStartedSession("other").Rotate(Hash("nope"), Hash("x"), Now);

        reuse.Error.Should().Be(IdentityErrors.InvalidRefreshToken);
        unknown.Error.Should().Be(IdentityErrors.InvalidRefreshToken);
    }

    // ---------- Other rotation failures, BR-IDN-019 ----------

    [Test]
    public void Rotate_WithAnUnknownToken_Fails_AndLeavesTheSessionAlone()
    {
        var session = AStartedSession("t1");

        var result = session.Rotate(Hash("never-issued"), Hash("t2"), Now.AddMinutes(5));

        result.Error.Should().Be(IdentityErrors.InvalidRefreshToken);
        session.IsRevoked.Should().BeFalse("an unknown token is not evidence of theft");
        session.Tokens.Should().ContainSingle();
    }

    [Test]
    public void Rotate_AfterTheSessionExpired_Fails()
    {
        var session = AStartedSession("t1");

        var result = session.Rotate(Hash("t1"), Hash("t2"), Now.Add(Lifetime).AddSeconds(1));

        result.Error.Should().Be(IdentityErrors.InvalidRefreshToken);
    }

    [Test]
    public void Rotate_AfterTheSessionWasRevoked_Fails()
    {
        var session = AStartedSession("t1");
        session.Revoke(SessionRevocationReason.RevokedByUser, Now.AddMinutes(1));

        var result = session.Rotate(Hash("t1"), Hash("t2"), Now.AddMinutes(2));

        result.Error.Should().Be(IdentityErrors.InvalidRefreshToken);
    }

    // ---------- Revocation ----------

    [Test]
    public void Revoke_EndsTheSessionAndRaisesTheEvent()
    {
        var session = AStartedSession();

        session.Revoke(SessionRevocationReason.SignedOut, Now.AddMinutes(30));

        session.IsRevoked.Should().BeTrue();
        session.IsActive(Now.AddMinutes(31)).Should().BeFalse();
        session.RevokedAt.Should().Be(Now.AddMinutes(30));
        session.DomainEvents.Should().ContainSingle(e => e is SessionRevoked);
    }

    [Test]
    public void Revoke_Twice_IsIdempotentAndKeepsTheFirstReason()
    {
        // "Sign out everywhere" may touch a session that just ended. That must not overwrite why it
        // ended, or a security revocation could be masked by a routine one.
        var session = AStartedSession();
        session.Revoke(SessionRevocationReason.TokenReuseDetected, Now.AddMinutes(1));

        session.Revoke(SessionRevocationReason.SignedOut, Now.AddMinutes(2));

        session.RevokedReason.Should().Be(SessionRevocationReason.TokenReuseDetected);
        session.RevokedAt.Should().Be(Now.AddMinutes(1));
        session.DomainEvents.Should().ContainSingle(e => e is SessionRevoked);
    }

    [Test]
    public void IsActive_IsFalseOnceExpired()
    {
        var session = AStartedSession();

        session.IsActive(Now.Add(Lifetime)).Should().BeFalse();
        session.IsActive(Now.Add(Lifetime).AddSeconds(-1)).Should().BeTrue();
    }

    // ---------- Touch ----------

    [Test]
    public void Touch_MovesLastSeenForward()
    {
        var session = AStartedSession();

        session.Touch(Now.AddHours(2));

        session.LastSeenAt.Should().Be(Now.AddHours(2));
    }

    [Test]
    public void Touch_NeverMovesLastSeenBackwards()
    {
        // Out-of-order requests must not make a session look older than it is.
        var session = AStartedSession();
        session.Touch(Now.AddHours(2));

        session.Touch(Now.AddHours(1));

        session.LastSeenAt.Should().Be(Now.AddHours(2));
    }

    [Test]
    public void Touch_DoesNothingOnceRevoked()
    {
        var session = AStartedSession();
        session.Revoke(SessionRevocationReason.SignedOut, Now.AddMinutes(1));

        session.Touch(Now.AddHours(5));

        session.LastSeenAt.Should().Be(Now);
    }
}
