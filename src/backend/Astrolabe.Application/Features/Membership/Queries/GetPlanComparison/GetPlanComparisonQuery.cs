using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Membership;

namespace Astrolabe.Application.Features.Membership.Queries.GetPlanComparison;

/// <summary>The three plans side by side, marked relative to the caller's own. BR-MBR-002 to -009.</summary>
public sealed record GetPlanComparisonQuery : IQuery<IReadOnlyList<PlanOptionDto>>;
