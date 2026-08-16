using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Recommendations.Enums;
using Astrolabe.Domain.Features.Recommendations.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Recommendations.Entities;

/// <summary>
/// What one member was last shown. Implements BR-REC-006 and the memory half of BR-REC-007.
///
/// <para>
/// Persisted rather than held in a memory cache, for two reasons that both come from the rules.
/// BR-REC-007 needs the <em>last</em> set after a provider failure, and an evicted memory entry
/// cannot supply one. BR-REC-006 forbids generating per render, and a restart that emptied memory
/// would charge every member a regeneration to learn what was already known.
/// </para>
/// </summary>
public sealed class RecommendationSet : AggregateRoot
{
    private readonly List<RecommendationItem> _items = [];

    private RecommendationSet()
    {
    }

    private RecommendationSet(
        Guid id, Guid memberId, RecommendationSource source, Guid? generatedByLibraryId,
        IEnumerable<RecommendationItem> items, DateTimeOffset now, TimeSpan lifetime) : base(id)
    {
        MemberId = memberId;
        Source = source;
        GeneratedByLibraryId = generatedByLibraryId;
        GeneratedAt = now;
        ExpiresAt = now.Add(lifetime);

        _items.AddRange(items);
    }

    public Guid MemberId { get; private set; }

    public RecommendationSource Source { get; private set; }

    /// <summary>
    /// Which library's credential paid for this. Null for a fallback, which costs nobody anything.
    ///
    /// Stored so BR-REC-012 can be enforced by eviction: when a library switches off, the sets it
    /// generated are the ones that must stop being served.
    /// </summary>
    public Guid? GeneratedByLibraryId { get; private set; }

    public DateTimeOffset GeneratedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public IReadOnlyList<RecommendationItem> Items => _items;

    public bool IsFresh(DateTimeOffset now) => now < ExpiresAt;

    public static Result<RecommendationSet> FromModel(
        Guid memberId,
        Guid libraryId,
        IReadOnlyList<RecommendationItem> items,
        DateTimeOffset now,
        TimeSpan lifetime)
    {
        // A model that returned nothing usable is a failure, not an empty answer. The caller falls
        // back, which is what BR-REC-003 and BR-REC-007 both ask for.
        if (items.Count == 0)
        {
            return Result.Failure<RecommendationSet>(RecommendationErrors.NothingToRecommend);
        }

        return Result.Success(new RecommendationSet(
            Guid.NewGuid(), memberId, RecommendationSource.Model, libraryId, items, now, lifetime));
    }

    /// <summary>
    /// The most-borrowed ranking. Returns an entity rather than a result because this path must not
    /// be able to fail — it is where every other path goes when it does.
    /// </summary>
    public static RecommendationSet FromFallback(
        Guid memberId,
        IReadOnlyList<RecommendationItem> items,
        DateTimeOffset now,
        TimeSpan lifetime) =>
        new(Guid.NewGuid(), memberId, RecommendationSource.Fallback, null, items, now, lifetime);
}
