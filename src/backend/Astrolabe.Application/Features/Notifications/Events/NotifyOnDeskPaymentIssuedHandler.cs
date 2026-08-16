using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Application.Abstractions.Notifications;
using Astrolabe.Domain.Features.Billing.Events;
using Astrolabe.Domain.Features.Notifications.Enums;
using MediatR;

namespace Astrolabe.Application.Features.Notifications.Events;

/// <summary>
/// A desk payment is waiting to be settled in person. The code is the thing the member needs at the
/// counter, so it goes in the body rather than being left on a screen they have to find again.
/// </summary>
public sealed class NotifyOnDeskPaymentIssuedHandler(INotificationRaiser raiser)
    : INotificationHandler<DomainEventNotification<DeskPaymentIssued>>
{
    public Task Handle(
        DomainEventNotification<DeskPaymentIssued> notification, CancellationToken cancellationToken)
    {
        var desk = notification.DomainEvent;

        return raiser.RaiseAsync(
            desk.MemberId,
            NotificationKind.Desk,
            $"Pay {desk.Amount} at the desk",
            "Show your payment code at any library counter. It is in your fines and payments screen.",
            route: "/fines",
            cancellationToken);
    }
}
