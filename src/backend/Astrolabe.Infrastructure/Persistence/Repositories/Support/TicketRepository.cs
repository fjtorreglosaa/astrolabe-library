using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Support.Entities;
using Astrolabe.Domain.Features.Support.Enums;
using Astrolabe.Domain.Features.Support.Repositories;
using Astrolabe.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Support;

public sealed class TicketRepository(AstrolabeDbContext context)
    : Repository<Ticket>(context), ITicketRepository
{
    public async Task<Ticket?> GetWithMessagesAsync(
        Guid ticketId, CancellationToken cancellationToken = default) =>
        await Query.Include(t => t.Messages).FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);

    public async Task<PagedResult<Ticket>> SearchAsync(
        string? term,
        TicketStatus? status,
        Guid? memberId,
        IReadOnlyCollection<Guid>? libraryIds,
        SortDirection direction,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (normalisedPage, normalisedSize) = PagedResult<Ticket>.Normalise(page, pageSize);

        var query = ReadOnlyQuery.AsQueryable();

        if (memberId is { } member)
        {
            query = query.Where(t => t.MemberId == member);
        }

        // Null is unrestricted; an empty list is a staff user with no assignments and must return
        // nothing. Collapsing the two would hand them every ticket in the network.
        if (libraryIds is not null)
        {
            query = libraryIds.Count == 0
                ? query.Where(_ => false)
                : query.Where(t => libraryIds.Contains(t.LibraryId));
        }

        if (status is { } required)
        {
            query = query.Where(t => t.Status == required);
        }

        if (!string.IsNullOrWhiteSpace(term))
        {
            var pattern = $"%{term.Trim()}%";

            query = query.Where(t =>
                EF.Functions.ILike(t.Reference, pattern)
                || EF.Functions.ILike(t.Subject, pattern));
        }

        var total = await query.CountAsync(cancellationToken);

        var ordered = direction is SortDirection.Ascending
            ? query.OrderBy(t => t.UpdatedAt)
            : query.OrderByDescending(t => t.UpdatedAt);

        var items = await ordered
            // Id last, so two tickets updated in the same instant cannot swap between pages.
            .ThenBy(t => t.Id)
            .Skip((normalisedPage - 1) * normalisedSize)
            .Take(normalisedSize)
            .ToListAsync(cancellationToken);

        return PagedResult<Ticket>.Create(items, normalisedPage, normalisedSize, total);
    }

    public async Task<int> NextReferenceNumberAsync(CancellationToken cancellationToken = default)
    {
        // Counted rather than sequenced. The prototype starts at TCK-2038, and a gap-free run is not
        // a property anything depends on — the reference identifies, it does not audit.
        var count = await ReadOnlyQuery.CountAsync(cancellationToken);

        return 2038 + count;
    }
}
