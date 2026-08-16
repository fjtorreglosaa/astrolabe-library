using Astrolabe.Application.Contracts.Billing;
using Astrolabe.Application.Features.Billing.Commands.AddPaymentMethod;
using Astrolabe.Application.Features.Billing.Commands.IssueDeskPayment;
using Astrolabe.Application.Features.Billing.Commands.PayFines;
using Astrolabe.Application.Features.Billing.Commands.RemovePaymentMethod;
using Astrolabe.Application.Features.Billing.Queries.GetMyFines;
using Astrolabe.Application.Features.Billing.Queries.GetMyLedger;
using Astrolabe.Application.Features.Billing.Queries.GetMyPaymentMethods;
using Astrolabe.Domain.Primitives;
using Astrolabe.Presentation.Contracts.Billing;
using Astrolabe.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astrolabe.Presentation.Controllers;

/// <summary>
/// Fines and payments, for the member who owes them.
///
/// No route accepts a member identifier — BR-BIL-016 is enforced by the shape of the API — and no
/// route accepts a card number, because no part of this system has anywhere to put one.
/// </summary>
[Route("api/v1/billing")]
[Authorize(Policy = Policies.MemberOnly)]
public sealed class BillingController(ISender sender) : ApiControllerBase(sender)
{
    [HttpGet("fines")]
    [ProducesResponseType<FinesSummaryDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyFines(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetMyFinesQuery(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("ledger")]
    [ProducesResponseType<PagedResult<LedgerEntryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyLedger(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetMyLedgerQuery(page, pageSize), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("payment-methods")]
    [ProducesResponseType<IReadOnlyList<PaymentMethodDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPaymentMethods(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetMyPaymentMethodsQuery(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("payment-methods")]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    public async Task<IActionResult> AddPaymentMethod(
        [FromBody] AddPaymentMethodRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new AddPaymentMethodCommand(
            request.Brand, request.Last4, request.ExpiryMonthYear,
            request.CardholderName, request.MakePrimary), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetMyPaymentMethods), new { }, result.Value)
            : HandleFailure(result);
    }

    [HttpDelete("payment-methods/{paymentMethodId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemovePaymentMethod(
        Guid paymentMethodId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new RemovePaymentMethodCommand(paymentMethodId), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpPost("payments")]
    [ProducesResponseType<PaymentReceiptDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> PayFines(
        [FromBody] PayFinesRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new PayFinesCommand(request.FineIds, request.PaymentMethodId), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>
    /// Produces a code for a library counter. Nothing is charged: the fines stay owed until a
    /// librarian confirms they took the money.
    /// </summary>
    [HttpPost("desk-payments")]
    [ProducesResponseType<DeskPaymentDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> IssueDeskPayment(
        [FromBody] IssueDeskPaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new IssueDeskPaymentCommand(request.FineIds), cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetMyFines), new { }, result.Value)
            : HandleFailure(result);
    }
}
