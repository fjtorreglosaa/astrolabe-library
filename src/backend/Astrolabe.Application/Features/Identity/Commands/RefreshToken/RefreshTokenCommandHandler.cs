using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Identity;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Audit.Entities;
using Astrolabe.Domain.Features.Audit.Repositories;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.Events;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Identity.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(IIdentityUnitOfWork identity,
    IAuditUnitOfWork audit,
    ITokenGenerator tokenGenerator,
    IDateTimeProvider clock) : ICommandHandler<RefreshTokenCommand, TokenPair>
{
    public async Task<Result<TokenPair>> Handle(
        RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Result.Failure<TokenPair>(IdentityErrors.InvalidRefreshToken);
        }

        var now = clock.UtcNow;
        var presented = SecretHash.FromPlaintext(request.RefreshToken);

        // Loads the session with its whole token chain: reuse detection compares against rotated
        // links, so a partial load would silently disable BR-IDN-018.
        var session = await identity.Sessions.GetByRefreshTokenHashAsync(presented, cancellationToken);

        if (session is null)
        {
            return Result.Failure<TokenPair>(IdentityErrors.InvalidRefreshToken);
        }

        var replacement = tokenGenerator.CreateRefreshToken();

        // The aggregate decides. It rotates on the live token and revokes the whole session on a
        // rotated one, and the handler cannot get one without the other.
        var rotation = session.Rotate(presented, SecretHash.FromPlaintext(replacement), now);

        if (rotation.IsFailure)
        {
            await HandleFailedRotationAsync(session, now, request.IpAddress, cancellationToken);

            return Result.Failure<TokenPair>(rotation.Error);
        }

        var user = await identity.Users.GetByIdAsync(session.UserId, cancellationToken);

        // The account may have been blocked while the session was alive. A live session must not
        // outlive the right to sign in.
        if (user is null || user.EnsureCanSignIn(now).IsFailure)
        {
            // Revoking raises SessionRevoked, and its handler evicts from the revocation cache.
            session.Revoke(SessionRevocationReason.AccountClosed, now);

            await identity.SaveChangesAsync(cancellationToken);

            return Result.Failure<TokenPair>(IdentityErrors.InvalidRefreshToken);
        }

        try
        {
            await identity.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            // Another request rotated this token first. The optimistic concurrency token on the
            // session is what stops both from succeeding, so the loser simply holds a token that is
            // no longer current — reported like any other invalid token, per BR-IDN-019.
            return Result.Failure<TokenPair>(IdentityErrors.InvalidRefreshToken);
        }

        return Result.Success(new TokenPair(
            tokenGenerator.CreateAccessToken(user, session.Id),
            now.Add(tokenGenerator.AccessTokenLifetime),
            replacement,
            session.ExpiresAt,
            session.Id));
    }

    /// <summary>
    /// Persists whatever the aggregate decided, and audits a reuse. Cache eviction is not done here:
    /// the revocation raises an event, and its handler evicts, so no caller can forget it.
    /// </summary>
    private async Task HandleFailedRotationAsync(
        UserSession session, DateTimeOffset now, string? ipAddress, CancellationToken cancellationToken)
    {
        var reuse = session.DomainEvents.OfType<RefreshTokenReuseDetected>().FirstOrDefault();

        if (reuse is not null)
        {
            // The aggregate already revoked the session, which raises SessionRevoked; its handler
            // evicts from the cache. Only the audit trail is this handler's business.
            await audit.Entries.AddAsync(
                AuditEntry.Record(
                    "identity.refresh_token_reuse_detected", now,
                    subjectUserId: session.UserId, ipAddress: ipAddress,
                    detail: $"Session {session.Id} revoked."),
                cancellationToken);
        }

        await identity.SaveChangesAsync(cancellationToken);
    }
}
