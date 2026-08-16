using Astrolabe.Domain.Features.Membership.Enums;

namespace Astrolabe.Presentation.Contracts.Membership;

/// <summary>The body of a plan change. Direction is derived from rank, never sent by the caller.</summary>
public sealed record ChangePlanRequest(PlanTier TargetPlan);
