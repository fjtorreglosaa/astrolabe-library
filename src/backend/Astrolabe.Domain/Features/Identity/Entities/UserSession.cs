using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.Events;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Identity.Entities;

/// <summary>
/// One authenticated device: the unit a member sees and revokes.
///
/// A separate aggregate root from <see cref="User"/> because sessions change on every request while
/// the user record is nearly static.
/// </summary>
public sealed class UserSession : AggregateRoot
{
    private readonly List<RefreshToken> _tokens = [];

    private UserSession()
    {
    }

    private UserSession(
        Guid id,
        Guid userId,
        DeviceDescriptor device,
        string ipAddress,
        DateTimeOffset now,
        DateTimeOffset expiresAt) : base(id)
    {
        UserId = userId;
        Device = device;
        IpAddress = ipAddress;
        CreatedAt = now;
        LastSeenAt = now;
        ExpiresAt = expiresAt;
    }

    public Guid UserId { get; private set; }

    public DeviceDescriptor Device { get; private set; } = null!;

    public string IpAddress { get; private set; } = string.Empty;

    /// <summary>Always null in the MVP: GeoIP needs a licensed database. The field exists so adding it later is not a schema change.</summary>
    public string? ApproximateLocation { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset LastSeenAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public SessionRevocationReason? RevokedReason { get; private set; }

    /// <summary>The token chain, oldest first. Rotated tokens are kept so reuse can be detected.</summary>
    public IReadOnlyList<RefreshToken> Tokens => _tokens;

    public bool IsRevoked => RevokedAt is not null;

    public bool IsActive(DateTimeOffset now) => !IsRevoked && now < ExpiresAt;

    /// <summary>
    /// Opens a session and issues its first refresh token. Implements BR-IDN-020 and BR-IDN-021.
    /// A fresh session is created on every sign-in, even from a device seen before.
    /// </summary>
    public static UserSession Start(
        Guid userId,
        DeviceDescriptor device,
        string ipAddress,
        SecretHash refreshTokenHash,
        DateTimeOffset now,
        TimeSpan lifetime)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(refreshTokenHash);

        var expiresAt = now.Add(lifetime);
        var session = new UserSession(Guid.NewGuid(), userId, device, ipAddress ?? string.Empty, now, expiresAt);

        session._tokens.Add(RefreshToken.Issue(session.Id, refreshTokenHash, now, expiresAt));

        return session;
    }

    /// <summary>
    /// Exchanges the presented refresh token for a new one.
    ///
    /// <para>
    /// This method owns BR-IDN-017 and BR-IDN-018 <b>together</b>, and that is the point. Presenting
    /// the live token rotates it; presenting one that was already rotated is treated as theft and
    /// revokes the entire session. Splitting the two across a handler would make it possible to
    /// implement rotation and quietly forget revocation.
    /// </para>
    ///
    /// <para>
    /// Every failure returns the same error. BR-IDN-019 requires an unknown, expired, rotated or
    /// revoked token to be indistinguishable, so reuse revokes the session silently and reports it
    /// through <see cref="RefreshTokenReuseDetected"/> rather than telling the caller why.
    /// </para>
    /// </summary>
    /// <param name="presented">Hash of the token the client sent.</param>
    /// <param name="replacement">Hash of the token to issue in its place.</param>
    public Result<RefreshToken> Rotate(SecretHash presented, SecretHash replacement, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(presented);
        ArgumentNullException.ThrowIfNull(replacement);

        if (!IsActive(now))
        {
            return Result.Failure<RefreshToken>(IdentityErrors.InvalidRefreshToken);
        }

        var token = _tokens.FirstOrDefault(t => t.Hash.Equals(presented));

        if (token is null)
        {
            return Result.Failure<RefreshToken>(IdentityErrors.InvalidRefreshToken);
        }

        // Reuse. The token was already exchanged, so a copy of it exists somewhere it should not.
        // The whole chain dies, including the token the legitimate device now holds.
        if (token.IsRotated)
        {
            Revoke(SessionRevocationReason.TokenReuseDetected, now);
            Raise(new RefreshTokenReuseDetected(Guid.NewGuid(), now, UserId, Id, token.Id));

            return Result.Failure<RefreshToken>(IdentityErrors.InvalidRefreshToken);
        }

        if (token.IsExpired(now))
        {
            return Result.Failure<RefreshToken>(IdentityErrors.InvalidRefreshToken);
        }

        // The replacement inherits the session's expiry rather than extending it. A session lives
        // 30 days from sign-in; refreshing must not turn that into an unbounded lease.
        var issued = RefreshToken.Issue(Id, replacement, now, ExpiresAt);

        token.MarkRotated(now, issued.Id);
        _tokens.Add(issued);
        LastSeenAt = now;

        return Result.Success(issued);
    }

    /// <summary>Ends the session. Idempotent: revoking an ended session is not an error.</summary>
    public void Revoke(SessionRevocationReason reason, DateTimeOffset now)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAt = now;
        RevokedReason = reason;

        Raise(new SessionRevoked(Guid.NewGuid(), now, UserId, Id, reason));
    }

    /// <summary>Records activity, so the sessions screen can show a meaningful last access.</summary>
    public void Touch(DateTimeOffset now)
    {
        if (!IsRevoked && now > LastSeenAt)
        {
            LastSeenAt = now;
        }
    }

    /// <summary>Records the approximate location once a GeoIP source exists.</summary>
    public void SetApproximateLocation(string? location) => ApproximateLocation = location;
}
