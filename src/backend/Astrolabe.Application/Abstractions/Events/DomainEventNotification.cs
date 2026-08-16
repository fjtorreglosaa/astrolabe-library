using Astrolabe.Domain.Abstractions;
using MediatR;

namespace Astrolabe.Application.Abstractions.Events;

/// <summary>
/// Carries a domain event through MediatR.
///
/// The wrapper exists so <see cref="IDomainEvent"/> stays free of any framework: the Domain layer
/// has zero external packages, so it cannot implement <see cref="INotification"/> itself.
/// </summary>
public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;
