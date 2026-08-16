using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Contracts.Recommendations;
using Astrolabe.Application.Shared.Recommendations;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Features.Recommendations.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Recommendations.Queries.GetLibraryAiStatus;

public sealed class GetLibraryAiStatusQueryHandler(
    IRecommendationsUnitOfWork recommendations,
    ILibraryScopeProvider scope,
    ILibraryLocationProvider libraries,
    ICurrentUser currentUser)
    : IQueryHandler<GetLibraryAiStatusQuery, IReadOnlyList<LibraryAiStatusDto>>
{
    public async Task<Result<IReadOnlyList<LibraryAiStatusDto>>> Handle(
        GetLibraryAiStatusQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.Role is not { } role || !role.IsStaff())
        {
            return Result.Failure<IReadOnlyList<LibraryAiStatusDto>>(NetworkErrors.StaffRequired);
        }

        var reach = await scope.GetCurrentScopeAsync(cancellationToken);
        var locations = await libraries.GetAllAsync(cancellationToken);

        var mine = locations.Values
            .Where(location => reach.Covers(location.LibraryId))
            .OrderBy(location => location.LibraryName)
            .ToList();

        var configurations = (await recommendations.Configurations.GetByLibrariesAsync(
                [.. mine.Select(location => location.LibraryId)], cancellationToken))
            .ToDictionary(configuration => configuration.LibraryId);

        // A library with no row is unconfigured, which is a normal state and not a missing one.
        // Nothing in this projection can reach a credential — the DTO has nowhere to put one.
        var rows = mine
            .Select(location =>
            {
                var configuration = configurations.GetValueOrDefault(location.LibraryId);
                var connected = configuration?.IsConnected ?? false;

                return new LibraryAiStatusDto(
                    location.LibraryId,
                    location.LibraryName,
                    configuration is null ? null : RecommendationCopy.Label(configuration.Provider),
                    connected,
                    configuration?.IsEnabled ?? false,
                    configuration?.IsVerified ?? false,
                    configuration?.LastVerifiedAt,
                    RecommendationCopy.StatusFor(configuration?.Provider, connected),
                    RecommendationCopy.NoteFor(connected));
            })
            .ToList();

        return Result.Success<IReadOnlyList<LibraryAiStatusDto>>(rows);
    }
}
