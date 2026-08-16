using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Features.Catalog.Repositories;
using Astrolabe.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Catalog;

public sealed class ReviewRepository(AstrolabeDbContext context)
    : Repository<Review>(context), IReviewRepository
{
    public async Task<Review?> GetByMemberAndBookAsync(
        Guid memberId, Guid bookId, CancellationToken cancellationToken = default) =>
        await Query.FirstOrDefaultAsync(
            review => review.MemberId == memberId && review.BookId == bookId, cancellationToken);

    public async Task<PagedResult<Review>> GetByBookAsync(
        Guid bookId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (normalisedPage, normalisedSize) = PagedResult<Review>.Normalise(page, pageSize);

        var query = ReadOnlyQuery.Where(review => review.BookId == bookId);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(review => review.CreatedAt)
            .ThenBy(review => review.Id)
            .Skip((normalisedPage - 1) * normalisedSize)
            .Take(normalisedSize)
            .ToListAsync(cancellationToken);

        return PagedResult<Review>.Create(items, normalisedPage, normalisedSize, total);
    }

    public async Task<(decimal? Average, int Count)> GetRatingAsync(
        Guid bookId, CancellationToken cancellationToken = default)
    {
        // Computed in the database rather than by loading the reviews: a popular book would
        // otherwise cost more to rate the more people rated it.
        var rows = await ReadOnlyQuery
            .Where(review => review.BookId == bookId)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count = group.Count(),
                Average = group.Average(review => (decimal)review.Rating.Stars)
            })
            .FirstOrDefaultAsync(cancellationToken);

        // No rows at all means no reviews, which BR-CAT-030 distinguishes from a rating of zero.
        return rows is null ? (null, 0) : (Math.Round(rows.Average, 2), rows.Count);
    }
}
