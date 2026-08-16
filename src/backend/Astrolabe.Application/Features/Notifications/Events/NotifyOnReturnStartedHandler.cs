using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Application.Abstractions.Notifications;
using Astrolabe.Domain.Features.Notifications.Enums;
using Astrolabe.Domain.Features.Reservations.Events;
using MediatR;

namespace Astrolabe.Application.Features.Notifications.Events;

public sealed class NotifyOnReturnStartedHandler(INotificationRaiser raiser)
    : INotificationHandler<DomainEventNotification<ReturnStarted>>
{
    public Task Handle(
        DomainEventNotification<ReturnStarted> notification, CancellationToken cancellationToken)
    {
        var started = notification.DomainEvent;

        return raiser.RaiseAsync(
            started.MemberId,
            NotificationKind.Transit,
            "Your return is on its way",
            "The reservation closes once a librarian checks the copy in.",
            route: "/loans",
            cancellationToken);
    }
}
