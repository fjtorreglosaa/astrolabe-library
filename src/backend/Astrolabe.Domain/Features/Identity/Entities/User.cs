using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.Events;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Identity.Entities;

/// <summary>
/// An account: who someone is and how they prove it.
///
/// Sessions are a separate aggregate root. They churn on every request while this record is nearly
/// static, so folding them in here would force loading every session just to change a name.
/// </summary>
public sealed class User : AggregateRoot
{
    /// <summary>Failed attempts tolerated before the account locks (BR-IDN-011).</summary>
    public const int MaxFailedSignInAttempts = 5;

    /// <summary>How long a lock lasts, and the window failed attempts are counted over.</summary>
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private User()
    {
    }

    private User(
        Guid id,
        Email email,
        PasswordHash? passwordHash,
        string fullName,
        Guid? countryId,
        Guid? cityId,
        UserRole role,
        UserStatus status,
        DateTimeOffset now) : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        FullName = fullName;
        CountryId = countryId;
        CityId = cityId;
        Role = role;
        Status = status;
        CreatedAt = now;
    }

    public Email Email { get; private set; } = null!;

    /// <summary>
    /// Null only while a staff account is <see cref="UserStatus.Invited"/>: the invitee chooses
    /// their password when they accept, so there is nothing to store until then.
    /// </summary>
    public PasswordHash? PasswordHash { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    /// <summary>Null for a staff account, which has no city of residence.</summary>
    public Guid? CountryId { get; private set; }

    public Guid? CityId { get; private set; }

    public UserRole Role { get; private set; }

    public UserStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? VerifiedAt { get; private set; }

    public int FailedSignInAttempts { get; private set; }

    public DateTimeOffset? LockedUntil { get; private set; }

    /// <summary>Reserved for TOTP. Nothing reads it yet; the column exists so enabling 2FA later is not a migration of the whole table.</summary>
    public string? TotpSecret { get; private set; }

    // ---------- Creation ----------

    /// <summary>
    /// Public registration. Implements BR-IDN-001 and BR-IDN-003: the account starts unable to sign
    /// in, and a member always carries a country and a city.
    /// </summary>
    public static Result<User> Register(
        Email email,
        PasswordHash passwordHash,
        string fullName,
        Guid countryId,
        Guid cityId,
        UserRole plan,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(passwordHash);

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Result.Failure<User>(IdentityErrors.FullNameRequired);
        }

        // A member's role is their plan. Registering directly into a staff role would bypass
        // BR-NET-008, which reserves that to a super administrator.
        if (!plan.IsMember())
        {
            return Result.Failure<User>(IdentityErrors.InvalidCredentials);
        }

        var user = new User(
            Guid.NewGuid(), email, passwordHash, fullName.Trim(),
            countryId, cityId, plan, UserStatus.PendingVerification, now);

        user.Raise(new UserRegistered(Guid.NewGuid(), now, user.Id, email.Value, fullName.Trim()));

        return Result.Success(user);
    }

    /// <summary>
    /// Staff onboarding by invitation. Implements BR-IDN-006: the account exists but cannot sign in
    /// until the invitation is confirmed, and it has no password until then.
    /// </summary>
    public static Result<User> Invite(Email email, string fullName, UserRole role, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(email);

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Result.Failure<User>(IdentityErrors.FullNameRequired);
        }

        if (!role.IsStaff())
        {
            return Result.Failure<User>(IdentityErrors.InvalidCredentials);
        }

        return Result.Success(new User(
            Guid.NewGuid(), email, null, fullName.Trim(),
            null, null, role, UserStatus.Invited, now));
    }

    // ---------- Sign-in gate ----------

    /// <summary>
    /// The single place that answers "may this account authenticate".
    ///
    /// Every rejection returns the same error on purpose. BR-IDN-028 requires an unverified account,
    /// a blocked one, a deleted one, an invited one and a locked one to be indistinguishable from a
    /// wrong password: any difference would let an attacker enumerate accounts and their state.
    /// Keeping the checks together is what makes that auditable rather than aspirational.
    /// </summary>
    public Result EnsureCanSignIn(DateTimeOffset now)
    {
        if (Status is not UserStatus.Active)
        {
            return Result.Failure(IdentityErrors.InvalidCredentials);
        }

        if (IsLockedOut(now))
        {
            return Result.Failure(IdentityErrors.InvalidCredentials);
        }

        if (PasswordHash is null)
        {
            return Result.Failure(IdentityErrors.InvalidCredentials);
        }

        return Result.Success();
    }

    public bool IsLockedOut(DateTimeOffset now) => LockedUntil is { } until && now < until;

    /// <summary>
    /// Records a failed attempt and locks the account on the fifth within the window (BR-IDN-011).
    /// </summary>
    public void RecordFailedSignIn(DateTimeOffset now)
    {
        // A lock that has already expired resets the counter, so old failures cannot accumulate
        // across days and lock an account on a single mistake.
        if (LockedUntil is { } until && now >= until)
        {
            FailedSignInAttempts = 0;
            LockedUntil = null;
        }

        FailedSignInAttempts++;

        if (FailedSignInAttempts >= MaxFailedSignInAttempts)
        {
            LockedUntil = now.Add(LockoutDuration);
        }
    }

    public void RecordSuccessfulSignIn()
    {
        FailedSignInAttempts = 0;
        LockedUntil = null;
    }

    // ---------- Lifecycle ----------

    public Result Verify(DateTimeOffset now)
    {
        if (Status is UserStatus.Active)
        {
            return Result.Failure(IdentityErrors.AccountAlreadyVerified);
        }

        if (Status is not (UserStatus.PendingVerification or UserStatus.Invited))
        {
            return Result.Failure(IdentityErrors.CannotVerifyANonPendingAccount);
        }

        Status = UserStatus.Active;
        VerifiedAt = now;

        Raise(new UserVerified(Guid.NewGuid(), now, Id, Role));

        return Result.Success();
    }

    public Result Block(DateTimeOffset now)
    {
        if (Status is UserStatus.Deleted)
        {
            return Result.Failure(IdentityErrors.AccountDeleted);
        }

        if (Status is UserStatus.Blocked)
        {
            return Result.Failure(IdentityErrors.AccountAlreadyBlocked);
        }

        Status = UserStatus.Blocked;

        // BR-IDN-007: blocking must end every live session. The session aggregate acts on this.
        Raise(new UserAccessRevoked(Guid.NewGuid(), now, Id, SessionRevocationReason.AccountClosed));

        return Result.Success();
    }

    public Result Restore()
    {
        if (Status is not (UserStatus.Blocked or UserStatus.Deleted))
        {
            return Result.Failure(IdentityErrors.AccountAlreadyVerified);
        }

        // Restored to Active only if it had been verified; otherwise it returns to awaiting
        // verification, so BR-IDN-001 is not bypassed by blocking and restoring an account.
        Status = VerifiedAt is null ? UserStatus.PendingVerification : UserStatus.Active;
        FailedSignInAttempts = 0;
        LockedUntil = null;

        return Result.Success();
    }

    public Result Delete(DateTimeOffset now)
    {
        if (Status is UserStatus.Deleted)
        {
            return Result.Failure(IdentityErrors.AccountDeleted);
        }

        Status = UserStatus.Deleted;

        Raise(new UserAccessRevoked(Guid.NewGuid(), now, Id, SessionRevocationReason.AccountClosed));

        return Result.Success();
    }

    /// <summary>
    /// Sets a new password. Implements BR-IDN-013: every other session must end, which the raised
    /// event carries out.
    /// </summary>
    public Result ChangePassword(PasswordHash newHash, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(newHash);

        if (Status is UserStatus.Deleted)
        {
            return Result.Failure(IdentityErrors.AccountDeleted);
        }

        PasswordHash = newHash;
        FailedSignInAttempts = 0;
        LockedUntil = null;

        Raise(new PasswordChanged(Guid.NewGuid(), now, Id));

        return Result.Success();
    }

    /// <summary>
    /// Sets the initial password when a staff invitation is accepted, and activates the account.
    /// </summary>
    public Result AcceptInvitation(PasswordHash passwordHash, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(passwordHash);

        if (Status is not UserStatus.Invited)
        {
            return Result.Failure(IdentityErrors.CannotVerifyANonPendingAccount);
        }

        PasswordHash = passwordHash;
        Status = UserStatus.Active;
        VerifiedAt = now;

        Raise(new UserVerified(Guid.NewGuid(), now, Id, Role));

        return Result.Success();
    }

    /// <summary>Changes the role. Reserved to a super administrator, enforced by the handler.</summary>
    public void ChangeRole(UserRole role) => Role = role;

    /// <summary>Moves the member to another city of residence. Recalculates plan reach elsewhere.</summary>
    public void ChangeResidence(Guid countryId, Guid cityId)
    {
        CountryId = countryId;
        CityId = cityId;
    }
}
