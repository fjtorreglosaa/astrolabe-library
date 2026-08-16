using Astrolabe.Domain.Abstractions;

namespace Astrolabe.Domain.Features.Network.Events;

/// <summary>
/// An administrator lost authority over a library. Consumed for auditing (BR-NET-017).
/// </summary>
public sealed record LibraryAssignmentRevoked(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid UserId,
    Guid LibraryId,
    Guid RevokedByUserId) : IDomainEvent;
