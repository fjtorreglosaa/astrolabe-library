using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Network;
using Astrolabe.Domain.Features.Network.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Network.Queries.GetLibraries;

public sealed class GetLibrariesQueryHandler(INetworkUnitOfWork network)
    : IQueryHandler<GetLibrariesQuery, IReadOnlyList<LibraryDto>>
{
    public async Task<Result<IReadOnlyList<LibraryDto>>> Handle(
        GetLibrariesQuery request, CancellationToken cancellationToken)
    {
        var libraries = request.CityId is { } cityId
            ? await network.Libraries.GetByCityAsync(cityId, cancellationToken)
            : await network.Libraries.GetAllActiveAsync(cancellationToken);

        // Home-library status comes from the city, so the cities are fetched once rather than per
        // library, which would be an N+1.
        var cityIds = libraries.Select(l => l.CityId).Distinct().ToArray();
        var cities = await network.Cities.GetByIdsAsync(cityIds, cancellationToken);
        var homeLibraryIds = cities
            .Where(c => c.HomeLibraryId is not null)
            .Select(c => c.HomeLibraryId!.Value)
            .ToHashSet();

        IReadOnlyList<LibraryDto> result =
        [
            .. libraries.Select(l => new LibraryDto(
                l.Id, l.CityId, l.Name, l.IsActive, homeLibraryIds.Contains(l.Id)))
        ];

        return Result.Success(result);
    }
}
