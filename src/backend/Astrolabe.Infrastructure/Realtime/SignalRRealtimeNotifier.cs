using Astrolabe.Application.Abstractions.Realtime;
using Astrolabe.Application.Contracts.Realtime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Astrolabe.Infrastructure.Realtime;

/// <summary>
/// Sends a <see cref="RealtimeEvent"/> over SignalR.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every failure is swallowed and logged.</b> This runs in a domain event handler, after the
/// transaction has already committed: the fine exists, the payment is recorded, the loan is closed.
/// Letting a broken socket throw here would turn a delivery problem into an apparent failure of an
/// operation that in fact succeeded — and the caller, seeing an exception, would be right to retry
/// something that must not happen twice.
/// </para>
/// <para>
/// The same reasoning as <c>NotificationRaiser</c>, for the same reason, and it is worth stating at
/// both: the cost of a lost push is a screen that updates on its next fetch instead of instantly.
/// </para>
/// </remarks>
public sealed class SignalRRealtimeNotifier(
    IHubContext<RealtimeHub, IRealtimeClient> hub,
    ILogger<SignalRRealtimeNotifier> logger) : IRealtimeNotifier
{
    public Task NotifyMemberAsync(
        Guid memberId, RealtimeEvent @event, CancellationToken cancellationToken = default) =>
        SendAsync(
            () => hub.Clients.Group(RealtimeGroups.ForMember(memberId))
                .Changed(@event),
            @event.Name,
            cancellationToken);

    public Task NotifyStaffAsync(
        RealtimeEvent @event, CancellationToken cancellationToken = default) =>
        SendAsync(
            () => hub.Clients.Group(RealtimeGroups.Staff).Changed(@event),
            @event.Name,
            cancellationToken);

    private async Task SendAsync(
        Func<Task> send, string eventName, CancellationToken cancellationToken)
    {
        // A cancelled request must not stop the push: the work it describes is already committed,
        // and the client that walked away is not the only one listening.
        if (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug(
                "Sending {EventName} after the request was cancelled; the change is already committed.",
                eventName);
        }

        try
        {
            await send();
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Could not push {EventName}. The change is committed; screens will pick it up on their next fetch.",
                eventName);
        }
    }
}
