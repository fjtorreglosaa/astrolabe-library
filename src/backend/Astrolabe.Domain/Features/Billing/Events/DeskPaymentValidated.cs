using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Billing.Events;

/// <summary>A librarian confirmed they took the money. Carries identifiers and values only.</summary>
public sealed record DeskPaymentValidated(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid DeskPaymentId, Guid MemberId, Guid LibraryId, Money Amount) : IDomainEvent;
