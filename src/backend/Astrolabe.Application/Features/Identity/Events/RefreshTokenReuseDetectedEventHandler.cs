using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Domain.Features.Identity.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Astrolabe.Application.Features.Identity.Events;

/// <summary>
/// Records the highest-severity security event the identity domain raises. Implements BR-IDN-018.
///
/// A rotated refresh token resurfacing means a copy of it exists somewhere it should not, so this
/// is logged at error level even though the request itself returns an ordinary authentication
/// failure.
/// </summary>
public sealed class RefreshTokenReuseDetectedEventHandler(
    ILogger<RefreshTokenReuseDetectedEventHandler> logger)
    : INotificationHandler<DomainEventNotification<RefreshTokenReuseDetected>>
{
    public Task Handle(
        DomainEventNotification<RefreshTokenReuseDetected> notification,
        CancellationToken cancellationToken)
    {
        var reuse = notification.DomainEvent;

        logger.LogError(
            "Refresh token reuse detected. Session {SessionId} of user {UserId} was revoked. "
            + "Reused token: {TokenId}.",
            reuse.SessionId, reuse.UserId, reuse.ReusedTokenId);

        return Task.CompletedTask;
    }
}
