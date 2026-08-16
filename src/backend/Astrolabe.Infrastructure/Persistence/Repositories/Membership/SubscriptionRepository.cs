using Astrolabe.Domain.Features.Membership.Entities;
using Astrolabe.Domain.Features.Membership.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Membership;

public sealed class SubscriptionRepository(AstrolabeDbContext context)
    : Repository<Subscription>(context), ISubscriptionRepository
{
    public async Task<Subscription?> GetActiveForMemberAsync(
        Guid memberId, CancellationToken cancellationToken = default) =>
        await Query
            .FirstOrDefaultAsync(s => s.MemberId == memberId && s.EndedAt == null, cancellationToken);

    public async Task<IReadOnlyList<Subscription>> GetDueForRenewalAsync(
        DateTimeOffset asOf, int maxCount, CancellationToken cancellationToken = default) =>
        await Query
            .Where(s => s.EndedAt == null && s.Cycle.RenewsOn <= asOf)
            .OrderBy(s => s.Cycle.RenewsOn)
            .Take(maxCount)
            .ToListAsync(cancellationToken);
}
