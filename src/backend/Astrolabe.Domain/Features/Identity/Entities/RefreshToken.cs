using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.ValueObjects;

namespace Astrolabe.Domain.Features.Identity.Entities;

/// <summary>
/// One link in a session's token chain.
///
/// Only the SHA-256 hash is kept (BR-IDN-016): a database leak must not yield a usable token. Once
/// rotated, a token stays in the chain rather than being deleted, because its presence is exactly
/// what lets reuse be detected later (BR-IDN-018).
/// </summary>
public sealed class RefreshToken : Entity
{
    private RefreshToken()
    {
    }

    private RefreshToken(
        Guid id, Guid sessionId, SecretHash hash, DateTimeOffset issuedAt, DateTimeOffset expiresAt)
        : base(id)
    {
        SessionId = sessionId;
        Hash = hash;
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
    }

    public Guid SessionId { get; private set; }

    public SecretHash Hash { get; private set; } = null!;

    public DateTimeOffset IssuedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>Set when this token was exchanged for a new one. A rotated token is spent.</summary>
    public DateTimeOffset? RotatedAt { get; private set; }

    /// <summary>The token issued in exchange for this one. Makes the chain traversable.</summary>
    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsRotated => RotatedAt is not null;

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public bool IsUsable(DateTimeOffset now) => !IsRotated && !IsExpired(now);

    internal static RefreshToken Issue(
        Guid sessionId, SecretHash hash, DateTimeOffset now, DateTimeOffset expiresAt) =>
        new(Guid.NewGuid(), sessionId, hash, now, expiresAt);

    internal void MarkRotated(DateTimeOffset now, Guid replacedByTokenId)
    {
        RotatedAt = now;
        ReplacedByTokenId = replacedByTokenId;
    }
}
