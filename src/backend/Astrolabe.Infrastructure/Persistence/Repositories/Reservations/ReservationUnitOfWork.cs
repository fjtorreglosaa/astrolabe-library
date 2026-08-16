using Astrolabe.Domain.Features.Catalog.Repositories;
using Astrolabe.Domain.Features.Reservations.Repositories;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Reservations;

/// <summary>
/// Composes the reservation and book repositories over one shared context.
///
/// The book repository is here on purpose: taking a copy off a shelf and recording who took it is
/// one atomic fact, and a single <c>SaveChangesAsync</c> is what makes it one.
/// </summary>
public sealed class ReservationUnitOfWork(
    AstrolabeDbContext context,
    IReservationRepository reservations,
    IBookRepository books) : UnitOfWorkBase(context), IReservationUnitOfWork
{
    public IReservationRepository Reservations { get; } = reservations;

    public IBookRepository Books { get; } = books;
}
