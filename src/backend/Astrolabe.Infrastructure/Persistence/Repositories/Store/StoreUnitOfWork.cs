using Astrolabe.Domain.Features.Catalog.Repositories;
using Astrolabe.Domain.Features.Store.Repositories;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Store;

/// <summary>
/// Composes the store repositories over one shared context, alongside the catalogue it prices from.
/// The price on the receipt has to be the price that was read, in the same transaction.
/// </summary>
public sealed class StoreUnitOfWork(
    AstrolabeDbContext context,
    IOrderRepository orders,
    IPointsRepository points,
    IBookRepository books) : UnitOfWorkBase(context), IStoreUnitOfWork
{
    public IOrderRepository Orders { get; } = orders;

    public IPointsRepository Points { get; } = points;

    public IBookRepository Books { get; } = books;
}
