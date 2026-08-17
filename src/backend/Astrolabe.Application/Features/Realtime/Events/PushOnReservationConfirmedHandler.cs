using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Application.Abstractions.Realtime;
using Astrolabe.Application.Contracts.Realtime;
using Astrolabe.Domain.Features.Reservations.Events;
using MediatR;

namespace Astrolabe.Application.Features.Realtime.Events;

/// <summary>
/// A reservation is confirmed: the member gains a loan, and the copy leaves the shelf that
/// the staff catalogue screen is showing.
/// </summary>
public sealed class PushOnReservationConfirmedHandler(IRealtimeNotifier notifier)
    : INotificationHandler<DomainEventNotification<ReservationConfirmed>>
{
    public async Task Handle(
        DomainEventNotification<ReservationConfirmed> notification, CancellationToken cancellationToken)
    {
        var @event = notification.DomainEvent;
        var realtime = new RealtimeEvent(
            RealtimeEventNames.ReservationConfirmed, @event.OccurredAt, @event.ReservationId);

        // Both audiences, from one commit. A librarian watching stock sees the copy count fall without reloading.
        await notifier.NotifyMemberAsync(@event.MemberId, realtime, cancellationToken);
        await notifier.NotifyStaffAsync(realtime, cancellationToken);
    }
}
