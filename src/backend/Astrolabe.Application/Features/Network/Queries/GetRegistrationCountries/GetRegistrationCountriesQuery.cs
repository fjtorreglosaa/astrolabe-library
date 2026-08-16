using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Network;

namespace Astrolabe.Application.Features.Network.Queries.GetRegistrationCountries;

/// <summary>
/// Countries the registration form may offer. Implements BR-NET-004.
/// </summary>
public sealed record GetRegistrationCountriesQuery : IQuery<IReadOnlyList<CountryDto>>;
