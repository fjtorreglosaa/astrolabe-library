using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Catalog.Errors;
using Astrolabe.Domain.Features.Catalog.Events;
using Astrolabe.Domain.Features.Catalog.ValueObjects;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Catalog.Entities;

/// <summary>
/// A bibliographic work and the copies of it the network holds. Implements BR-CAT-001 to BR-CAT-005
/// and BR-CAT-021 to BR-CAT-026.
///
/// <para>
/// The copies are inside the aggregate because stock and lifecycle change together: a book sent for
/// repair and its shelves must not be updatable by two callers who disagree about which book they
/// are looking at.
/// </para>
/// </summary>
public sealed class Book : AggregateRoot
{
    private readonly List<BookCopy> _copies = [];

    private Book()
    {
    }

    private Book(
        Guid id, Isbn isbn, string title, string author, string? publisher,
        Genre genre, PlanTier tier, Money retailPrice, string? coverUrl, DateTimeOffset now)
        : base(id)
    {
        Isbn = isbn;
        Title = title;
        Author = author;
        Publisher = publisher;
        Genre = genre;
        Tier = tier;
        RetailPrice = retailPrice;
        CoverUrl = coverUrl;
        Status = BookStatus.Draft;
        CreatedAt = now;
    }

    public Isbn Isbn { get; private set; } = null!;

    public string Title { get; private set; } = string.Empty;

    public string Author { get; private set; } = string.Empty;

    public string? Publisher { get; private set; }

    public Genre Genre { get; private set; }

    /// <summary>
    /// The plan a member needs to borrow this book. Named <c>Tier</c> on a book and <c>Plan</c> on a
    /// member on purpose: the two are compared constantly, and matching names at the comparison site
    /// would invite the exact confusion the rule turns on.
    /// </summary>
    public PlanTier Tier { get; private set; }

    public Money RetailPrice { get; private set; }

    public BookStatus Status { get; private set; }

    public string? CoverUrl { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// The mean of this book's reviews, or null when it has none. A stored column rather than a
    /// join: a listing shows a rating on every row, and an aggregate query per book would be an
    /// N+1 by construction. Maintained by the review event handler. Implements BR-CAT-030.
    /// </summary>
    public decimal? AverageRating { get; private set; }

    public int ReviewCount { get; private set; }

    public IReadOnlyList<BookCopy> Copies => _copies;

    /// <summary>Only a book in the catalogue is offered to members. Implements BR-CAT-020.</summary>
    public bool IsVisibleToMembers => Status is BookStatus.Catalog;

    public static Result<Book> CreateDraft(
        Isbn isbn, string? title, string? author, string? publisher,
        Genre genre, PlanTier tier, Money retailPrice, string? coverUrl, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(isbn);

        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure<Book>(CatalogErrors.TitleRequired);
        }

        if (string.IsNullOrWhiteSpace(author))
        {
            return Result.Failure<Book>(CatalogErrors.AuthorRequired);
        }

        if (retailPrice.IsNegative)
        {
            return Result.Failure<Book>(CatalogErrors.PriceInvalid);
        }

        return Result.Success(new Book(
            Guid.NewGuid(), isbn, title.Trim(), author.Trim(),
            string.IsNullOrWhiteSpace(publisher) ? null : publisher.Trim(),
            genre, tier, retailPrice,
            string.IsNullOrWhiteSpace(coverUrl) ? null : coverUrl.Trim(), now));
    }

