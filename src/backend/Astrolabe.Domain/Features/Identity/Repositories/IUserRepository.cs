using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Identity.Repositories;

/// <summary>Persistence for <see cref="User"/>.</summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Looks up by normalised address. Excludes deleted accounts, matching the unique index that
    /// enforces BR-IDN-002, so a deleted account never blocks re-registration.
    /// </summary>
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

    Task<bool> EmailIsTakenAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>
    /// The staff user directory. Implements the listing half of Stage 6.
    /// </summary>
    /// <param name="cityIds">
    /// The cities a staff caller may see, or null for a super administrator, who sees everything.
    /// An <b>empty</b> list is not the same as null and must return nothing: BR-NET-010 requires an
    /// administrator with no assignments to see no administrative data.
    /// </param>
    /// <param name="includeDeleted">
    /// Deleted accounts are excluded by default, per BR-IDN-008. The directory is the one screen
    /// that may ask for them, because it is where an account is restored from.
    /// </param>
    Task<PagedResult<User>> SearchAsync(
        string? term,
        UserStatus? status,
        UserRole? role,
        IReadOnlyCollection<Guid>? cityIds,
        bool includeDeleted,
        UserSortKey sortBy,
        SortDirection direction,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
