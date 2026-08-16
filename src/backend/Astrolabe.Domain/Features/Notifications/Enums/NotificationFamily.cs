namespace Astrolabe.Domain.Features.Notifications.Enums;

/// <summary>
/// What a member mutes. BR-NTF-002: they silence a family, never a single kind.
///
/// Coarser than <see cref="NotificationKind"/> on purpose — somebody who turns off payments means
/// all of them, and offering eight switches where five decisions exist is offering the member work.
/// </summary>
public enum NotificationFamily
{
    Due = 0,
    Payments = 1,
    Returns = 2,
    Holds = 3,
    Support = 4
}
