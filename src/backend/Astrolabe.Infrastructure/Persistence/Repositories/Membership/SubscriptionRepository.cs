using Astrolabe.Domain.Features.Membership.Entities;
using Astrolabe.Domain.Features.Membership.Enums;
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

    public async Task<IReadOnlyDictionary<Guid, PlanTier>> GetActivePlansForAsync(
        IReadOnlyCollection<Guid> memberIds, CancellationToken cancellationToken = default)
    {
        if (memberIds.Count == 0)
        {
            return new Dictionary<Guid, PlanTier>();
        }

        // Projected to two columns rather than materialising the aggregates: a listing needs the
        // tier and nothing else, and loading whole subscriptions with their owned cycle would pull
        // eight more columns per row for a label.
        var rows = await ReadOnlyQuery
            .Where(s => s.EndedAt == null && memberIds.Contains(s.MemberId))
            .Select(s => new { s.MemberId, s.Plan })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.MemberId, row => row.Plan);
    }

    public async Task<IReadOnlyList<Subscription>> GetDueForRenewalAsync(
        DateTimeOffset asOf, int maxCount, CancellationToken cancellationToken = default) =>
        await Query
            .Where(s => s.EndedAt == null && s.Cycle.RenewsOn <= asOf)
            .OrderBy(s => s.Cycle.RenewsOn)
            .Take(maxCount)
            .ToListAsync(cancellationToken);
}
