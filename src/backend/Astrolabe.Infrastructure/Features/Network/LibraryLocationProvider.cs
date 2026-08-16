using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Shared.Catalog;
using Astrolabe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Features.Network;

/// <summary>
/// Reads every library with its city in one join, and memoises the result for the request.
///
/// Inactive branches are returned rather than filtered out, because callers need to tell "withdrawn"
/// apart from "unknown": a withdrawn branch is hidden from members, while an unrecognised identifier
/// is a data fault that must not silently shrink a book's holdings.
///
/// Scoped, like every other provider here: the geography cannot change mid-request, and a longer
/// cache would let a newly created library go unseen until a restart.
/// </summary>
public sealed class LibraryLocationProvider(AstrolabeDbContext context) : ILibraryLocationProvider
{
    private IReadOnlyDictionary<Guid, BookProjection.LibraryLocation>? _memoised;
    private IReadOnlyDictionary<Guid, Guid>? _memoisedHomeLibraries;

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
                    library.Id, library.Name, city.Id, city.Name, library.IsActive))
            .ToListAsync(cancellationToken);

        _memoised = rows.ToDictionary(row => row.LibraryId);

        return _memoised;
    }

    public async Task<IReadOnlyDictionary<Guid, Guid>> GetHomeLibraryByCityAsync(
        CancellationToken cancellationToken = default)
    {
        if (_memoisedHomeLibraries is not null)
        {
            return _memoisedHomeLibraries;
        }

        // A city without a designated home library is a BR-NET-003 violation rather than a normal
        // state, but it is skipped instead of throwing: a directory listing is not the place to
        // discover it, and a null would only crash the screen that reports the problem.
        var rows = await context.Cities
            .AsNoTracking()
            .Where(city => city.HomeLibraryId != null)
            .Select(city => new { city.Id, HomeLibraryId = city.HomeLibraryId!.Value })
            .ToListAsync(cancellationToken);

        _memoisedHomeLibraries = rows.ToDictionary(row => row.Id, row => row.HomeLibraryId);

        return _memoisedHomeLibraries;
    }
}
