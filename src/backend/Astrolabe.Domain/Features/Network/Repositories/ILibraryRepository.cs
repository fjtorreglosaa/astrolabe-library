using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Network.Entities;

namespace Astrolabe.Domain.Features.Network.Repositories;

/// <summary>Persistence for <see cref="Library"/>.</summary>
public interface ILibraryRepository : IRepository<Library>
{
    Task<IReadOnlyList<Library>> GetByCityAsync(Guid cityId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Library>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Backs BR-NET-002. The unique index is the real guard; this gives a clean error first.</summary>
    Task<bool> ExistsWithNameInCityAsync(
        Guid cityId, string name, CancellationToken cancellationToken = default);
}
