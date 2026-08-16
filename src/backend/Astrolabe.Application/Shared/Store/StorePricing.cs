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

    private static List<CopyLocation> LocationsOf(
        Book book, IReadOnlyDictionary<Guid, BookProjection.LibraryLocation> libraries) =>
        book.Copies
            .Select(copy => new CopyLocation(
                copy.LibraryId,
                libraries.GetValueOrDefault(copy.LibraryId)?.CityId ?? Guid.Empty,
                copy.AvailableCount))
            .ToList();
}
