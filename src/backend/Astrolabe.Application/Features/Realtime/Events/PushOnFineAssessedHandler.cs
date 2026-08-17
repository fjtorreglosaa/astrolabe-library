using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Application.Abstractions.Realtime;
using Astrolabe.Application.Contracts.Realtime;
using Astrolabe.Domain.Features.Billing.Events;
using MediatR;

namespace Astrolabe.Application.Features.Realtime.Events;

/// <summary>
/// A fine is raised. Money the member did not owe a moment ago.
/// </summary>
public sealed class PushOnFineAssessedHandler(IRealtimeNotifier notifier)
    : INotificationHandler<DomainEventNotification<FineAssessed>>
{
    public Task Handle(
        DomainEventNotification<FineAssessed> notification, CancellationToken cancellationToken)
    {
        var @event = notification.DomainEvent;

        return notifier.NotifyMemberAsync(
            @event.MemberId,
            new RealtimeEvent(RealtimeEventNames.FineAssessed, @event.OccurredAt, @event.FineId),
            cancellationToken);
    }
}
