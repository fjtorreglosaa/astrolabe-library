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

    public static readonly Error SessionNotFound =
        Error.NotFound("identity.session_not_found", "Session not found.");

    public static readonly Error SessionNotOwnedByCaller =
        Error.Authorization("identity.session_not_owned_by_caller",
            "You can only manage your own sessions.");

    public static readonly Error SessionAlreadyRevoked =
        Error.Conflict("identity.session_already_revoked", "This session has already ended.");
}
