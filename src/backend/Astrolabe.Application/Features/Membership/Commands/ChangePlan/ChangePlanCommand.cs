using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Membership;
using Astrolabe.Domain.Features.Membership.Enums;

namespace Astrolabe.Application.Features.Membership.Commands.ChangePlan;

/// <summary>
/// Moves the caller to another plan. One command covers both directions: the member presses one
/// button, and which rules apply is decided by plan rank inside the aggregate. Two commands would
/// let a caller apply upgrade rules to a downgrade. Implements BR-MBR-013 to BR-MBR-020.
/// </summary>
public sealed record ChangePlanCommand(PlanTier TargetPlan) : ICommand<PlanChangeResultDto>;
