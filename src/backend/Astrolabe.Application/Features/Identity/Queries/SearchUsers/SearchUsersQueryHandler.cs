using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Contracts.Identity;
using Astrolabe.Application.Shared.Identity;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Membership.Repositories;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Identity.Queries.SearchUsers;

/// <summary>
/// Lists users a staff caller is entitled to see.
///
/// <para>
/// <b>Scoped by city, not by library.</b> A member belongs to a city of residence and reaches a
/// branch through it; there is no assignment tying a member to one library, so a city is the
/// finest honest granularity. BR-NET-010 is what makes the scoping compulsory rather than a
/// nicety — an administrator holding no assignments must see no administrative data, and an
/// unscoped directory would hand them the whole network.
/// </para>
/// </summary>
public sealed class SearchUsersQueryHandler(
    IIdentityUnitOfWork identity,
    IMembershipUnitOfWork membership,
    ILibraryScopeProvider scope,
    ILibraryLocationProvider libraries,
    ICurrentUser currentUser)
    : IQueryHandler<SearchUsersQuery, PagedResult<UserSummaryDto>>
{
    public async Task<Result<PagedResult<UserSummaryDto>>> Handle(
        SearchUsersQuery request, CancellationToken cancellationToken)
    {
        if (currentUser is not { UserId: { } actorId, Role: { } actorRole })
        {
            return Result.Failure<PagedResult<UserSummaryDto>>(NetworkErrors.StaffRequired);
        }

        if (!actorRole.IsStaff())
        {
            return Result.Failure<PagedResult<UserSummaryDto>>(NetworkErrors.StaffRequired);
        }

        var reach = await scope.GetCurrentScopeAsync(cancellationToken);
        var locations = await libraries.GetAllAsync(cancellationToken);

        // Null for a super administrator and a list for everyone else, including the empty list.
        // The repository treats the two differently on purpose; collapsing them here would turn an
        // administrator with no assignments into one with unrestricted reach.
        var cityIds = reach.IsUnrestricted
            ? null
            : locations.Values
                .Where(location => reach.Covers(location.LibraryId))
                .Select(location => location.CityId)
                .Distinct()
                .ToList();

        var homeLibraries = await libraries.GetHomeLibraryByCityAsync(cancellationToken);

        var page = await identity.Users.SearchAsync(
            request.Term, request.Status, request.Role, cityIds, request.IncludeDeleted,
            request.SortBy, request.Direction, request.Page, request.PageSize, cancellationToken);

        // One query for the whole page rather than one per row.
        var plans = await membership.Subscriptions.GetActivePlansForAsync(
            [.. page.Items.Select(user => user.Id)], cancellationToken);

        var items = page.Items
            .Select(user => UserProjection.ToSummary(
                user, actorId, actorRole, plans, locations, homeLibraries))
            .ToList();

        return Result.Success(PagedResult<UserSummaryDto>.Create(
            items, page.Page, page.PageSize, page.TotalCount));
    }
}
