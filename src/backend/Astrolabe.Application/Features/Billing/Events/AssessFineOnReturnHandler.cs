using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Application.Features.Billing.Commands.AssessFine;
using Astrolabe.Domain.Features.Reservations.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Astrolabe.Application.Features.Billing.Events;

/// <summary>
/// Prices a fine the moment the library checks a copy in.
///
/// <para>
/// This runs <b>after</b> the commit and may be lost, which by our own rule bars it from carrying a
/// business outcome on its own. It does not: <c>AssessOutstandingFinesJob</c> sweeps whatever never
/// arrived. This handler exists so the member sees what they owe immediately rather than up to a day
/// later, and the job exists so nothing is ever missed. Neither alone is enough.
/// </para>
/// </summary>
public sealed class AssessFineOnReturnHandler(
    ISender sender,
    ILogger<AssessFineOnReturnHandler> logger)
    : INotificationHandler<DomainEventNotification<ReservationReturned>>
{
    public async Task Handle(
        DomainEventNotification<ReservationReturned> notification, CancellationToken cancellationToken)
    {
        // Nothing to price. Checked here as well as in the command so an on-time return — the
        // overwhelming majority — costs nothing at all.
        if (notification.DomainEvent.DaysLate <= 0)
        {
            return;
        }

        var result = await sender.Send(
            new AssessFineCommand(notification.DomainEvent.ReservationId), cancellationToken);

        if (result.IsFailure)
        {
            // Logged rather than thrown: the commit that returned the copy has already happened, and
            // failing here would not undo it. The sweep will find this reservation.
            logger.LogWarning(
                "Could not assess a fine for reservation {ReservationId}: {Error}. The daily sweep will retry.",
                notification.DomainEvent.ReservationId, result.Error.Code);
        }
    }
}
