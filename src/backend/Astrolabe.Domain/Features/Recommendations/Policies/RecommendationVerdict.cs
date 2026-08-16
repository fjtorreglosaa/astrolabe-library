namespace Astrolabe.Domain.Features.Recommendations.Policies;

/// <summary>
/// What the recommendations surface owes a given member.
///
/// Three outcomes, not two, because "you cannot see this" and "here is the most-borrowed list" are
/// different answers to different questions — and collapsing them would either hide a plan benefit
/// behind a fallback or show a Basic member a surface BR-REC-002 forbids.
/// </summary>
public enum RecommendationVerdict
{
    /// <summary>BR-REC-002. The plan excludes it, and the member is told which plans include it.</summary>
    NotIncludedInPlan = 0,

    /// <summary>BR-REC-003. No connected library in their city; the most-borrowed ranking is served.</summary>
    Fallback = 1,

    ModelGenerated = 2
}
