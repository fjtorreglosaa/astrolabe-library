using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Catalog.Enums;

namespace Astrolabe.Domain.Features.Catalog.Events;

/// <summary>
/// A book was withdrawn for repair. Carries the stated reason because BR-CAT-025 requires the audit
/// entry to record it, and recovering it later from the entity would be impossible after a second
/// transition.
/// </summary>
public sealed record BookSentToRepair(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid BookId,
    string Title,
    RepairReason Reason,
    DateTimeOffset? ExpectedBack,
    string? Notes) : IDomainEvent;
