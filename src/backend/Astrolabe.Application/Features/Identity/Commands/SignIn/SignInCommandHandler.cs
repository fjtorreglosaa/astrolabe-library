using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Identity;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Audit.Entities;
using Astrolabe.Domain.Features.Audit.Repositories;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Identity.Commands.SignIn;

public sealed class SignInCommandHandler(IIdentityUnitOfWork identity,
    IAuditUnitOfWork audit,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator,
    IDeviceParser deviceParser,
    IDateTimeProvider clock) : ICommandHandler<SignInCommand, TokenPair>
{
    public async Task<Result<TokenPair>> Handle(
        SignInCommand request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var email = Email.Create(request.Email);

        // Every failure below returns InvalidCredentials. BR-IDN-028 requires a malformed address,
        // an unknown one, a wrong password and every inactive account state to be indistinguishable.
        if (email.IsFailure)
        {
            return Result.Failure<TokenPair>(IdentityErrors.InvalidCredentials);
        }

        var user = await identity.Users.GetByEmailAsync(email.Value, cancellationToken);

        if (user is null)
        {
            // The password is still hashed for an unknown address, so the response takes the same
            // time whether or not the account exists. Skipping it would leak existence by timing.
            passwordHasher.Hash(request.Password ?? string.Empty);

            await RecordFailureAsync(null, request.IpAddress, now, cancellationToken);

            return Result.Failure<TokenPair>(IdentityErrors.InvalidCredentials);
        }

        var canSignIn = user.EnsureCanSignIn(now);

        if (canSignIn.IsFailure)
        {
            await RecordFailureAsync(user.Id, request.IpAddress, now, cancellationToken);

            return Result.Failure<TokenPair>(IdentityErrors.InvalidCredentials);
        }

        if (!passwordHasher.Verify(request.Password ?? string.Empty, user.PasswordHash!))
        {
            user.RecordFailedSignIn(now);

            await RecordFailureAsync(user.Id, request.IpAddress, now, cancellationToken);
            await identity.SaveChangesAsync(cancellationToken);

            return Result.Failure<TokenPair>(IdentityErrors.InvalidCredentials);
        }

        user.RecordSuccessfulSignIn();

        var refreshToken = tokenGenerator.CreateRefreshToken();

        var session = UserSession.Start(
            user.Id,
            deviceParser.Parse(request.UserAgent, request.ClientDeviceId),
            request.IpAddress ?? string.Empty,
            SecretHash.FromPlaintext(refreshToken),
            now,
            tokenGenerator.RefreshTokenLifetime);

        await identity.Sessions.AddAsync(session, cancellationToken);

        await audit.Entries.AddAsync(
            AuditEntry.Record(
                "identity.sign_in_succeeded", now,
                actorUserId: user.Id, subjectUserId: user.Id,
                ipAddress: request.IpAddress, detail: session.Device.Name),
            cancellationToken);

        await identity.SaveChangesAsync(cancellationToken);

        return Result.Success(new TokenPair(
            tokenGenerator.CreateAccessToken(user, session.Id),
            now.Add(tokenGenerator.AccessTokenLifetime),
            refreshToken,
            session.ExpiresAt,
            session.Id));
    }

    private async Task RecordFailureAsync(
        Guid? userId, string? ipAddress, DateTimeOffset now, CancellationToken cancellationToken) =>
        await audit.Entries.AddAsync(
            AuditEntry.Record("identity.sign_in_failed", now, subjectUserId: userId, ipAddress: ipAddress),
            cancellationToken);
}
