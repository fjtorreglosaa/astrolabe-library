using Astrolabe.Application.Contracts.Reservations;
using Astrolabe.Application.Features.Reservations.Commands.CheckInReservation;
using Astrolabe.Application.Features.Reservations.Queries.GetLibraryReservations;
using Astrolabe.Domain.Features.Reservations.Enums;
using Astrolabe.Domain.Primitives;
using Astrolabe.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astrolabe.Presentation.Controllers;

/// <summary>
/// The desk: what is out on loan from the caller's libraries, and receiving copies back.
///
/// Separate from <see cref="ReservationsController"/> behind a controller-wide staff policy, as the
/// catalogue is. These routes expose other members' loans, and a per-method attribute is the kind of
/// thing that gets forgotten on the next endpoint.
/// </summary>
[Route("api/v1/admin/reservations")]
[Authorize(Policy = Policies.StaffOnly)]
public sealed class AdminReservationsController(ISender sender) : ApiControllerBase(sender)
{
    [HttpGet]
    [ProducesResponseType<PagedResult<StaffReservationDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForMyLibraries(
        [FromQuery] ReservationStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new GetLibraryReservationsQuery(status, page, pageSize), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>
    /// Receives a copy. The only act that puts a volume back on the shelf, and the only one that
    /// completes a loan.
    /// </summary>
    [HttpPost("{reservationId:guid}/check-in")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CheckIn(Guid reservationId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new CheckInReservationCommand(reservationId), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
}