    public Result UpdateDetails(
        string? title, string? author, string? publisher,
        Genre genre, PlanTier tier, Money retailPrice, string? coverUrl)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure(CatalogErrors.TitleRequired);
        }

        if (string.IsNullOrWhiteSpace(author))
        {
            return Result.Failure(CatalogErrors.AuthorRequired);
        }

        if (retailPrice.IsNegative)
        {
            return Result.Failure(CatalogErrors.PriceInvalid);
        }

        Title = title.Trim();
        Author = author.Trim();
        Publisher = string.IsNullOrWhiteSpace(publisher) ? null : publisher.Trim();
        Genre = genre;
        Tier = tier;
        RetailPrice = retailPrice;
        CoverUrl = string.IsNullOrWhiteSpace(coverUrl) ? null : coverUrl.Trim();

        return Result.Success();
    }

    // ---------- Lifecycle, BR-CAT-021 ----------

    public Result Publish(DateTimeOffset now)
    {
        if (Status is not BookStatus.Draft)
        {
            return Result.Failure(CatalogErrors.InvalidTransition(Status.ToString(), nameof(BookStatus.Catalog)));
        }

        Status = BookStatus.Catalog;
        Raise(new BookPublished(Guid.NewGuid(), now, Id, Title, Tier));

        return Result.Success();
    }

    /// <summary>
    /// Withdraws the book for repair. Implements BR-CAT-023 and BR-CAT-026: loans already running on
    /// its copies are untouched, and the book simply stops appearing in member-facing search.
    /// </summary>
    public Result SendToRepair(
        RepairReason reason, DateTimeOffset? expectedBack, string? notes, DateTimeOffset now)
    {
        if (Status is not BookStatus.Catalog)
        {
            return Result.Failure(CatalogErrors.InvalidTransition(Status.ToString(), nameof(BookStatus.Repair)));
        }

        Status = BookStatus.Repair;
        Raise(new BookSentToRepair(Guid.NewGuid(), now, Id, Title, reason, expectedBack, notes));

        return Result.Success();
    }

    public Result ReturnFromRepair(DateTimeOffset now)
    {
        if (Status is not BookStatus.Repair)
        {
            return Result.Failure(CatalogErrors.InvalidTransition(Status.ToString(), nameof(BookStatus.Catalog)));
        }

        Status = BookStatus.Catalog;
        Raise(new BookRestored(Guid.NewGuid(), now, Id, Title));

        return Result.Success();
    }

    public Result Remove(RemovalReason reason, string? notes, DateTimeOffset now)
    {
        if (Status is BookStatus.Deleted)
        {
            return Result.Failure(CatalogErrors.InvalidTransition(Status.ToString(), nameof(BookStatus.Deleted)));
        }

        Status = BookStatus.Deleted;
        Raise(new BookRemoved(Guid.NewGuid(), now, Id, Title, reason, notes));

        return Result.Success();
    }

    /// <summary>
    /// Returns a removed book to the catalogue. Its reviews and rating survive, because removing
    /// them would silently change a score the book earned.
    /// </summary>
    public Result Restore(DateTimeOffset now)
    {
        if (Status is not BookStatus.Deleted)
        {
            return Result.Failure(CatalogErrors.InvalidTransition(Status.ToString(), nameof(BookStatus.Catalog)));
        }

        Status = BookStatus.Catalog;
        Raise(new BookRestored(Guid.NewGuid(), now, Id, Title));

        return Result.Success();
    }

    // ---------- Stock ----------

    /// <summary>
    /// Adds volumes at a library, merging into the existing holding when there is one. A second row
    /// for the same library would make the stock of a branch a sum nobody remembers to compute.
    /// </summary>
    public Result AddCopies(Guid libraryId, int quantity)
    {
        var existing = _copies.FirstOrDefault(copy => copy.LibraryId == libraryId);

        if (existing is not null)
        {
            return existing.Add(quantity);
        }

        var created = BookCopy.Create(Id, libraryId, quantity);

        if (created.IsFailure)
        {
            return Result.Failure(created.Error);
        }

        _copies.Add(created.Value);

        return Result.Success();
    }

    public BookCopy? CopyAt(Guid libraryId) =>
        _copies.FirstOrDefault(copy => copy.LibraryId == libraryId);

    // ---------- Reviews, BR-CAT-030 and BR-CAT-031 ----------

    /// <summary>
    /// Records the recomputed rating. Takes the values rather than the reviews: the reviews are a
    /// separate aggregate, and loading all of them to average them would defeat the stored column.
    /// A book with no reviews reports no rating, never a rating of zero.
    /// </summary>
    public void SetRating(decimal? average, int reviewCount)
    {
        AverageRating = reviewCount > 0 ? average : null;
        ReviewCount = reviewCount;
    }
}
