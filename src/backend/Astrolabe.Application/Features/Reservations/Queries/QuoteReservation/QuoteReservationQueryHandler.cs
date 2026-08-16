using Astrolabe.Application.Abstractions.Membership;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Contracts.Reservations;
using Astrolabe.Application.Shared.Catalog;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Catalog.Errors;
using Astrolabe.Domain.Features.Catalog.Policies;
using Astrolabe.Domain.Features.Catalog.ValueObjects;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Reservations.Entities;
using Astrolabe.Domain.Features.Reservations.Repositories;
using Astrolabe.Domain.Features.Reservations.ValueObjects;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Reservations.Queries.QuoteReservation;

public sealed class QuoteReservationQueryHandler(
    IReservationUnitOfWork reservations,
    IEntitlementProvider entitlements,
    ILibraryLocationProvider libraries,
    IDateTimeProvider clock) : IQueryHandler<QuoteReservationQuery, ReservationQuoteDto>
{
    public async Task<Result<ReservationQuoteDto>> Handle(
        QuoteReservationQuery request, CancellationToken cancellationToken)
    {
        var book = await reservations.Books.GetWithCopiesAsync(request.BookId, cancellationToken);

        if (book is null || !book.IsVisibleToMembers)
        {
            return Result.Failure<ReservationQuoteDto>(CatalogErrors.BookNotFound);
        }

        var member = await entitlements.GetForCurrentMemberAsync(cancellationToken);
        var locations = await libraries.GetAllAsync(cancellationToken);

        var copies = book.Copies.Select(copy =>
        {
            var location = locations.GetValueOrDefault(copy.LibraryId);

            var verdict = CatalogAccessPolicy.EvaluateCopy(
                member, book.Tier,
                new CopyLocation(copy.LibraryId, location?.CityId ?? Guid.Empty, copy.AvailableCount));

            return new ReservableCopyDto(
                copy.LibraryId,
                location?.LibraryName ?? "Unknown library",
                location?.CityName ?? string.Empty,
                copy.AvailableCount,
                verdict.CanReserve,
                verdict.Reason?.ToString());
        }).ToList();

        var fee = Reservation.FeeFor(request.Delivery);

        // Quoted from the same clock the confirmation will use, so the date on the modal is the date
        // the member gets.
        var dueOn = LoanPeriod.StartingAt(clock.UtcNow).DueOn;

        return Result.Success(new ReservationQuoteDto(
            book.Id, book.Title, book.Author, book.CoverUrl,
            book.Tier.ToString(), book.Genre.ToString(),
            PlanNoteFor(member.Plan, member.Reach, locations, member),
            (int)fee.Cents,
            (int)fee.Cents,
            dueOn,
            copies));
    }

    /// <summary>
    /// The sentence the prototype puts under the title, explaining the member's reach in their own
    /// terms rather than making them infer it from the disabled rows.
    /// </summary>
    private static string PlanNoteFor(
        PlanTier plan,
        ReachKind reach,
        IReadOnlyDictionary<Guid, BookProjection.LibraryLocation> locations,
        Domain.Features.Membership.ValueObjects.MemberEntitlement member) => reach switch
    {
        ReachKind.HomeLibraryOnly =>
            $"{plan} plan: Basic-catalog titles, "
            + $"{(member.HomeLibraryId is { } id ? locations.GetValueOrDefault(id)?.LibraryName : null) ?? "home library"} only.",

        ReachKind.City =>
            $"{plan} plan: any title at "
            + $"{(member.CityId is { } city ? locations.Values.FirstOrDefault(l => l.CityId == city)?.CityName : null) ?? "your city"} libraries.",

        _ => $"{plan} plan: any title at any library on the platform."
    };
}
