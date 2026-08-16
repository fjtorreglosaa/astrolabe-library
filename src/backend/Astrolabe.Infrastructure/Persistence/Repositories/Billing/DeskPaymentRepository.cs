using Astrolabe.Domain.Features.Billing.Entities;
using Astrolabe.Domain.Features.Billing.Enums;
using Astrolabe.Domain.Features.Billing.Repositories;
using Astrolabe.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Billing;

public sealed class DeskPaymentRepository(AstrolabeDbContext context)
    : Repository<DeskPayment>(context), IDeskPaymentRepository
{
    public async Task<DeskPayment?> GetByCodeAsync(
        string code, CancellationToken cancellationToken = default) =>
        await Query.FirstOrDefaultAsync(d => d.Code.Value == code, cancellationToken);

    public async Task<PagedResult<DeskPayment>> GetForLibrariesAsync(
        IReadOnlyCollection<Guid> libraryIds, DeskPaymentStatus? status,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (normalisedPage, normalisedSize) = PagedResult<DeskPayment>.Normalise(page, pageSize);

        // An administrator with no assignments sees an empty queue rather than the network's.
        if (libraryIds.Count == 0)
        {
            return PagedResult<DeskPayment>.Empty(normalisedPage, normalisedSize);
        }

        var query = ReadOnlyQuery.Where(d => libraryIds.Contains(d.LibraryId));

        if (status is { } required)
        {
            query = query.Where(d => d.Status == required);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(d => d.IssuedAt)
            .ThenBy(d => d.Id)
            .Skip((normalisedPage - 1) * normalisedSize)
            .Take(normalisedSize)
            .ToListAsync(cancellationToken);

        return PagedResult<DeskPayment>.Create(items, normalisedPage, normalisedSize, total);
    }

    public async Task<IReadOnlyList<DeskPayment>> GetForMemberAsync(
        Guid memberId, CancellationToken cancellationToken = default) =>
        await Query
            .Where(d => d.MemberId == memberId)
            .OrderByDescending(d => d.IssuedAt)
            .ToListAsync(cancellationToken);
}
