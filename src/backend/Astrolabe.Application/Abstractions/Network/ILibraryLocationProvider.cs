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
}
