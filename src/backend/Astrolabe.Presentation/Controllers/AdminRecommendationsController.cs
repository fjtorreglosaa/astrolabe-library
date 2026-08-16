using Astrolabe.Application.Contracts.Recommendations;
using Astrolabe.Application.Features.Recommendations.Commands.ConfigureLibraryAi;
using Astrolabe.Application.Features.Recommendations.Commands.DisableLibraryAi;
using Astrolabe.Application.Features.Recommendations.Queries.GetLibraryAiStatus;
using Astrolabe.Presentation.Contracts.Recommendations;
using Astrolabe.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astrolabe.Presentation.Controllers;

/// <summary>
/// The per-library AI configuration panel. Staff only, and scoped inside each handler by
/// BR-NET-006 — a key is money, and spending another library's is not a violation to find out about
/// afterwards.
/// </summary>
[Route("api/v1/admin/recommendations")]
[Authorize(Policy = Policies.StaffOnly)]
public sealed class AdminRecommendationsController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>
    /// Every library the caller administers. No response from this controller carries a credential
    /// in any form — see <c>LibraryAiStatusDto</c>.
    /// </summary>
    [HttpGet("libraries")]
    [ProducesResponseType<IReadOnlyList<LibraryAiStatusDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetLibraryAiStatusQuery(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>The prototype's "Save and test". Verifies before anything goes live — BR-REC-008.</summary>
    [HttpPut("libraries/{libraryId:guid}")]
    [ProducesResponseType<LibraryAiStatusDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Configure(
        Guid libraryId, ConfigureLibraryAiRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new ConfigureLibraryAiCommand(libraryId, request.Provider, request.Credential),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Switches recommendations off and keeps the credential — BR-REC-012.</summary>
    [HttpDelete("libraries/{libraryId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Disable(Guid libraryId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new DisableLibraryAiCommand(libraryId), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
}
