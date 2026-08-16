using Astrolabe.Application.Contracts.Recommendations;
using Astrolabe.Domain.Features.Membership.ValueObjects;
using Astrolabe.Domain.Features.Recommendations.Entities;

namespace Astrolabe.Application.Abstractions.Recommendations;

/// <summary>
/// Produces a recommendation set, falling back as far as it must. Implements BR-REC-003 and
/// BR-REC-007 as a single ordered decision.
///
/// <para>
/// A seam rather than code in the query handler because two callers need exactly this sequence —
/// reading a stale cache and regenerating on purpose — and a sequence with a fallback chain in it is
/// the last thing that should exist twice. The read path and the refresh path must reach the same
/// answer, or a member who pressed refresh would see something the screen would not.
/// </para>
/// </summary>
public interface IRecommendationGenerator
{
    /// <summary>
    /// Generates, persists and describes a set. Never throws and never returns an error: it walks
    /// model → previous set → fallback, and the fallback cannot fail.
    /// </summary>
    Task<RecommendationSetDto> GenerateAsync(
        Guid memberId,
        MemberEntitlement member,
        RecommendationSet? previous,
        CancellationToken cancellationToken = default);

    /// <summary>Renders an existing set, resolving its book titles.</summary>
    Task<RecommendationSetDto> DescribeAsync(
        RecommendationSet set, CancellationToken cancellationToken = default);
}
