using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Mail;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Shared.Mail;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Identity.Commands.ResendVerification;

public sealed class ResendVerificationCommandHandler(IIdentityUnitOfWork identity,
    ITokenGenerator tokenGenerator,
    IEmailSender emailSender,
    IdentityMailTemplates mailTemplates,
    IDateTimeProvider clock) : ICommandHandler<ResendVerificationCommand>
{
    public async Task<Result> Handle(
        ResendVerificationCommand request, CancellationToken cancellationToken)
    {
        var email = Email.Create(request.Email);

        // Always succeeds from the caller's point of view, for the same reason as registration:
        // a distinguishable response would confirm whether an address has an account.
        if (email.IsFailure)
        {
            return Result.Success();
        }

        var user = await identity.Users.GetByEmailAsync(email.Value, cancellationToken);

        if (user is null || user.Status is not UserStatus.PendingVerification)
        {
            return Result.Success();
        }

        var now = clock.UtcNow;

        // BR-IDN-005: the previous link must stop working. Otherwise a member who requested a new
        // one would leave a live link behind in their inbox.
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

        await identity.SaveChangesAsync(cancellationToken);

        await emailSender.SendAsync(
            mailTemplates.BuildVerification(user.Email, user.FullName, plaintext),
            cancellationToken);

        return Result.Success();
    }
}
