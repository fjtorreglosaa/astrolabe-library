using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Catalog.Repositories;

/// <summary>Persistence for <see cref="Review"/>.</summary>
public interface IReviewRepository : IRepository<Review>
{
    /// <summary>A member's review of a book, or null. Backs the one-per-book rule, BR-CAT-027.</summary>
    Task<Review?> GetByMemberAndBookAsync(
        Guid memberId, Guid bookId, CancellationToken cancellationToken = default);

    Task<PagedResult<Review>> GetByBookAsync(
        Guid bookId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// The mean rating and the count for a book, computed in the database. Returning both together
    /// keeps BR-CAT-030 honest: a count of zero is what distinguishes "no rating" from "zero stars".
    /// </summary>
    Task<(decimal? Average, int Count)> GetRatingAsync(
        Guid bookId, CancellationToken cancellationToken = default);
}
