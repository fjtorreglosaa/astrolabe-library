namespace Astrolabe.Application.Abstractions.Catalog;

/// <summary>
/// Answers whether a member has finished with a book, for the rules that depend on it.
/// </summary>
/// <remarks>
/// <para>
/// A seam because the fact lives in <c>reservations</c> and the rule that needs it lives in
/// <c>catalog</c>. A catalogue handler cannot reach another context's unit of work, and giving it
/// one would let it reach everything else in there too. The same shape as
/// <c>IMemberActivityProbe</c> and <c>ILibraryObligationsProbe</c>.
/// </para>
/// </remarks>
public interface IBorrowingHistoryProbe
{
    /// <summary>
    /// Whether this member has ever borrowed this book and given it back.
    /// </summary>
    /// <remarks>
    /// Deliberately "ever", not "currently". A member who read a book two years ago still has an
    /// opinion worth publishing, and a rule that expired their right to say so would delete
    /// standing reviews the moment the loan aged out.
    /// </remarks>
    Task<bool> HasReturnedAsync(
        Guid memberId, Guid bookId, CancellationToken cancellationToken = default);
}
