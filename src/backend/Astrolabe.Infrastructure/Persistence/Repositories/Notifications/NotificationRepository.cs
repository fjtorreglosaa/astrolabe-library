using Astrolabe.Domain.Features.Notifications.Entities;
using Astrolabe.Domain.Features.Notifications.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Notifications;

public sealed class NotificationRepository(AstrolabeDbContext context)
    : Repository<Notification>(context), INotificationRepository
{
    public async Task<IReadOnlyList<Notification>> GetForMemberAsync(
        Guid memberId, int limit, CancellationToken cancellationToken = default) =>
        await ReadOnlyQuery
            .Where(n => n.MemberId == memberId)
            .OrderByDescending(n => n.OccurredAt)
            .ThenBy(n => n.Id)
            .Take(Math.Clamp(limit, 1, 100))
            .ToListAsync(cancellationToken);

    public async Task<int> CountUnreadAsync(
        Guid memberId, CancellationToken cancellationToken = default) =>
        // BR-NTF-010. Counted over everything, not over the page: a badge that only counted the
        // first thirty would quietly stop growing at thirty.
        await ReadOnlyQuery.CountAsync(
            n => n.MemberId == memberId && n.ReadAt == null, cancellationToken);

    public async Task<int> MarkAllReadAsync(
        Guid memberId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var unread = await Query
            .Where(n => n.MemberId == memberId && n.ReadAt == null)
            .ToListAsync(cancellationToken);

        foreach (var notification in unread)
        {
            notification.MarkRead(now);
        }

        // BR-NTF-006: the second call finds nothing unread and writes nothing.
        return unread.Count;
    }

    public async Task ClearForMemberAsync(
        Guid memberId, CancellationToken cancellationToken = default)
    {
        // Filtered inside the repository, so there is no call site where the member could be
        // omitted and the whole table cleared.
        var all = await Query.Where(n => n.MemberId == memberId).ToListAsync(cancellationToken);

        foreach (var notification in all)
        {
            Remove(notification);
        }
    }
}
