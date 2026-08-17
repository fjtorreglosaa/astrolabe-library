using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Application.Abstractions.Realtime;
using Astrolabe.Application.Contracts.Realtime;
using Astrolabe.Domain.Features.Reservations.Events;
using MediatR;

namespace Astrolabe.Application.Features.Realtime.Events;

/// <summary>
/// A return code is issued. The member has something to show, and a desk has something to expect.
/// </summary>
public sealed class PushOnReturnStartedHandler(IRealtimeNotifier notifier)
    : INotificationHandler<DomainEventNotification<ReturnStarted>>
{
    public async Task Handle(
        DomainEventNotification<ReturnStarted> notification, CancellationToken cancellationToken)
    {
        var @event = notification.DomainEvent;
        var realtime = new RealtimeEvent(
            RealtimeEventNames.ReturnStarted, @event.OccurredAt, @event.ReservationId);

        // Both audiences, from one commit. The code is worthless until a desk can see it, and the desk did not ask for it.
        await notifier.NotifyMemberAsync(@event.MemberId, realtime, cancellationToken);
        await notifier.NotifyStaffAsync(realtime, cancellationToken);
    }
}
