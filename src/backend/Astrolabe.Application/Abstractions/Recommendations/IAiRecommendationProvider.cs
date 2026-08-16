using Astrolabe.Application.Contracts.Recommendations;
using Astrolabe.Domain.Features.Recommendations.Enums;

namespace Astrolabe.Application.Abstractions.Recommendations;

/// <summary>
/// Calls a model vendor. One implementation per <see cref="AiProvider"/>.
///
/// <para>
/// Both methods answer with a result rather than throwing. BR-REC-007 says a member must never see
/// an error on this surface, and a seam that throws makes that the caller's problem at every call
/// site — including the ones written later.
/// </para>
/// </summary>
public interface IAiRecommendationProvider
{
    AiProvider Provider { get; }

    /// <summary>
    /// BR-REC-008. Does the credential work? A cheap call, made when staff press "Save and test".
    /// </summary>
    Task<bool> VerifyCredentialAsync(string credential, CancellationToken cancellationToken = default);

    /// <summary>
    /// Suggestions, or an empty list when the provider failed. Never an exception: the caller's job
    /// is to fall back, not to catch.
    /// </summary>
    Task<IReadOnlyList<ProviderSuggestion>> SuggestAsync(
        string credential, ReadingProfile profile, int count,
        CancellationToken cancellationToken = default);
}
