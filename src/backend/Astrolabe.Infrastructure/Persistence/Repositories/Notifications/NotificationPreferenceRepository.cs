using Astrolabe.Domain.Features.Notifications.Entities;
using Astrolabe.Domain.Features.Notifications.Enums;
using Astrolabe.Domain.Features.Notifications.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Notifications;

public sealed class NotificationPreferenceRepository(AstrolabeDbContext context)
    : Repository<NotificationPreference>(context), INotificationPreferenceRepository
{
    public async Task<IReadOnlyList<NotificationFamily>> GetMutedAsync(
        Guid memberId, CancellationToken cancellationToken = default) =>
        await ReadOnlyQuery
            .Where(p => p.MemberId == memberId)
            .Select(p => p.Family)
            .ToListAsync(cancellationToken);

    public async Task<NotificationPreference?> GetAsync(
        Guid memberId, NotificationFamily family, CancellationToken cancellationToken = default) =>
        await Query.FirstOrDefaultAsync(
            p => p.MemberId == memberId && p.Family == family, cancellationToken);
}
