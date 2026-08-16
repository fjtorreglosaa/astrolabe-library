using Astrolabe.Application.Contracts.Network;
using Astrolabe.Application.Features.Network.Commands.AcceptInvitation;
using Astrolabe.Application.Features.Network.Commands.AssignLibraries;
using Astrolabe.Application.Features.Network.Commands.CreateLibrary;
using Astrolabe.Application.Features.Network.Commands.DeactivateLibrary;
using Astrolabe.Application.Features.Network.Commands.DesignateHomeLibrary;
using Astrolabe.Application.Features.Network.Commands.InviteAdmin;
using Astrolabe.Application.Features.Network.Commands.RevokeAdmin;
using Astrolabe.Application.Features.Network.Queries.GetAdminTeam;
using Astrolabe.Application.Features.Network.Queries.GetCitiesByCountry;
using Astrolabe.Application.Features.Network.Queries.GetLibraries;
using Astrolabe.Application.Features.Network.Queries.GetMyScope;
using Astrolabe.Application.Features.Network.Queries.GetRegistrationCountries;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astrolabe.Presentation.Controllers;

/// <summary>
/// The library network: geography for the registration form, and the super administrator's team.
/// </summary>
[Route("api/v1/network")]
[Authorize]
public sealed class NetworkController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>
    /// Anonymous by necessity: the registration form needs this before anyone has an account.
    /// It exposes only countries and cities that have an active library, never anything sensitive.
    /// </summary>
    [HttpGet("countries")]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<CountryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCountries(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetRegistrationCountriesQuery(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("countries/{countryId:guid}/cities")]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<CityDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCities(Guid countryId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetCitiesByCountryQuery(countryId), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("libraries")]
    [Authorize]
    [ProducesResponseType<IReadOnlyList<LibraryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLibraries(
        [FromQuery] Guid? cityId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetLibrariesQuery(cityId), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("my-scope")]
    [Authorize(Policy = Policies.StaffOnly)]
    [ProducesResponseType<LibraryScopeDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyScope(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetMyScopeQuery(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("admins")]
    [Authorize(Policy = Policies.SuperAdminOnly)]
    [ProducesResponseType<IReadOnlyList<AdminDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdmins(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetAdminTeamQuery(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("admins")]
    [Authorize(Policy = Policies.SuperAdminOnly)]
    public async Task<IActionResult> InviteAdmin(
        InviteAdminRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new InviteAdminCommand(
                request.Email, request.FullName, request.Role, request.LibraryIds, request.Message),
            cancellationToken);

        return result.IsSuccess ? Ok(new InvitationResponse(result.Value)) : HandleFailure(result);
    }

    /// <summary>Anonymous: the invitee has no account until they accept.</summary>
    [HttpPost("admins/accept-invitation")]
    [AllowAnonymous]
    public async Task<IActionResult> AcceptInvitation(
        AcceptInvitationRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new AcceptInvitationCommand(request.Token, request.Password), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpPut("admins/{userId:guid}/libraries")]
    [Authorize(Policy = Policies.SuperAdminOnly)]
    public async Task<IActionResult> AssignLibraries(
        Guid userId, AssignLibrariesRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new AssignLibrariesCommand(userId, request.LibraryIds), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpDelete("admins/{userId:guid}")]
    [Authorize(Policy = Policies.SuperAdminOnly)]
    public async Task<IActionResult> RevokeAdmin(Guid userId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RevokeAdminCommand(userId), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpPost("libraries")]
    [Authorize(Policy = Policies.SuperAdminOnly)]
    public async Task<IActionResult> CreateLibrary(
        CreateLibraryRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new CreateLibraryCommand(request.CityId, request.Name), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetLibraries), new { }, new { id = result.Value })
            : HandleFailure(result);
    }

    [HttpDelete("libraries/{libraryId:guid}")]
    [Authorize(Policy = Policies.SuperAdminOnly)]
    public async Task<IActionResult> DeactivateLibrary(
        Guid libraryId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new DeactivateLibraryCommand(libraryId), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpPut("cities/{cityId:guid}/home-library")]
    [Authorize(Policy = Policies.SuperAdminOnly)]
    public async Task<IActionResult> DesignateHomeLibrary(
        Guid cityId, DesignateHomeLibraryRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new DesignateHomeLibraryCommand(cityId, request.LibraryId), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
}

public sealed record InviteAdminRequest(
    string Email, string FullName, UserRole Role, IReadOnlyList<Guid> LibraryIds, string? Message);

public sealed record AcceptInvitationRequest(string Token, string Password);

public sealed record AssignLibrariesRequest(IReadOnlyList<Guid> LibraryIds);

public sealed record CreateLibraryRequest(Guid CityId, string Name);

public sealed record DesignateHomeLibraryRequest(Guid LibraryId);

public sealed record InvitationResponse(Guid InvitationId);
