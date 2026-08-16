using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Network.Entities;

namespace Astrolabe.Domain.Features.Network.Repositories;

/// <summary>Persistence for <see cref="City"/>.</summary>
public interface ICityRepository : IRepository<City>
{
    Task<IReadOnlyList<City>> GetByCountryAsync(
        Guid countryId, CancellationToken cancellationToken = default);

    /// <summary>Cities of a country that hold at least one active library. See BR-NET-004.</summary>
    Task<IReadOnlyList<City>> GetRegisterableByCountryAsync(
        Guid countryId, CancellationToken cancellationToken = default);
}
