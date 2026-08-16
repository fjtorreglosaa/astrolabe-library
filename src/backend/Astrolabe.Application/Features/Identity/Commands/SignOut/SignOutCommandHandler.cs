using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Audit.Entities;
using Astrolabe.Domain.Features.Audit.Repositories;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Identity.Commands.SignOut;

public sealed class SignOutCommandHandler(IIdentityUnitOfWork identity,
    IAuditUnitOfWork audit,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<SignOutCommand>
{
    public async Task<Result> Handle(SignOutCommand request, CancellationToken cancellationToken)
    {
        if (currentUser is not { UserId: { } userId, SessionId: { } sessionId })
        {
            return Result.Failure(IdentityErrors.SessionNotFound);
        }

        var session = await identity.Sessions.GetByIdAsync(sessionId, cancellationToken);

        if (session is null || session.UserId != userId)
        {
            return Result.Failure(IdentityErrors.SessionNotFound);
        }

        var now = clock.UtcNow;

        session.Revoke(SessionRevocationReason.SignedOut, now);

        await audit.Entries.AddAsync(
            AuditEntry.Record("identity.signed_out", now, actorUserId: userId, subjectUserId: userId),
            cancellationToken);

        await identity.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
