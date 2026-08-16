using System.Linq.Expressions;
using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Catalog.Repositories;
using Astrolabe.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Catalog;

public sealed class BookRepository(AstrolabeDbContext context)
    : Repository<Book>(context), IBookRepository
{
    public async Task<Book?> GetWithCopiesAsync(
        Guid bookId, CancellationToken cancellationToken = default) =>
        await Query
            .Include(book => book.Copies)
            .FirstOrDefaultAsync(book => book.Id == bookId, cancellationToken);

    public async Task<bool> ExistsWithIsbnAsync(
        string normalisedIsbn, CancellationToken cancellationToken = default) =>
        await ReadOnlyQuery.AnyAsync(book => book.Isbn.Value == normalisedIsbn, cancellationToken);

    public async Task<PagedResult<Book>> SearchAsync(
        string? term,
        Genre? genre,
        BookStatus? status,
        BookSortKey sortBy,
        SortDirection direction,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (normalisedPage, normalisedSize) = PagedResult<Book>.Normalise(page, pageSize);

        // Read-only: a listing never mutates what it lists, and tracking a page of books would keep
        // them all in the change tracker for the rest of the request.
        var query = ReadOnlyQuery.Include(book => book.Copies).AsQueryable();

        if (status is { } required)
        {
            query = query.Where(book => book.Status == required);
        }

        if (genre is { } requiredGenre)
        {
            query = query.Where(book => book.Genre == requiredGenre);
        }

        if (!string.IsNullOrWhiteSpace(term))
        {
            // BR-CAT-018: case-insensitive and indifferent to surrounding whitespace. ILike keeps
            // the comparison in Postgres rather than pulling rows back to compare them here.
            var pattern = $"%{term.Trim()}%";

            query = query.Where(book =>
                EF.Functions.ILike(book.Title, pattern)
                || EF.Functions.ILike(book.Author, pattern)
                || EF.Functions.ILike(book.Isbn.Value, pattern)
                || (book.Publisher != null && EF.Functions.ILike(book.Publisher, pattern)));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await OrderBy(query, sortBy, direction)
            // Id last, always: without the tiebreaker two books sharing a title could swap between
            // pages and one of them would never be seen.
            .ThenBy(book => book.Id)
            .Skip((normalisedPage - 1) * normalisedSize)
            .Take(normalisedSize)
            .ToListAsync(cancellationToken);

        return PagedResult<Book>.Create(items, normalisedPage, normalisedSize, total);
    }

    /// <summary>
    /// Applies the sort in the database. Availability sums the branch counts as a subquery rather
    /// than in memory, which is what lets it be sorted on at all across a paged result.
    /// </summary>
    private static IOrderedQueryable<Book> OrderBy(
        IQueryable<Book> query, BookSortKey sortBy, SortDirection direction)
    {
        var ascending = direction is SortDirection.Ascending;

        return sortBy switch
        {
            BookSortKey.Author => Apply(query, book => book.Author, ascending),
            BookSortKey.Genre => Apply(query, book => book.Genre, ascending),
            BookSortKey.Tier => Apply(query, book => book.Tier, ascending),
            BookSortKey.Availability =>
                Apply(query, book => book.Copies.Sum(copy => copy.AvailableCount), ascending),

            // An unrated book sorts as zero rather than being dropped: a listing that hides
            // unreviewed books when sorted by rating would look like missing data.
            BookSortKey.Rating => Apply(query, book => book.AverageRating ?? 0m, ascending),
            BookSortKey.Price => Apply(query, book => book.RetailPrice.Cents, ascending),
            _ => Apply(query, book => book.Title, ascending)
        };
    }

    private static IOrderedQueryable<Book> Apply<TKey>(
        IQueryable<Book> query, Expression<Func<Book, TKey>> key, bool ascending) =>
        ascending ? query.OrderBy(key) : query.OrderByDescending(key);
}
