using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.ValueObjects;

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
}
