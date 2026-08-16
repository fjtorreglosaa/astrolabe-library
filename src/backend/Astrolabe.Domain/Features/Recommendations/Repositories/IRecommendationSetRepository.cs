using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Recommendations.Entities;

namespace Astrolabe.Domain.Features.Recommendations.Repositories;

public interface IRecommendationSetRepository : IRepository<RecommendationSet>
{
    /// <summary>
    /// The member's most recent set, fresh or not.
    ///
    /// Deliberately not filtered by expiry: BR-REC-007 needs a stale set when a provider fails, and
    /// a repository that hid it would leave the handler with nothing to fall back to.
    /// </summary>
    Task<RecommendationSet?> GetLatestForMemberAsync(
        Guid memberId, CancellationToken cancellationToken = default);

    /// <summary>Drops every set a given library generated. BR-REC-012.</summary>
    Task RemoveGeneratedByAsync(Guid libraryId, CancellationToken cancellationToken = default);
}
