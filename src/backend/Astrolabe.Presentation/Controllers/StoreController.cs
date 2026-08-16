using Astrolabe.Application.Contracts.Store;
using Astrolabe.Application.Features.Store.Commands.PlaceOrder;
using Astrolabe.Application.Features.Store.Queries.GetMyOrders;
using Astrolabe.Application.Features.Store.Queries.GetMyPoints;
using Astrolabe.Application.Features.Store.Queries.QuoteOrder;
using Astrolabe.Domain.Features.Store.Enums;
using Astrolabe.Domain.Primitives;
using Astrolabe.Presentation.Contracts.Store;
using Astrolabe.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astrolabe.Presentation.Controllers;

/// <summary>
/// Buying books, and the reward points buying earns.
///
/// No route accepts a member identifier, and none accepts a price: what a book costs comes from the
/// catalogue, so a caller cannot name their own.
///
/// There is no redemption endpoint. `BR-STR-007` is undefined and `BLOCK-002` is open.
/// </summary>
[Route("api/v1/store")]
[Authorize(Policy = Policies.MemberOnly)]
public sealed class StoreController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>
    /// Prices an order without charging anything. A GET because the modal asks again each time the
    /// member switches fulfilment.
    /// </summary>
    [HttpGet("quote")]
    [ProducesResponseType<OrderQuoteDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Quote(
        [FromQuery] Guid[] bookIds,
        [FromQuery] OrderFulfilment fulfilment = OrderFulfilment.Collection,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new QuoteOrderQuery(bookIds, fulfilment), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("orders")]
    [ProducesResponseType<PagedResult<OrderDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetMyOrdersQuery(page, pageSize), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("points")]
    [ProducesResponseType<PointsSummaryDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPoints(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetMyPointsQuery(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("orders")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> PlaceOrder(
        [FromBody] PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new PlaceOrderCommand(
            request.Lines.Select(line => new OrderLineRequest(line.BookId, line.Quantity)).ToList(),
            request.Fulfilment,
            request.PaymentMethodId,
            request.IdempotencyKey), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetMyOrders), new { }, result.Value)
            : HandleFailure(result);
    }
}
