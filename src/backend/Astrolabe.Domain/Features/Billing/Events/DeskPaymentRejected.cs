using Astrolabe.Domain.Abstractions;

namespace Astrolabe.Domain.Features.Billing.Events;

/// <summary>
/// The desk refused the payment. The reason travels because a rejection puts a debt back on a
/// member's account, and they are entitled to know why.
/// </summary>
public sealed record DeskPaymentRejected(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid DeskPaymentId,
    Guid MemberId,
    string Reason) : IDomainEvent;
