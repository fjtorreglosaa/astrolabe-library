using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Catalog.Repositories;

/// <summary>Persistence for <see cref="Book"/>.</summary>
public interface IBookRepository : IRepository<Book>
{
    /// <summary>
    /// The stored cover for a book, or null. Kept off <c>Book</c> on purpose — see
    /// <see cref="Entities.BookCoverImage"/> — so a listing never drags image bytes with it.
    /// </summary>
    Task<BookCoverImage?> GetCoverAsync(Guid bookId, CancellationToken cancellationToken = default);

    Task AddCoverAsync(BookCoverImage cover, CancellationToken cancellationToken = default);

    void RemoveCover(BookCoverImage cover);

    /// <summary>The book with its copies loaded, for any operation that touches stock or access.</summary>
    Task<Book?> GetWithCopiesAsync(Guid bookId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Several books with their copies, in one query.
    ///
    /// Exists because pricing a purchase needs to know where each book is held, and the generic
    /// <c>GetByIdsAsync</c> does not load the copies. Using that one instead makes every book look
    /// as though no library holds it, which silently turns every plan discount into zero.
    /// </summary>
    Task<IReadOnlyList<Book>> GetByIdsWithCopiesAsync(
        IReadOnlyCollection<Guid> bookIds, CancellationToken cancellationToken = default);

    /// <summary>Backs BR-CAT-003. The unique index is the real guard; this gives a clean error first.</summary>
    Task<bool> ExistsWithIsbnAsync(string normalisedIsbn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Books matching a search, with their copies. Implements BR-CAT-017 to BR-CAT-020.
    ///
    /// <para>
    /// <paramref name="status"/> is required rather than optional so that no caller can produce a
    /// member-facing listing that includes drafts by forgetting to filter. BR-CAT-020 is enforced by
    /// the signature, not by remembering.
    /// </para>
    /// <para>
    /// Ordering is applied here rather than by the caller, because the results are paged: sorting a
    /// page that the database already chose would order twenty rows out of two hundred and quietly
    /// answer a different question.
    /// </para>
    /// </summary>
    Task<PagedResult<Book>> SearchAsync(
        string? term,
        Genre? genre,
        BookStatus? status,
        BookSortKey sortBy,
        SortDirection direction,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
