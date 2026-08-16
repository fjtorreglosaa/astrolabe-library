using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Abstractions.Recommendations;
using Astrolabe.Application.Contracts.Recommendations;
using Astrolabe.Application.Shared.Recommendations;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Audit.Entities;
using Astrolabe.Domain.Features.Audit.Repositories;
using Astrolabe.Domain.Features.Recommendations.Entities;
using Astrolabe.Domain.Features.Recommendations.Errors;
using Astrolabe.Domain.Features.Recommendations.Repositories;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Recommendations.Commands.ConfigureLibraryAi;

public sealed class ConfigureLibraryAiCommandHandler(
    IRecommendationsUnitOfWork recommendations,
    IAuditUnitOfWork audit,
    ISecretProtector protector,
    IAiProviderRegistry providers,
    ILibraryScopeProvider scope,
    ILibraryLocationProvider libraries,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<ConfigureLibraryAiCommand, LibraryAiStatusDto>
{
    public async Task<Result<LibraryAiStatusDto>> Handle(
        ConfigureLibraryAiCommand request, CancellationToken cancellationToken)
    {
        if (currentUser is not { UserId: { } actorId, Role: { } role })
        {
            return Result.Failure<LibraryAiStatusDto>(NetworkErrors.StaffRequired);
        }

        if (!role.IsStaff())
        {
            return Result.Failure<LibraryAiStatusDto>(NetworkErrors.StaffRequired);
        }

        if (string.IsNullOrWhiteSpace(request.Credential))
        {
            return Result.Failure<LibraryAiStatusDto>(RecommendationErrors.CredentialEmpty);
        }

        // BR-REC-013. An administrator configures the libraries assigned to them and no others —
        // a key is money, and spending somebody else's is not a scope violation to discover later.
        var reach = await scope.GetCurrentScopeAsync(cancellationToken);

        if (!reach.Covers(request.LibraryId))
        {
            return Result.Failure<LibraryAiStatusDto>(RecommendationErrors.LibraryOutOfScope);
        }

        var provider = providers.For(request.Provider);
        var now = clock.UtcNow;

        // BR-REC-008. Tested before anything is stored as live. A key the vendor refuses leaves the
        // library exactly as unconnected as it was, which is what the prototype's flow does.
        var verified = await provider.VerifyCredentialAsync(request.Credential, cancellationToken);

        var configuration = await recommendations.Configurations.GetByLibraryAsync(
            request.LibraryId, cancellationToken);

        // Encrypted before it touches the aggregate, so no entity ever holds plaintext.
        var secret = protector.Protect(request.Credential);

        if (configuration is null)
        {
            var created = LibraryAiConfiguration.Configure(
                request.LibraryId, request.Provider, secret, now);

            configuration = created.Value;
            await recommendations.Configurations.AddAsync(configuration, cancellationToken);
        }
        else
        {
            configuration.Replace(request.Provider, secret, now);
        }

        if (!verified)
        {
            // Stored anyway, and unverified. The staff member can correct a typo without retyping
            // the whole key, and the library is not connected — which is the part that matters.
            configuration.MarkFailed(now);
            await recommendations.SaveChangesAsync(cancellationToken);

            return Result.Failure<LibraryAiStatusDto>(
                RecommendationErrors.CredentialRejectedByProvider);
        }

        configuration.MarkVerified(now);
        configuration.Enable();

        // The trail records that a credential changed, never the credential. BR-REC-004 has no
        // exception for the audit table, which is exactly where somebody would think to make one.
        await audit.Entries.AddAsync(
            AuditEntry.Record(
                "recommendations.library_configured", now,
                actorUserId: actorId, subjectUserId: null,
                detail: $"{request.LibraryId} · {RecommendationCopy.Label(request.Provider)}"),
            cancellationToken);

        await recommendations.SaveChangesAsync(cancellationToken);

        var locations = await libraries.GetAllAsync(cancellationToken);
        var name = locations.GetValueOrDefault(request.LibraryId)?.LibraryName ?? "Unknown library";

        return Result.Success(new LibraryAiStatusDto(
            configuration.LibraryId,
            name,
            RecommendationCopy.Label(configuration.Provider),
            configuration.IsConnected,
            configuration.IsEnabled,
            configuration.IsVerified,
            configuration.LastVerifiedAt,
            RecommendationCopy.StatusFor(configuration.Provider, configuration.IsConnected),
            RecommendationCopy.NoteFor(configuration.IsConnected)));
    }
}
