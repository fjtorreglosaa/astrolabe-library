using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Recommendations.Entities;

namespace Astrolabe.Domain.Features.Recommendations.Repositories;

public interface ILibraryAiConfigurationRepository : IRepository<LibraryAiConfiguration>
{
    Task<LibraryAiConfiguration?> GetByLibraryAsync(
        Guid libraryId, CancellationToken cancellationToken = default);

    /// <summary>The configurations for a named set of libraries, for the staff status panel.</summary>
    Task<IReadOnlyList<LibraryAiConfiguration>> GetByLibrariesAsync(
        IReadOnlyCollection<Guid> libraryIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// How many of the given libraries are connected. BR-REC-003 turns on this count and nothing
    /// else, so it is answered in the database rather than by loading configurations to count them.
    /// </summary>
    Task<int> CountConnectedAsync(
        IReadOnlyCollection<Guid> libraryIds, CancellationToken cancellationToken = default);
}
