using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Application.Abstractions.Realtime;
using Astrolabe.Application.Contracts.Realtime;
using Astrolabe.Domain.Features.Billing.Events;
using MediatR;

namespace Astrolabe.Application.Features.Realtime.Events;

/// <summary>
/// A librarian refuses a code. The fines stay owed and the member needs to know at once.
/// </summary>
public sealed class PushOnDeskPaymentRejectedHandler(IRealtimeNotifier notifier)
    : INotificationHandler<DomainEventNotification<DeskPaymentRejected>>
{
    public async Task Handle(
        DomainEventNotification<DeskPaymentRejected> notification, CancellationToken cancellationToken)
    {
        var @event = notification.DomainEvent;
        var realtime = new RealtimeEvent(
            RealtimeEventNames.DeskPaymentRejected, @event.OccurredAt, @event.DeskPaymentId);

        // Both audiences, from one commit. The pending queue changes for staff whichever way the decision went.
        await notifier.NotifyMemberAsync(@event.MemberId, realtime, cancellationToken);
        await notifier.NotifyStaffAsync(realtime, cancellationToken);
    }
}
