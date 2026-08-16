using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Mail;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Shared.Mail;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Audit.Entities;
using Astrolabe.Domain.Features.Audit.Repositories;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Identity.Commands.ResendVerificationForUser;

public sealed class ResendVerificationForUserCommandHandler(
    IIdentityUnitOfWork identity,
    IAuditUnitOfWork audit,
    ILibraryScopeProvider scope,
    ILibraryLocationProvider libraries,
    ITokenGenerator tokenGenerator,
    IEmailSender emailSender,
    IdentityMailTemplates mailTemplates,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<ResendVerificationForUserCommand>
{
    public async Task<Result> Handle(
        ResendVerificationForUserCommand request, CancellationToken cancellationToken)
    {
        if (currentUser is not { UserId: { } actorId, Role: { } actorRole })
        {
            return Result.Failure(NetworkErrors.StaffRequired);
        }

        if (!actorRole.IsStaff())
        {
            return Result.Failure(NetworkErrors.StaffRequired);
        }

        var user = await identity.Users.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(IdentityErrors.AccountNotFound);
        }

        if (user.Status is not UserStatus.PendingVerification)
        {
            return Result.Failure(IdentityErrors.AccountNotPendingVerification);
        }

        var reach = await scope.GetCurrentScopeAsync(cancellationToken);

        if (!reach.IsUnrestricted)
        {
            var locations = await libraries.GetAllAsync(cancellationToken);

            var covered = user.CityId is { } cityId
                && locations.Values.Any(l => l.CityId == cityId && reach.Covers(l.LibraryId));

            if (!covered)
            {
                return Result.Failure(IdentityErrors.AccountOutOfScope);
            }
        }

        var now = clock.UtcNow;

        // BR-IDN-005: the previous link must stop working, whoever asked for the new one.
        var outstanding = await identity.Tokens.GetOutstandingAsync(
            user.Id, SingleUseTokenPurpose.EmailVerification, cancellationToken);

        foreach (var previous in outstanding)
        {
            previous.Invalidate(now);
        }

        var plaintext = tokenGenerator.CreateRefreshToken();

        await identity.Tokens.AddAsync(
            SingleUseToken.IssueVerification(user.Id, SecretHash.FromPlaintext(plaintext), now),
            cancellationToken);

        await audit.Entries.AddAsync(
            AuditEntry.Record(
                "identity.verification_resent", now,
                actorUserId: actorId, subjectUserId: user.Id, detail: user.Email.Value),
            cancellationToken);

        // Committed before the send. If the mail provider is down the old link is already dead and
        // the new one is already valid, so the administrator can simply try again — the reverse
        // order would leave a live link the member could still use after being told it was replaced.
        await identity.SaveChangesAsync(cancellationToken);

        await emailSender.SendAsync(
            mailTemplates.BuildVerification(user.Email, user.FullName, plaintext),
            cancellationToken);

        return Result.Success();
    }
}
