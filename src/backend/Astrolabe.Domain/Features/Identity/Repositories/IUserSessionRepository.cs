using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.ValueObjects;

namespace Astrolabe.Domain.Features.Identity.Repositories;

/// <summary>
/// Persistence for <see cref="UserSession"/>. Sessions are revoked, never deleted, so every method
/// is explicit about whether it wants the live ones.
/// </summary>
public interface IUserSessionRepository : IRepository<UserSession>
{
    Task<IReadOnlyList<UserSession>> GetActiveByUserAsync(
        Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the session owning a refresh token, with its whole chain loaded. The chain is required:
    /// reuse detection needs the rotated tokens, not just the live one.
    /// </summary>
    Task<UserSession?> GetByRefreshTokenHashAsync(
        SecretHash hash, CancellationToken cancellationToken = default);

    Task<UserSession?> GetWithTokensAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
