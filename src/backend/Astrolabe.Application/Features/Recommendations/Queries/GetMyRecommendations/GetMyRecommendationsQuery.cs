using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Recommendations;

namespace Astrolabe.Application.Features.Recommendations.Queries.GetMyRecommendations;

/// <summary>
/// The member's current recommendations. No identifier: it comes from the token.
///
/// A query and not a command although it can generate a set: from the member's point of view this
/// is reading a screen, and BR-REC-006 makes generation an implementation detail of a cache miss.
/// Regenerating on purpose is <c>RegenerateRecommendationsCommand</c>, which is rate limited.
/// </summary>
public sealed record GetMyRecommendationsQuery : IQuery<RecommendationSetDto>;
