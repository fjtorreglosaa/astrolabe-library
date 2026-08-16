using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Identity;

public sealed class SingleUseTokenRepository(AstrolabeDbContext context)
    : Repository<SingleUseToken>(context), ISingleUseTokenRepository
{
    public async Task<SingleUseToken?> GetUsableByHashAsync(
        SecretHash hash, SingleUseTokenPurpose purpose, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hash);

        // Expiry is judged by the entity, not filtered here: the caller must receive the token so
        // Consume can report the same error whether it expired, was used, or was superseded.
        return await Query.FirstOrDefaultAsync(
            t => t.Hash == hash && t.Purpose == purpose, cancellationToken);
    }

    public async Task<IReadOnlyList<SingleUseToken>> GetOutstandingAsync(
        Guid userId, SingleUseTokenPurpose purpose, CancellationToken cancellationToken = default) =>
        await Query
            .Where(t => t.UserId == userId
                     && t.Purpose == purpose
                     && t.ConsumedAt == null
                     && t.InvalidatedAt == null)
            .ToListAsync(cancellationToken);
}
