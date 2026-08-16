namespace Astrolabe.Application.Contracts.Reservations;

/// <summary>
/// One reservation as the loans table renders it.
///
/// <c>IsOverdue</c> and <c>DaysLate</c> are computed server-side from the due date. The client must
/// not derive them: a browser clock that is wrong, or in another zone, would show a member a
/// different lateness than the desk does.
/// </summary>
public sealed record ReservationDto(
    Guid Id,
    Guid BookId,
    string Title,
    string Author,
    string? CoverUrl,
    string LibraryName,
    string CityName,
    string Delivery,
    int DeliveryFeeCents,
    DateTimeOffset BorrowedOn,
    DateTimeOffset DueOn,
    string Status,
    bool IsOverdue,
    int DaysLate,
    int DaysRemaining,
    string? ReturnMethod,
    DateTimeOffset? HandedOverAt,
    DateTimeOffset? CheckedInAt);
