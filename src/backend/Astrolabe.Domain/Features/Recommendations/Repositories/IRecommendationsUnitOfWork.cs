using Astrolabe.Domain.Abstractions.Persistence;

namespace Astrolabe.Domain.Features.Recommendations.Repositories;

/// <summary>
/// The recommendations context. Exposes only its own repositories, so a handler here cannot reach
/// another context's — the catalogue it recommends from arrives through a seam instead.
/// </summary>
public interface IRecommendationsUnitOfWork : IUnitOfWork
{
    ILibraryAiConfigurationRepository Configurations { get; }

    IRecommendationSetRepository Sets { get; }
}
