using Astrolabe.Domain.Abstractions;

namespace Astrolabe.Domain.Features.Identity.Events;

/// <summary>
/// An already-rotated refresh token was presented. Theft is assumed, so the whole session chain is
/// revoked (BR-IDN-018).
///
/// This is the highest-severity security event the identity domain raises: it means a token left
/// the device it was issued to.
/// </summary>
public sealed record RefreshTokenReuseDetected(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid UserId,
    Guid SessionId,
    Guid ReusedTokenId) : IDomainEvent;
