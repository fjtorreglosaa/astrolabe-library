using Astrolabe.Application.Abstractions.Membership;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Contracts.Catalog;
using Astrolabe.Application.Shared.Catalog;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Catalog.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Catalog.Queries.SearchBooks;

public sealed class SearchBooksQueryHandler(
    ICatalogUnitOfWork catalog,
    IEntitlementProvider entitlements,
    ILibraryLocationProvider libraries)
    : IQueryHandler<SearchBooksQuery, PagedResult<BookSummaryDto>>
{
    public async Task<Result<PagedResult<BookSummaryDto>>> Handle(
        SearchBooksQuery request, CancellationToken cancellationToken)
    {
        // The entitlement and the geography are each resolved once for the whole page. Asking per
        // book would turn a listing of twenty into forty extra round trips for two facts that
        // cannot change between rows.
        var member = await entitlements.GetForCurrentMemberAsync(cancellationToken);
        var locations = await libraries.GetAllAsync(cancellationToken);

        var page = await catalog.Books.SearchAsync(
            request.Term, request.Genre, BookStatus.Catalog,
            request.SortBy, request.Direction,
            request.Page, request.PageSize, cancellationToken);

        var items = page.Items
            .Select(book => BookProjection.ToSummary(book, member, locations))
            .ToList();

        return Result.Success(PagedResult<BookSummaryDto>.Create(
            items, page.Page, page.PageSize, page.TotalCount));
    }
}
