using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Enums;

namespace Astrolabe.Domain.Features.Identity.Events;

/// <summary>
/// An account was blocked or deleted. Every live session must end (BR-IDN-007).
/// Carries identifiers and values only, never entity references.
/// </summary>
public sealed record UserAccessRevoked(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid UserId, SessionRevocationReason Reason) : IDomainEvent;
