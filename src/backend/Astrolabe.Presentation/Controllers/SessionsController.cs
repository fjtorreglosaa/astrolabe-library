using Astrolabe.Application.Contracts.Identity;
using Astrolabe.Application.Features.Identity.Commands.RevokeSessions;
using Astrolabe.Application.Features.Identity.Queries.GetMySessions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astrolabe.Presentation.Controllers;

/// <summary>
/// The devices screen: list the caller's live sessions and end them.
///
/// Every endpoint acts on the caller's own sessions only. There is deliberately no parameter for
/// whose sessions to operate on, so BR-IDN-025 cannot be bypassed by supplying someone else's
/// identifier.
/// </summary>
[Route("api/v1/sessions")]
[Authorize]
public sealed class SessionsController(ISender sender) : ApiControllerBase(sender)
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SessionDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetMySessionsQuery(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpDelete("{sessionId:guid}")]
    public async Task<IActionResult> Revoke(Guid sessionId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new RevokeSessionsCommand(RevocationScope.Specified, [sessionId]), cancellationToken);

        return result.IsSuccess ? Ok(new RevokedResponse(result.Value)) : HandleFailure(result);
    }

    [HttpPost("revoke")]
    [ProducesResponseType<RevokedResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeMany(
        RevokeSessionsRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new RevokeSessionsCommand(request.Scope, request.SessionIds), cancellationToken);

        return result.IsSuccess ? Ok(new RevokedResponse(result.Value)) : HandleFailure(result);
    }
}

public sealed record RevokeSessionsRequest(RevocationScope Scope, IReadOnlyList<Guid>? SessionIds);

public sealed record RevokedResponse(int Revoked);
