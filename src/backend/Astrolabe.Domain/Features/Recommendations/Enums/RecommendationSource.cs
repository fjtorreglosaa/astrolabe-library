namespace Astrolabe.Domain.Features.Recommendations.Enums;

/// <summary>
/// Where a set came from. Carried to the member, because the two read differently: one is a
/// personalised answer and the other is the most-borrowed list, and the prototype says so plainly
/// rather than passing the fallback off as a recommendation.
/// </summary>
public enum RecommendationSource
{
    Model = 0,
    Fallback = 1
}
