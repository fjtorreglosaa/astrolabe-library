using Astrolabe.Application.Abstractions.Recommendations;
using Astrolabe.Domain.Features.Recommendations.Enums;

namespace Astrolabe.Infrastructure.Integrations.Ai;

/// <summary>
/// Picks the client for a provider.
///
/// Throws when one is missing, and deliberately: a provider with no implementation is a wiring
/// mistake, and the alternative — answering null and letting the caller fall back — would hide it
/// behind a member quietly seeing the most-borrowed list forever.
/// </summary>
public sealed class AiProviderRegistry(IEnumerable<IAiRecommendationProvider> providers)
    : IAiProviderRegistry
{
    private readonly Dictionary<AiProvider, IAiRecommendationProvider> _byProvider =
        providers.ToDictionary(provider => provider.Provider);

    public IAiRecommendationProvider For(AiProvider provider) =>
        _byProvider.TryGetValue(provider, out var client)
            ? client
            : throw new InvalidOperationException(
                $"No client is registered for the {provider} provider.");
}
