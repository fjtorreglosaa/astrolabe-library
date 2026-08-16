using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Shared.Catalog;
using Astrolabe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Features.Network;

/// <summary>
/// Reads every library with its city in one join, and memoises the result for the request.
///
/// Scoped, like every other provider here: the geography cannot change mid-request, and a longer
/// cache would let a newly created library go unseen until a restart.
/// </summary>
public sealed class LibraryLocationProvider(AstrolabeDbContext context) : ILibraryLocationProvider
{
    private IReadOnlyDictionary<Guid, BookProjection.LibraryLocation>? _memoised;

    public async Task<IReadOnlyDictionary<Guid, BookProjection.LibraryLocation>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        if (_memoised is not null)
        {
            return _memoised;
        }

        var rows = await context.Libraries
            .AsNoTracking()
            .Join(context.Cities, library => library.CityId, city => city.Id,
                (library, city) => new BookProjection.LibraryLocation(
                    library.Id, library.Name, city.Id, city.Name))
            .ToListAsync(cancellationToken);

        _memoised = rows.ToDictionary(row => row.LibraryId);

        return _memoised;
    }
}
