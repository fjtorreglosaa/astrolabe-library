using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Notifications.Entities;
using Astrolabe.Domain.Features.Notifications.Enums;

namespace Astrolabe.Domain.Features.Notifications.Repositories;

public interface INotificationPreferenceRepository : IRepository<NotificationPreference>
{
    /// <summary>The families this member has muted. Empty means they hear everything.</summary>
    Task<IReadOnlyList<NotificationFamily>> GetMutedAsync(
        Guid memberId, CancellationToken cancellationToken = default);

    Task<NotificationPreference?> GetAsync(
        Guid memberId, NotificationFamily family, CancellationToken cancellationToken = default);
}
