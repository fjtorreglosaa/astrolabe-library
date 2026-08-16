using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Store.Entities;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Store.Repositories;

/// <summary>Persistence for <see cref="Order"/>.</summary>
public interface IOrderRepository : IRepository<Order>
{
    /// <summary>A member's orders with their lines, newest first.</summary>
    Task<PagedResult<Order>> GetForMemberAsync(
        Guid memberId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Backs BR-STR-015: a replayed purchase finds its own first attempt.</summary>
    Task<Order?> GetByIdempotencyKeyAsync(
        Guid memberId, string idempotencyKey, CancellationToken cancellationToken = default);
}
