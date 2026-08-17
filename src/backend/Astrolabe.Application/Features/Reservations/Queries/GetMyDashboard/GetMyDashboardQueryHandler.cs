using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Contracts.Reservations;
using Astrolabe.Application.Shared.Reservations;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Reservations.Enums;
using Astrolabe.Domain.Features.Reservations.Errors;
using Astrolabe.Domain.Features.Reservations.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Reservations.Queries.GetMyDashboard;

public sealed class GetMyDashboardQueryHandler(
    IReservationUnitOfWork reservations,
    ILibraryLocationProvider libraries,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : IQueryHandler<GetMyDashboardQuery, MemberDashboardDto>
{
    /// <summary>How many of the soonest-due loans the dashboard card shows, as the prototype does.</summary>
    private const int DashboardRows = 4;

    public async Task<Result<MemberDashboardDto>> Handle(
        GetMyDashboardQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure<MemberDashboardDto>(ReservationErrors.NotYours);
        }

        var now = clock.UtcNow;

        var active = await reservations.Reservations.GetActiveForMemberAsync(memberId, cancellationToken);

        // Returned loans are read whole rather than paged: they are what the topic breakdown is
        // built from, and a member's lifetime borrowing is a bounded set in this product.
        var returned = await reservations.Reservations.GetForMemberAsync(
            memberId, ReservationStatus.Returned, term: null, page: 1,
            pageSize: PagedResult<int>.MaxPageSize,
            cancellationToken);

        var bookIds = active.Select(r => r.BookId)
            .Concat(returned.Items.Select(r => r.BookId))
            .Distinct()
            .ToList();

        var books = (await reservations.Books.GetByIdsAsync(bookIds, cancellationToken))
            .ToDictionary(b => b.Id);
        var locations = await libraries.GetAllAsync(cancellationToken);

        var soonest = active
            .Take(DashboardRows)
            .Select(r => ReservationProjection.ToDto(r, books.GetValueOrDefault(r.BookId), locations, now))
            .ToList();

        // Derived from the member's own returned loans rather than from a stored profile, so the
        // topics cannot drift from what they actually read.
        var genres = returned.Items
            .Select(r => books.GetValueOrDefault(r.BookId))
            .Where(b => b is not null)
            .GroupBy(b => b!.Genre)
            .Select(group => new { Genre = group.Key.ToString(), Count = group.Count() })
            .OrderByDescending(entry => entry.Count)
            .ThenBy(entry => entry.Genre)
            .ToList();

        var totalReturned = genres.Sum(entry => entry.Count);

        var topics = genres
            .Select(entry => new TopicInterestDto(
                entry.Genre,
                entry.Count,
                totalReturned == 0 ? 0 : (int)Math.Round(entry.Count * 100d / totalReturned)))
            .ToList();

        return Result.Success(new MemberDashboardDto(
            ActiveReservations: active.Count,
            DueThisWeek: active.Count(r => !r.IsOverdueAt(now) && r.Period.DaysRemainingAt(now) <= 7),
            Overdue: active.Count(r => r.IsOverdueAt(now)),
            ReturnedAllTime: returned.TotalCount,
            ReadThisYear: returned.Items.Count(r => r.CheckedInAt?.Year == now.Year),
            ActiveSoonest: soonest,
            Topics: topics));
    }
}
