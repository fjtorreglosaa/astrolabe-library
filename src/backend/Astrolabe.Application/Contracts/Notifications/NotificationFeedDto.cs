namespace Astrolabe.Application.Contracts.Notifications;

/// <summary>
/// The bell and its list.
///
/// <c>UnreadCount</c> is counted rather than stored — BR-NTF-010 — and travels with the feed so the
/// badge and the list can never disagree about the same moment.
/// </summary>
public sealed record NotificationFeedDto(
    int UnreadCount,
    IReadOnlyList<string> MutedFamilies,
    IReadOnlyList<NotificationDto> Items);
