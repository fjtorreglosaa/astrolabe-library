using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Store.Errors;
using Astrolabe.Domain.Features.Store.Policies;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Store.Entities;

/// <summary>
/// One book on an order, at the price it was bought for. Implements BR-STR-004 and BR-STR-009.
///
/// <para>
/// The discount is computed and rounded <b>here</b>, per line. Rounding once on the order total and
/// rounding per line disagree by a cent often enough that the receipt stops adding up — and that is
/// the kind of defect a member reports and nobody can reproduce from the report.
/// </para>
/// </summary>
public sealed class OrderLine : Entity
{
    private OrderLine()
    {
    }

    private OrderLine(
        Guid id, Guid bookId, string bookTitle, int quantity,
        Money unitPrice, int discountPercent, Money discountAmount, Money lineTotal) : base(id)
    {
        BookId = bookId;
        BookTitle = bookTitle;
        Quantity = quantity;
        UnitPrice = unitPrice;
        DiscountPercent = discountPercent;
        DiscountAmount = discountAmount;
        LineTotal = lineTotal;
    }

    public Guid OrderId { get; private set; }

    public Guid BookId { get; private set; }

    /// <summary>
    /// Copied, not referenced. An order is a receipt, and a book removed from the catalogue must not
    /// turn a line of somebody's purchase history into a blank.
    /// </summary>
    public string BookTitle { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public Money UnitPrice { get; private set; }

    public int DiscountPercent { get; private set; }

    public Money DiscountAmount { get; private set; }

    /// <summary>What the member actually pays for this line, after its own discount.</summary>
    public Money LineTotal { get; private set; }

    /// <summary>The line before any discount, which is what the subtotal is built from.</summary>
    public Money GrossTotal => UnitPrice * Quantity;

    public static Result<OrderLine> Create(
        Guid bookId, string? bookTitle, int quantity, Money unitPrice, int discountPercent)
    {
        if (quantity <= 0)
        {
            return Result.Failure<OrderLine>(StoreErrors.QuantityInvalid);
        }

        if (unitPrice.Cents <= 0)
        {
            return Result.Failure<OrderLine>(StoreErrors.PriceInvalid);
        }

        var gross = unitPrice * quantity;
        var discount = PurchaseDiscountPolicy.DiscountOn(gross, discountPercent);

        return Result.Success(new OrderLine(
            Guid.NewGuid(), bookId,
            string.IsNullOrWhiteSpace(bookTitle) ? "Unknown title" : bookTitle.Trim(),
            quantity, unitPrice, discountPercent, discount, gross - discount));
    }
}
