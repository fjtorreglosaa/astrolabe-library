using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Contracts.Support;
using Astrolabe.Application.Shared.Support;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Support.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Support.Queries.SearchTickets;

/// <summary>
/// One query, two audiences. A member gets their own tickets; staff get the tickets of the libraries
/// they administer. The difference is a filter this handler chooses, never a parameter a caller
/// supplies — which is what makes BR-SUP-004 a property of the shape.
/// </summary>
public sealed class SearchTicketsQueryHandler(
    ISupportUnitOfWork support,
    IUserRepository users,
    ILibraryScopeProvider scope,
    ILibraryLocationProvider libraries,
    ICurrentUser currentUser)
    : IQueryHandler<SearchTicketsQuery, PagedResult<TicketSummaryDto>>
{
    public async Task<Result<PagedResult<TicketSummaryDto>>> Handle(
        SearchTicketsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser is not { UserId: { } callerId, Role: { } role })
        {
            return Result.Failure<PagedResult<TicketSummaryDto>>(IdentityErrors.InvalidCredentials);
        }

        Guid? memberId = null;
        IReadOnlyCollection<Guid>? libraryIds = null;

        if (role.IsStaff())
        {
            var reach = await scope.GetCurrentScopeAsync(cancellationToken);

            // Null is unrestricted and an empty list is an administrator with no assignments. The
            // two must not be conflated — BR-NET-010 again, and the same trap as the user directory.
            libraryIds = reach.IsUnrestricted ? null : [.. reach.LibraryIds];
        }
        else
        {
            memberId = callerId;
        }

        var page = await support.Tickets.SearchAsync(
            request.Term, request.Status, memberId, libraryIds,
            SortDirection.Descending, request.Page, request.PageSize, cancellationToken);

        var locations = await libraries.GetAllAsync(cancellationToken);

        // One lookup for the page rather than one per row.
        var memberIds = page.Items.Select(ticket => ticket.MemberId).Distinct().ToList();
        var names = new Dictionary<Guid, string>();

        foreach (var id in memberIds)
        {
            var user = await users.GetByIdAsync(id, cancellationToken);
            names[id] = user?.FullName ?? "Unknown";
        }

        return Result.Success(PagedResult<TicketSummaryDto>.Create(
            [.. page.Items.Select(ticket => TicketProjection.ToSummary(
                ticket,
                locations.GetValueOrDefault(ticket.LibraryId)?.LibraryName ?? "Unknown library",
                names.GetValueOrDefault(ticket.MemberId, "Unknown")))],
            page.Page, page.PageSize, page.TotalCount));
    }
}
