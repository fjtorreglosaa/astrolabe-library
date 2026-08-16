using Astrolabe.Application.Contracts.Catalog;
using Astrolabe.Application.Features.Catalog.Commands.PublishReview;
using Astrolabe.Application.Features.Catalog.Commands.RemoveReview;
using Astrolabe.Application.Features.Catalog.Queries.GetBookDetail;
using Astrolabe.Application.Features.Catalog.Queries.GetBookReviews;
using Astrolabe.Application.Features.Catalog.Queries.SearchBooks;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Primitives;
using Astrolabe.Presentation.Contracts.Catalog;
using Astrolabe.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Astrolabe.Application.Features.Catalog.Queries.GetBookCover;

namespace Astrolabe.Presentation.Controllers;

/// <summary>
/// The member-facing catalogue: finding books, opening one, and reviewing it.
///
/// Every response already carries the caller's access verdict, so the client never has to decide
/// whether a book is reservable. Staff operations live in <see cref="AdminCatalogController"/>,
/// because they answer a different question and need a different policy.
/// </summary>
[Route("api/v1/catalog")]
[Authorize(Policy = Policies.MemberOnly)]
public sealed class CatalogController(ISender sender) : ApiControllerBase(sender)
{
    [HttpGet("books")]
    [ProducesResponseType<PagedResult<BookSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchBooks(
        [FromQuery] string? term,
        [FromQuery] Genre? genre,
        [FromQuery] BookSortKey sortBy = BookSortKey.Title,
        [FromQuery] SortDirection direction = SortDirection.Ascending,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new SearchBooksQuery(term, genre, sortBy, direction, page, pageSize), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpGet("books/{bookId:guid}")]
    [ProducesResponseType<BookDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBook(Guid bookId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetBookDetailQuery(bookId), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>
    /// Serves a book's cover.
    ///
    /// <para>
    /// Its own resource rather than bytes inside a listing, which is the whole reason the image is
    /// not stored on the book: a page of twenty books carries twenty short paths, and the browser
    /// caches each picture once instead of receiving it again with every search.
    /// </para>
    /// <para>
    /// Cached for a day and keyed by the book, which is safe because replacing a cover changes
    /// nothing about the URL — so the response also carries an entity tag from the upload time,
    /// and a replaced image invalidates on its own.
    /// </para>
    /// </summary>
    [HttpGet("books/{bookId:guid}/cover")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCover(Guid bookId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetBookCoverQuery(bookId), cancellationToken);

        if (result.IsFailure)
        {
            return HandleFailure(result);
        }

        var cover = result.Value;

        Response.Headers.CacheControl = "private, max-age=86400";

        return File(cover.Content, cover.ContentType, cover.UploadedAt, EntityTagOf(cover));
    }

    /// <summary>
    /// Derived from the upload time, so replacing a cover replaces the tag and every cached copy
    /// expires at once — without the URL ever having to change.
    /// </summary>
    private static Microsoft.Net.Http.Headers.EntityTagHeaderValue EntityTagOf(
        Application.Contracts.Catalog.BookCoverDto cover) =>
        new($"\"{cover.UploadedAt.UtcTicks:x}\"");

    [HttpGet("books/{bookId:guid}/reviews")]
    [ProducesResponseType<PagedResult<ReviewDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReviews(
        Guid bookId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new GetBookReviewsQuery(bookId, page, pageSize), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>
    /// Writes or rewrites the caller's review. A PUT rather than a POST because a member has at most
    /// one review per book: sending it twice must leave one review, not two.
    /// </summary>
    [HttpPut("books/{bookId:guid}/review")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PublishReview(
        Guid bookId,
        [FromBody] PublishReviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new PublishReviewCommand(bookId, request.Rating, request.Comment), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    /// <summary>Takes no review identifier, so a member can only ever remove their own.</summary>
    [HttpDelete("books/{bookId:guid}/review")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveReview(Guid bookId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RemoveReviewCommand(bookId), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
}
