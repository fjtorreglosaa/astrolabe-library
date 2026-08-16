namespace Astrolabe.Application.Contracts.Reservations;

/// <summary>
/// What a reservation would cost and when it would be due, before anything is committed.
///
/// Every branch holding the book travels with it, each carrying its own access verdict, because the
/// modal asks the member to pick one and must show why the others are closed.
/// </summary>
public sealed record ReservationQuoteDto(
    Guid BookId,
    string Title,
    string Author,
    string? CoverUrl,
    string Tier,
    string Genre,
    string PlanNote,
    int DeliveryFeeCents,
    int TotalCents,
    DateTimeOffset DueOn,
    IReadOnlyList<ReservableCopyDto> Copies);

/// <summary>One branch's holding, with the caller's verdict for it.</summary>
public sealed record ReservableCopyDto(
    Guid LibraryId,
    string LibraryName,
    string CityName,
    int AvailableCount,
    bool CanReserve,
    string? Reason);
