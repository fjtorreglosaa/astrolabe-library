using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Audit.Entities;
using Astrolabe.Domain.Features.Audit.Repositories;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Features.Network.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Network.Commands.RevokeAdmin;

public sealed class RevokeAdminCommandHandler(
    IIdentityUnitOfWork identity,
    IAuditUnitOfWork audit,
    INetworkUnitOfWork network,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<RevokeAdminCommand>
{
    public async Task<Result> Handle(RevokeAdminCommand request, CancellationToken cancellationToken)
    {
        if (currentUser is not { Role: UserRole.SuperAdmin, UserId: { } actorId })
        {
            return Result.Failure(NetworkErrors.SuperAdminRequired);
        }

        // BR-NET-012. The guard lives here rather than in the aggregate because it needs to know who
        // is calling, and the Domain layer must not know about the caller.
        if (request.UserId == actorId)
        {
            return Result.Failure(NetworkErrors.CannotRevokeYourself);
        }

        var target = await identity.Users.GetByIdAsync(request.UserId, cancellationToken);

        if (target is null || !target.Role.IsStaff())
        {
            return Result.Failure(NetworkErrors.AdminNotFound);
        }

        var now = clock.UtcNow;

        foreach (var assignment in await network.Assignments.GetActiveByUserAsync(
                     request.UserId, cancellationToken))
        {
            assignment.Revoke(actorId, now);
        }

        // BR-NET-016: an invited account never confirmed, so it is removed entirely. An active one
        // is preserved, losing only its role and assignments — its audit history stays meaningful.
        if (target.Status is UserStatus.Invited)
        {
            foreach (var invitation in await network.Invitations.GetPendingByUserAsync(
                         request.UserId, cancellationToken))
            {
                invitation.Revoke(now);
            }

            target.Delete(now);
        }
        else
        {
            // Demoted to the free plan rather than deleted: the person may still be a member.
            target.ChangeRole(UserRole.Member);
        }

        await audit.Entries.AddAsync(
            AuditEntry.Record(
                "network.admin_revoked", now, actorUserId: actorId, subjectUserId: request.UserId,
                detail: target.Status is UserStatus.Deleted ? "Invitation withdrawn." : "Role removed."),
            cancellationToken);

        await network.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
