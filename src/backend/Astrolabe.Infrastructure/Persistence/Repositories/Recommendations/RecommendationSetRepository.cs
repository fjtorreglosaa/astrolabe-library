using Astrolabe.Domain.Features.Recommendations.Entities;
using Astrolabe.Domain.Features.Recommendations.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Recommendations;

public sealed class RecommendationSetRepository(AstrolabeDbContext context)
    : Repository<RecommendationSet>(context), IRecommendationSetRepository
{
    public async Task<RecommendationSet?> GetLatestForMemberAsync(
        Guid memberId, CancellationToken cancellationToken = default) =>
        await Query
            .Include(s => s.Items)
            // Deliberately not filtered by expiry. BR-REC-007 needs a stale set when a provider
            // fails, and a repository that hid it would leave the generator nothing to fall back to.
            .OrderByDescending(s => s.GeneratedAt)
            .FirstOrDefaultAsync(s => s.MemberId == memberId, cancellationToken);

    public async Task RemoveGeneratedByAsync(
        Guid libraryId, CancellationToken cancellationToken = default)
    {
        var stale = await Query
            .Where(s => s.GeneratedByLibraryId == libraryId)
            .ToListAsync(cancellationToken);

        foreach (var set in stale)
        {
            Remove(set);
        }
    }
}
