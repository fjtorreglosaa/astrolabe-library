using Astrolabe.Domain.Features.Network.Entities;
using Astrolabe.Domain.Features.Network.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Network;

public sealed class LibraryAssignmentRepository(AstrolabeDbContext context)
    : Repository<LibraryAssignment>(context), ILibraryAssignmentRepository
{
    public async Task<IReadOnlyList<LibraryAssignment>> GetActiveByUserAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        await Query
            .Where(a => a.UserId == userId && a.RevokedAt == null)
            .ToListAsync(cancellationToken);

    public async Task<LibraryAssignment?> GetActiveAsync(
        Guid userId, Guid libraryId, CancellationToken cancellationToken = default) =>
        await Query.FirstOrDefaultAsync(
            a => a.UserId == userId && a.LibraryId == libraryId && a.RevokedAt == null,
            cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetActiveLibraryIdsByUserAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        await ReadOnlyQuery
            .Where(a => a.UserId == userId && a.RevokedAt == null)
            .Select(a => a.LibraryId)
            .ToListAsync(cancellationToken);
}
