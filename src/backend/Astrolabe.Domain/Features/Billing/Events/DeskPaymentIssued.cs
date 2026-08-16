using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Billing.Events;

/// <summary>
/// A code was printed. Nothing has been paid — the member still owes the money, and the expiry
/// travels so a reminder can be sent before the code goes stale.
/// </summary>
public sealed record DeskPaymentIssued(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid DeskPaymentId,
    Guid MemberId,
    Guid LibraryId,
    Money Amount,
    DateTimeOffset ExpiresAt) : IDomainEvent;
