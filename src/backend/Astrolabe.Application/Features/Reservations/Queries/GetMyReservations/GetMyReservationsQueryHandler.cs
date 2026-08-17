using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Contracts.Reservations;
using Astrolabe.Application.Shared.Reservations;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Reservations.Errors;
using Astrolabe.Domain.Features.Reservations.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Reservations.Queries.GetMyReservations;

public sealed class GetMyReservationsQueryHandler(
    IReservationUnitOfWork reservations,
    ILibraryLocationProvider libraries,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : IQueryHandler<GetMyReservationsQuery, PagedResult<ReservationDto>>
{
    public async Task<Result<PagedResult<ReservationDto>>> Handle(
        GetMyReservationsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure<PagedResult<ReservationDto>>(ReservationErrors.NotYours);
        }

        var page = await reservations.Reservations.GetForMemberAsync(
            memberId, request.Status, request.Term, request.Page, request.PageSize, cancellationToken);

        // The books and the geography are each fetched once for the page rather than per row: a
        // listing of twenty loans would otherwise be forty extra round trips for facts that cannot
        // change between rows.
        var bookIds = page.Items.Select(r => r.BookId).Distinct().ToList();
        var books = (await reservations.Books.GetByIdsAsync(bookIds, cancellationToken))
            .ToDictionary(b => b.Id);
        var locations = await libraries.GetAllAsync(cancellationToken);

        var now = clock.UtcNow;

        var items = page.Items
            .Select(r => ReservationProjection.ToDto(
                r, books.GetValueOrDefault(r.BookId), locations, now))
            .ToList();

        return Result.Success(PagedResult<ReservationDto>.Create(
            items, page.Page, page.PageSize, page.TotalCount));
    }
}
