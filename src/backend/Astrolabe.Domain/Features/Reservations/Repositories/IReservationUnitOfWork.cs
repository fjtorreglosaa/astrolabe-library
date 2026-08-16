using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Catalog.Repositories;

namespace Astrolabe.Domain.Features.Reservations.Repositories;

/// <summary>
/// The reservations bounded context's unit of work.
///
/// <para>
/// It exposes <see cref="IBookRepository"/> alongside its own, which is a deliberate exception to
/// "only this context's repositories". Taking a copy off a shelf and recording who took it is one
/// atomic fact: they must share a change tracker, or a crash between the two saves leaves a
/// reservation for a copy the catalogue still believes is available — or a missing copy nobody
/// holds. The alternative, a second unit of work in the handler, gives two commits and no atomicity.
/// </para>
/// </summary>
public interface IReservationUnitOfWork : IUnitOfWork
{
    IReservationRepository Reservations { get; }

    /// <summary>The catalogue's books, because stock and reservation move together or not at all.</summary>
    IBookRepository Books { get; }
}
