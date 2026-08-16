using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Audit.Entities;
using Astrolabe.Domain.Features.Audit.Repositories;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Network.Commands.GrantSuperAdmin;

public sealed class GrantSuperAdminCommandHandler(
    IIdentityUnitOfWork identity,
    IAuditUnitOfWork audit,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<GrantSuperAdminCommand>
{
    public async Task<Result> Handle(
        GrantSuperAdminCommand request, CancellationToken cancellationToken)
    {
        // BR-NET-008. The most consequential grant in the product, so the narrowest gate.
        if (currentUser is not { Role: UserRole.SuperAdmin, UserId: { } actorId })
        {
            return Result.Failure(NetworkErrors.SuperAdminRequired);
        }

        var target = await identity.Users.GetByIdAsync(request.UserId, cancellationToken);

        if (target is null)
        {
            return Result.Failure(IdentityErrors.AccountNotFound);
        }

        if (target.Role is UserRole.SuperAdmin)
        {
            return Result.Failure(NetworkErrors.AlreadyASuperAdmin);
        }

        // An administrator, and an active one. A member cannot be elevated straight past the
        // invitation flow that BR-NET-006 and BR-NET-013 exist to control, and an account still
        // waiting on its invitation has not proved it owns the address yet.
        if (target.Role is not UserRole.Admin || target.Status is not UserStatus.Active)
        {
            return Result.Failure(NetworkErrors.NotAnAdministrator);
        }

        target.ChangeRole(UserRole.SuperAdmin);

        // In the same transaction as the change, never in a reaction. This is the entry an auditor
        // looks for first, and a trail that can silently lose it is not a trail.
        await audit.Entries.AddAsync(
            AuditEntry.Record(
                "network.super_admin_granted", clock.UtcNow,
                actorUserId: actorId, subjectUserId: target.Id,
                detail: target.Email.Value),
            cancellationToken);

        await identity.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
