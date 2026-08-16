using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Store;
using Astrolabe.Domain.Features.Store.Enums;

namespace Astrolabe.Application.Features.Store.Queries.QuoteOrder;

/// <summary>
/// What an order would cost, before anything is charged.
///
/// Priced by the same policy the purchase uses. Computing it in the frontend would put money
/// arithmetic in two languages, and the day they disagree the member is charged something other
/// than what they agreed to.
/// </summary>
/// <param name="PointsToRedeem">
/// Reward point-cents the member wants to apply, or zero. The quote answers with what it actually
/// allowed, so the modal never shows a redemption the purchase would then refuse.
/// </param>
public sealed record QuoteOrderQuery(
    IReadOnlyList<Guid> BookIds,
    OrderFulfilment Fulfilment,
    int PointsToRedeem) : IQuery<OrderQuoteDto>;
