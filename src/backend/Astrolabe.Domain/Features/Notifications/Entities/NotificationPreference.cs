using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Notifications.Enums;

namespace Astrolabe.Domain.Features.Notifications.Entities;

/// <summary>
/// A member's decision to stop hearing about one family. Implements BR-NTF-002 and BR-NTF-003.
///
/// <para>
/// A row exists only for a family that is <b>muted</b>. Absence means "on", so a member who has
/// never opened the settings has no rows and receives everything — which is what they expect, and
/// which saves a write on every registration to record a default nobody chose.
/// </para>
/// <para>
/// Unmuting is therefore a delete. That is the point: there is no third state to get wrong, and no
/// way for a row to say "on" while another says otherwise.
/// </para>
/// </summary>
public sealed class NotificationPreference : AggregateRoot
{
    private NotificationPreference()
    {
    }

    private NotificationPreference(
        Guid id, Guid memberId, NotificationFamily family, DateTimeOffset now) : base(id)
    {
        MemberId = memberId;
        Family = family;
        MutedAt = now;
    }

    public Guid MemberId { get; private set; }

    public NotificationFamily Family { get; private set; }

    public DateTimeOffset MutedAt { get; private set; }

    public static NotificationPreference Mute(
        Guid memberId, NotificationFamily family, DateTimeOffset now) =>
        new(Guid.NewGuid(), memberId, family, now);
}
