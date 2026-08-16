using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Billing.Entities;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Billing.Repositories;

/// <summary>
/// Persistence for <see cref="LedgerEntry"/>.
///
/// <para>
/// <b>Append and read only.</b> It deliberately does not extend <c>IRepository&lt;T&gt;</c>: that
/// contract offers <c>Update</c> and <c>Remove</c>, and a ledger must not have them. BR-BIL-012 is
/// then enforced by the shape of the interface rather than by a convention somebody has to keep.
/// </para>
/// </summary>
public interface ILedgerRepository
{
    Task AddAsync(LedgerEntry entry, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<LedgerEntry> entries, CancellationToken cancellationToken = default);

    Task<PagedResult<LedgerEntry>> GetForMemberAsync(
        Guid memberId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// The member's balance, summed in the database. BR-BIL-011: never a stored column, because a
    /// stored balance is a second source of truth for what the entries already say.
    /// </summary>
    Task<Money> GetBalanceAsync(Guid memberId, CancellationToken cancellationToken = default);
}
