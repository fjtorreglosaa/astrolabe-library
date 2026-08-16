using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Recommendations;

namespace Astrolabe.Application.Features.Recommendations.Commands.RegenerateRecommendations;

/// <summary>
/// The member asks for a fresh set. Implements BR-REC-011.
///
/// A command rather than a query with a flag, because it spends a library's money. That is a write
/// whoever reads the code should have to notice.
/// </summary>
public sealed record RegenerateRecommendationsCommand : ICommand<RecommendationSetDto>;
