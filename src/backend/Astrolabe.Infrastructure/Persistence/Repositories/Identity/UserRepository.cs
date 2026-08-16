using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Astrolabe.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Identity;

public sealed class UserRepository(AstrolabeDbContext context)
    : Repository<User>(context), IUserRepository
{
    /// <summary>
    /// Matches the unique filtered index behind BR-IDN-002: a deleted account must not block the
    /// address from being registered again.
    /// </summary>
    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        return await Query.FirstOrDefaultAsync(
            u => u.Email == email && u.Status != UserStatus.Deleted, cancellationToken);
    }

    public async Task<bool> EmailIsTakenAsync(Email email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        return await ReadOnlyQuery.AnyAsync(
            u => u.Email == email && u.Status != UserStatus.Deleted, cancellationToken);
    }

    public async Task<PagedResult<User>> SearchAsync(
        string? term,
        UserStatus? status,
        UserRole? role,
        IReadOnlyCollection<Guid>? cityIds,
        bool includeDeleted,
        UserSortKey sortBy,
        SortDirection direction,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (normalisedPage, normalisedSize) = PagedResult<User>.Normalise(page, pageSize);

        var query = ReadOnlyQuery;

        // BR-NET-010. Null is "unrestricted"; an empty collection is an administrator with no
        // assignments, and the two must not be conflated — the second has to return nothing, and a
        // `cityIds?.Count > 0` style guard would silently turn it into the first.
        if (cityIds is not null)
        {
            query = cityIds.Count == 0
                ? query.Where(_ => false)
                // Staff have no city of residence, so they are never inside a scoped view. A super
                // administrator asks with null and sees them.
                : query.Where(u => u.CityId != null && cityIds.Contains(u.CityId.Value));
        }

        // BR-IDN-008 by default. The directory opts in, because it is where a deletion is undone.
        if (!includeDeleted)
        {
            query = query.Where(u => u.Status != UserStatus.Deleted);
        }

        if (status is { } requiredStatus)
        {
            query = query.Where(u => u.Status == requiredStatus);
        }

        if (role is { } requiredRole)
        {
            query = query.Where(u => u.Role == requiredRole);
        }

        if (!string.IsNullOrWhiteSpace(term))
        {
            // ILike keeps the comparison in Postgres. The prototype searches name and email; the
            // address is what an administrator is given when somebody reports a problem.
            var pattern = $"%{term.Trim()}%";

            query = query.Where(u =>
                EF.Functions.ILike(u.FullName, pattern)
                || EF.Functions.ILike(u.Email.Value, pattern));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await OrderBy(query, sortBy, direction)
            // Id last, always: without the tiebreaker two accounts sharing a name could swap
            // between pages and one of them would never be seen.
            .ThenBy(u => u.Id)
            .Skip((normalisedPage - 1) * normalisedSize)
            .Take(normalisedSize)
            .ToListAsync(cancellationToken);

        return PagedResult<User>.Create(items, normalisedPage, normalisedSize, total);
    }

    /// <summary>
    /// Ordering stays here rather than in a handler: <c>IQueryable</c> and EF ordering never leave
    /// Infrastructure, and expressing it in memory would sort one page instead of the whole set.
    /// </summary>
    private static IOrderedQueryable<User> OrderBy(
        IQueryable<User> query, UserSortKey sortBy, SortDirection direction)
    {
        var ascending = direction is SortDirection.Ascending;

        return sortBy switch
        {
            UserSortKey.FullName => ascending
                ? query.OrderBy(u => u.FullName)
                : query.OrderByDescending(u => u.FullName),

            UserSortKey.Email => ascending
                ? query.OrderBy(u => u.Email.Value)
                : query.OrderByDescending(u => u.Email.Value),

            UserSortKey.Role => ascending
                ? query.OrderBy(u => u.Role)
                : query.OrderByDescending(u => u.Role),

            UserSortKey.Status => ascending
                ? query.OrderBy(u => u.Status)
                : query.OrderByDescending(u => u.Status),

            // Newest first when nobody asked, which is what a directory is opened to see.
            _ => ascending
                ? query.OrderBy(u => u.CreatedAt)
                : query.OrderByDescending(u => u.CreatedAt)
        };
    }
}
