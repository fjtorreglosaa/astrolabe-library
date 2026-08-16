using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Catalog;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Catalog.Queries.SearchBooks;

/// <summary>
/// The member-facing catalogue. Implements BR-CAT-017 to BR-CAT-020: it never exposes a book outside
/// the catalogue state, and it lists books the caller cannot reserve, because reach restricts
/// borrowing and not discovery.
///
/// The default order is title ascending, matching the prototype's own default for this table.
/// </summary>
public sealed record SearchBooksQuery(
    string? Term,
    Genre? Genre,
    BookSortKey SortBy = BookSortKey.Title,
    SortDirection Direction = SortDirection.Ascending,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<BookSummaryDto>>;
