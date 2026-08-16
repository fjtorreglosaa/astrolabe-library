using Astrolabe.Domain.Features.Catalog.Repositories;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Catalog;

/// <summary>
/// Composes the catalog repositories over one shared context, so their staged work commits together.
/// </summary>
public sealed class CatalogUnitOfWork(
    AstrolabeDbContext context,
    IBookRepository books,
    IReviewRepository reviews) : UnitOfWorkBase(context), ICatalogUnitOfWork
{
    public IBookRepository Books { get; } = books;

    public IReviewRepository Reviews { get; } = reviews;
}
