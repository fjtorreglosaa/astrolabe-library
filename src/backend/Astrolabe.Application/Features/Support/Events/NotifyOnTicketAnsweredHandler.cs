using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Application.Abstractions.Notifications;
using Astrolabe.Domain.Features.Notifications.Enums;
using Astrolabe.Domain.Features.Support.Events;
using MediatR;

namespace Astrolabe.Application.Features.Support.Events;

/// <summary>
/// Tells a member their ticket was answered. Implements BR-SUP-012.
///
/// A reaction, not a step. The reply already committed, and a member who is not told still has the
/// answer waiting — where a reply that failed because a notification did would lose it.
/// </summary>
public sealed class NotifyOnTicketAnsweredHandler(INotificationRaiser raiser)
    : INotificationHandler<DomainEventNotification<TicketAnswered>>
{
    public Task Handle(
        DomainEventNotification<TicketAnswered> notification, CancellationToken cancellationToken)
    {
        var answered = notification.DomainEvent;

        return raiser.RaiseAsync(
            answered.MemberId,
            NotificationKind.Support,
            $"{answered.Reference} — somebody replied",
            answered.Subject,
            route: "/support",
            cancellationToken);
    }
}
