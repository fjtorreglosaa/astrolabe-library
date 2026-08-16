namespace Astrolabe.Application.Contracts.Identity;

/// <summary>
/// What a member has been doing, for the directory's detail panel. The four statistics the
/// prototype shows: active reservations, outstanding fines, purchases, on-time returns.
/// </summary>
/// <param name="OnTimeReturnPercent">
/// Null when nothing has been returned yet. The prototype renders that as an em dash rather than
/// 0%, which would read as a terrible record instead of no record at all.
/// </param>
public sealed record MemberActivity(
    DateTimeOffset? LastActiveAt,
    int ActiveReservations,
    int OutstandingFineCents,
    int Purchases,
    int? OnTimeReturnPercent)
{
    public static readonly MemberActivity None = new(null, 0, 0, 0, null);
}
