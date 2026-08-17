using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Application.Abstractions.Realtime;
using Astrolabe.Application.Contracts.Realtime;
using Astrolabe.Domain.Features.Billing.Events;
using MediatR;

namespace Astrolabe.Application.Features.Realtime.Events;

/// <summary>
/// A librarian confirms the money was taken. The fines settle.
/// </summary>
public sealed class PushOnDeskPaymentValidatedHandler(IRealtimeNotifier notifier)
    : INotificationHandler<DomainEventNotification<DeskPaymentValidated>>
{
    public async Task Handle(
        DomainEventNotification<DeskPaymentValidated> notification, CancellationToken cancellationToken)
    {
        var @event = notification.DomainEvent;
        var realtime = new RealtimeEvent(
            RealtimeEventNames.DeskPaymentValidated, @event.OccurredAt, @event.DeskPaymentId);

        // Both audiences, from one commit. One librarian validating a code removes it from every other librarian's queue.
        await notifier.NotifyMemberAsync(@event.MemberId, realtime, cancellationToken);
        await notifier.NotifyStaffAsync(realtime, cancellationToken);
    }
}
