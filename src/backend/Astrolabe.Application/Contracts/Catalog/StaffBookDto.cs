namespace Astrolabe.Application.Contracts.Catalog;

/// <summary>
/// A book as the staff management table renders it. Carries the lifecycle state, which members never
/// see, and carries no access verdict, because staff act on books rather than borrow them.
/// </summary>
public sealed record StaffBookDto(
    Guid Id,
    string Isbn,
    string Title,
    string Author,
    string Genre,
    string Tier,
    string Status,
    int RetailPriceCents,
    int AvailableCount,
    int TotalCount,
    DateTimeOffset CreatedAt);
