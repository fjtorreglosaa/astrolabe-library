namespace Astrolabe.Application.Contracts.Catalog;

/// <summary>
/// A book as its detail panel renders it. Implements BR-CAT-016: a book a member cannot reserve
/// still opens, so this is returned whatever the verdict.
/// </summary>
public sealed record BookDetailDto(
    Guid Id,
    string Isbn,
    string Title,
    string Author,
    string? Publisher,
    string Genre,
    string Tier,
    int RetailPriceCents,
    string? CoverUrl,
    decimal? AverageRating,
    int ReviewCount,
    bool CanReserve,
    string? Badge,
    IReadOnlyList<CopyAvailabilityDto> Copies);
