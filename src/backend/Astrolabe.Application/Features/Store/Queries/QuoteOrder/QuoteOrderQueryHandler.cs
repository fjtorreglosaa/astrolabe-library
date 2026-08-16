using Astrolabe.Application.Abstractions.Identity;
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
    ILibraryLocationProvider libraries,
    ICurrentUser currentUser) : IQueryHandler<QuoteOrderQuery, OrderQuoteDto>
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

        var balance = currentUser.UserId is { } memberId
            ? await store.Points.GetBalanceAsync(memberId, cancellationToken)
            : 0;

        var maxRedeemable = RewardRedemptionPolicy.MaxRedeemable(member.Plan, balance, afterDiscount);

        // Clamped rather than refused. A quote is not a commitment, and answering an over-large
        // request with an error would leave the modal with nothing to render while the member is
        // still dragging a slider.
        var redeemed = Math.Clamp(request.PointsToRedeem, 0, maxRedeemable);

        var settledInMoney = afterDiscount - Money.FromCents(redeemed);

        return Result.Success(new OrderQuoteDto(
            SubtotalCents: (int)subtotal.Cents,
            DiscountTotalCents: (int)discount.Cents,
            ShippingFeeCents: (int)shipping.Cents,
            TotalCents: (int)(afterDiscount + shipping).Cents,
            PointsBalance: balance,
            MaxRedeemablePointCents: maxRedeemable,
            PointsRedeemed: redeemed,
            AmountChargedCents: (int)(settledInMoney + shipping).Cents,
            // Neither the fee nor the part paid in points earns, so the quote must not imply either.
            PointsWouldEarn: RewardPointsPolicy.Earned(member.Plan, settledInMoney),
            DiscountNote: StorePricing.DiscountNote(member, lines.Value),
            RedemptionNote: StorePricing.RedemptionNote(member.Plan, balance, maxRedeemable),
            Lines: lines.Value.Select(StorePricing.ToDto).ToList()));
    }
}
