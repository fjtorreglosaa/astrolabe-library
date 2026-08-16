using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Domain.Abstractions;
using MediatR;

namespace Astrolabe.Infrastructure.Persistence;

/// <summary>
/// Publishes domain events through MediatR.
///
/// Each event is wrapped in <see cref="DomainEventNotification{TDomainEvent}"/> at its concrete type
/// so handlers can subscribe to the event they care about rather than filtering a stream. The
/// reflection is the price of keeping the Domain layer free of MediatR.
/// </summary>
public sealed class DomainEventDispatcher(IPublisher publisher) : IDomainEventDispatcher
{
    public async Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var notificationType = typeof(DomainEventNotification<>)
                .MakeGenericType(domainEvent.GetType());

            var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;

            await publisher.Publish(notification, cancellationToken);
        }
    }
}
