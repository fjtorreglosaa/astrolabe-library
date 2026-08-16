using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Membership.Commands.CancelScheduledPlanChange;

/// <summary>Withdraws a pending downgrade, leaving the member on their current plan. BR-MBR-018.</summary>
public sealed record CancelScheduledPlanChangeCommand : ICommand;
