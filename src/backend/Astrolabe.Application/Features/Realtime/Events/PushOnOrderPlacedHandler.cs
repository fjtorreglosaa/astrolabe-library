using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Application.Abstractions.Realtime;
using Astrolabe.Application.Contracts.Realtime;
using Astrolabe.Domain.Features.Store.Events;
using MediatR;

namespace Astrolabe.Application.Features.Realtime.Events;

/// <summary>
/// A purchase completes. Points, the statement and the order list all move together.
/// </summary>
public sealed class PushOnOrderPlacedHandler(IRealtimeNotifier notifier)
    : INotificationHandler<DomainEventNotification<OrderPlaced>>
{
    public Task Handle(
        DomainEventNotification<OrderPlaced> notification, CancellationToken cancellationToken)
    {
        var @event = notification.DomainEvent;

        return notifier.NotifyMemberAsync(
            @event.MemberId,
            new RealtimeEvent(RealtimeEventNames.OrderPlaced, @event.OccurredAt, @event.OrderId),
            cancellationToken);
    }
}
