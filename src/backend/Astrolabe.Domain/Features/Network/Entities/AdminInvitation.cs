using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Features.Network.Events;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Network.Entities;

/// <summary>
/// How a staff account is onboarded. Implements BR-NET-013, BR-NET-014 and BR-NET-015.
///
/// The invitation carries its own role and libraries rather than applying them at send time. That is
/// what grants access only on confirmation, and what lets an invitation survive its sender being
/// revoked — see the edge cases in network.business.md section 5.
/// </summary>
public sealed class AdminInvitation : AggregateRoot
{
    private readonly List<Guid> _libraryIds = [];

    private AdminInvitation()
    {
    }

    private AdminInvitation(
        Guid id,
        Guid userId,
        UserRole role,
        IReadOnlyList<Guid> libraryIds,
        byte[] tokenHash,
        Guid invitedByUserId,
        DateTimeOffset now,
        TimeSpan lifetime) : base(id)
    {
        UserId = userId;
        Role = role;
        _libraryIds.AddRange(libraryIds);
        TokenHash = tokenHash;
        InvitedByUserId = invitedByUserId;
        CreatedAt = now;
        ExpiresAt = now.Add(lifetime);

        Raise(new AdminInvited(Guid.NewGuid(), now, id, userId, role, _libraryIds.AsReadOnly()));
    }

    public Guid UserId { get; private set; }

    public UserRole Role { get; private set; }

    public IReadOnlyList<Guid> LibraryIds => _libraryIds;

    /// <summary>SHA-256 of the emailed token. The plaintext is never stored.</summary>
    public byte[] TokenHash { get; private set; } = [];

    public Guid InvitedByUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? AcceptedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsPending => AcceptedAt is null && RevokedAt is null;

    public static Result<AdminInvitation> Create(
        Guid id,
        Guid userId,
        UserRole role,
        IReadOnlyList<Guid> libraryIds,
        byte[] tokenHash,
        Guid invitedByUserId,
        DateTimeOffset now,
        TimeSpan lifetime)
    {
        ArgumentNullException.ThrowIfNull(libraryIds);
        ArgumentNullException.ThrowIfNull(tokenHash);

        if (!role.IsStaff())
        {
            return Result.Failure<AdminInvitation>(NetworkErrors.InvitationRoleInvalid);
        }

        // A super administrator has unrestricted scope by definition (BR-NET-007), so naming
        // libraries for one would be meaningless. An Admin without libraries could never act.
        if (role == UserRole.Admin && libraryIds.Count == 0)
        {
            return Result.Failure<AdminInvitation>(NetworkErrors.InvitationLibrariesRequired);
        }

        return Result.Success(new AdminInvitation(
            id, userId, role, libraryIds, tokenHash, invitedByUserId, now, lifetime));
    }

    /// <summary>
    /// Confirms the invitation. Deliberately does not check who is calling: the token is the proof,
    /// and the recipient is not yet authenticated when they follow the link.
    /// </summary>
    public Result Accept(DateTimeOffset now)
    {
        if (AcceptedAt is not null)
        {
            return Result.Failure(NetworkErrors.InvitationAlreadyAccepted);
        }

        if (RevokedAt is not null)
        {
            return Result.Failure(NetworkErrors.InvitationRevoked);
        }

        if (now >= ExpiresAt)
        {
            return Result.Failure(NetworkErrors.InvitationExpired);
        }

        AcceptedAt = now;
        return Result.Success();
    }

    /// <summary>
    /// Invalidates this invitation. Used both when revoking an invited administrator and when
    /// resending, since BR-NET-015 requires the previous token to stop working.
    /// </summary>
    public void Revoke(DateTimeOffset now)
    {
        if (IsPending)
        {
            RevokedAt = now;
        }
    }
}
