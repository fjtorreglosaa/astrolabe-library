using Astrolabe.Domain.Features.Store.Entities;
using Astrolabe.Domain.Features.Store.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Store;

/// <summary>
/// Append and read only. It does not extend <c>Repository&lt;T&gt;</c> on purpose: that base offers
/// <c>Update</c> and <c>Remove</c>, and inheriting them would hand every caller two operations that
/// value must not have.
/// </summary>
public sealed class PointsRepository(AstrolabeDbContext context) : IPointsRepository
{
    public async Task AddAsync(PointsMovement movement, CancellationToken cancellationToken = default) =>
        await context.PointsMovements.AddAsync(movement, cancellationToken);

    public async Task<IReadOnlyList<PointsMovement>> GetForMemberAsync(
        Guid memberId, int limit, CancellationToken cancellationToken = default) =>
        await context.PointsMovements
            .AsNoTracking()
            .Where(movement => movement.MemberId == memberId)
            .OrderByDescending(movement => movement.OccurredAt)
            .ThenBy(movement => movement.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<int> GetBalanceAsync(
        Guid memberId, CancellationToken cancellationToken = default) =>
        // Summed in the database. BR-STR-018: a stored balance would be a second source of truth
        // for what these movements already say.
        await context.PointsMovements
            .AsNoTracking()
            .Where(movement => movement.MemberId == memberId)
            .SumAsync(movement => movement.PointCents, cancellationToken);
}
