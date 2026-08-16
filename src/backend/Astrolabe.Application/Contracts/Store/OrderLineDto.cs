namespace Astrolabe.Application.Contracts.Store;

/// <summary>One book on an order, with the discount it earned on its own.</summary>
public sealed record OrderLineDto(
    Guid BookId,
    string BookTitle,
    int Quantity,
    int UnitPriceCents,
    int DiscountPercent,
    int DiscountAmountCents,
    int LineTotalCents);
