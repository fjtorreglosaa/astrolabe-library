using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Catalog.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Catalog.Entities;

/// <summary>
/// How many copies of one book one library holds. Implements BR-CAT-002.
///
/// <para>
/// A count per library, not a row per physical volume. The prototype tracks "4 / 6" per branch and
/// never identifies an individual volume, so a row per volume would invent data the product does not
/// have and multiply the table by an order of magnitude for nothing.
/// </para>
/// </summary>
public sealed class BookCopy : Entity
{
    private BookCopy()
    {
    }

    private BookCopy(Guid id, Guid bookId, Guid libraryId, int totalCount) : base(id)
    {
        BookId = bookId;
        LibraryId = libraryId;
        TotalCount = totalCount;
        AvailableCount = totalCount;
    }

    public Guid BookId { get; private set; }

    public Guid LibraryId { get; private set; }

    public int TotalCount { get; private set; }

    public int AvailableCount { get; private set; }

    public bool HasStock => AvailableCount > 0;

    public static Result<BookCopy> Create(Guid bookId, Guid libraryId, int quantity) =>
        quantity > 0
            ? Result.Success(new BookCopy(Guid.NewGuid(), bookId, libraryId, quantity))
            : Result.Failure<BookCopy>(CatalogErrors.CopyQuantityInvalid);

    /// <summary>Adds volumes to an existing holding. Both counts move together.</summary>
    public Result Add(int quantity)
    {
        if (quantity <= 0)
        {
            return Result.Failure(CatalogErrors.CopyQuantityInvalid);
        }

        TotalCount += quantity;
        AvailableCount += quantity;

        return Result.Success();
    }

    /// <summary>
    /// Takes one volume off the shelf, for <c>reservations</c> to hold.
    ///
    /// The guard here is not the whole answer to two members racing for the last copy — that is
    /// resolved by the row's concurrency token at commit. This only keeps the count honest.
    /// </summary>
    public Result Take()
    {
        if (!HasStock)
        {
            return Result.Failure(CatalogErrors.NoCopiesAvailable);
        }

        AvailableCount--;

        return Result.Success();
    }

    /// <summary>
    /// Puts a volume back. Clamped to the total, so a duplicated return cannot inflate the shelf
    /// above what the library actually owns.
    /// </summary>
    public void Return() => AvailableCount = Math.Min(AvailableCount + 1, TotalCount);
}
