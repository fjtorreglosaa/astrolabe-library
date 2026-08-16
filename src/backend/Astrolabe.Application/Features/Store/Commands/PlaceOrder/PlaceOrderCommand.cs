using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Store;
using Astrolabe.Domain.Features.Store.Enums;

namespace Astrolabe.Application.Features.Store.Commands.PlaceOrder;

/// <summary>
/// Buys one or more books. Implements BR-STR-001 to BR-STR-017.
///
/// No member identifier: BR-STR-016 is enforced by the contract. The idempotency key deduplicates a
/// retried purchase — unlike a payment, which settles named fines and is naturally a no-op, an order
/// creates something new each time and needs a key to be safe on a flaky connection.
/// </summary>
/// <param name="PointsToRedeem">
/// Reward point-cents to put toward this purchase, or zero to pay entirely by card. BR-STR-007 caps
/// it at half the book total and refuses anything under 100.
/// </param>
public sealed record PlaceOrderCommand(
    IReadOnlyList<OrderLineRequest> Lines,
    OrderFulfilment Fulfilment,
    Guid PaymentMethodId,
    int PointsToRedeem,
    string? IdempotencyKey) : ICommand<OrderDto>;
