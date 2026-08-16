using Astrolabe.Application.Contracts.Reservations;
using Astrolabe.Application.Features.Reservations.Commands.BeginReturn;
using Astrolabe.Application.Features.Reservations.Commands.ConfirmReservation;
using Astrolabe.Application.Features.Reservations.Queries.GetMyDashboard;
using Astrolabe.Application.Features.Reservations.Queries.GetMyReservations;
using Astrolabe.Application.Features.Reservations.Queries.QuoteReservation;
using Astrolabe.Domain.Features.Reservations.Enums;
using Astrolabe.Domain.Primitives;
using Astrolabe.Presentation.Contracts.Reservations;
using Astrolabe.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astrolabe.Presentation.Controllers;

/// <summary>
/// The member's loans: taking a copy, seeing what they hold, and handing it back.
///
/// No route accepts a member identifier. BR-RSV-021 is enforced by the shape of the API rather than
/// by a check inside it, so one member cannot read another's loans by guessing an id.
/// </summary>
[Route("api/v1/reservations")]
[Authorize(Policy = Policies.MemberOnly)]
public sealed class ReservationsController(ISender sender) : ApiControllerBase(sender)
{
    [HttpGet]
    [ProducesResponseType<PagedResult<ReservationDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(
        [FromQuery] ReservationStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new GetMyReservationsQuery(status, page, pageSize), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("dashboard")]
    [ProducesResponseType<MemberDashboardDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetMyDashboardQuery(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>
    /// Prices a reservation and lists every branch with its verdict. A GET because it commits
    /// nothing: the modal asks again each time the member switches delivery method.
    /// </summary>
    [HttpGet("quote")]
    [ProducesResponseType<ReservationQuoteDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Quote(
        [FromQuery] Guid bookId,
        [FromQuery] DeliveryMethod delivery = DeliveryMethod.Collection,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new QuoteReservationQuery(bookId, delivery), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost]
    [ProducesResponseType<ReservationDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Confirm(
        [FromBody] ConfirmReservationRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ConfirmReservationCommand(
            request.BookId, request.LibraryId, request.Delivery, request.IdempotencyKey),
            cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetMine), new { }, result.Value)
            : HandleFailure(result);
    }

    /// <summary>
    /// The member's half of the return. It does not complete the loan — the library's check-in does,
    /// and until then the copy is somewhere between them.
    /// </summary>
    [HttpPost("{reservationId:guid}/return")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> BeginReturn(
        Guid reservationId,
        [FromBody] BeginReturnRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new BeginReturnCommand(reservationId, request.Method, request.Code), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
}
