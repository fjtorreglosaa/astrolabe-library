using Astrolabe.Domain.Features.Network.Entities;
using Astrolabe.Domain.Features.Network.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Network;

public sealed class LibraryRepository(AstrolabeDbContext context)
    : Repository<Library>(context), ILibraryRepository
{
    public async Task<IReadOnlyList<Library>> GetByCityAsync(
        Guid cityId, CancellationToken cancellationToken = default) =>
        await ReadOnlyQuery
            .Where(l => l.CityId == cityId)
            .OrderBy(l => l.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Library>> GetAllActiveAsync(
        CancellationToken cancellationToken = default) =>
        await ReadOnlyQuery
            .Where(l => l.IsActive)
            .OrderBy(l => l.Name)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsWithNameInCityAsync(
        Guid cityId, string name, CancellationToken cancellationToken = default) =>
        await ReadOnlyQuery.AnyAsync(l => l.CityId == cityId && l.Name == name, cancellationToken);
}
