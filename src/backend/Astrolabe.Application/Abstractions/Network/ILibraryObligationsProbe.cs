using Astrolabe.Application.Contracts.Network;

namespace Astrolabe.Application.Abstractions.Network;

/// <summary>
/// Reports what a library still holds: copies, active reservations, and unresolved fines.
/// Backs the reporting half of BR-NET-005.
///
/// <para>
/// A seam rather than a direct query because the facts live in <c>catalog</c>, <c>reservations</c>
/// and <c>billing</c>, and <c>network</c> must not reach into those domains.
/// </para>
/// </summary>
public interface ILibraryObligationsProbe
{
    Task<LibraryObligations> GetAsync(Guid libraryId, CancellationToken cancellationToken = default);
}
