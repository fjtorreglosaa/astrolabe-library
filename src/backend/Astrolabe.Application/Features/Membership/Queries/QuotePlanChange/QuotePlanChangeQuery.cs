using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Membership;
using Astrolabe.Domain.Features.Membership.Enums;

namespace Astrolabe.Application.Features.Membership.Queries.QuotePlanChange;

/// <summary>
/// What a change to <paramref name="TargetPlan"/> would cost and what it would cost the member in
/// entitlements. Exists so the confirmation modal can state both before anything is committed.
/// Implements BR-MBR-014 and BR-MBR-020.
/// </summary>
public sealed record QuotePlanChangeQuery(PlanTier TargetPlan) : IQuery<PlanChangeQuoteDto>;
