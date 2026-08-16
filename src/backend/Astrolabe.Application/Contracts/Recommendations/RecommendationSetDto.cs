namespace Astrolabe.Application.Contracts.Recommendations;

/// <summary>
/// What the AI screen renders.
///
/// <c>Source</c> travels because the two answers read differently and the prototype says which is
/// which rather than passing the fallback off as a personalised pick. <c>Note</c> is the sentence
/// that explains it, in the member's own terms.
/// </summary>
public sealed record RecommendationSetDto(
    string Source,
    string Note,
    DateTimeOffset GeneratedAt,
    bool CanRegenerate,
    IReadOnlyList<RecommendationDto> Items);
