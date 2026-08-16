using Astrolabe.Application.Contracts.Notifications;
using Astrolabe.Application.Features.Notifications.Commands.ClearNotifications;
using Astrolabe.Application.Features.Notifications.Commands.MarkNotificationsRead;
using Astrolabe.Application.Features.Notifications.Commands.SetNotificationPreference;
using Astrolabe.Application.Features.Notifications.Queries.GetMyNotifications;
using Astrolabe.Domain.Features.Notifications.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astrolabe.Presentation.Controllers;

/// <summary>
/// The notification centre. Every route acts on the caller's own, and BR-NTF-007 is enforced by the
/// shape rather than by a check: no route accepts another member's identifier.
///
/// Authenticated rather than member-only. Staff hold accounts too, and a ticket answered on their
/// own membership is still theirs to read.
/// </summary>
[Route("api/v1/notifications")]
[Authorize]
public sealed class NotificationsController(ISender sender) : ApiControllerBase(sender)
{
    [HttpGet]
    [ProducesResponseType<NotificationFeedDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(
        [FromQuery] int limit = 30, CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetMyNotificationsQuery(limit), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("{notificationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkRead(
        Guid notificationId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new MarkNotificationsReadCommand(notificationId), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpPost("read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new MarkNotificationsReadCommand(), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    /// <summary>BR-NTF-008. Permanent, and no undo is offered.</summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ClearNotificationsCommand(), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    /// <summary>Mutes or unmutes one family. BR-NTF-002 — never a single kind.</summary>
    [HttpPut("preferences/{family}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetPreference(
        NotificationFamily family, [FromQuery] bool muted, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new SetNotificationPreferenceCommand(family, muted), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }
}
