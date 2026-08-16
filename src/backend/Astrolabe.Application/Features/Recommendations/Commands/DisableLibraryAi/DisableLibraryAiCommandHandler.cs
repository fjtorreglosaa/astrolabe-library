using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Audit.Entities;
using Astrolabe.Domain.Features.Audit.Repositories;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Features.Recommendations.Errors;
using Astrolabe.Domain.Features.Recommendations.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Recommendations.Commands.DisableLibraryAi;

public sealed class DisableLibraryAiCommandHandler(
    IRecommendationsUnitOfWork recommendations,
    IAuditUnitOfWork audit,
    ILibraryScopeProvider scope,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<DisableLibraryAiCommand>
{
    public async Task<Result> Handle(
        DisableLibraryAiCommand request, CancellationToken cancellationToken)
    {
        if (currentUser is not { UserId: { } actorId, Role: { } role })
        {
            return Result.Failure(NetworkErrors.StaffRequired);
        }

        if (!role.IsStaff())
        {
            return Result.Failure(NetworkErrors.StaffRequired);
        }

        var reach = await scope.GetCurrentScopeAsync(cancellationToken);

        if (!reach.Covers(request.LibraryId))
        {
            return Result.Failure(RecommendationErrors.LibraryOutOfScope);
        }

        var configuration = await recommendations.Configurations.GetByLibraryAsync(
            request.LibraryId, cancellationToken);

        if (configuration is null)
        {
            return Result.Failure(RecommendationErrors.ConfigurationNotFound);
        }

        var now = clock.UtcNow;

        // The aggregate raises LibraryAiDisabled, and a handler evicts the sets this library
        // generated. That is how BR-REC-012's "immediate" holds without this handler — or the next
        // one somebody writes — having to remember that switching off is not enough on its own.
        configuration.Disable(now);

        await audit.Entries.AddAsync(
            AuditEntry.Record(
                "recommendations.library_disabled", now,
                actorUserId: actorId, subjectUserId: null,
                detail: request.LibraryId.ToString()),
            cancellationToken);

        await recommendations.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
