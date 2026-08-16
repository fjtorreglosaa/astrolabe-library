using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Network.Entities;

namespace Astrolabe.Domain.Features.Network.Repositories;

/// <summary>
/// Persistence for <see cref="LibraryAssignment"/>. Assignments are revoked, never deleted, so every
/// method here is explicit about wanting only the active ones.
/// </summary>
public interface ILibraryAssignmentRepository : IRepository<LibraryAssignment>
{
    Task<IReadOnlyList<LibraryAssignment>> GetActiveByUserAsync(
        Guid userId, CancellationToken cancellationToken = default);

    Task<LibraryAssignment?> GetActiveAsync(
        Guid userId, Guid libraryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Projects to identifiers only. Runs once per staff request to build the caller's library
    /// scope, so it must not materialise entities it will not use.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetActiveLibraryIdsByUserAsync(
        Guid userId, CancellationToken cancellationToken = default);
}
