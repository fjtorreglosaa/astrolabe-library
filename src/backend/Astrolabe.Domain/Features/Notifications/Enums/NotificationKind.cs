namespace Astrolabe.Domain.Features.Notifications.Enums;

/// <summary>
/// What specifically happened. Transcribed from the prototype's <c>NOTE_KINDS</c>.
///
/// Finer than the family a member mutes by, because the icon and the wording differ even when the
/// decision to hear about them does not.
/// </summary>
public enum NotificationKind
{
    Due = 0,
    Pending = 1,
    Paid = 2,
    Transit = 3,
    Returned = 4,
    Hold = 5,
    Desk = 6,
    Support = 7
}
