using Astrolabe.Domain.Features.Store.Entities;
using Astrolabe.Domain.Features.Store.Repositories;
using Astrolabe.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Store;

public sealed class OrderRepository(AstrolabeDbContext context)
    : Repository<Order>(context), IOrderRepository
{
    public async Task<PagedResult<Order>> GetForMemberAsync(
        Guid memberId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (normalisedPage, normalisedSize) = PagedResult<Order>.Normalise(page, pageSize);

        var query = ReadOnlyQuery.Where(order => order.MemberId == memberId);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(order => order.Lines)
            // Newest first, then by identifier: two orders placed in the same instant must not swap
            // between pages and hide one of themselves.
            .OrderByDescending(order => order.PlacedAt)
            .ThenBy(order => order.Id)
            .Skip((normalisedPage - 1) * normalisedSize)
            .Take(normalisedSize)
            .ToListAsync(cancellationToken);

        return PagedResult<Order>.Create(items, normalisedPage, normalisedSize, total);
    }

    public async Task<Order?> GetByIdempotencyKeyAsync(
        Guid memberId, string idempotencyKey, CancellationToken cancellationToken = default) =>
        await Query
            .Include(order => order.Lines)
            .FirstOrDefaultAsync(
                order => order.MemberId == memberId && order.IdempotencyKey == idempotencyKey,
                cancellationToken);
}
