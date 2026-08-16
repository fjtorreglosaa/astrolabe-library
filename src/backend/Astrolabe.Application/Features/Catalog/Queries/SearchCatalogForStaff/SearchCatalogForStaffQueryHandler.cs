using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Catalog;
using Astrolabe.Application.Shared.Catalog;
using Astrolabe.Domain.Features.Catalog.Repositories;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Catalog.Queries.SearchCatalogForStaff;

public sealed class SearchCatalogForStaffQueryHandler(
    ICatalogUnitOfWork catalog,
    ICurrentUser currentUser)
    : IQueryHandler<SearchCatalogForStaffQuery, PagedResult<StaffBookDto>>
{
    public async Task<Result<PagedResult<StaffBookDto>>> Handle(
        SearchCatalogForStaffQuery request, CancellationToken cancellationToken)
    {
        // Checked here as well as by the endpoint's policy. The status filter is what makes drafts
        // reachable, so the guard belongs where the filter is, not only at the edge.
        if (currentUser.Role is not { } role || !role.IsStaff())
        {
            return Result.Failure<PagedResult<StaffBookDto>>(NetworkErrors.StaffRequired);
        }

        var page = await catalog.Books.SearchAsync(
            request.Term, genre: null, request.Status,
            request.SortBy, request.Direction,
            request.Page, request.PageSize, cancellationToken);

        var items = page.Items.Select(BookProjection.ToStaffRow).ToList();

        return Result.Success(PagedResult<StaffBookDto>.Create(
            items, page.Page, page.PageSize, page.TotalCount));
    }
}
