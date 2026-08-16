using Astrolabe.Application.Contracts.Support;
using Astrolabe.Application.Features.Support.Commands.OpenTicket;
using Astrolabe.Application.Features.Support.Commands.RateTicket;
using Astrolabe.Application.Features.Support.Commands.ReplyToTicket;
using Astrolabe.Application.Features.Support.Commands.TransitionTicket;
using Astrolabe.Application.Features.Support.Queries.GetTicket;
using Astrolabe.Application.Features.Support.Queries.SearchTickets;
using Astrolabe.Domain.Features.Support.Enums;
using Astrolabe.Domain.Primitives;
using Astrolabe.Presentation.Contracts.Support;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astrolabe.Presentation.Controllers;

/// <summary>
/// Support tickets, for members and staff alike.
///
/// <para>
/// One controller rather than two, because the routes are the same conversation seen from two sides
/// and every one of them decides its audience inside the handler. `BR-SUP-004` and `BR-SUP-010` are
/// enforced there because a policy cannot see which library a ticket belongs to, nor who opened it.
/// </para>
/// </summary>
[Route("api/v1/support")]
[Authorize]
public sealed class SupportController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>
    /// A member's own tickets, or a staff user's queue. The handler decides which by role — there is
    /// no parameter here that could ask for somebody else's.
    /// </summary>
    [HttpGet("tickets")]
    [ProducesResponseType<PagedResult<TicketSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? term,
        [FromQuery] TicketStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new SearchTicketsQuery(term, status, page, pageSize), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("tickets/{ticketId:guid}")]
    [ProducesResponseType<TicketDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid ticketId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetTicketQuery(ticketId), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("tickets")]
    [ProducesResponseType<TicketDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Open(
        OpenTicketRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new OpenTicketCommand(request.Subject, request.Body, request.Category, request.LibraryId),
            cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { ticketId = result.Value.Id }, result.Value)
            : HandleFailure(result);
    }

    [HttpPost("tickets/{ticketId:guid}/messages")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reply(
        Guid ticketId, ReplyToTicketRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new ReplyToTicketCommand(ticketId, request.Text), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    /// <summary>Assign, resolve or reopen. Staff only, enforced inside the handler.</summary>
    [HttpPost("tickets/{ticketId:guid}/{transition}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Transition(
        Guid ticketId, TicketTransition transition, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new TransitionTicketCommand(ticketId, transition), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    /// <summary>BR-SUP-005. The member's verdict, and only once the ticket is resolved.</summary>
    [HttpPost("tickets/{ticketId:guid}/rating")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Rate(
        Guid ticketId, RateTicketRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new RateTicketCommand(ticketId, request.Stars, request.Review), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
}
