namespace Astrolabe.Application.Contracts.Notifications;

/// <summary>One entry in the centre. <c>Route</c> is where it goes — BR-NTF-009.</summary>
public sealed record NotificationDto(
    Guid Id,
    string Kind,
    string Family,
    string Title,
    string Body,
    string? Route,
    DateTimeOffset OccurredAt,
    bool IsRead);
