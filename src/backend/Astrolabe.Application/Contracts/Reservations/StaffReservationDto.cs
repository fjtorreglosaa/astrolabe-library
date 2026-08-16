namespace Astrolabe.Application.Contracts.Reservations;

/// <summary>
/// A reservation as the desk sees it. Carries the member, which the member's own view never needs,
/// and no handover code — staff read the code out, they do not type it.
/// </summary>
public sealed record StaffReservationDto(
    Guid Id,
    string MemberName,
    string Title,
    string Author,
    string LibraryName,
    DateTimeOffset BorrowedOn,
    DateTimeOffset DueOn,
    string Status,
    bool IsOverdue,
    int DaysLate,
    string? ReturnMethod);
