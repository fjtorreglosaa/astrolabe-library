using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Application.Abstractions.Notifications;
using Astrolabe.Domain.Features.Billing.Events;
using Astrolabe.Domain.Features.Notifications.Enums;
using MediatR;

namespace Astrolabe.Application.Features.Notifications.Events;

/// <summary>
/// Tells a member a fine has been assessed. BR-NTF-005.
///
/// A reaction, not a step: the fine already committed, and this running or not cannot change that.
/// The raiser swallows its own failures for the same reason.
/// </summary>
public sealed class NotifyOnFineAssessedHandler(INotificationRaiser raiser)
    : INotificationHandler<DomainEventNotification<FineAssessed>>
{
    public Task Handle(
        DomainEventNotification<FineAssessed> notification, CancellationToken cancellationToken)
    {
        var fine = notification.DomainEvent;

        return raiser.RaiseAsync(
            fine.MemberId,
            NotificationKind.Due,
            $"A late fine of {fine.Amount} was added",
            $"{fine.DaysLate} days overdue. It stops growing once the book is back.",
            route: "/fines",
            cancellationToken);
    }
}
