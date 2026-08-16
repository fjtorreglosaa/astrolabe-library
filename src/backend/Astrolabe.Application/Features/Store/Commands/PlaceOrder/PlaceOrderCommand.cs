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
public sealed record PlaceOrderCommand(
    IReadOnlyList<OrderLineRequest> Lines,
    OrderFulfilment Fulfilment,
    Guid PaymentMethodId,
    string? IdempotencyKey) : ICommand<OrderDto>;

/// <summary>One book and how many of it.</summary>
public sealed record OrderLineRequest(Guid BookId, int Quantity);
