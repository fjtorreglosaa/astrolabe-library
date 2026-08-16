using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Enums;

namespace Astrolabe.Domain.Features.Identity.Events;

/// <summary>
/// A session ended. Consumed to evict it from the revocation cache, which is what makes BR-IDN-023
/// — rejection on the next request rather than at token expiry — true in practice.
/// </summary>
public sealed record SessionRevoked(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid UserId,
    Guid SessionId,
    SessionRevocationReason Reason) : IDomainEvent;
