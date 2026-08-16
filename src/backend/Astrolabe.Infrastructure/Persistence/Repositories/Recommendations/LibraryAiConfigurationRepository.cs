using Astrolabe.Domain.Features.Recommendations.Entities;
using Astrolabe.Domain.Features.Recommendations.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Recommendations;

public sealed class LibraryAiConfigurationRepository(AstrolabeDbContext context)
    : Repository<LibraryAiConfiguration>(context), ILibraryAiConfigurationRepository
{
    public async Task<LibraryAiConfiguration?> GetByLibraryAsync(
        Guid libraryId, CancellationToken cancellationToken = default) =>
        await Query.FirstOrDefaultAsync(c => c.LibraryId == libraryId, cancellationToken);

    public async Task<IReadOnlyList<LibraryAiConfiguration>> GetByLibrariesAsync(
        IReadOnlyCollection<Guid> libraryIds, CancellationToken cancellationToken = default)
    {
        if (libraryIds.Count == 0)
        {
            return [];
        }

        // Tracked, not read-only: the generator marks a configuration failed when a vendor refuses,
        // and an untracked entity would let that correction disappear silently.
        return await Query
            .Where(c => libraryIds.Contains(c.LibraryId))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountConnectedAsync(
        IReadOnlyCollection<Guid> libraryIds, CancellationToken cancellationToken = default)
    {
        if (libraryIds.Count == 0)
        {
            return 0;
        }

        // Counted in the database. BR-REC-003 turns on this number alone, and loading whole
        // configurations to count them would pull every ciphertext across the wire to do it.
        return await ReadOnlyQuery.CountAsync(
            c => libraryIds.Contains(c.LibraryId) && c.IsEnabled && c.IsVerified,
            cancellationToken);
    }
}
