using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Identity.Commands.VerifyEmail;

public sealed class VerifyEmailCommandHandler(IIdentityUnitOfWork identity,
    IDateTimeProvider clock) : ICommandHandler<VerifyEmailCommand>
{
    public async Task<Result> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return Result.Failure(IdentityErrors.InvalidVerificationToken);
        }

        var now = clock.UtcNow;

        var token = await identity.Tokens.GetUsableByHashAsync(
            SecretHash.FromPlaintext(request.Token),
            SingleUseTokenPurpose.EmailVerification,
            cancellationToken);

        if (token is null)
        {
            return Result.Failure(IdentityErrors.InvalidVerificationToken);
        }

        // The entity decides whether the token is spendable, so an expired, consumed and superseded
        // token all report identically.
        var consumed = token.Consume(now);

        if (consumed.IsFailure)
        {
            return consumed;
        }

        var user = await identity.Users.GetByIdAsync(token.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(IdentityErrors.InvalidVerificationToken);
        }

        var verified = user.Verify(now);

        if (verified.IsFailure)
        {
            return verified;
        }

        await identity.Audit.AddAsync(
            AuditEntry.Record("identity.email_verified", now, subjectUserId: user.Id),
            cancellationToken);

        await identity.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
