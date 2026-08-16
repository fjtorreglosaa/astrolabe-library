using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Enums;

namespace Astrolabe.Domain.Features.Identity.Events;

/// <summary>
/// Registration succeeded. Triggers the verification email.
/// Carries identifiers and values only, never entity references.
/// </summary>
public sealed record UserRegistered(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid UserId, string Email, string FullName) : IDomainEvent;
