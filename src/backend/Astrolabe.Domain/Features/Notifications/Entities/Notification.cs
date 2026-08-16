using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Notifications.Enums;
using Astrolabe.Domain.Features.Notifications.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Notifications.Entities;

/// <summary>
/// One thing that happened to one member. Implements BR-NTF-001, BR-NTF-006 and BR-NTF-009.
/// </summary>
public sealed class Notification : AggregateRoot
{
    public const int MaxTitleLength = 160;
    public const int MaxBodyLength = 400;

    private Notification()
    {
    }

    private Notification(
        Guid id, Guid memberId, NotificationKind kind, string title, string body,
        string? route, DateTimeOffset now) : base(id)
    {
        MemberId = memberId;
        Kind = kind;
        Title = title;
        Body = body;
        Route = route;
        OccurredAt = now;
    }

    public Guid MemberId { get; private set; }

    public NotificationKind Kind { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    /// <summary>
    /// Where to go about it. BR-NTF-009 — a notification that can only be read is half a
    /// notification, and the member is left to find the screen themselves.
    /// </summary>
    public string? Route { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public bool IsRead => ReadAt is not null;

    public static Result<Notification> Raise(
        Guid memberId, NotificationKind kind, string title, string body,
        string? route, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure<Notification>(NotificationErrors.TitleRequired);
        }

        return Result.Success(new Notification(
            Guid.NewGuid(), memberId, kind,
            Truncate(title.Trim(), MaxTitleLength),
            Truncate((body ?? string.Empty).Trim(), MaxBodyLength),
            string.IsNullOrWhiteSpace(route) ? null : route.Trim(),
            now));
    }

    /// <summary>
    /// BR-NTF-006. Idempotent, and keeps the first time rather than the latest: "when did I read
    /// this" has one answer, and a second click is not a second reading.
    /// </summary>
    public void MarkRead(DateTimeOffset now)
    {
        ReadAt ??= now;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
