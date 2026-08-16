namespace Astrolabe.Application.Abstractions.Network;

/// <summary>
/// Answers whether a library still holds anything that must be settled before it can be deactivated:
/// copies, active reservations, or unresolved fines. Backs BR-NET-005.
///
/// <para>
/// It is a seam rather than a direct query because the facts live in <c>catalog</c>,
/// <c>reservations</c> and <c>billing</c>. <c>network</c> must not reach into those domains, and
/// they do not exist yet, so the seam lets BR-NET-005 be implemented and tested now and wired to
/// real data as each domain lands.
/// </para>
/// </summary>
public interface ILibraryObligationsProbe
{
    Task<bool> HasOpenObligationsAsync(Guid libraryId, CancellationToken cancellationToken = default);
}
