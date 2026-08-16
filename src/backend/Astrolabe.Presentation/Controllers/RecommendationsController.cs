using Astrolabe.Application.Contracts.Recommendations;
using Astrolabe.Application.Features.Recommendations.Commands.RegenerateRecommendations;
using Astrolabe.Application.Features.Recommendations.Queries.GetMyRecommendations;
using Astrolabe.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astrolabe.Presentation.Controllers;

/// <summary>
/// The member's recommendations surface.
///
/// <c>MemberOnly</c> is the outer door; BR-REC-002 is enforced inside the handlers, because a policy
/// cannot see a plan — the role stopped carrying one at GLOBAL-019.
/// </summary>
[Route("api/v1/recommendations")]
[Authorize(Policy = Policies.MemberOnly)]
public sealed class RecommendationsController(ISender sender) : ApiControllerBase(sender)
{
    [HttpGet]
    [ProducesResponseType<RecommendationSetDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetMyRecommendationsQuery(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>
    /// A fresh set, on request. A POST because it spends a library's money — BR-REC-011 rate limits
    /// it, and a GET that charged somebody would be a GET that a browser could repeat by itself.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType<RecommendationSetDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RegenerateRecommendationsCommand(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
}
