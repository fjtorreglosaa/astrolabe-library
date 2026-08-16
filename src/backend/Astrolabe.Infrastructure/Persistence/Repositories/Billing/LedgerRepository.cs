using Astrolabe.Domain.Features.Billing.Entities;
using Astrolabe.Domain.Features.Billing.Repositories;
using Astrolabe.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Billing;

/// <summary>
/// Append and read only. It does not extend <c>Repository&lt;T&gt;</c> on purpose: that base offers
/// <c>Update</c> and <c>Remove</c>, and inheriting them would hand every caller the two operations a
/// ledger must not have.
/// </summary>
public sealed class LedgerRepository(AstrolabeDbContext context) : ILedgerRepository
{
    public async Task AddAsync(LedgerEntry entry, CancellationToken cancellationToken = default) =>
        await context.LedgerEntries.AddAsync(entry, cancellationToken);

    public async Task AddRangeAsync(
        IEnumerable<LedgerEntry> entries, CancellationToken cancellationToken = default) =>
        await context.LedgerEntries.AddRangeAsync(entries, cancellationToken);

    public async Task<PagedResult<LedgerEntry>> GetForMemberAsync(
        Guid memberId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (normalisedPage, normalisedSize) = PagedResult<LedgerEntry>.Normalise(page, pageSize);

        var query = context.LedgerEntries.AsNoTracking().Where(e => e.MemberId == memberId);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            // Newest first, then by identifier: without the tiebreaker two movements on the same
            // instant could swap between pages and one would never be seen.
            .OrderByDescending(e => e.OccurredAt)
            .ThenBy(e => e.Id)
            .Skip((normalisedPage - 1) * normalisedSize)
            .Take(normalisedSize)
            .ToListAsync(cancellationToken);

        return PagedResult<LedgerEntry>.Create(items, normalisedPage, normalisedSize, total);
    }

    public async Task<Money> GetBalanceAsync(
        Guid memberId, CancellationToken cancellationToken = default)
    {
        // Summed in the database, never read from a column. BR-BIL-011: a stored balance is a second
        // source of truth for what these entries already say.
        var cents = await context.LedgerEntries
            .AsNoTracking()
            .Where(e => e.MemberId == memberId)
            .SumAsync(e => e.Amount.Cents, cancellationToken);

        return Money.FromCents(cents);
    }
}
