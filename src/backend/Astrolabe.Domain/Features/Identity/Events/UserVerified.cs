using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Enums;

namespace Astrolabe.Domain.Features.Identity.Events;

/// <summary>
/// Verification succeeded. The membership domain starts the subscription.
/// Carries identifiers and values only, never entity references.
/// </summary>
public sealed record UserVerified(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid UserId, UserRole Role) : IDomainEvent;
