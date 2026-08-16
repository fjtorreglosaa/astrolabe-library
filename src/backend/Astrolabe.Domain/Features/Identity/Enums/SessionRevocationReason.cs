namespace Astrolabe.Domain.Features.Identity.Enums;

/// <summary>
/// Why a session ended. Recorded so the audit trail can distinguish a routine sign-out from a
/// security response.
/// </summary>
public enum SessionRevocationReason
{
    /// <summary>The member signed out of this device (BR-IDN-027).</summary>
    SignedOut = 0,

    /// <summary>The member revoked it from their sessions screen (BR-IDN-024).</summary>
    RevokedByUser = 1,

    /// <summary>A password change or reset revoked it (BR-IDN-013).</summary>
    PasswordChanged = 2,

    /// <summary>An already-rotated refresh token was presented. Theft is assumed (BR-IDN-018).</summary>
    TokenReuseDetected = 3,

    /// <summary>The account was blocked or deleted (BR-IDN-007).</summary>
    AccountClosed = 4,

    /// <summary>The session reached its expiry.</summary>
    Expired = 5
}
