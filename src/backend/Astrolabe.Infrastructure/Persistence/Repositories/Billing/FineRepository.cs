using Astrolabe.Domain.Features.Billing.Entities;
using Astrolabe.Domain.Features.Billing.Enums;
using Astrolabe.Domain.Features.Billing.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Billing;

public sealed class FineRepository(AstrolabeDbContext context)
    : Repository<Fine>(context), IFineRepository
{
    public async Task<Fine?> GetByReservationAsync(
        Guid reservationId, CancellationToken cancellationToken = default) =>
        await Query.FirstOrDefaultAsync(f => f.ReservationId == reservationId, cancellationToken);

    public async Task<IReadOnlyList<Fine>> GetForMemberAsync(
        Guid memberId, FineStatus? status, CancellationToken cancellationToken = default)
    {
        var query = Query.Where(f => f.MemberId == memberId);

        if (status is { } required)
        {
            query = query.Where(f => f.Status == required);
        }

        return await query.OrderByDescending(f => f.AssessedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Fine>> GetByIdsForMemberAsync(
        Guid memberId, IReadOnlyCollection<Guid> fineIds, CancellationToken cancellationToken = default)
    {
        if (fineIds.Count == 0)
        {
            return [];
        }

        // Filtered by member as well as by identifier, so a caller cannot reach somebody else's
        // fine by guessing one. BR-BIL-016 holds even if a handler forgets to check.
        return await Query
            .Where(f => f.MemberId == memberId && fineIds.Contains(f.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Fine>> GetByDeskPaymentAsync(
        Guid deskPaymentId, CancellationToken cancellationToken = default) =>
        await Query.Where(f => f.DeskPaymentId == deskPaymentId).ToListAsync(cancellationToken);
}
