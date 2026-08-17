using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Application.Abstractions.Realtime;
using Astrolabe.Application.Contracts.Realtime;
using Astrolabe.Domain.Features.Support.Events;
using MediatR;

namespace Astrolabe.Application.Features.Realtime.Events;

/// <summary>
/// A ticket gains a message.
/// </summary>
public sealed class PushOnTicketAnsweredHandler(IRealtimeNotifier notifier)
    : INotificationHandler<DomainEventNotification<TicketAnswered>>
{
    public async Task Handle(
        DomainEventNotification<TicketAnswered> notification, CancellationToken cancellationToken)
    {
        var @event = notification.DomainEvent;
        var realtime = new RealtimeEvent(
            RealtimeEventNames.TicketAnswered, @event.OccurredAt, @event.TicketId);

        // Both audiences, from one commit. Either side may have written it, and the other side is the one waiting. Sending to both
        // costs one frame and removes the need to know here which of them typed it.
        await notifier.NotifyMemberAsync(@event.MemberId, realtime, cancellationToken);
        await notifier.NotifyStaffAsync(realtime, cancellationToken);
    }
}
