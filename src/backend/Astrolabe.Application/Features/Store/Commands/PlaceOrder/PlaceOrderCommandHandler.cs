using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Membership;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Contracts.Store;
using Astrolabe.Application.Shared.Store;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Audit.Entities;
using Astrolabe.Domain.Features.Audit.Repositories;
using Astrolabe.Domain.Features.Billing.Entities;
using Astrolabe.Domain.Features.Billing.Errors;
using Astrolabe.Domain.Features.Billing.Repositories;
using Astrolabe.Domain.Features.Store.Entities;
using Astrolabe.Domain.Features.Store.Errors;
using Astrolabe.Domain.Features.Store.Policies;
using Astrolabe.Domain.Features.Store.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Store.Commands.PlaceOrder;

/// <summary>
/// Places and pays for an order.
///
/// <para>
/// One commit covers the order, the charge, the payment and the points. A purchase is paid the
/// moment it is placed — unlike a fine, which is charged first and settled later — so both ledger
/// entries are written together. An order that existed without its ledger entries would be a
/// purchase the member's own statement denies.
/// </para>
/// </summary>
public sealed class PlaceOrderCommandHandler(
    IStoreUnitOfWork store,
    IBillingUnitOfWork billing,
    IAuditUnitOfWork audit,
    IEntitlementProvider entitlements,
    ILibraryLocationProvider libraries,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<PlaceOrderCommand, OrderDto>
{
    public async Task<Result<OrderDto>> Handle(
        PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure<OrderDto>(StoreErrors.OrderNotYours);
        }

        if (request.Lines.Count == 0)
        {
            return Result.Failure<OrderDto>(StoreErrors.NothingToBuy);
        }

        // BR-STR-015, checked first so a replay never re-prices anything or charges again.
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await store.Orders.GetByIdempotencyKeyAsync(
                memberId, request.IdempotencyKey.Trim(), cancellationToken);

            if (existing is not null)
            {
                return Result.Success(StorePricing.ToDto(existing));
            }
        }

        // Looked up by member as well as by id, so a card belonging to somebody else is simply not
        // found rather than refused after being read.
        var card = await billing.PaymentMethods.GetForMemberAsync(
            memberId, request.PaymentMethodId, cancellationToken);

        if (card is null)
        {
            return Result.Failure<OrderDto>(BillingErrors.PaymentMethodNotFound);
        }

        var bookIds = request.Lines.Select(line => line.BookId).Distinct().ToList();
        // With their copies: the discount depends on where each book is held, and loading them
        // without would make every book look unheld and silently zero every discount.
        var books = (await store.Books.GetByIdsWithCopiesAsync(bookIds, cancellationToken))
            .ToDictionary(book => book.Id);

        var member = await entitlements.GetForCurrentMemberAsync(cancellationToken);
        var locations = await libraries.GetAllAsync(cancellationToken);

        var lines = StorePricing.BuildLines(
            books,
            request.Lines.Select(line => (line.BookId, line.Quantity)).ToList(),
            member,
            locations);

        if (lines.IsFailure)
        {
            return Result.Failure<OrderDto>(lines.Error);
        }

        var now = clock.UtcNow;

        // BR-STR-007. The balance is a fact about the member, not about the order, so it is checked
        // here; the cap is a pure function of the lines and the aggregate checks that itself.
        // Read after the lines are priced, because the cap depends on what they came to.
        var afterDiscount = Money.FromCents(lines.Value.Sum(line => line.LineTotal.Cents));
        var balance = await store.Points.GetBalanceAsync(memberId, cancellationToken);

        var redemption = RewardRedemptionPolicy.EnsureValid(
            member.Plan, request.PointsToRedeem, balance, afterDiscount);

        if (redemption.IsFailure)
        {
            return Result.Failure<OrderDto>(redemption.Error);
        }

        var order = Order.Place(
            memberId, request.Fulfilment, lines.Value, member.Plan,
            request.PointsToRedeem, request.IdempotencyKey, now);

        if (order.IsFailure)
        {
            return Result.Failure<OrderDto>(order.Error);
        }

        await store.Orders.AddAsync(order.Value, cancellationToken);

        // BR-STR-014. Charged and paid together, because the card is taken at the moment of
        // purchase. Writing only the charge would leave every member permanently in debit by the
        // value of everything they have ever bought.
        //
        // The charge is the full total and the tenders settle it between them. Points are a way of
        // paying, not a discount, so netting them off the charge would hide from the member's own
        // statement that they had spent a reward at all.
        var entries = new List<LedgerEntry>
        {
            LedgerEntry.Charge(memberId, order.Value.Total, order.Value.Description, null, null, now),
        };

        if (order.Value.AmountCharged.Cents > 0)
        {
            entries.Add(LedgerEntry.Payment(
                memberId, order.Value.AmountCharged,
                $"Card payment — {order.Value.Description}", null, now));
        }

        if (order.Value.PointsRedeemed > 0)
        {
            // The second tender. Without it the two sides would not meet and the member would be
            // left standing in debit by exactly the points they had just spent.
            entries.Add(LedgerEntry.Payment(
                memberId, Money.FromCents(order.Value.PointsRedeemed),
                $"Reward points — {order.Value.Description}", null, now));
        }

        await billing.Ledger.AddRangeAsync(entries, cancellationToken);

        // BR-STR-005. Zero for everyone but Max, and the aggregate already decided that.
        if (order.Value.PointsEarned > 0)
        {
            await store.Points.AddAsync(
                PointsMovement.Earned(
                    memberId, order.Value.PointsEarned,
                    order.Value.Description, order.Value.Id, now),
                cancellationToken);
        }

        // BR-STR-007. Written in the same commit as the order, never in a reaction: a redemption
        // that could be lost after the commit would give the books away for points the member
        // still holds.
        if (order.Value.PointsRedeemed > 0)
        {
            await store.Points.AddAsync(
                PointsMovement.Redeemed(
                    memberId, order.Value.PointsRedeemed,
                    order.Value.Description, order.Value.Id, now),
                cancellationToken);
        }

        await audit.Entries.AddAsync(
            AuditEntry.Record(
                "store.order_placed", now,
                actorUserId: memberId, subjectUserId: memberId,
                detail: $"{order.Value.Description} · {order.Value.Total}"),
            cancellationToken);

        // BR-STR-013 is satisfied by omission: nothing here touches a copy. A sale is a new copy,
        // and the library's shelves belong to reservations alone.
        await store.SaveChangesAsync(cancellationToken);

        return Result.Success(StorePricing.ToDto(order.Value));
    }
}
