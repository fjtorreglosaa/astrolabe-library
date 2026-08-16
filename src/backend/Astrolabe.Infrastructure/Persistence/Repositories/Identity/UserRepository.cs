using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Identity.ValueObjects;
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
}
