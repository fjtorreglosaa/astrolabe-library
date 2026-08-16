using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Audit.Entities;
using Astrolabe.Domain.Features.Audit.Repositories;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.Policies;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Identity.Commands.AdministerUser;

public sealed class AdministerUserCommandHandler(
    IIdentityUnitOfWork identity,
    IAuditUnitOfWork audit,
    ILibraryScopeProvider scope,
    ILibraryLocationProvider libraries,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<AdministerUserCommand>
{
    public async Task<Result> Handle(
        AdministerUserCommand request, CancellationToken cancellationToken)
    {
        if (currentUser is not { UserId: { } actorId, Role: { } actorRole })
        {
            return Result.Failure(NetworkErrors.StaffRequired);
        }

        if (!actorRole.IsStaff())
        {
            return Result.Failure(NetworkErrors.StaffRequired);
        }

        var target = await identity.Users.GetByIdAsync(request.UserId, cancellationToken);

        if (target is null)
        {
            return Result.Failure(IdentityErrors.AccountNotFound);
        }

        // Authority first, then reach. Both refuse, but they refuse for different reasons and the
        // administrator can act on only one of them.
        var authority = UserAdministrationPolicy.EnsureCanAdminister(
            actorId, actorRole, target.Id, target.Role);

        if (authority.IsFailure)
        {
            return authority;
        }

        var reach = await EnsureInScopeAsync(target, cancellationToken);

        if (reach.IsFailure)
        {
            return reach;
        }

        var now = clock.UtcNow;

        var result = request.Action switch
        {
            UserAdministrationAction.Block => target.Block(now),
            UserAdministrationAction.Unblock => target.Restore(),
            UserAdministrationAction.Delete => target.Delete(now),
            UserAdministrationAction.Restore => target.Restore(),
            _ => Result.Failure(IdentityErrors.InvalidCredentials)
        };

        if (result.IsFailure)
        {
            return result;
        }

        // Inside the command, in the same transaction as the change it describes. Never in an event
        // handler: a reaction runs after the commit and may be lost, and a trail that can silently
        // skip a blocking is not a trail.
        await audit.Entries.AddAsync(
            AuditEntry.Record(
                ActionNameOf(request.Action), now,
                actorUserId: actorId, subjectUserId: target.Id,
                detail: $"{target.Email.Value} · {target.Status}"),
            cancellationToken);

        // Blocking and deleting raise UserAccessRevoked, which ends every live session. Dispatched
        // after this commit, so BR-IDN-007 holds without the caller having to remember it.
        await identity.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// BR-NET-006 and BR-NET-010. A super administrator reaches everyone; an administrator reaches
    /// the cities their libraries sit in, and an administrator with no assignments reaches nobody.
    /// </summary>
    private async Task<Result> EnsureInScopeAsync(User target, CancellationToken cancellationToken)
    {
        var reach = await scope.GetCurrentScopeAsync(cancellationToken);

        if (reach.IsUnrestricted)
        {
            return Result.Success();
        }

        // Staff have no city of residence, so they sit outside every scoped view. The authority
        // check above has already refused them for an administrator, so reaching here means a super
        // administrator — who never gets this far.
        if (target.CityId is not { } cityId)
        {
            return Result.Failure(IdentityErrors.AccountOutOfScope);
        }

        var locations = await libraries.GetAllAsync(cancellationToken);

        var covered = locations.Values.Any(
            location => location.CityId == cityId && reach.Covers(location.LibraryId));

        return covered ? Result.Success() : Result.Failure(IdentityErrors.AccountOutOfScope);
    }

    /// <summary>
    /// The trail records what was intended, not what the row now says. "Unblock" and "restore" both
    /// land on Active, and an auditor reading only the outcome could not tell a lifted block from a
    /// reinstated deletion.
    /// </summary>
    private static string ActionNameOf(UserAdministrationAction action) => action switch
    {
        UserAdministrationAction.Block => "identity.blocked",
        UserAdministrationAction.Unblock => "identity.unblocked",
        UserAdministrationAction.Delete => "identity.deleted",
        _ => "identity.restored"
    };
}
