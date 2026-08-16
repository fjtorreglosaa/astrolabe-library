using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Mail;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Shared.Mail;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Audit.Entities;
using Astrolabe.Domain.Features.Audit.Repositories;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Astrolabe.Domain.Features.Network.Entities;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Features.Network.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Network.Commands.ResendInvitation;

public sealed class ResendInvitationCommandHandler(
    IIdentityUnitOfWork identity,
    INetworkUnitOfWork network,
    IAuditUnitOfWork audit,
    ICurrentUser currentUser,
    ITokenGenerator tokenGenerator,
    IEmailSender emailSender,
    NetworkMailTemplates mailTemplates,
    IDateTimeProvider clock) : ICommandHandler<ResendInvitationCommand, Guid>
{
    /// <summary>The same window a first invitation gets. A resend is a fresh start, not an extension.</summary>
    private static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(7);

    public async Task<Result<Guid>> Handle(
        ResendInvitationCommand request, CancellationToken cancellationToken)
    {
        // BR-NET-008. Creating administrators is a super administrator's act, and so is doing it
        // twice — otherwise resending would be a way around the rule rather than a repeat of it.
        if (currentUser is not { Role: UserRole.SuperAdmin, UserId: { } actorId })
        {
            return Result.Failure<Guid>(NetworkErrors.SuperAdminRequired);
        }

        var user = await identity.Users.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<Guid>(IdentityErrors.AccountNotFound);
        }

        // Only an account still waiting. Resending to somebody who already accepted would email
        // them a live link to an account they are signed into, which is a way to lose control of it.
        if (user.Status is not UserStatus.Invited)
        {
            return Result.Failure<Guid>(NetworkErrors.InvitationNotPending);
        }

        var outstanding = await network.Invitations.GetPendingByUserAsync(
            user.Id, cancellationToken);

        if (outstanding.Count == 0)
        {
            return Result.Failure<Guid>(NetworkErrors.InvitationNotFound);
        }

        var now = clock.UtcNow;

        // BR-NET-015. Every outstanding one, not merely the newest: an account invited twice by
        // mistake would otherwise leave a live link behind, and the rule says the previous one must
        // stop working.
        foreach (var previous in outstanding)
        {
            previous.Revoke(now);
        }

        // The role and libraries are carried forward from the invitation being replaced, so a
        // resend cannot quietly change what the invitee is being offered. Changing that is
        // AssignLibrariesCommand's job, and it leaves its own trail.
        var superseded = outstanding[^1];
        var plaintext = tokenGenerator.CreateRefreshToken();

        var invitation = AdminInvitation.Create(
            Guid.NewGuid(), user.Id, superseded.Role, superseded.LibraryIds,
            SecretHash.FromPlaintext(plaintext).ToByteArray(), actorId, now, InvitationLifetime);

        if (invitation.IsFailure)
        {
            return Result.Failure<Guid>(invitation.Error);
        }

        invitation.Value.ClearDomainEvents();

        await network.Invitations.AddAsync(invitation.Value, cancellationToken);

        await audit.Entries.AddAsync(
            AuditEntry.Record(
                "network.invitation_resent", now,
                actorUserId: actorId, subjectUserId: user.Id,
                detail: $"{user.Email.Value} · {outstanding.Count} superseded"),
            cancellationToken);

        // Committed before the send. If the mail provider is down the old links are already dead and
        // the new one already valid, so the super administrator can simply try again — the reverse
        // order would leave a live link the invitee could still use after being told it was replaced.
        await network.SaveChangesAsync(cancellationToken);

        await emailSender.SendAsync(
            mailTemplates.BuildAdminInvitation(user.Email, user.FullName, plaintext, null),
            cancellationToken);

        return Result.Success(invitation.Value.Id);
    }
}
