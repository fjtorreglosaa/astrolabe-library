using Astrolabe.Domain.Features.Recommendations.Enums;

namespace Astrolabe.Application.Abstractions.Recommendations;

/// <summary>
/// Picks the client for a provider.
///
/// A registry rather than injecting <c>IEnumerable&lt;IAiRecommendationProvider&gt;</c> and hunting
/// through it at each call site: the lookup is the same everywhere, and a missing implementation
/// should fail once, loudly, at resolution rather than as a silent empty answer in a handler.
/// </summary>
public interface IAiProviderRegistry
{
    IAiRecommendationProvider For(AiProvider provider);
}
