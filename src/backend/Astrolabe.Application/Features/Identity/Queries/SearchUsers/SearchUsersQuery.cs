using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Identity;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Identity.Queries.SearchUsers;

/// <summary>
/// The staff user directory. Filters, search, sort and paging transcribed from the prototype's
/// users table.
/// </summary>
/// <param name="Status">
/// The prototype's status chip filter. Null is "All", which still excludes deleted accounts unless
/// they are asked for by name — see <paramref name="IncludeDeleted"/>.
/// </param>
/// <param name="IncludeDeleted">
/// BR-IDN-008 hides deleted accounts from member-facing queries. This directory is not one: it is
/// where a deletion is undone, so it may ask.
/// </param>
public sealed record SearchUsersQuery(
    string? Term,
    UserStatus? Status,
    UserRole? Role,
    bool IncludeDeleted,
    UserSortKey SortBy,
    SortDirection Direction,
    int Page,
    int PageSize) : IQuery<PagedResult<UserSummaryDto>>;
