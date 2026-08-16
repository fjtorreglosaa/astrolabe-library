using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Catalog.Policies;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Catalog.Entities;

/// <summary>
/// The bytes of one book's cover.
///
/// <para>
/// Its own row rather than a column on <see cref="Book"/>, and that is the whole design decision. A
/// catalogue page loads twenty books; if the image lived on the book it would travel with every one
/// of those rows, on every listing, for every member. Here it is fetched once per cover by the
/// browser, cached like any other image, and never touched by a search.
/// </para>
/// <para>
/// <see cref="Book.CoverUrl"/> holds the path to it. That keeps the existing column, the existing
/// DTOs and the existing <c>BookCover</c> component working unchanged — a book with an uploaded
/// cover and a book pointing at an external URL are the same thing to everything downstream.
/// </para>
/// </summary>
public sealed class BookCoverImage : Entity
{
    private BookCoverImage()
    {
    }

    private BookCoverImage(
        Guid id, Guid bookId, string contentType, byte[] content, DateTimeOffset now) : base(id)
    {
        BookId = bookId;
        ContentType = contentType;
        Content = content;
        UploadedAt = now;
    }

    public Guid BookId { get; private set; }

    public string ContentType { get; private set; } = string.Empty;

    public byte[] Content { get; private set; } = [];

    public DateTimeOffset UploadedAt { get; private set; }

    public static Result<BookCoverImage> Create(
        Guid bookId, string? contentType, byte[]? content, DateTimeOffset now)
    {
        var acceptable = CoverImagePolicy.EnsureAcceptable(contentType, content);

        if (acceptable.IsFailure)
        {
            return Result.Failure<BookCoverImage>(acceptable.Error);
        }

        return Result.Success(new BookCoverImage(
            Guid.NewGuid(), bookId, contentType!.ToLowerInvariant(), content!, now));
    }

    /// <summary>Replaces the bytes in place, so a book keeps one cover row for its whole life.</summary>
    public Result Replace(string? contentType, byte[]? content, DateTimeOffset now)
    {
        var acceptable = CoverImagePolicy.EnsureAcceptable(contentType, content);

        if (acceptable.IsFailure)
        {
            return acceptable;
        }

        ContentType = contentType!.ToLowerInvariant();
        Content = content!;
        UploadedAt = now;

        return Result.Success();
    }
}
