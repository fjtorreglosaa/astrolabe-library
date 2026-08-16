using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.Events;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using FluentAssertions;

namespace Astrolabe.Domain.Tests.Features.Identity;

/// <summary>
/// Covers the account lifecycle and the sign-in gate: BR-IDN-001, BR-IDN-003, BR-IDN-006 to
/// BR-IDN-011, BR-IDN-013 and BR-IDN-028.
/// </summary>
[TestFixture]
public sealed class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);

    private static Email AnEmail(string value = "ada@example.com") => Email.Create(value).Value;

    private static PasswordHash AHash() => PasswordHash.FromHashedValue("AQAAAAIAAYag...");

    private static User ARegisteredUser(UserRole plan = UserRole.Plus) =>
        User.Register(AnEmail(), AHash(), "Ada Lovelace", Guid.NewGuid(), Guid.NewGuid(), plan, Now).Value;

    private static User AnActiveUser(UserRole plan = UserRole.Plus)
    {
        var user = ARegisteredUser(plan);
        user.Verify(Now);
        user.ClearDomainEvents();
        return user;
    }

    // ---------- Registration, BR-IDN-001 and BR-IDN-003 ----------

    [Test]
    public void Register_StartsPendingVerification()
    {
        var user = ARegisteredUser();

        user.Status.Should().Be(UserStatus.PendingVerification);
        user.VerifiedAt.Should().BeNull();
        user.DomainEvents.Should().ContainSingle(e => e is UserRegistered);
    }

    [Test]
    public void Register_CannotSignInUntilVerified()
    {
        // AC-IDN-001 and the whole point of BR-IDN-001.
        ARegisteredUser().EnsureCanSignIn(Now).Error.Should().Be(IdentityErrors.InvalidCredentials);
    }

    [Test]
    public void Register_KeepsCountryAndCity()
    {
        var country = Guid.NewGuid();
        var city = Guid.NewGuid();

        var user = User.Register(AnEmail(), AHash(), "Ada", country, city, UserRole.Basic, Now).Value;

        user.CountryId.Should().Be(country);
        user.CityId.Should().Be(city);
    }

    [Test]
    public void Register_TrimsTheFullName()
    {
        User.Register(AnEmail(), AHash(), "  Ada Lovelace  ", Guid.NewGuid(), Guid.NewGuid(),
            UserRole.Plus, Now).Value.FullName.Should().Be("Ada Lovelace");
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Register_WithoutAName_Fails(string name)
    {
        User.Register(AnEmail(), AHash(), name, Guid.NewGuid(), Guid.NewGuid(), UserRole.Plus, Now)
            .Error.Should().Be(IdentityErrors.FullNameRequired);
    }

    [TestCase(UserRole.Admin)]
    [TestCase(UserRole.SuperAdmin)]
    public void Register_DirectlyIntoAStaffRole_IsRefused(UserRole role)
    {
        // Otherwise public registration would bypass BR-NET-008, which reserves staff creation to a
        // super administrator.
        User.Register(AnEmail(), AHash(), "Ada", Guid.NewGuid(), Guid.NewGuid(), role, Now)
            .IsFailure.Should().BeTrue();
    }

    // ---------- Invitation, BR-IDN-006 ----------

    [Test]
    public void Invite_CreatesAnInvitedAccountWithNoPassword()
    {
        var user = User.Invite(AnEmail("dana@astrolabe.co"), "Dana Whitfield", UserRole.Admin, Now).Value;

        user.Status.Should().Be(UserStatus.Invited);
        user.PasswordHash.Should().BeNull("the invitee chooses their password when they accept");
        user.CityId.Should().BeNull("staff have no city of residence");
    }

    [Test]
    public void Invite_CannotSignIn()
    {
        User.Invite(AnEmail(), "Dana", UserRole.Admin, Now).Value
            .EnsureCanSignIn(Now).Error.Should().Be(IdentityErrors.InvalidCredentials);
    }

    [TestCase(UserRole.Basic)]
    [TestCase(UserRole.Max)]
    public void Invite_WithAMemberRole_Fails(UserRole role)
    {
        User.Invite(AnEmail(), "Dana", role, Now).IsFailure.Should().BeTrue();
    }

    [Test]
    public void AcceptInvitation_SetsThePasswordAndActivates()
    {
        var user = User.Invite(AnEmail(), "Dana", UserRole.Admin, Now).Value;

        user.AcceptInvitation(AHash(), Now.AddDays(1)).IsSuccess.Should().BeTrue();

        user.Status.Should().Be(UserStatus.Active);
        user.PasswordHash.Should().NotBeNull();
        user.EnsureCanSignIn(Now.AddDays(1)).IsSuccess.Should().BeTrue();
    }

    // ---------- The sign-in gate, BR-IDN-028 ----------

    [Test]
    public void EnsureCanSignIn_ReturnsTheSameErrorForEveryRejection()
    {
        // AC-IDN-005: an unverified, blocked, deleted, invited and locked account must all be
        // indistinguishable from a wrong password. Any difference enables account enumeration.
        var pending = ARegisteredUser();

        var blocked = AnActiveUser();
        blocked.Block(Now);

        var deleted = AnActiveUser();
        deleted.Delete(Now);

        var invited = User.Invite(AnEmail(), "Dana", UserRole.Admin, Now).Value;

        var locked = AnActiveUser();
        for (var i = 0; i < User.MaxFailedSignInAttempts; i++)
        {
            locked.RecordFailedSignIn(Now);
        }

        foreach (var user in new[] { pending, blocked, deleted, invited, locked })
        {
            user.EnsureCanSignIn(Now).Error.Should().Be(IdentityErrors.InvalidCredentials);
        }
    }

    [Test]
    public void EnsureCanSignIn_SucceedsForAnActiveVerifiedAccount()
    {
        AnActiveUser().EnsureCanSignIn(Now).IsSuccess.Should().BeTrue();
    }

    // ---------- Lockout, BR-IDN-011 ----------

    [Test]
    public void RecordFailedSignIn_LocksOnTheFifthAttempt()
    {
        var user = AnActiveUser();

        for (var i = 0; i < User.MaxFailedSignInAttempts - 1; i++)
        {
            user.RecordFailedSignIn(Now);
        }

        user.IsLockedOut(Now).Should().BeFalse("four failures are not enough");

        user.RecordFailedSignIn(Now);

        user.IsLockedOut(Now).Should().BeTrue();
    }

    [Test]
    public void Lockout_ExpiresAfterTheConfiguredWindow()
    {
        var user = AnActiveUser();
        for (var i = 0; i < User.MaxFailedSignInAttempts; i++)
        {
            user.RecordFailedSignIn(Now);
        }

        user.IsLockedOut(Now.Add(User.LockoutDuration)).Should().BeFalse();
    }

    [Test]
    public void FailedAttempts_DoNotAccumulateAcrossAnExpiredLock()
    {
        // Without the reset, one failure months later would relock an account that had already
        // served its lockout.
        var user = AnActiveUser();
        for (var i = 0; i < User.MaxFailedSignInAttempts; i++)
        {
            user.RecordFailedSignIn(Now);
        }

        var later = Now.Add(User.LockoutDuration).AddMinutes(1);
        user.RecordFailedSignIn(later);

        user.IsLockedOut(later).Should().BeFalse();
        user.FailedSignInAttempts.Should().Be(1);
    }

    [Test]
    public void SuccessfulSignIn_ClearsTheCounter()
    {
        var user = AnActiveUser();
        user.RecordFailedSignIn(Now);
        user.RecordFailedSignIn(Now);

        user.RecordSuccessfulSignIn();

        user.FailedSignInAttempts.Should().Be(0);
        user.LockedUntil.Should().BeNull();
    }

    // ---------- Lifecycle ----------

    [Test]
    public void Verify_ActivatesAndRaisesTheEvent()
    {
        var user = ARegisteredUser();

        user.Verify(Now.AddHours(1)).IsSuccess.Should().BeTrue();

        user.Status.Should().Be(UserStatus.Active);
        user.VerifiedAt.Should().Be(Now.AddHours(1));
        user.DomainEvents.Should().ContainSingle(e => e is UserVerified);
    }

    [Test]
    public void Verify_Twice_Fails()
    {
        var user = AnActiveUser();

        user.Verify(Now).Error.Should().Be(IdentityErrors.AccountAlreadyVerified);
    }

    [Test]
    public void Block_RaisesTheEventThatEndsEverySession()
    {
        // BR-IDN-007: blocking must revoke live sessions, which the event carries out.
        var user = AnActiveUser();

        user.Block(Now).IsSuccess.Should().BeTrue();

        user.Status.Should().Be(UserStatus.Blocked);
        user.DomainEvents.Should().ContainSingle(e => e is UserAccessRevoked);
    }

    [Test]
    public void Block_Twice_Fails()
    {
        var user = AnActiveUser();
        user.Block(Now);

        user.Block(Now).Error.Should().Be(IdentityErrors.AccountAlreadyBlocked);
    }

    [Test]
    public void Restore_ReturnsAVerifiedAccountToActive()
    {
        var user = AnActiveUser();
        user.Block(Now);

        user.Restore().IsSuccess.Should().BeTrue();

        user.Status.Should().Be(UserStatus.Active);
    }

    [Test]
    public void Restore_ReturnsAnUnverifiedAccountToPending()
    {
        // Blocking and restoring must not become a way around BR-IDN-001.
        var user = ARegisteredUser();
        user.Block(Now);

        user.Restore();

        user.Status.Should().Be(UserStatus.PendingVerification);
        user.EnsureCanSignIn(Now).IsFailure.Should().BeTrue();
    }

    [Test]
    public void Restore_ClearsAnyLockout()
    {
        var user = AnActiveUser();
        for (var i = 0; i < User.MaxFailedSignInAttempts; i++)
        {
            user.RecordFailedSignIn(Now);
        }
        user.Block(Now);

        user.Restore();

        user.IsLockedOut(Now).Should().BeFalse();
    }

    [Test]
    public void Delete_BlocksEveryFurtherOperation()
    {
        var user = AnActiveUser();
        user.Delete(Now);

        user.Block(Now).Error.Should().Be(IdentityErrors.AccountDeleted);
        user.Delete(Now).Error.Should().Be(IdentityErrors.AccountDeleted);
        user.ChangePassword(AHash(), Now).Error.Should().Be(IdentityErrors.AccountDeleted);
    }

    // ---------- Password change, BR-IDN-013 ----------

    [Test]
    public void ChangePassword_RaisesTheEventThatRevokesOtherSessions()
    {
        var user = AnActiveUser();

        user.ChangePassword(PasswordHash.FromHashedValue("new-hash"), Now).IsSuccess.Should().BeTrue();

        user.DomainEvents.Should().ContainSingle(e => e is PasswordChanged);
    }

    [Test]
    public void ChangePassword_ClearsAnyLockout()
    {
        // Resetting a forgotten password is exactly what a locked-out user does next.
        var user = AnActiveUser();
        for (var i = 0; i < User.MaxFailedSignInAttempts; i++)
        {
            user.RecordFailedSignIn(Now);
        }

        user.ChangePassword(PasswordHash.FromHashedValue("new-hash"), Now);

        user.IsLockedOut(Now).Should().BeFalse();
        user.FailedSignInAttempts.Should().Be(0);
    }
}
