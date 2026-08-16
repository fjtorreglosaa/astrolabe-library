using Astrolabe.Application.Abstractions.Network;
using Microsoft.Extensions.Logging;

namespace Astrolabe.Infrastructure.Features.Network;

/// <summary>
/// Placeholder for BR-NET-005's obligations check.
///
/// <para>
/// <b>This always answers "no obligations".</b> Copies live in <c>catalog</c>, active reservations in
/// <c>reservations</c>, and unresolved fines in <c>billing</c> — none of which exist yet. Until they
/// do, deactivating a library cannot be blocked by outstanding work, which means BR-NET-005 is only
/// half enforced: the home-library guard works, the obligations guard does not.
/// </para>
///
/// <para>
/// It logs a warning on every call so the gap is visible in any environment that exercises it, and
/// it must be replaced by <c>NET-025</c> before the MVP is accepted.
/// </para>
/// </summary>
public sealed class UnimplementedLibraryObligationsProbe(
    ILogger<UnimplementedLibraryObligationsProbe> logger) : ILibraryObligationsProbe
{
    public Task<bool> HasOpenObligationsAsync(
        Guid libraryId, CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "Library {LibraryId} was checked for open obligations, but catalog, reservations and "
            + "billing are not implemented yet. Answering 'none'. See NET-025.",
            libraryId);

        return Task.FromResult(false);
    }
}
