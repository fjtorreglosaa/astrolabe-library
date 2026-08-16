using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Mail;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Shared.Mail;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Network.Repositories;
using Astrolabe.Domain.Primitives;
using Microsoft.Extensions.Logging;

namespace Astrolabe.Application.Features.Identity.Commands.Register;

public sealed class RegisterCommandHandler(IIdentityUnitOfWork identity,
    INetworkUnitOfWork network,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator,
    IEmailSender emailSender,
    IdentityMailTemplates mailTemplates,
    IDateTimeProvider clock,
    ILogger<RegisterCommandHandler> logger) : ICommandHandler<RegisterCommand>
{
    public async Task<Result> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var email = Email.Create(request.Email);

        if (email.IsFailure)
        {
            return Result.Failure(email.Error);
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 12)
        {
            return Result.Failure(IdentityErrors.PasswordTooShort);
        }

        var now = clock.UtcNow;

        // BR-IDN-030: a taken address must produce the same response as a fresh registration.
        // Returning a conflict here would let anyone test whether an address has an account.
        var existing = await identity.Users.GetByEmailAsync(email.Value, cancellationToken);

        if (existing is not null)
        {
            await NotifyExistingAccountAsync(existing, now, cancellationToken);
            return Result.Success();
        }

        var city = await network.Cities.GetByIdAsync(request.CityId, cancellationToken);

        if (city is null || city.CountryId != request.CountryId)
        {
            return Result.Failure(IdentityErrors.InvalidCredentials);
        }

        var user = User.Register(
            email.Value, passwordHasher.Hash(request.Password), request.FullName,
            request.CountryId, request.CityId, request.Plan, now);

        if (user.IsFailure)
        {
            return Result.Failure(user.Error);
        }

        await identity.Users.AddAsync(user.Value, cancellationToken);

        var plaintext = tokenGenerator.CreateRefreshToken();

        await identity.Tokens.AddAsync(
            SingleUseToken.IssueVerification(
                user.Value.Id, SecretHash.FromPlaintext(plaintext), now),
            cancellationToken);

        await identity.Audit.AddAsync(
            AuditEntry.Record("identity.registered", now, subjectUserId: user.Value.Id),
            cancellationToken);

        // Committed before the email is sent. If sending fails the account still exists and the
        // member can request a new link — an email outage must never lose an account.
        await identity.SaveChangesAsync(cancellationToken);

        var delivery = await emailSender.SendAsync(
            mailTemplates.BuildVerification(email.Value, user.Value.FullName, plaintext),
            cancellationToken);

        if (!delivery.Accepted)
        {
            logger.LogError(
                "Verification email was not accepted for a new account. Reason: {Reason}",
                delivery.FailureReason);
        }

        return Result.Success();
    }

    private async Task NotifyExistingAccountAsync(
        User existing, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await identity.Audit.AddAsync(
            AuditEntry.Record(
                "identity.duplicate_registration_attempt", now, subjectUserId: existing.Id),
            cancellationToken);

        await identity.SaveChangesAsync(cancellationToken);

        // The account holder is told, the person at the keyboard is not.
        await emailSender.SendAsync(
            mailTemplates.BuildDuplicateRegistrationNotice(existing.Email, existing.FullName),
            cancellationToken);
    }
}
