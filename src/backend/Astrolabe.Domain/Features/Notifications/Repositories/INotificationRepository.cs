using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Notifications.Entities;

namespace Astrolabe.Domain.Features.Notifications.Repositories;

public interface INotificationRepository : IRepository<Notification>
{
    Task<IReadOnlyList<Notification>> GetForMemberAsync(
        Guid memberId, int limit, CancellationToken cancellationToken = default);

    /// <summary>BR-NTF-010. Counted in the database, never stored as a column.</summary>
    Task<int> CountUnreadAsync(Guid memberId, CancellationToken cancellationToken = default);

    Task<int> MarkAllReadAsync(
        Guid memberId, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>BR-NTF-008. Permanent, and no undo is offered anywhere.</summary>
    Task ClearForMemberAsync(Guid memberId, CancellationToken cancellationToken = default);
}
