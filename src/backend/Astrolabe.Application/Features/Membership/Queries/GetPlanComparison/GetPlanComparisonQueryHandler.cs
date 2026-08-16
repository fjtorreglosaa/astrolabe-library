using Astrolabe.Application.Abstractions.Membership;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Membership;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Membership.ValueObjects;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Membership.Queries.GetPlanComparison;

public sealed class GetPlanComparisonQueryHandler(IEntitlementProvider entitlements)
    : IQueryHandler<GetPlanComparisonQuery, IReadOnlyList<PlanOptionDto>>
{
    public async Task<Result<IReadOnlyList<PlanOptionDto>>> Handle(
        GetPlanComparisonQuery request, CancellationToken cancellationToken)
    {
        var entitlement = await entitlements.GetForCurrentMemberAsync(cancellationToken);

        // The plan table is read from the catalogue rather than transcribed here, so a price change
        // lands in one place.
        var options = PlanCatalog.All
            .Select(plan => new PlanOptionDto(
                plan.Tier.ToString(),
                (int)plan.MonthlyPrice.Cents,
                plan.Reach.ToString(),
                plan.DiscountPercent,
                plan.EarnsPoints,
                plan.SeesRecommendations,
                IsCurrent: plan.Tier == entitlement.Plan,
                Direction: DirectionFrom(entitlement.Plan, plan.Tier)))
            .ToList();

        return Result.Success<IReadOnlyList<PlanOptionDto>>(options);
    }

    private static string? DirectionFrom(PlanTier current, PlanTier candidate)
    {
        if (candidate == current)
        {
            return null;
        }

        return candidate.IsHigherThan(current) ? "upgrade" : "downgrade";
    }
}
