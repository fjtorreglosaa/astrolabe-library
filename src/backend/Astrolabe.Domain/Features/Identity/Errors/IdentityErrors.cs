using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Identity.Errors;

/// <summary>
/// Reusable, strongly typed errors for the identity domain. Declared once so no call site invents a
/// magic string. See GUIDELINES.md section 18.
/// </summary>
public static class IdentityErrors
{
    // ---------- Value objects ----------

    public static readonly Error EmailRequired =
        Error.Validation("identity.email_required", "An email address is required.");

    public static readonly Error EmailInvalid =
        Error.Validation("identity.email_invalid", "This email address is not valid.");

    public static readonly Error FullNameRequired =
        Error.Validation("identity.full_name_required", "A full name is required.");

    public static readonly Error PasswordTooShort =
        Error.Validation("identity.password_too_short",
            "A password must be at least 12 characters long.");

    // ---------- Sign-in ----------

    /// <summary>
    /// The single failure returned for a wrong password, an unknown address, an unverified account,
    /// a blocked account, a deleted account, and a locked account.
    ///
    /// BR-IDN-028 requires all six to be indistinguishable: a distinct message for any of them would
    /// let an attacker enumerate accounts and learn their state.
    /// </summary>
    public static readonly Error InvalidCredentials =
        Error.Authentication("identity.invalid_credentials",
            "The email address or password is incorrect.");

    // ---------- Tokens ----------

    /// <summary>
    /// Returned for a refresh token that is unknown, expired, already rotated, or belongs to a
    /// revoked session. BR-IDN-019 requires these to be indistinguishable, so reuse detection
    /// revokes the session silently rather than announcing why.
    /// </summary>
    public static readonly Error InvalidRefreshToken =
        Error.Authentication("identity.invalid_refresh_token",
            "This session has ended. Please sign in again.");

    public static readonly Error InvalidVerificationToken =
        Error.Validation("identity.invalid_verification_token",
            "This link is no longer valid. Request a new one.");

    public static readonly Error InvalidRecoveryToken =
        Error.Validation("identity.invalid_recovery_token",
            "This link is no longer valid. Request a new one.");

    // ---------- Account lifecycle ----------

    public static readonly Error EmailAlreadyRegistered =
        Error.Conflict("identity.email_already_registered",
            "An account already exists for this email address.");

    public static readonly Error AccountNotFound =
        Error.NotFound("identity.account_not_found", "Account not found.");

    public static readonly Error AccountAlreadyVerified =
        Error.Conflict("identity.account_already_verified", "This account is already verified.");

    public static readonly Error AccountAlreadyBlocked =
        Error.Conflict("identity.account_already_blocked", "This account is already blocked.");

    public static readonly Error AccountDeleted =
        Error.Conflict("identity.account_deleted", "This account has been deleted.");

    public static readonly Error CannotVerifyANonPendingAccount =
        Error.Conflict("identity.cannot_verify_non_pending_account",
            "Only an account awaiting verification can be verified.");

    // ---------- Sessions ----------

    // ---------- The user directory, Stage 6 ----------

    /// <summary>
    /// Ahead of every other refusal. Blocking your own account is the one mistake in this console
    /// that cannot be undone from inside it.
    /// </summary>
    public static readonly Error CannotAdministerYourself =
        Error.Conflict("identity.cannot_administer_yourself",
            "This is your own account. You cannot block or delete yourself.");

    public static readonly Error StaffRequired =
        Error.Authorization("identity.staff_required",
            "Only staff can administer accounts.");

    /// <summary>BR-NET-012. The network must never be left without a super administrator.</summary>
    public static readonly Error CannotAdministerASuperAdmin =
        Error.Authorization("identity.cannot_administer_a_super_admin",
            "Super administrators cannot be blocked or deleted from this console.");

    /// <summary>BR-NET-008, reached sideways. An administrator must not manage another.</summary>
    public static readonly Error SuperAdminRequiredForStaff =
        Error.Authorization("identity.super_admin_required_for_staff",
            "Only a super administrator can block or delete another administrator.");

    /// <summary>
    /// BR-NET-006 and BR-NET-010. Distinct from "not found": a super administrator asking for the
    /// same account gets it, so the account plainly exists.
    /// </summary>
    public static readonly Error AccountOutOfScope =
        Error.Authorization("identity.account_out_of_scope",
            "That account belongs to a city you do not administer.");

    public static readonly Error AccountNotPendingVerification =
        Error.Conflict("identity.account_not_pending_verification",
            "That account has already been verified.");

    public static readonly Error SessionNotFound =
        Error.NotFound("identity.session_not_found", "Session not found.");

    public static readonly Error SessionNotOwnedByCaller =
        Error.Authorization("identity.session_not_owned_by_caller",
            "You can only manage your own sessions.");

    public static readonly Error SessionAlreadyRevoked =
        Error.Conflict("identity.session_already_revoked", "This session has already ended.");
}
