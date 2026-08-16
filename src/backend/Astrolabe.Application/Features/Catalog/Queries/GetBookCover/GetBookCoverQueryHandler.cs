using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Catalog;
using Astrolabe.Domain.Features.Catalog.Errors;
using Astrolabe.Domain.Features.Catalog.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Catalog.Queries.GetBookCover;

/// <summary>
/// Serves a cover.
///
/// No access check beyond being signed in, deliberately: a cover is the picture on a book anyone
/// browsing the catalogue can already see, and gating it per plan would make a member's own
/// recommendations render as broken images.
/// </summary>
public sealed class GetBookCoverQueryHandler(ICatalogUnitOfWork catalog)
    : IQueryHandler<GetBookCoverQuery, BookCoverDto>
{
    public async Task<Result<BookCoverDto>> Handle(
        GetBookCoverQuery request, CancellationToken cancellationToken)
    {
        var cover = await catalog.Books.GetCoverAsync(request.BookId, cancellationToken);

        if (cover is null)
        {
            return Result.Failure<BookCoverDto>(CatalogErrors.CoverImageNotFound);
        }

        return Result.Success(new BookCoverDto(cover.ContentType, cover.Content, cover.UploadedAt));
    }
}
