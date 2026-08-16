namespace Astrolabe.Domain.Features.Identity.Enums;

/// <summary>
/// The lifecycle state of an account. See identity.business.md section 6.2.
/// Only <see cref="Active"/> may authenticate.
/// </summary>
public enum UserStatus
{
    /// <summary>Registered but the email address is unconfirmed. Cannot sign in (BR-IDN-001).</summary>
    PendingVerification = 0,

    Active = 1,

    /// <summary>Blocked by staff. Cannot sign in; active sessions are revoked at the moment of blocking.</summary>
    Blocked = 2,

    /// <summary>Deleted. Cannot sign in and is hidden from member-facing queries; history is preserved.</summary>
    Deleted = 3,

    /// <summary>A staff account awaiting confirmation of its invitation (BR-NET-013).</summary>
    Invited = 4
}
