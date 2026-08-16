using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Application.Abstractions.Notifications;
using Astrolabe.Domain.Features.Billing.Events;
using Astrolabe.Domain.Features.Notifications.Enums;
using MediatR;

namespace Astrolabe.Application.Features.Notifications.Events;

public sealed class NotifyOnFinePaidHandler(INotificationRaiser raiser)
    : INotificationHandler<DomainEventNotification<FinePaid>>
{
    public Task Handle(
        DomainEventNotification<FinePaid> notification, CancellationToken cancellationToken)
    {
        var payment = notification.DomainEvent;

        return raiser.RaiseAsync(
            payment.MemberId,
            NotificationKind.Paid,
            $"Payment received — {payment.Amount}",
            "Your receipt is in your payment history.",
            route: "/fines",
            cancellationToken);
    }
}
