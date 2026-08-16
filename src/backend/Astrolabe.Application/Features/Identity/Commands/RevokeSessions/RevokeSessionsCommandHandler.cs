using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Identity.Commands.RevokeSessions;

public sealed class RevokeSessionsCommandHandler(IIdentityUnitOfWork identity,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<RevokeSessionsCommand, int>
{
    public async Task<Result<int>> Handle(
        RevokeSessionsCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result.Failure<int>(IdentityErrors.InvalidCredentials);
        }

        var now = clock.UtcNow;
        var live = await identity.Sessions.GetActiveByUserAsync(userId, now, cancellationToken);

        var targets = request.Scope switch
        {
            RevocationScope.All => live,
            RevocationScope.AllOthers => [.. live.Where(s => s.Id != currentUser.SessionId)],
            _ => ResolveSpecified(live, request.SessionIds)
        };

        // BR-IDN-025 holds structurally: the candidate set only ever contains the caller's own
        // sessions, so naming someone else's identifier simply matches nothing.
        if (request.Scope is RevocationScope.Specified && targets.Count == 0)
        {
            return Result.Failure<int>(IdentityErrors.SessionNotFound);
        }

        foreach (var session in targets)
        {
            session.Revoke(SessionRevocationReason.RevokedByUser, now);
        }

        var revoked = targets.Count;

        await identity.Audit.AddAsync(
            AuditEntry.Record(
                "identity.sessions_revoked", now, actorUserId: userId, subjectUserId: userId,
                detail: $"{request.Scope}: {revoked} session(s)."),
            cancellationToken);

        await identity.SaveChangesAsync(cancellationToken);

        return Result.Success(revoked);
    }

    private static IReadOnlyList<UserSession> ResolveSpecified(
        IReadOnlyList<UserSession> live, IReadOnlyList<Guid>? requested) =>
        requested is null or { Count: 0 }
            ? []
            : [.. live.Where(s => requested.Contains(s.Id))];
}
