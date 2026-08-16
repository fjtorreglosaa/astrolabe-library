using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using FluentAssertions;

namespace Astrolabe.Domain.Tests.Features.Identity;

/// <summary>
/// Covers the identity value objects and the single-use token, including the redaction rules that
/// keep BR-IDN-010 and BR-IDN-016 enforceable.
/// </summary>
[TestFixture]
public sealed class ValueObjectTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);

    // ---------- Email ----------

    [Test]
    public void Email_IsNormalisedToLowerCaseAndTrimmed()
    {
        // Without this, "Ada@Example.com" and "ada@example.com" would be two accounts and the
        // unique index behind BR-IDN-002 would never catch it.
        Email.Create("  Ada@Example.COM  ").Value.Value.Should().Be("ada@example.com");
    }

    [Test]
    public void Email_ComparesByValue()
    {
        Email.Create("ada@example.com").Value.Should().Be(Email.Create("ADA@example.com").Value);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Email_WhenBlank_Fails(string value)
    {
        Email.Create(value).Error.Should().Be(IdentityErrors.EmailRequired);
    }

    [TestCase("no-at-sign")]
    [TestCase("@example.com")]
    [TestCase("two@@example.com")]
    [TestCase("ada@example")]
    [TestCase("ada@.com")]
    [TestCase("ada@example.")]
    [TestCase("ada lovelace@example.com")]
    public void Email_WhenMalformed_Fails(string value)
    {
        Email.Create(value).Error.Should().Be(IdentityErrors.EmailInvalid);
    }

    [TestCase("ada@example.com")]
    [TestCase("ada+books@example.co.uk")]
    [TestCase("ada.lovelace@sub.example.org")]
    [TestCase("fjtorreglosaa@gmail.com")]
    public void Email_AcceptsLegalAddresses(string value)
    {
        // Deliberately permissive: only the verification email proves an address exists, so
        // rejecting unusual but legal addresses locks out real people for no security gain.
        Email.Create(value).IsSuccess.Should().BeTrue();
    }

    [Test]
    public void Email_LongerThanTheLimit_Fails()
    {
        var tooLong = new string('a', Email.MaxLength) + "@example.com";

        Email.Create(tooLong).Error.Should().Be(IdentityErrors.EmailInvalid);
    }

    // ---------- PasswordHash ----------

    [Test]
    public void PasswordHash_RedactsItselfWhenPrintedOrLogged()
    {
        // BR-IDN-010. Structured loggers call ToString, so this is what stops a hash reaching a log.
        PasswordHash.FromHashedValue("AQAAAAIAAYag-real-hash").ToString().Should().Be("[redacted]");
    }

    [Test]
    public void PasswordHash_StillExposesItsValueToThePersistenceLayer()
    {
        PasswordHash.FromHashedValue("stored-hash").Value.Should().Be("stored-hash");
    }

    [Test]
    public void PasswordHash_ComparesByValue()
    {
        PasswordHash.FromHashedValue("h").Should().Be(PasswordHash.FromHashedValue("h"));
        PasswordHash.FromHashedValue("h").Should().NotBe(PasswordHash.FromHashedValue("other"));
    }

    [Test]
    public void PasswordHash_RejectsABlankValue()
    {
        var act = () => PasswordHash.FromHashedValue("  ");

        act.Should().Throw<ArgumentException>();
    }

    // ---------- SecretHash ----------

    [Test]
    public void SecretHash_IsDeterministicForTheSamePlaintext()
    {
        SecretHash.FromPlaintext("token-abc").Should().Be(SecretHash.FromPlaintext("token-abc"));
    }

    [Test]
    public void SecretHash_DiffersForDifferentPlaintext()
    {
        SecretHash.FromPlaintext("token-a").Should().NotBe(SecretHash.FromPlaintext("token-b"));
    }

    [Test]
    public void SecretHash_IsAlwaysThirtyTwoBytes()
    {
        SecretHash.FromPlaintext("anything").ToByteArray().Should().HaveCount(SecretHash.ByteLength);
    }

    [Test]
    public void SecretHash_RedactsItself()
    {
        // BR-IDN-016: a token hash must never reach a log.
        SecretHash.FromPlaintext("token").ToString().Should().Be("[redacted]");
    }

    [Test]
    public void SecretHash_RoundTripsThroughStorage()
    {
        var original = SecretHash.FromPlaintext("token-abc");

        SecretHash.FromStoredValue(original.ToByteArray()).Should().Be(original);
    }

    [Test]
    public void SecretHash_RejectsAStoredValueOfTheWrongLength()
    {
        var act = () => SecretHash.FromStoredValue([1, 2, 3]);

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void SecretHash_ToByteArray_ReturnsACopy()
    {
        // A caller mutating the returned array must not corrupt the stored hash.
        var hash = SecretHash.FromPlaintext("token");
        var exported = hash.ToByteArray();

        exported[0] ^= 0xFF;

        hash.Should().Be(SecretHash.FromPlaintext("token"));
    }

    // ---------- DeviceDescriptor ----------

    [Test]
    public void DeviceDescriptor_KeepsTheLabelAndTheClientIdentifier()
    {
        var device = DeviceDescriptor.Create("Chrome on macOS", DeviceType.Web, "device-abc");

        device.Name.Should().Be("Chrome on macOS");
        device.Type.Should().Be(DeviceType.Web);
        device.ClientDeviceId.Should().Be("device-abc");
    }

    [Test]
    public void DeviceDescriptor_FallsBackWhenTheNameIsMissing()
    {
        DeviceDescriptor.Create(null, DeviceType.Unknown).Name.Should().Be("Unknown device");
    }

    [Test]
    public void DeviceDescriptor_TruncatesAnOverlongName()
    {
        // A user agent is attacker-controlled, so its length must not be trusted.
        var device = DeviceDescriptor.Create(new string('x', 500), DeviceType.Web);

        device.Name.Should().HaveLength(DeviceDescriptor.MaxNameLength);
    }

    [Test]
    public void DeviceDescriptor_TreatsABlankClientIdentifierAsAbsent()
    {
        DeviceDescriptor.Create("Chrome", DeviceType.Web, "   ").ClientDeviceId.Should().BeNull();
    }

    // ---------- SingleUseToken ----------

    [Test]
    public void VerificationToken_LastsTwentyFourHours()
    {
        var token = SingleUseToken.IssueVerification(
            Guid.NewGuid(), SecretHash.FromPlaintext("t"), Now);

        token.ExpiresAt.Should().Be(Now.AddHours(24));
        token.Purpose.Should().Be(SingleUseTokenPurpose.EmailVerification);
    }

    [Test]
    public void RecoveryToken_LastsOneHour()
    {
        var token = SingleUseToken.IssueRecovery(Guid.NewGuid(), SecretHash.FromPlaintext("t"), Now);

        token.ExpiresAt.Should().Be(Now.AddHours(1));
    }

    [Test]
    public void SingleUseToken_CanOnlyBeConsumedOnce()
    {
        // AC-IDN-002: a verification link opened twice succeeds once and then fails.
        var token = SingleUseToken.IssueVerification(
            Guid.NewGuid(), SecretHash.FromPlaintext("t"), Now);

        token.Consume(Now.AddMinutes(1)).IsSuccess.Should().BeTrue();
        token.Consume(Now.AddMinutes(2)).IsFailure.Should().BeTrue();
    }

    [Test]
    public void SingleUseToken_CannotBeConsumedAfterExpiry()
    {
        // AC-IDN-003.
        var token = SingleUseToken.IssueVerification(
            Guid.NewGuid(), SecretHash.FromPlaintext("t"), Now);

        token.Consume(Now.AddHours(25)).Error.Should().Be(IdentityErrors.InvalidVerificationToken);
    }

    [Test]
    public void SingleUseToken_CannotBeConsumedOnceSuperseded()
    {
        // BR-IDN-005: requesting a new email must kill the previous link.
        var token = SingleUseToken.IssueVerification(
            Guid.NewGuid(), SecretHash.FromPlaintext("t"), Now);
        token.Invalidate(Now.AddMinutes(1));

        token.Consume(Now.AddMinutes(2)).IsFailure.Should().BeTrue();
    }

    [Test]
    public void SingleUseToken_InvalidatingAConsumedTokenChangesNothing()
    {
        var token = SingleUseToken.IssueRecovery(Guid.NewGuid(), SecretHash.FromPlaintext("t"), Now);
        token.Consume(Now.AddMinutes(1));

        token.Invalidate(Now.AddMinutes(2));

        token.InvalidatedAt.Should().BeNull();
    }

    [Test]
    public void RecoveryToken_ReportsItsOwnErrorKind()
    {
        var token = SingleUseToken.IssueRecovery(Guid.NewGuid(), SecretHash.FromPlaintext("t"), Now);

        token.Consume(Now.AddHours(2)).Error.Should().Be(IdentityErrors.InvalidRecoveryToken);
    }

    // ---------- AuditEntry ----------

    [Test]
    public void AuditEntry_RecordsWhoDidWhatToWhomAndWhen()
    {
        var actor = Guid.NewGuid();
        var subject = Guid.NewGuid();

        var entry = AuditEntry.Record(
            "identity.sign_in_succeeded", Now, actor, subject, "203.0.113.10", "Chrome on macOS");

        entry.Action.Should().Be("identity.sign_in_succeeded");
        entry.ActorUserId.Should().Be(actor);
        entry.SubjectUserId.Should().Be(subject);
        entry.IpAddress.Should().Be("203.0.113.10");
        entry.OccurredAt.Should().Be(Now);
    }

    [Test]
    public void AuditEntry_AllowsAnAnonymousActor()
    {
        // A failed sign-in has no known actor, and is exactly the event most worth recording.
        AuditEntry.Record("identity.sign_in_failed", Now).ActorUserId.Should().BeNull();
    }

    [Test]
    public void AuditEntry_RequiresAnAction()
    {
        var act = () => AuditEntry.Record("  ", Now);

        act.Should().Throw<ArgumentException>();
    }
}
