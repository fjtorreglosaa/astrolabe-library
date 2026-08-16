using Astrolabe.Domain.Features.Notifications.Enums;

namespace Astrolabe.Domain.Features.Notifications.Policies;

/// <summary>
/// Which family each kind belongs to. Implements BR-NTF-002.
///
/// <para>
/// A total map rather than a switch with a default: a kind added tomorrow without a family would
/// otherwise fall into whichever one the default names and be muted by the wrong switch. Here it
/// fails to compile instead.
/// </para>
/// Transcribed from the prototype's <c>NOTE_KINDS</c>, which is the authority for the mapping.
/// </summary>
public static class NotificationFamilies
{
    private static readonly Dictionary<NotificationKind, NotificationFamily> Map = new()
    {
        [NotificationKind.Due] = NotificationFamily.Due,
        [NotificationKind.Pending] = NotificationFamily.Payments,
        [NotificationKind.Paid] = NotificationFamily.Payments,
        [NotificationKind.Desk] = NotificationFamily.Payments,
        [NotificationKind.Transit] = NotificationFamily.Returns,
        [NotificationKind.Returned] = NotificationFamily.Returns,
        [NotificationKind.Hold] = NotificationFamily.Holds,
        [NotificationKind.Support] = NotificationFamily.Support,
    };

    public static NotificationFamily Of(NotificationKind kind) => Map[kind];

    /// <summary>Every kind is mapped. Asserted by a test, so the map cannot fall behind the enum.</summary>
    public static IReadOnlyDictionary<NotificationKind, NotificationFamily> All => Map;
}
