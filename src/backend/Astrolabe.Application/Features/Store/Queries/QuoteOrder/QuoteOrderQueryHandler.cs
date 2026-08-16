using Astrolabe.Application.Abstractions.Membership;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Contracts.Store;
using Astrolabe.Application.Shared.Store;
using Astrolabe.Domain.Features.Store.Entities;
using Astrolabe.Domain.Features.Store.Enums;
using Astrolabe.Domain.Features.Store.Errors;
using Astrolabe.Domain.Features.Store.Policies;
using Astrolabe.Domain.Features.Store.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Store.Queries.QuoteOrder;

public sealed class QuoteOrderQueryHandler(
    IStoreUnitOfWork store,
    IEntitlementProvider entitlements,
    ILibraryLocationProvider libraries) : IQueryHandler<QuoteOrderQuery, OrderQuoteDto>
{
    public async Task<Result<OrderQuoteDto>> Handle(
        QuoteOrderQuery request, CancellationToken cancellationToken)
    {
        if (request.BookIds.Count == 0)
        {
            return Result.Failure<OrderQuoteDto>(StoreErrors.NothingToBuy);
        }

        // With their copies: the discount depends on where each book is held, and loading them
        // without would make every book look unheld and silently zero every discount.
        var books = (await store.Books.GetByIdsWithCopiesAsync(request.BookIds, cancellationToken))
            .ToDictionary(book => book.Id);

        var member = await entitlements.GetForCurrentMemberAsync(cancellationToken);
        var locations = await libraries.GetAllAsync(cancellationToken);

        var lines = StorePricing.BuildLines(
            books,
            request.BookIds.Select(id => (id, 1)).ToList(),
            member,
            locations);

        if (lines.IsFailure)
        {
            return Result.Failure<OrderQuoteDto>(lines.Error);
        }

        var subtotal = Money.FromCents(lines.Value.Sum(line => line.GrossTotal.Cents));
        var discount = Money.FromCents(lines.Value.Sum(line => line.DiscountAmount.Cents));
        var afterDiscount = Money.FromCents(lines.Value.Sum(line => line.LineTotal.Cents));

        var shipping = request.Fulfilment is OrderFulfilment.Shipping
            ? Order.ShippingCost
            : Money.Zero;

        return Result.Success(new OrderQuoteDto(
            SubtotalCents: (int)subtotal.Cents,
            DiscountTotalCents: (int)discount.Cents,
            ShippingFeeCents: (int)shipping.Cents,
            TotalCents: (int)(afterDiscount + shipping).Cents,
            // The fee earns nothing, so the quote must not imply it does.
            PointsWouldEarn: RewardPointsPolicy.Earned(member.Plan, afterDiscount),
            DiscountNote: StorePricing.DiscountNote(member, lines.Value),
            Lines: lines.Value.Select(StorePricing.ToDto).ToList()));
    }
}
