using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Catalog.Repositories;

namespace Astrolabe.Domain.Features.Store.Repositories;

/// <summary>
/// The store bounded context's unit of work.
///
/// It exposes <see cref="IBookRepository"/> because an order is priced from the catalogue and must
/// read it inside the same transaction it commits in — the price on the receipt has to be the price
/// that was read.
/// </summary>
public interface IStoreUnitOfWork : IUnitOfWork
{
    IOrderRepository Orders { get; }

    IPointsRepository Points { get; }

    IBookRepository Books { get; }
}
