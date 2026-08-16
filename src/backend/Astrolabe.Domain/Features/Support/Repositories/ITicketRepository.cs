using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Support.Entities;
using Astrolabe.Domain.Features.Support.Enums;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Support.Repositories;

public interface ITicketRepository : IRepository<Ticket>
{
    Task<Ticket?> GetWithMessagesAsync(Guid ticketId, CancellationToken cancellationToken = default);

    Task<PagedResult<Ticket>> SearchAsync(
        string? term,
        TicketStatus? status,
        Guid? memberId,
        IReadOnlyCollection<Guid>? libraryIds,
        SortDirection direction,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>The next `TCK-NNNN`. Sequential, because a member reads it aloud.</summary>
    Task<int> NextReferenceNumberAsync(CancellationToken cancellationToken = default);
}
