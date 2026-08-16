using Astrolabe.Domain.Features.Recommendations.Repositories;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Recommendations;

/// <summary>
/// The recommendations context. Shares the request's change tracker with every other unit of work,
/// so a configuration change and its audit entry commit together or not at all.
/// </summary>
public sealed class RecommendationsUnitOfWork(
    AstrolabeDbContext context,
    ILibraryAiConfigurationRepository configurations,
    IRecommendationSetRepository sets) : UnitOfWorkBase(context), IRecommendationsUnitOfWork
{
    public ILibraryAiConfigurationRepository Configurations { get; } = configurations;

    public IRecommendationSetRepository Sets { get; } = sets;
}
