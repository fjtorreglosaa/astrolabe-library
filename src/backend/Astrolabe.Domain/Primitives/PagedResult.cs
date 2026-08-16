namespace Astrolabe.Domain.Primitives;

/// <summary>
/// A page of results together with the totals a caller needs to render pagination.
///
/// Exists so that list operations cannot quietly return an unbounded set, which GUIDELINES.md
/// section 68 forbids and section 25 requires paging for.
/// </summary>
public sealed record PagedResult<T>
{
    public const int MaxPageSize = 100;

    private PagedResult(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public IReadOnlyList<T> Items { get; }

    /// <summary>One-based page number.</summary>
    public int Page { get; }

    public int PageSize { get; }

    /// <summary>Total matching rows, ignoring paging. Needed to render the pager.</summary>
    public int TotalCount { get; }

    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;

    public bool IsEmpty => Items.Count == 0;

    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);
        return new PagedResult<T>(items, page, pageSize, totalCount);
    }

    public static PagedResult<T> Empty(int page, int pageSize) =>
        new([], page, pageSize, 0);

    /// <summary>
    /// Clamps caller-supplied paging into a safe range. A page size of zero or a negative page would
    /// otherwise translate into an unbounded or invalid query.
    /// </summary>
    public static (int Page, int PageSize) Normalise(int page, int pageSize)
    {
        var safePage = page < 1 ? 1 : page;
        var safeSize = pageSize switch
        {
            < 1 => 1,
            > MaxPageSize => MaxPageSize,
            _ => pageSize
        };

        return (safePage, safeSize);
    }
}
