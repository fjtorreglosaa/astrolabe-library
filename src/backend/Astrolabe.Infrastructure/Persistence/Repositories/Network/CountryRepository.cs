using Astrolabe.Domain.Features.Network.Entities;
using Astrolabe.Domain.Features.Network.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Network;

public sealed class CountryRepository(AstrolabeDbContext context)
    : Repository<Country>(context), ICountryRepository
{
    public override async Task<IReadOnlyList<Country>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        await Query.OrderBy(c => c.Name).ToListAsync(cancellationToken);

    public async Task<Country?> GetByIsoCodeAsync(
        string isoCode, CancellationToken cancellationToken = default) =>
        await Query.FirstOrDefaultAsync(c => c.IsoCode == isoCode, cancellationToken);

    /// <summary>
    /// Implements BR-NET-004. Availability is derived from the existence of an active library rather
    /// than read from a flag, so the rule holds even if seed data is later trimmed.
    /// </summary>
    public async Task<IReadOnlyList<Country>> GetAvailableForRegistrationAsync(
        CancellationToken cancellationToken = default)
    {
        var countryIdsWithActiveLibraries = Context.Libraries
            .Where(l => l.IsActive)
            .Join(Context.Cities, l => l.CityId, c => c.Id, (l, c) => c.CountryId)
            .Distinct();

        return await ReadOnlyQuery
            .Where(c => !c.IsHiddenFromRegistration && countryIdsWithActiveLibraries.Contains(c.Id))
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }
}
