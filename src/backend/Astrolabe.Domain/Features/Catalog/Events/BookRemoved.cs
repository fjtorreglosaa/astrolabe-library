using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Catalog.Enums;

namespace Astrolabe.Domain.Features.Catalog.Events;

/// <summary>A book left the collection, with the reason the audit entry must record.</summary>
public sealed record BookRemoved(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid BookId,
    string Title,
    RemovalReason Reason,
    string? Notes) : IDomainEvent;
