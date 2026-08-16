namespace Astrolabe.Application.Contracts.Recommendations;

/// <summary>
/// One suggestion as a provider returned it, before the domain has judged it.
///
/// Separate from <c>RecommendationItem</c> on purpose: this is untrusted input. A model can return a
/// book that is not a candidate, a reason that is blank, or a percentage above 100, and the domain
/// is where each of those stops being possible.
/// </summary>
public sealed record ProviderSuggestion(Guid BookId, string Reason, int MatchPercent);
