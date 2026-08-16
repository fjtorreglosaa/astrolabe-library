namespace Astrolabe.Application.Contracts.Reservations;

/// <summary>The home screen: the stat cards and the reservations that need attention.</summary>
public sealed record MemberDashboardDto(
    int ActiveReservations,
    int DueThisWeek,
    int Overdue,
    int ReturnedAllTime,
    int ReadThisYear,
    IReadOnlyList<ReservationDto> ActiveSoonest,
    IReadOnlyList<TopicInterestDto> Topics);

/// <summary>
/// A genre the member borrows, with how often. Derived from their own returned loans rather than
/// from a stored profile, so it cannot drift from what they actually read.
/// </summary>
public sealed record TopicInterestDto(string Genre, int Count, int Percent);
