using Astrolabe.Domain.Features.Reservations.Entities;
using Astrolabe.Domain.Features.Reservations.Enums;
using Astrolabe.Domain.Features.Reservations.Repositories;
using Astrolabe.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Reservations;

public sealed class ReservationRepository(AstrolabeDbContext context)
    : Repository<Reservation>(context), IReservationRepository
{
    private static readonly ReservationStatus[] ActiveStatuses =
        [ReservationStatus.Reserved, ReservationStatus.InTransit];

    public async Task<PagedResult<Reservation>> GetForMemberAsync(
        Guid memberId, ReservationStatus? status, int page, int pageSize,
        CancellationToken cancellationToken = default) =>
        await PageAsync(
            ReadOnlyQuery.Where(r => r.MemberId == memberId), status, page, pageSize, cancellationToken);

    public async Task<bool> HasActiveForCopyAsync(
        Guid memberId, Guid bookCopyId, CancellationToken cancellationToken = default) =>
        await ReadOnlyQuery.AnyAsync(
            r => r.MemberId == memberId
                 && r.BookCopyId == bookCopyId
                 && ActiveStatuses.Contains(r.Status),
            cancellationToken);

    public async Task<Reservation?> GetByIdempotencyKeyAsync(
        Guid memberId, string idempotencyKey, CancellationToken cancellationToken = default) =>
        await Query.FirstOrDefaultAsync(
            r => r.MemberId == memberId && r.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<IReadOnlyList<Reservation>> GetActiveForMemberAsync(
        Guid memberId, CancellationToken cancellationToken = default) =>
        await ReadOnlyQuery
            .Where(r => r.MemberId == memberId && ActiveStatuses.Contains(r.Status))
            .OrderBy(r => r.Period.DueOn)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Reservation>> GetLateReturnsAsync(
        int maxCount, CancellationToken cancellationToken = default) =>
        await ReadOnlyQuery
            .Where(r => r.Status == ReservationStatus.Returned && r.DaysLateAtCheckIn > 0)
            // Oldest first: a fine that has been missed longest is the one most worth catching up.
            .OrderBy(r => r.CheckedInAt)
            .Take(maxCount)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<Reservation>> GetForLibrariesAsync(
        IReadOnlyCollection<Guid> libraryIds, ReservationStatus? status, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        // An administrator with no assignments sees an empty list rather than everything. The same
        // rule as BR-NET-010, and the failure mode of getting it wrong is the whole network.
        if (libraryIds.Count == 0)
        {
            var (emptyPage, emptySize) = PagedResult<Reservation>.Normalise(page, pageSize);
            return PagedResult<Reservation>.Empty(emptyPage, emptySize);
        }

        return await PageAsync(
            ReadOnlyQuery.Where(r => libraryIds.Contains(r.LibraryId)),
            status, page, pageSize, cancellationToken);
    }

    /// <summary>
    /// Shared paging. Ordered by due date, then by identifier: without the tiebreaker two loans
    /// falling due the same day could swap between pages and one would never be seen.
    /// </summary>
    private static async Task<PagedResult<Reservation>> PageAsync(
        IQueryable<Reservation> query, ReservationStatus? status, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        var (normalisedPage, normalisedSize) = PagedResult<Reservation>.Normalise(page, pageSize);

        if (status is { } required)
        {
            query = query.Where(r => r.Status == required);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(r => r.Period.DueOn)
            .ThenBy(r => r.Id)
            .Skip((normalisedPage - 1) * normalisedSize)
            .Take(normalisedSize)
            .ToListAsync(cancellationToken);

        return PagedResult<Reservation>.Create(items, normalisedPage, normalisedSize, total);
    }
}
