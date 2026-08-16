using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Membership.Policies;
using FluentAssertions;

namespace Astrolabe.Domain.Tests.Features.Membership;

/// <summary>
/// Covers BR-MBR-020: what the member is told they lose before a downgrade is confirmed.
///
/// Every downward transition is exercised, because the rule is a disclosure obligation — a loss the
/// policy forgets is a loss the member is never warned about. The list is checked exactly rather
/// than by containment, so a disclosure the prototype does not make cannot creep in either.
/// </summary>
[TestFixture]
public sealed class PlanChangePolicyTests
{
    [Test]
    public void AnUpgrade_LosesNothing()
    {
        PlanChangePolicy.LossesOf(PlanTier.Basic, PlanTier.Max).Should().BeEmpty();
        PlanChangePolicy.LossesOf(PlanTier.Plus, PlanTier.Max).Should().BeEmpty();
        PlanChangePolicy.LossesOf(PlanTier.Basic, PlanTier.Plus).Should().BeEmpty();
    }

    [Test]
    public void StayingOnTheSamePlan_LosesNothing()
    {
        PlanChangePolicy.LossesOf(PlanTier.Plus, PlanTier.Plus).Should().BeEmpty();
    }

    [Test]
    public void LeavingMaxForPlus_LosesOnlyThePoints()
    {
        // The prototype's own list for this transition is a single line. Plus keeps the full
        // catalogue and keeps recommendations, so there is nothing else to disclose.
        PlanChangePolicy.LossesOf(PlanTier.Max, PlanTier.Plus)
            .Should().Equal(PlanChangeLoss.RewardPoints);
    }

    [Test]
    public void LeavingMaxForBasic_LosesPointsBorrowingReachAndRecommendations()
    {
        PlanChangePolicy.LossesOf(PlanTier.Max, PlanTier.Basic).Should().Equal(
            PlanChangeLoss.RewardPoints,
            PlanChangeLoss.HomeLibraryAndBasicCatalog,
            PlanChangeLoss.Recommendations);
    }

    [Test]
    public void LeavingPlusForBasic_LosesReachAndRecommendationsButNotPoints()
    {
        // A Plus member never accrued points, so warning them about losing points would be false.
        PlanChangePolicy.LossesOf(PlanTier.Plus, PlanTier.Basic).Should().Equal(
            PlanChangeLoss.HomeLibraryAndBasicCatalog,
            PlanChangeLoss.Recommendations);
    }

    [Test]
    public void EveryMoveToBasic_WarnsThatRecommendationsTurnOff()
    {
        PlanChangePolicy.LossesOf(PlanTier.Max, PlanTier.Basic)
            .Should().Contain(PlanChangeLoss.Recommendations);
        PlanChangePolicy.LossesOf(PlanTier.Plus, PlanTier.Basic)
            .Should().Contain(PlanChangeLoss.Recommendations);
    }
}
