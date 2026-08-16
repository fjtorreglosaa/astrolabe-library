using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Network;
using Astrolabe.Domain.Features.Network.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Network.Queries.GetRegistrationCountries;

public sealed class GetRegistrationCountriesQueryHandler(INetworkUnitOfWork network)
    : IQueryHandler<GetRegistrationCountriesQuery, IReadOnlyList<CountryDto>>
{
    public async Task<Result<IReadOnlyList<CountryDto>>> Handle(
        GetRegistrationCountriesQuery request, CancellationToken cancellationToken)
    {
        // Availability is derived from the existence of an active library rather than read from a
        // flag, so BR-NET-004 holds even if seed data is later trimmed.
        var available = await network.Countries.GetAvailableForRegistrationAsync(cancellationToken);

        IReadOnlyList<CountryDto> result =
            [.. available.Select(c => new CountryDto(c.Id, c.Name, c.IsoCode))];

        return Result.Success(result);
    }
}
