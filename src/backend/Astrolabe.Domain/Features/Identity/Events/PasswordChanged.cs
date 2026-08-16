using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Enums;

namespace Astrolabe.Domain.Features.Identity.Events;

/// <summary>
/// A password was changed or reset. Every other session must end (BR-IDN-013).
/// Carries identifiers and values only, never entity references.
/// </summary>
public sealed record PasswordChanged(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid UserId) : IDomainEvent;
