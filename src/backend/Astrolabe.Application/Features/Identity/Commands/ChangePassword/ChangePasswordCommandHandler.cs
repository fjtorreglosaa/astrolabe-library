using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Audit.Entities;
using Astrolabe.Domain.Features.Audit.Repositories;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Identity.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler(IIdentityUnitOfWork identity,
    IAuditUnitOfWork audit,
    ICurrentUser currentUser,
    IPasswordHasher passwordHasher,
    IDateTimeProvider clock) : ICommandHandler<ChangePasswordCommand>
{
    public async Task<Result> Handle(
        ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result.Failure(IdentityErrors.InvalidCredentials);
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 12)
        {
            return Result.Failure(IdentityErrors.PasswordTooShort);
        }

        var user = await identity.Users.GetByIdAsync(userId, cancellationToken);

        if (user?.PasswordHash is null)
        {
            return Result.Failure(IdentityErrors.InvalidCredentials);
        }

        // Proving the current password is what stops a stolen access token from changing it and
        // locking the real owner out.
        if (!passwordHasher.Verify(request.CurrentPassword ?? string.Empty, user.PasswordHash))
        {
            return Result.Failure(IdentityErrors.InvalidCredentials);
        }

        var now = clock.UtcNow;
        var changed = user.ChangePassword(passwordHasher.Hash(request.NewPassword), now);

        if (changed.IsFailure)
        {
            return changed;
        }

        // BR-IDN-013 spares the current session: the member who just changed their password should
        // not have to sign in again on the device they did it from.
        var live = await identity.Sessions.GetActiveByUserAsync(userId, now, cancellationToken);
        var others = live.Where(s => s.Id != currentUser.SessionId).ToList();

        foreach (var session in others)
        {
            session.Revoke(SessionRevocationReason.PasswordChanged, now);
        }

        var revoked = others.Count;

        await audit.Entries.AddAsync(
            AuditEntry.Record(
                "identity.password_changed", now, actorUserId: userId, subjectUserId: userId,
                detail: $"{revoked} other session(s) revoked."),
            cancellationToken);

        await identity.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
