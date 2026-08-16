using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Catalog.Errors;
using Astrolabe.Domain.Features.Catalog.Events;
using Astrolabe.Domain.Features.Catalog.ValueObjects;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Catalog.Entities;

/// <summary>
/// A member's rating and optional comment on a book. Implements BR-CAT-027 to BR-CAT-031.
///
/// <para>
/// Its own aggregate rather than a child of <see cref="Book"/>: a popular book may carry hundreds of
/// reviews, and loading them all to add one would make writing a review cost more the more popular
/// the book is.
/// </para>
/// </summary>
public sealed class Review : AggregateRoot
{
    private Review()
    {
    }

    private Review(Guid id, Guid bookId, Guid memberId, StarRating rating, string? comment, DateTimeOffset now)
        : base(id)
    {
        BookId = bookId;
        MemberId = memberId;
        Rating = rating;
        Comment = comment;
        CreatedAt = now;
    }

    public Guid BookId { get; private set; }

    public Guid MemberId { get; private set; }

    public StarRating Rating { get; private set; } = null!;

    public string? Comment { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? EditedAt { get; private set; }

    public static Review Publish(
        Guid bookId, Guid memberId, StarRating rating, string? comment, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(rating);

        var review = new Review(Guid.NewGuid(), bookId, memberId, rating, Clean(comment), now);
        review.Raise(new ReviewPublished(Guid.NewGuid(), now, bookId, memberId));

        return review;
    }

    /// <summary>
    /// Rewrites the member's own review. BR-CAT-027 allows only one per book, so reviewing a book a
    /// second time edits the first rather than creating another.
    /// </summary>
    public Result Edit(Guid memberId, StarRating rating, string? comment, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(rating);

        // BR-CAT-028. Checked on the entity rather than only in the handler, so no future caller can
        // reach it by a path that forgot the check.
        if (MemberId != memberId)
        {
            return Result.Failure(CatalogErrors.ReviewNotYours);
        }

        Rating = rating;
        Comment = Clean(comment);
        EditedAt = now;

        Raise(new ReviewPublished(Guid.NewGuid(), now, BookId, memberId));

        return Result.Success();
    }

    public Result Remove(Guid memberId, DateTimeOffset now)
    {
        if (MemberId != memberId)
        {
            return Result.Failure(CatalogErrors.ReviewNotYours);
        }

        Raise(new ReviewRemoved(Guid.NewGuid(), now, BookId, memberId));

        return Result.Success();
    }

    private static string? Clean(string? comment) =>
        string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
}
