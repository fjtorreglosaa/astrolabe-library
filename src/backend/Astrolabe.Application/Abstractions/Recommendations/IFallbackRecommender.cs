using Astrolabe.Application.Contracts.Recommendations;

namespace Astrolabe.Application.Abstractions.Recommendations;

/// <summary>
/// The most-borrowed ranking. Backs BR-REC-003 and the last resort of BR-REC-007.
///
/// <para>
/// A plain catalogue query with no provider involved, which is the point: this is where every other
/// path goes when it fails, so it must not depend on the thing that failed. It returns suggestions
/// with reasons like every other path, because BR-REC-010 does not exempt it.
/// </para>
/// </summary>
public interface IFallbackRecommender
{
    Task<IReadOnlyList<ProviderSuggestion>> GetAsync(
        Guid memberId, int count, CancellationToken cancellationToken = default);
}
