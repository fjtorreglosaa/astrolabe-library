using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Astrolabe.Domain.Features.Network.Entities;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Features.Network.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Network.Commands.AcceptInvitation;

public sealed class AcceptInvitationCommandHandler(
    IIdentityUnitOfWork identity,
    INetworkUnitOfWork network,
    IPasswordHasher passwordHasher,
    IDateTimeProvider clock) : ICommandHandler<AcceptInvitationCommand>
{
    public async Task<Result> Handle(
        AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return Result.Failure(NetworkErrors.InvitationRevoked);
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 12)
        {
            return Result.Failure(IdentityErrors.PasswordTooShort);
        }

        var invitation = await network.Invitations.GetPendingByTokenHashAsync(
            SecretHash.FromPlaintext(request.Token).ToByteArray(), cancellationToken);

        if (invitation is null)
        {
            return Result.Failure(NetworkErrors.InvitationRevoked);
        }

        var now = clock.UtcNow;
        var accepted = invitation.Accept(now);

        if (accepted.IsFailure)
        {
            return accepted;
        }

        var user = await identity.Users.GetByIdAsync(invitation.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(IdentityErrors.AccountNotFound);
        }

        // The password is set now, not at invitation time: BR-NET-013 requires that no access exists
        // until the recipient confirms, and an account with a password is an account that can be
        // signed into.
        var activated = user.AcceptInvitation(passwordHasher.Hash(request.Password), now);

        if (activated.IsFailure)
        {
            return activated;
        }

        // The libraries the invitation carried are granted only now, which is what lets an
        // invitation survive its sender being revoked.
        foreach (var libraryId in invitation.LibraryIds)
        {
            await network.Assignments.AddAsync(
                LibraryAssignment.Grant(
                    Guid.NewGuid(), user.Id, libraryId, invitation.InvitedByUserId, now),
                cancellationToken);
        }

        await identity.Audit.AddAsync(
            AuditEntry.Record(
                "network.invitation_accepted", now, actorUserId: user.Id, subjectUserId: user.Id,
                detail: $"{invitation.LibraryIds.Count} library assignment(s) granted."),
            cancellationToken);

        await network.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
