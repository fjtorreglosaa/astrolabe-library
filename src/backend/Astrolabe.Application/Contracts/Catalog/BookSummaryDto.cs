namespace Astrolabe.Application.Contracts.Catalog;

/// <summary>
/// One book as the catalogue listing renders it, already carrying the caller's access verdict.
///
/// <para>
/// The verdict is computed server-side and sent with the row rather than left to the client: the
/// same rule decides whether <c>reservations</c> accepts the loan, and two implementations of it
/// would eventually disagree in front of the member. <c>Badge</c> is null when the book is
/// reservable, and otherwise names the single reason the card shows.
/// </para>
/// </summary>
public sealed record BookSummaryDto(
    Guid Id,
    string Isbn,
    string Title,
    string Author,
    string Genre,
    string Tier,
    int RetailPriceCents,
    string? CoverUrl,
    decimal? AverageRating,
    int ReviewCount,
    int AvailableCount,
    int TotalCount,
    bool CanReserve,
    string? Badge);
