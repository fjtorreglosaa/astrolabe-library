using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Mail;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Shared.Mail;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Astrolabe.Domain.Features.Network.Entities;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Features.Network.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Network.Commands.InviteAdmin;

public sealed class InviteAdminCommandHandler(
    IIdentityUnitOfWork identity,
    INetworkUnitOfWork network,
    ICurrentUser currentUser,
    ITokenGenerator tokenGenerator,
    IEmailSender emailSender,
    NetworkMailTemplates mailTemplates,
    IDateTimeProvider clock) : ICommandHandler<InviteAdminCommand, Guid>
{
    /// <summary>Long enough to survive a weekend, short enough that a stale link stops working.</summary>
    private static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(7);

    public async Task<Result<Guid>> Handle(
        InviteAdminCommand request, CancellationToken cancellationToken)
    {
        if (currentUser is not { Role: UserRole.SuperAdmin, UserId: { } inviterId })
        {
            return Result.Failure<Guid>(NetworkErrors.SuperAdminRequired);
        }

        var email = Email.Create(request.Email);

        if (email.IsFailure)
        {
            return Result.Failure<Guid>(email.Error);
        }

        if (await identity.Users.EmailIsTakenAsync(email.Value, cancellationToken))
        {
            // Unlike public registration, this is not anonymous: a super administrator is entitled
            // to know the address is already in use, and hiding it would only waste their time.
            return Result.Failure<Guid>(NetworkErrors.InvitedAddressAlreadyInUse);
        }

        // Every named library must exist, or the invitation would grant authority over nothing.
        var libraries = await network.Libraries.GetByIdsAsync(request.LibraryIds, cancellationToken);

        if (libraries.Count != request.LibraryIds.Count)
        {
            return Result.Failure<Guid>(NetworkErrors.LibraryNotFound);
        }

        var now = clock.UtcNow;
        var user = User.Invite(email.Value, request.FullName, request.Role, now);

        if (user.IsFailure)
        {
            return Result.Failure<Guid>(user.Error);
        }

        var plaintext = tokenGenerator.CreateRefreshToken();

        var invitation = AdminInvitation.Create(
            Guid.NewGuid(), user.Value.Id, request.Role, request.LibraryIds,
            SecretHash.FromPlaintext(plaintext).ToByteArray(), inviterId, now, InvitationLifetime);

        if (invitation.IsFailure)
        {
            return Result.Failure<Guid>(invitation.Error);
        }

        await identity.Users.AddAsync(user.Value, cancellationToken);
        await network.Invitations.AddAsync(invitation.Value, cancellationToken);

        // One commit: the two units of work share the request's change tracker, so the account and
        // its invitation land together or not at all.
        await network.SaveChangesAsync(cancellationToken);

        await emailSender.SendAsync(
            mailTemplates.BuildAdminInvitation(
                email.Value, request.FullName, plaintext, request.Message),
            cancellationToken);

        return Result.Success(invitation.Value.Id);
    }
}
