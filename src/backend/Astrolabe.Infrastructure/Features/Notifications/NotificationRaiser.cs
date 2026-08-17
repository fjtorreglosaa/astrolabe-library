using Astrolabe.Application.Abstractions.Notifications;
using Astrolabe.Application.Abstractions.Realtime;
using Astrolabe.Application.Contracts.Realtime;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Notifications.Entities;
using Astrolabe.Domain.Features.Notifications.Enums;
using Astrolabe.Domain.Features.Notifications.Policies;
using Astrolabe.Domain.Features.Notifications.Repositories;
using Microsoft.Extensions.Logging;

namespace Astrolabe.Infrastructure.Features.Notifications;

/// <summary>
/// The single place a notification is created. Implements BR-NTF-003 and BR-NTF-005.
///
/// <para>
/// The mute check happens here, before anything is written, which is what lets BR-NTF-003 say
/// "produce no notification at all" rather than "hide it on read". A hidden notification is one that
/// reappears the day somebody writes a query that forgets the filter.
/// </para>
/// <para>
/// <b>Nothing here throws.</b> Every caller is a domain event handler reacting to a committed
/// outcome, and BR-NTF-005 is explicit that a message about a reservation must never be able to cost
/// somebody the reservation. A failure is logged and swallowed, deliberately.
/// </para>
/// </summary>
public sealed class NotificationRaiser(
    INotificationsUnitOfWork notifications,
    IRealtimeNotifier realtime,
    IDateTimeProvider clock,
    ILogger<NotificationRaiser> logger) : INotificationRaiser
{
    public async Task RaiseAsync(
        Guid memberId,
        NotificationKind kind,
        string title,
        string body,
        string? route = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var family = NotificationFamilies.Of(kind);
            var muted = await notifications.Preferences.GetMutedAsync(memberId, cancellationToken);

            if (muted.Contains(family))
            {
                return;
            }

            var notification = Notification.Raise(
                memberId, kind, title, body, route, clock.UtcNow);

            if (notification.IsFailure)
            {
                logger.LogWarning(
                    "A {Kind} notification for member {MemberId} was refused: {Error}.",
                    kind, memberId, notification.Error.Code);

                return;
            }

            await notifications.Notifications.AddAsync(notification.Value, cancellationToken);
            await notifications.SaveChangesAsync(cancellationToken);

            // Pushed from here rather than from the six handlers that call this, because here is the
            // only place that knows a notification was actually written. A member who muted the
            // family gets no row and must get no bell either — and the mute is checked above, so a
            // push emitted by the caller would ring for a notification that does not exist.
            await realtime.NotifyMemberAsync(
                memberId,
                new RealtimeEvent(
                    RealtimeEventNames.NotificationRaised,
                    notification.Value.OccurredAt,
                    notification.Value.Id),
                cancellationToken);
        }
        catch (Exception exception)
        {
            // Swallowed on purpose. The reservation, payment or ticket that caused this already
            // committed, and failing here would either roll back something that succeeded or leave
            // the caller believing it had not.
            logger.LogError(
                exception,
                "Could not raise a {Kind} notification for member {MemberId}.", kind, memberId);
        }
    }
}
