using Astrolabe.Domain.Features.Store.Entities;

namespace Astrolabe.Domain.Features.Store.Repositories;

/// <summary>
/// Persistence for <see cref="PointsMovement"/>.
///
/// <para>
/// <b>Append and read only</b>, and deliberately not extending <c>IRepository&lt;T&gt;</c> — that
/// contract offers <c>Update</c> and <c>Remove</c>, which value must not have. The same reasoning as
/// the billing ledger, because points are money by another name.
/// </para>
/// </summary>
public interface IPointsRepository
{
    Task AddAsync(PointsMovement movement, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PointsMovement>> GetForMemberAsync(
        Guid memberId, int limit, CancellationToken cancellationToken = default);

    /// <summary>The balance, summed in the database. BR-STR-018: never a stored column.</summary>
    Task<int> GetBalanceAsync(Guid memberId, CancellationToken cancellationToken = default);
}
