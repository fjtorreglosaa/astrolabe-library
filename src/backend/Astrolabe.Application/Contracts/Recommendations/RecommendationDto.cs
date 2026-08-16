namespace Astrolabe.Application.Contracts.Recommendations;

/// <summary>One suggestion as the member sees it. Always carries its reason — BR-REC-010.</summary>
public sealed record RecommendationDto(
    Guid BookId,
    string Title,
    string Author,
    string? CoverUrl,
    string Reason,
    int MatchPercent);
