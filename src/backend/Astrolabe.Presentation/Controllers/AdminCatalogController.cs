using Astrolabe.Application.Contracts.Catalog;
using Astrolabe.Application.Features.Catalog.Commands.CreateBookDraft;
using Astrolabe.Application.Features.Catalog.Commands.PublishBook;
using Astrolabe.Application.Features.Catalog.Commands.RemoveBook;
using Astrolabe.Application.Features.Catalog.Commands.RestoreBook;
using Astrolabe.Application.Features.Catalog.Commands.ReturnBookFromRepair;
using Astrolabe.Application.Features.Catalog.Commands.SendBookToRepair;
using Astrolabe.Application.Features.Catalog.Commands.UpdateBook;
using Astrolabe.Application.Features.Catalog.Queries.SearchCatalogForStaff;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Primitives;
using Astrolabe.Presentation.Contracts.Catalog;
using Astrolabe.Presentation.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astrolabe.Presentation.Controllers;

/// <summary>
/// Book management: creating, correcting and moving books through their lifecycle.
///
/// Separate from <see cref="CatalogController"/> rather than folded into it behind per-endpoint
/// attributes. These routes can return drafts and removed books, which members must never see, and a
/// controller-wide staff policy makes that structural instead of a per-method attribute somebody can
/// forget.
/// </summary>
[Route("api/v1/admin/catalog")]
[Authorize(Policy = Policies.StaffOnly)]
public sealed class AdminCatalogController(ISender sender) : ApiControllerBase(sender)
{
    [HttpGet("books")]
    [ProducesResponseType<PagedResult<StaffBookDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchBooks(
        [FromQuery] string? term,
        [FromQuery] BookStatus? status,
        [FromQuery] BookSortKey sortBy = BookSortKey.Title,
        [FromQuery] SortDirection direction = SortDirection.Ascending,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(
            new SearchCatalogForStaffQuery(term, status, sortBy, direction, page, pageSize),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("books")]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateBook(
        [FromBody] CreateBookRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new CreateBookDraftCommand(
            request.Isbn, request.Title, request.Author, request.Publisher, request.Genre,
            request.Tier, request.RetailPriceCents, request.CoverUrl,
            request.Copies.Select(c => new CopyAllocation(c.LibraryId, c.Quantity)).ToList()),
            cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(CreateBook), new { bookId = result.Value }, result.Value)
            : HandleFailure(result);
    }

    [HttpPut("books/{bookId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateBook(
        Guid bookId, [FromBody] UpdateBookRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new UpdateBookCommand(
            bookId, request.Title, request.Author, request.Publisher, request.Genre,
            request.Tier, request.RetailPriceCents, request.CoverUrl), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpPost("books/{bookId:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PublishBook(Guid bookId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new PublishBookCommand(bookId), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpPost("books/{bookId:guid}/repair")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SendToRepair(
        Guid bookId, [FromBody] SendToRepairRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new SendBookToRepairCommand(bookId, request.Reason, request.ExpectedBack, request.Notes),
            cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpPost("books/{bookId:guid}/return-from-repair")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReturnFromRepair(Guid bookId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ReturnBookFromRepairCommand(bookId), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    /// <summary>
    /// A POST rather than a DELETE: the book is withdrawn, not erased, and it carries a mandatory
    /// reason in its body. A DELETE with a payload would misdescribe both.
    /// </summary>
    [HttpPost("books/{bookId:guid}/remove")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveBook(
        Guid bookId, [FromBody] RemoveBookRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new RemoveBookCommand(bookId, request.Reason, request.Notes), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpPost("books/{bookId:guid}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RestoreBook(Guid bookId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new RestoreBookCommand(bookId), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
}
