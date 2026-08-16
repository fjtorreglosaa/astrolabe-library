using Astrolabe.Application.Contracts.Billing;
using Astrolabe.Application.Features.Billing.Commands.RejectDeskPayment;
using Astrolabe.Application.Features.Billing.Commands.ValidateDeskPayment;
using Astrolabe.Application.Features.Billing.Queries.GetDeskPayments;
using Astrolabe.Domain.Features.Billing.Enums;
using Astrolabe.Domain.Primitives;
using Astrolabe.Presentation.Contracts.Billing;
using Astrolabe.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astrolabe.Presentation.Controllers;

/// <summary>
/// The desk: payments waiting to be taken, and taking them.
///
/// Separate from <see cref="BillingController"/> behind a controller-wide staff policy. These routes
/// expose other members' debts and move money, which is not the kind of authority to leave to a
/// per-method attribute somebody might forget on the next endpoint.
/// </summary>
[Route("api/v1/admin/payments")]
[Authorize(Policy = Policies.StaffOnly)]
public sealed class AdminPaymentsController(ISender sender) : ApiControllerBase(sender)
{
    [HttpGet]
    [ProducesResponseType<PagedResult<DeskPaymentDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQueue(
        [FromQuery] DeskPaymentStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new GetDeskPaymentsQuery(status, page, pageSize), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>Confirms the money was taken. The only thing that settles a desk payment.</summary>
    [HttpPost("{code}/validate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Validate(string code, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ValidateDeskPaymentCommand(code), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpPost("{code}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reject(
        string code,
        [FromBody] RejectDeskPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new RejectDeskPaymentCommand(code, request.Reason), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
}
