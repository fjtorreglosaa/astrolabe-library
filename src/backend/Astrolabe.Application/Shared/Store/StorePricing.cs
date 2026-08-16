using Astrolabe.Application.Contracts.Store;
using Astrolabe.Application.Shared.Catalog;
using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Features.Catalog.ValueObjects;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Membership.ValueObjects;
using Astrolabe.Domain.Features.Store.Entities;
using Astrolabe.Domain.Features.Store.Policies;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Shared.Store;

/// <summary>
/// Prices an order the same way whether it is being quoted or placed.
///
/// The modal and the purchase must agree to the cent. Two implementations of the same arithmetic —
/// one to show a total and one to charge it — is how a member ends up billed something other than
/// what they agreed to.
/// </summary>
public static class StorePricing
{
    /// <summary>Builds the priced lines for a set of books, each with its own discount.</summary>
    public static Result<List<OrderLine>> BuildLines(
        IReadOnlyDictionary<Guid, Book> books,
        IReadOnlyList<(Guid BookId, int Quantity)> requested,
        MemberEntitlement member,
        IReadOnlyDictionary<Guid, BookProjection.LibraryLocation> libraries)
    {
        var lines = new List<OrderLine>(requested.Count);

        foreach (var (bookId, quantity) in requested)
        {
            if (!books.TryGetValue(bookId, out var book) || !book.IsVisibleToMembers)
            {
                // BR-STR-017. A draft or removed book is not for sale, and saying so is better than
                // silently dropping the line and charging for less than the member chose.
                return Result.Failure<List<OrderLine>>(
                    Domain.Features.Store.Errors.StoreErrors.BookNotForSale);
            }

            var percent = PurchaseDiscountPolicy.PercentFor(member, LocationsOf(book, libraries));

            var line = OrderLine.Create(book.Id, book.Title, quantity, book.RetailPrice, percent);

            if (line.IsFailure)
            {
                return Result.Failure<List<OrderLine>>(line.Error);
            }

            lines.Add(line.Value);
        }

        return Result.Success(lines);
    }

    public static OrderLineDto ToDto(OrderLine line) =>
        new(line.BookId, line.BookTitle, line.Quantity,
            (int)line.UnitPrice.Cents, line.DiscountPercent,
            (int)line.DiscountAmount.Cents, (int)line.LineTotal.Cents);

    public static OrderDto ToDto(Order order) =>
        new(order.Id,
            order.Fulfilment.ToString(),
            (int)order.Subtotal.Cents,
            (int)order.DiscountTotal.Cents,
            (int)order.ShippingFee.Cents,
            (int)order.Total.Cents,
            order.PointsRedeemed,
            (int)order.AmountCharged.Cents,
            order.PointsEarned,
            order.PlacedAt,
            order.Description,
            order.Lines.Select(ToDto).ToList());

    /// <summary>
    /// Why the discount is what it is. A Plus member shown 0% on a book held only in another city is
    /// entitled to know that is the rule rather than a fault.
    /// </summary>
    public static string DiscountNote(MemberEntitlement member, IReadOnlyList<OrderLine> lines)
    {
        var earned = lines.Any(line => line.DiscountPercent > 0);

        return member.Plan switch
        {
            PlanTier.Max => "Max plan: 15% off every book on the platform.",

            PlanTier.Plus when earned => "Plus plan: 10% off books held by a library in your city.",
            PlanTier.Plus => "Plus plan: 10% applies to books held in your city. These are held elsewhere.",

            _ => "Basic plan: no purchase discount. Plus and Max members save on every book."
        };
    }

    /// <summary>
    /// Why the redemption control looks the way it does.
    ///
    /// A member holding points who is offered none on this order would read it as a fault. Saying
    /// which of the two rules is biting — the half-the-purchase cap or the $1.00 floor — is the
    /// difference between an explanation and a dead control.
    /// </summary>
    public static string RedemptionNote(PlanTier plan, int balancePointCents, int maxRedeemable)
    {
        if (balancePointCents <= 0)
        {
            return "You have no reward points yet. Max members earn one for every $1.50 on books.";
        }

        // BR-STR-008. The balance is not lost, and a member looking at points they cannot touch is
        // owed that sentence more than anyone.
        if (!RewardRedemptionPolicy.CanRedeemOn(plan))
        {
            return $"Your {balancePointCents} points are safe. Spending them needs the Max plan.";
        }

        if (maxRedeemable == 0 && balancePointCents < RewardRedemptionPolicy.MinimumRedemptionPointCents)
        {
            return $"You need {RewardRedemptionPolicy.MinimumRedemptionPointCents} points "
                + "before you can spend them. Yours keep until then.";
        }

        if (maxRedeemable == 0)
        {
            return "This purchase is too small to spend points on. Points cover at most half of it.";
        }

        return maxRedeemable < balancePointCents
            ? $"Points can cover half of this purchase, so up to {maxRedeemable} of yours."
            : $"You can put all {maxRedeemable} of your points toward this purchase.";
    }

    private static List<CopyLocation> LocationsOf(
        Book book, IReadOnlyDictionary<Guid, BookProjection.LibraryLocation> libraries) =>
        book.Copies
            .Select(copy => new CopyLocation(
                copy.LibraryId,
                libraries.GetValueOrDefault(copy.LibraryId)?.CityId ?? Guid.Empty,
                copy.AvailableCount))
            .ToList();
}
