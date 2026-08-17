using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Application.Abstractions.Realtime;
using Astrolabe.Application.Contracts.Realtime;
using Astrolabe.Domain.Features.Billing.Events;
using MediatR;

namespace Astrolabe.Application.Features.Realtime.Events;

/// <summary>
/// A fine is settled by card. The balance and the statement both move.
/// </summary>
public sealed class PushOnFinePaidHandler(IRealtimeNotifier notifier)
    : INotificationHandler<DomainEventNotification<FinePaid>>
{
    public Task Handle(
        DomainEventNotification<FinePaid> notification, CancellationToken cancellationToken)
    {
        var @event = notification.DomainEvent;

        return notifier.NotifyMemberAsync(
            @event.MemberId,
            new RealtimeEvent(RealtimeEventNames.FinePaid, @event.OccurredAt, @event.FineId),
            cancellationToken);
    }
}
