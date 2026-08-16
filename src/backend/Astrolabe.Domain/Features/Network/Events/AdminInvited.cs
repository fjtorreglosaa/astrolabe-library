using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Enums;

namespace Astrolabe.Domain.Features.Network.Events;

/// <summary>
/// A staff invitation was created. Triggers the invitation email (BR-NET-013).
/// </summary>
public sealed record AdminInvited(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid InvitationId,
    Guid UserId,
    UserRole Role,
    IReadOnlyList<Guid> LibraryIds) : IDomainEvent;
