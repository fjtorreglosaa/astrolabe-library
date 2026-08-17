using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Application.Abstractions.Realtime;
using Astrolabe.Application.Contracts.Realtime;
using Astrolabe.Domain.Features.Reservations.Events;
using MediatR;

namespace Astrolabe.Application.Features.Realtime.Events;

/// <summary>
/// A return is accepted. The loan closes for the member and the copy returns to stock.
/// </summary>
public sealed class PushOnReservationReturnedHandler(IRealtimeNotifier notifier)
    : INotificationHandler<DomainEventNotification<ReservationReturned>>
{
    public async Task Handle(
        DomainEventNotification<ReservationReturned> notification, CancellationToken cancellationToken)
    {
        var @event = notification.DomainEvent;
        var realtime = new RealtimeEvent(
            RealtimeEventNames.ReservationReturned, @event.OccurredAt, @event.ReservationId);

        // Both audiences, from one commit. The queue a librarian is working through is exactly what just got shorter.
        await notifier.NotifyMemberAsync(@event.MemberId, realtime, cancellationToken);
        await notifier.NotifyStaffAsync(realtime, cancellationToken);
    }
}
