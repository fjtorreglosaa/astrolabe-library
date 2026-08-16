using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Catalog;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Catalog.Queries.SearchCatalogForStaff;

/// <summary>
/// The staff management table. Implements BR-CAT-022: unlike the member query it can return drafts,
/// books in repair and removed books, because managing them is the point.
/// </summary>
public sealed record SearchCatalogForStaffQuery(
    string? Term,
    BookStatus? Status,
    BookSortKey SortBy = BookSortKey.Title,
    SortDirection Direction = SortDirection.Ascending,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<StaffBookDto>>;
