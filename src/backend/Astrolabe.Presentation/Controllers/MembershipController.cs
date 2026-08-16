using Astrolabe.Application.Contracts.Membership;
using Astrolabe.Application.Features.Membership.Commands.CancelScheduledPlanChange;
using Astrolabe.Application.Features.Membership.Commands.ChangeCityOfResidence;
using Astrolabe.Application.Features.Membership.Commands.ChangePlan;
using Astrolabe.Application.Features.Membership.Queries.GetMyMembership;
using Astrolabe.Application.Features.Membership.Queries.GetPlanComparison;
using Astrolabe.Application.Features.Membership.Queries.QuotePlanChange;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Presentation.Contracts.Membership;
using Astrolabe.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astrolabe.Presentation.Controllers;

/// <summary>
/// The caller's own membership: what they are on, what else is offered, and moving between them.
///
/// Every endpoint acts on the caller and takes no member identifier, so one member can never read or
/// change another's plan by guessing an id.
/// </summary>
[Route("api/v1/membership")]
[Authorize(Policy = Policies.MemberOnly)]
public sealed class MembershipController(ISender sender) : ApiControllerBase(sender)
{
    [HttpGet("me")]
    [ProducesResponseType<MembershipDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyMembership(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetMyMembershipQuery(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("plans")]
    [ProducesResponseType<IReadOnlyList<PlanOptionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlans(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetPlanComparisonQuery(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>
    /// What a change would cost and what it would cost in entitlements. A GET because it changes
    /// nothing: the modal may ask repeatedly as the member compares plans.
    /// </summary>
    [HttpGet("plans/{targetPlan}/quote")]
    [ProducesResponseType<PlanChangeQuoteDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> QuotePlanChange(
        PlanTier targetPlan, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new QuotePlanChangeQuery(targetPlan), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("plan")]
    [ProducesResponseType<PlanChangeResultDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangePlan(
        [FromBody] ChangePlanRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ChangePlanCommand(request.TargetPlan), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpDelete("plan/scheduled-change")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CancelScheduledPlanChange(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new CancelScheduledPlanChangeCommand(), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpPut("residence")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangeResidence(
        [FromBody] ChangeResidenceRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new ChangeCityOfResidenceCommand(request.CountryId, request.CityId), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
}
