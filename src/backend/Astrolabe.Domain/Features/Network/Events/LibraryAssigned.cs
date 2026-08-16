using Astrolabe.Domain.Abstractions;

namespace Astrolabe.Domain.Features.Network.Events;

/// <summary>
/// An administrator gained authority over a library. Consumed for auditing (BR-NET-017).
/// Carries identifiers only, never entity references.
/// </summary>
public sealed record LibraryAssigned(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid UserId,
    Guid LibraryId,
    Guid GrantedByUserId) : IDomainEvent;
