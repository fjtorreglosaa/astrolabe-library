using Astrolabe.Application.Shared.Catalog;

namespace Astrolabe.Application.Abstractions.Network;

/// <summary>
/// Resolves where each library sits. The network owns the geography; other domains ask for it here
/// rather than joining to it themselves.
/// </summary>
public interface ILibraryLocationProvider
{
    /// <summary>
    /// Every library, keyed by identifier.
    ///
    /// The whole set rather than a requested subset, and memoised for the request: the network holds
    /// tens of libraries, not thousands, so one query answers a page of twenty books where a lookup
    /// per book would be twenty round trips.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, BookProjection.LibraryLocation>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Each city's designated home library, keyed by city. BR-NET-003 guarantees exactly one.
    ///
    /// Here rather than on <c>LibraryLocation</c> because it is a fact about a city, not about a
    /// library, and hanging it off every library row would repeat it once per branch.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, Guid>> GetHomeLibraryByCityAsync(
        CancellationToken cancellationToken = default);
}
