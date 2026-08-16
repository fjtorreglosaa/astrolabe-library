using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Features.Network.Events;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Network.Entities;

/// <summary>
/// Grants an administrator authority over one library. Implements BR-NET-009 and BR-NET-011.
///
/// Assignments are revoked, never deleted, so BR-NET-017 always has something to audit against.
/// </summary>
public sealed class LibraryAssignment : AggregateRoot
{
    private LibraryAssignment()
    {
    }

    private LibraryAssignment(Guid id, Guid userId, Guid libraryId, Guid grantedByUserId, DateTimeOffset now)
        : base(id)
    {
        UserId = userId;
        LibraryId = libraryId;
        GrantedByUserId = grantedByUserId;
        GrantedAt = now;

        Raise(new LibraryAssigned(Guid.NewGuid(), now, userId, libraryId, grantedByUserId));
    }

    public Guid UserId { get; private set; }

    public Guid LibraryId { get; private set; }

    public Guid GrantedByUserId { get; private set; }

    public DateTimeOffset GrantedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid? RevokedByUserId { get; private set; }

    public bool IsActive => RevokedAt is null;

    public static LibraryAssignment Grant(
        Guid id, Guid userId, Guid libraryId, Guid grantedByUserId, DateTimeOffset now) =>
        new(id, userId, libraryId, grantedByUserId, now);

    public Result Revoke(Guid revokedByUserId, DateTimeOffset now)
    {
        if (!IsActive)
        {
            return Result.Failure(NetworkErrors.AssignmentAlreadyRevoked);
        }

        RevokedAt = now;
        RevokedByUserId = revokedByUserId;

        Raise(new LibraryAssignmentRevoked(Guid.NewGuid(), now, UserId, LibraryId, revokedByUserId));

        return Result.Success();
    }
}
