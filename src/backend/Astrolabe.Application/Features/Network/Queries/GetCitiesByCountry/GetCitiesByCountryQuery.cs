using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Network;

namespace Astrolabe.Application.Features.Network.Queries.GetCitiesByCountry;

/// <summary>
/// Cities of a country that a member may register into. Implements BR-NET-004.
/// </summary>
public sealed record GetCitiesByCountryQuery(Guid CountryId) : IQuery<IReadOnlyList<CityDto>>;
