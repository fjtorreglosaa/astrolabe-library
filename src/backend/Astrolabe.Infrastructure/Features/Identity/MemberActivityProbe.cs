using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Contracts.Identity;
using Astrolabe.Domain.Features.Billing.Enums;
using Astrolabe.Domain.Features.Reservations.Enums;
using Astrolabe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Features.Identity;

/// <summary>
/// Reads a member's activity for the staff directory's detail panel.
///
/// <para>
/// Five small aggregates rather than one join: the tables share only the member, so a join would
/// multiply rows and every count would have to be made distinct again to be true. Cheap enough —
/// this runs once, when a row is opened, never per row of a listing.
/// </para>
/// </summary>
public sealed class MemberActivityProbe(AstrolabeDbContext context) : IMemberActivityProbe
{
    public async Task<MemberActivity> GetAsync(
        Guid memberId, CancellationToken cancellationToken = default)
    {
        var lastActive = await context.UserSessions
            .AsNoTracking()
            .Where(session => session.UserId == memberId)
            .MaxAsync(session => (DateTimeOffset?)session.LastSeenAt, cancellationToken);

        var activeReservations = await context.Reservations
            .AsNoTracking()
            .CountAsync(
                r => r.MemberId == memberId
                    && (r.Status == ReservationStatus.Reserved
                        || r.Status == ReservationStatus.InTransit),
                cancellationToken);

        // Awaiting validation counts as outstanding: the member has paid at a desk but no
        // administrator has confirmed it, so the money is not settled. An administrator about to
        // block somebody needs to see it.
        var outstanding = await context.Fines
            .AsNoTracking()
            .Where(fine => fine.MemberId == memberId
                && (fine.Status == FineStatus.Outstanding
                    || fine.Status == FineStatus.AwaitingValidation))
            .SumAsync(fine => (long)fine.Amount.Cents, cancellationToken);

        var purchases = await context.Orders
            .AsNoTracking()
            .CountAsync(order => order.MemberId == memberId, cancellationToken);

        var returned = await context.Reservations
            .AsNoTracking()
            .Where(r => r.MemberId == memberId && r.Status == ReservationStatus.Returned)
            .Select(r => r.DaysLateAtCheckIn)
            .ToListAsync(cancellationToken);

        // Null, not zero, when nothing has come back yet. Zero would read as a member who has never
        // returned anything on time, which is the opposite of the truth.
        int? onTime = returned.Count == 0
            ? null
            : (int)Math.Round(returned.Count(days => days <= 0) * 100.0 / returned.Count);

        return new MemberActivity(
            lastActive, activeReservations, (int)outstanding, purchases, onTime);
    }
}
