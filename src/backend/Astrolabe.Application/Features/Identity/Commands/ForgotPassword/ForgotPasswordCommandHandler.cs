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

namespace Astrolabe.Application.Features.Identity.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler(IIdentityUnitOfWork identity,
    ITokenGenerator tokenGenerator,
    IEmailSender emailSender,
    IdentityMailTemplates mailTemplates,
    IDateTimeProvider clock) : ICommandHandler<ForgotPasswordCommand>
{
    public async Task<Result> Handle(
        ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        // BR-IDN-029: the response is identical whether or not the address is registered. Every
        // early return below is a success for exactly that reason.
        var email = Email.Create(request.Email);

        if (email.IsFailure)
        {
            return Result.Success();
        }

        var user = await identity.Users.GetByEmailAsync(email.Value, cancellationToken);

        if (user is null || user.Status is not UserStatus.Active)
        {
            return Result.Success();
        }

        var now = clock.UtcNow;

        var outstanding = await identity.Tokens.GetOutstandingAsync(
            user.Id, SingleUseTokenPurpose.PasswordRecovery, cancellationToken);

        foreach (var previous in outstanding)
        {
            previous.Invalidate(now);
        }

        var plaintext = tokenGenerator.CreateRefreshToken();

        await identity.Tokens.AddAsync(
            SingleUseToken.IssueRecovery(user.Id, SecretHash.FromPlaintext(plaintext), now),
            cancellationToken);

        await identity.SaveChangesAsync(cancellationToken);

        await emailSender.SendAsync(
            mailTemplates.BuildPasswordRecovery(user.Email, user.FullName, plaintext),
            cancellationToken);

        return Result.Success();
    }
}
