using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Identity;

public sealed class UserSessionRepository(AstrolabeDbContext context)
    : Repository<UserSession>(context), IUserSessionRepository
{
    public async Task<IReadOnlyList<UserSession>> GetActiveByUserAsync(
        Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default) =>
        await Query
            .Where(s => s.UserId == userId && s.RevokedAt == null && s.ExpiresAt > now)
            .OrderByDescending(s => s.LastSeenAt)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Loads the session together with its whole token chain. The chain is mandatory here: reuse
    /// detection compares against rotated tokens, so fetching only the live one would silently
    /// disable BR-IDN-018.
    /// </summary>
    public async Task<UserSession?> GetByRefreshTokenHashAsync(
        SecretHash hash, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hash);

        var sessionId = await Context.RefreshTokens
            .AsNoTracking()
            .Where(t => t.Hash == hash)
            .Select(t => (Guid?)t.SessionId)
            .FirstOrDefaultAsync(cancellationToken);

        return sessionId is null
            ? null
            : await GetWithTokensAsync(sessionId.Value, cancellationToken);
    }

    public async Task<UserSession?> GetWithTokensAsync(
        Guid sessionId, CancellationToken cancellationToken = default) =>
        await Query
            .Include(s => s.Tokens)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
}
