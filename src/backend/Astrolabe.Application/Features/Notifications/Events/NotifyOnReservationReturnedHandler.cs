using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Application.Abstractions.Notifications;
using Astrolabe.Domain.Features.Notifications.Enums;
using Astrolabe.Domain.Features.Reservations.Events;
using MediatR;

namespace Astrolabe.Application.Features.Notifications.Events;

public sealed class NotifyOnReservationReturnedHandler(INotificationRaiser raiser)
    : INotificationHandler<DomainEventNotification<ReservationReturned>>
{
    public Task Handle(
        DomainEventNotification<ReservationReturned> notification,
        CancellationToken cancellationToken)
    {
        var returned = notification.DomainEvent;

        return raiser.RaiseAsync(
            returned.MemberId,
            NotificationKind.Returned,
            "Your book is back with us",
            "The librarian checked the copy in. Nothing else is pending on this reservation.",
            route: "/loans",
            cancellationToken);
    }
}
