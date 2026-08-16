using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Contracts.Reservations;
using Astrolabe.Application.Shared.Reservations;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Features.Network.Repositories;
using Astrolabe.Domain.Features.Reservations.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Reservations.Queries.GetLibraryReservations;

public sealed class GetLibraryReservationsQueryHandler(
    IReservationUnitOfWork reservations,
    INetworkUnitOfWork network,
    IIdentityUnitOfWork identity,
    ILibraryLocationProvider libraries,
    ILibraryScopeProvider scope,
    ICurrentUser currentUser,
    IDateTimeProvider clock)
    : IQueryHandler<GetLibraryReservationsQuery, PagedResult<StaffReservationDto>>
{
    public async Task<Result<PagedResult<StaffReservationDto>>> Handle(
        GetLibraryReservationsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.Role is not { } role || !role.IsStaff())
        {
            return Result.Failure<PagedResult<StaffReservationDto>>(NetworkErrors.StaffRequired);
        }

        var reach = await scope.GetCurrentScopeAsync(cancellationToken);

        // A super administrator has no assignment list to enumerate, so the whole network stands in
        // for one. An administrator with no assignments gets an empty set, and the repository turns
        // that into an empty page rather than into everything.
        var libraryIds = reach.IsUnrestricted
            ? (await network.Libraries.GetAllAsync(cancellationToken)).Select(l => l.Id).ToList()
            : reach.LibraryIds.ToList();

        var page = await reservations.Reservations.GetForLibrariesAsync(
            libraryIds, request.Status, request.Page, request.PageSize, cancellationToken);

        var bookIds = page.Items.Select(r => r.BookId).Distinct().ToList();
        var memberIds = page.Items.Select(r => r.MemberId).Distinct().ToList();

        var books = (await reservations.Books.GetByIdsAsync(bookIds, cancellationToken))
            .ToDictionary(b => b.Id);
        var members = (await identity.Users.GetByIdsAsync(memberIds, cancellationToken))
            .ToDictionary(u => u.Id, u => u.FullName);
        var locations = await libraries.GetAllAsync(cancellationToken);

        var now = clock.UtcNow;

        var items = page.Items
            .Select(r => ReservationProjection.ToStaffDto(
                r,
                books.GetValueOrDefault(r.BookId),
                // A deleted account's loans stay on the desk's list: the library still wants its
                // book back, whoever the borrower has become.
                members.GetValueOrDefault(r.MemberId) ?? "Former member",
                locations,
                now))
            .ToList();

        return Result.Success(PagedResult<StaffReservationDto>.Create(
            items, page.Page, page.PageSize, page.TotalCount));
    }
}
