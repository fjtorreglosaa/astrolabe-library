using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Application.Abstractions.Realtime;
using Astrolabe.Application.Contracts.Realtime;
using Astrolabe.Domain.Features.Billing.Events;
using MediatR;

namespace Astrolabe.Application.Features.Realtime.Events;

/// <summary>
/// A desk payment code is produced. Nothing is charged yet.
/// </summary>
public sealed class PushOnDeskPaymentIssuedHandler(IRealtimeNotifier notifier)
    : INotificationHandler<DomainEventNotification<DeskPaymentIssued>>
{
    public async Task Handle(
        DomainEventNotification<DeskPaymentIssued> notification, CancellationToken cancellationToken)
    {
        var @event = notification.DomainEvent;
        var realtime = new RealtimeEvent(
            RealtimeEventNames.DeskPaymentIssued, @event.OccurredAt, @event.DeskPaymentId);

        // Both audiences, from one commit. The code appears on the manual payments screen the moment it exists, which is the
        // whole point of a code somebody is about to read out at a counter.
        await notifier.NotifyMemberAsync(@event.MemberId, realtime, cancellationToken);
        await notifier.NotifyStaffAsync(realtime, cancellationToken);
    }
}
