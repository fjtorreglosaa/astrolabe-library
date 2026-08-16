using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Identity.Entities;

/// <summary>
/// A single-use secret emailed to a user: email verification (BR-IDN-004) or password recovery
/// (BR-IDN-012).
///
/// <para>
/// One entity covers both because the rules are identical — hashed at rest, usable once, expiring,
/// invalidated when a newer one is issued — and only the lifetime and the purpose differ. Two
/// near-identical classes would mean two places to fix the same bug.
/// </para>
/// </summary>
public sealed class SingleUseToken : Entity
{
    public static readonly TimeSpan VerificationLifetime = TimeSpan.FromHours(24);
    public static readonly TimeSpan RecoveryLifetime = TimeSpan.FromHours(1);

    private SingleUseToken()
    {
    }

    private SingleUseToken(
        Guid id, Guid userId, SingleUseTokenPurpose purpose, SecretHash hash,
        DateTimeOffset issuedAt, DateTimeOffset expiresAt) : base(id)
    {
        UserId = userId;
        Purpose = purpose;
        Hash = hash;
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
    }

    public Guid UserId { get; private set; }

    public SingleUseTokenPurpose Purpose { get; private set; }

    public SecretHash Hash { get; private set; } = null!;

    public DateTimeOffset IssuedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    /// <summary>Set when a newer token superseded this one (BR-IDN-005).</summary>
    public DateTimeOffset? InvalidatedAt { get; private set; }

    public bool IsUsable(DateTimeOffset now) =>
        ConsumedAt is null && InvalidatedAt is null && now < ExpiresAt;

    public static SingleUseToken IssueVerification(Guid userId, SecretHash hash, DateTimeOffset now) =>
        new(Guid.NewGuid(), userId, SingleUseTokenPurpose.EmailVerification, hash,
            now, now.Add(VerificationLifetime));

    public static SingleUseToken IssueRecovery(Guid userId, SecretHash hash, DateTimeOffset now) =>
        new(Guid.NewGuid(), userId, SingleUseTokenPurpose.PasswordRecovery, hash,
            now, now.Add(RecoveryLifetime));

    /// <summary>
    /// Spends the token. Returns failure when it was already used, superseded, or has expired —
    /// callers must not distinguish between those, per BR-IDN-004 and BR-IDN-012.
    /// </summary>
    public Result Consume(DateTimeOffset now)
    {
        if (!IsUsable(now))
        {
            return Result.Failure(Purpose is SingleUseTokenPurpose.EmailVerification
                ? Errors.IdentityErrors.InvalidVerificationToken
                : Errors.IdentityErrors.InvalidRecoveryToken);
        }

        ConsumedAt = now;
        return Result.Success();
    }

    /// <summary>Retires the token because a newer one was issued. Idempotent.</summary>
    public void Invalidate(DateTimeOffset now)
    {
        if (ConsumedAt is null && InvalidatedAt is null)
        {
            InvalidatedAt = now;
        }
    }
}
