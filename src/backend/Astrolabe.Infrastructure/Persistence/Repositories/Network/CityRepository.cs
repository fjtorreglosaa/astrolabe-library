using Astrolabe.Domain.Features.Network.Entities;
using Astrolabe.Domain.Features.Network.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Network;

public sealed class CityRepository(AstrolabeDbContext context)
    : Repository<City>(context), ICityRepository
{
    public async Task<IReadOnlyList<City>> GetByCountryAsync(
        Guid countryId, CancellationToken cancellationToken = default) =>
        await ReadOnlyQuery
            .Where(c => c.CountryId == countryId)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<City>> GetRegisterableByCountryAsync(
        Guid countryId, CancellationToken cancellationToken = default)
    {
        var cityIdsWithActiveLibraries = Context.Libraries
            .Where(l => l.IsActive)
            .Select(l => l.CityId)
            .Distinct();

        return await ReadOnlyQuery
            .Where(c => c.CountryId == countryId && cityIdsWithActiveLibraries.Contains(c.Id))
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }
}
