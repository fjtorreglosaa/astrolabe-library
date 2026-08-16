using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Network;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Features.Network.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Network.Queries.GetCitiesByCountry;

public sealed class GetCitiesByCountryQueryHandler(INetworkUnitOfWork network) : IQueryHandler<GetCitiesByCountryQuery, IReadOnlyList<CityDto>>
{
    public async Task<Result<IReadOnlyList<CityDto>>> Handle(
        GetCitiesByCountryQuery request, CancellationToken cancellationToken)
    {
        // Distinguishes "no such country" from "a country with no registerable cities". The second
        // is a valid empty list; the first is a bad request.
        if (!await network.Countries.ExistsAsync(request.CountryId, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<CityDto>>(NetworkErrors.CountryNotFound);
        }

        var registerable = await network.Cities.GetRegisterableByCountryAsync(
            request.CountryId, cancellationToken);

        IReadOnlyList<CityDto> result =
            [.. registerable.Select(c => new CityDto(c.Id, c.CountryId, c.Name, c.HomeLibraryId))];

        return Result.Success(result);
    }
}
