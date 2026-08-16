using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Network.Entities;

namespace Astrolabe.Domain.Features.Network.Repositories;

/// <summary>
/// Persistence for <see cref="Country"/>. Generic operations come from
/// <see cref="IRepository{TEntity}"/>; only country-specific capabilities are declared here.
/// </summary>
public interface ICountryRepository : IRepository<Country>
{
    Task<Country?> GetByIsoCodeAsync(string isoCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Countries that can be offered at registration: not hidden, and holding at least one city with
    /// at least one active library. Derived rather than flag-driven, so BR-NET-004 cannot be
    /// violated by misconfiguration.
    /// </summary>
    Task<IReadOnlyList<Country>> GetAvailableForRegistrationAsync(
        CancellationToken cancellationToken = default);
}
