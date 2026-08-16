using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Audit.Entities;
using Astrolabe.Domain.Features.Audit.Repositories;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Network.Entities;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Features.Network.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Network.Commands.AssignLibraries;

public sealed class AssignLibrariesCommandHandler(
    IIdentityUnitOfWork identity,
    IAuditUnitOfWork audit,
    INetworkUnitOfWork network,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<AssignLibrariesCommand>
{
    public async Task<Result> Handle(
        AssignLibrariesCommand request, CancellationToken cancellationToken)
    {
        if (currentUser is not { Role: UserRole.SuperAdmin, UserId: { } actorId })
        {
            return Result.Failure(NetworkErrors.SuperAdminRequired);
        }

        var target = await identity.Users.GetByIdAsync(request.UserId, cancellationToken);

        if (target is null)
        {
            return Result.Failure(NetworkErrors.AdminNotFound);
        }

        if (!target.Role.IsStaff())
        {
            return Result.Failure(NetworkErrors.NotAStaffAccount);
        }

        var libraries = await network.Libraries.GetByIdsAsync(request.LibraryIds, cancellationToken);

        if (libraries.Count != request.LibraryIds.Count)
        {
            return Result.Failure(NetworkErrors.LibraryNotFound);
        }

        var now = clock.UtcNow;
        var current = await network.Assignments.GetActiveByUserAsync(request.UserId, cancellationToken);

        // Revoke what is no longer wanted rather than deleting: BR-NET-017 needs something to audit
        // against, and revocation is what makes BR-NET-011 observable.
        var removed = 0;

        foreach (var assignment in current.Where(a => !request.LibraryIds.Contains(a.LibraryId)))
        {
            assignment.Revoke(actorId, now);
            removed++;
        }

        // Only grant what is missing, so re-submitting the same set is a no-op rather than a churn
        // of revoke-and-regrant that would bury the real changes in the audit trail.
        var existing = current.Select(a => a.LibraryId).ToHashSet();
        var added = 0;

        foreach (var libraryId in request.LibraryIds.Where(id => !existing.Contains(id)))
        {
            await network.Assignments.AddAsync(
                LibraryAssignment.Grant(Guid.NewGuid(), request.UserId, libraryId, actorId, now),
                cancellationToken);
            added++;
        }

        await audit.Entries.AddAsync(
            AuditEntry.Record(
                "network.libraries_assigned", now, actorUserId: actorId, subjectUserId: request.UserId,
                detail: $"{added} granted, {removed} revoked."),
            cancellationToken);

        await network.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
