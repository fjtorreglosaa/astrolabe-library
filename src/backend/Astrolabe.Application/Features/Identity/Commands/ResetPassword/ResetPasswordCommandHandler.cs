using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Audit.Entities;
using Astrolabe.Domain.Features.Audit.Repositories;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Identity.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler(IIdentityUnitOfWork identity,
    IAuditUnitOfWork audit,
    IPasswordHasher passwordHasher,
    IDateTimeProvider clock) : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> Handle(
        ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return Result.Failure(IdentityErrors.InvalidRecoveryToken);
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 12)
        {
            return Result.Failure(IdentityErrors.PasswordTooShort);
        }

        var now = clock.UtcNow;

        var token = await identity.Tokens.GetUsableByHashAsync(
            SecretHash.FromPlaintext(request.Token),
            SingleUseTokenPurpose.PasswordRecovery,
            cancellationToken);

        if (token is null)
        {
            return Result.Failure(IdentityErrors.InvalidRecoveryToken);
        }

        var consumed = token.Consume(now);

        if (consumed.IsFailure)
        {
            return consumed;
        }

        var user = await identity.Users.GetByIdAsync(token.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(IdentityErrors.InvalidRecoveryToken);
        }

        var changed = user.ChangePassword(passwordHasher.Hash(request.NewPassword), now);

        if (changed.IsFailure)
        {
            return changed;
        }

        // BR-IDN-013. A reset is performed by someone who is not signed in, so there is no session
        // to spare: every one of them ends. Whoever knew the old password loses access.
        //
        // Eviction from the revocation cache is not done here: each Revoke raises SessionRevoked,
        // and the event handler evicts. That is what stops any caller from forgetting it.
        var live = await identity.Sessions.GetActiveByUserAsync(user.Id, now, cancellationToken);

        foreach (var session in live)
        {
            session.Revoke(SessionRevocationReason.PasswordChanged, now);
        }

        var revoked = live.Count;

        await audit.Entries.AddAsync(
            AuditEntry.Record(
                "identity.password_reset", now, subjectUserId: user.Id,
                detail: $"{revoked} session(s) revoked."),
            cancellationToken);

        await identity.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
