using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Domain.Features.Identity.Events;
using MediatR;

namespace Astrolabe.Application.Features.Identity.Events;

/// <summary>
/// Evicts a revoked session from the revocation cache. Implements BR-IDN-023.
///
/// <para>
/// Driven by the event rather than by each caller remembering to do it. Four separate rules end a
/// session — sign-out, a password change, blocking an account, and reuse detection — and every one
/// of them must evict. Reacting to the event makes that structural: the eviction cannot be forgotten
/// because nobody has to remember it.
/// </para>
/// </summary>
public sealed class SessionRevokedEventHandler(
    ISessionRevocationCache revocationCache,
    ITokenGenerator tokenGenerator)
    : INotificationHandler<DomainEventNotification<SessionRevoked>>
{
    public Task Handle(
        DomainEventNotification<SessionRevoked> notification, CancellationToken cancellationToken)
    {
        var revoked = notification.DomainEvent;

        // The entry only has to outlive the access tokens that reference the session; past that
        // point every one of them is expired anyway.
        revocationCache.Revoke(
            revoked.SessionId, revoked.OccurredAt.Add(tokenGenerator.AccessTokenLifetime));

        return Task.CompletedTask;
    }
}
