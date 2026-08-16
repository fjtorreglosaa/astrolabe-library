using Astrolabe.Domain.Abstractions.Persistence;

namespace Astrolabe.Domain.Features.Catalog.Repositories;

/// <summary>
/// The catalog bounded context's unit of work. Exposes only this context's repositories.
/// See <c>IIdentityUnitOfWork</c> for the rationale.
/// </summary>
public interface ICatalogUnitOfWork : IUnitOfWork
{
    IBookRepository Books { get; }

    IReviewRepository Reviews { get; }
}
