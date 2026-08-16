using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Network.ValueObjects;

namespace Astrolabe.Application.Abstractions.Network;

/// <summary>
/// Resolves which libraries a staff user may act on. The single entry point for BR-NET-006 and
/// BR-NET-007; no other domain queries assignments directly.
/// </summary>
public interface ILibraryScopeProvider
{
    /// <summary>
    /// The calling user's scope. Returns <see cref="LibraryScope.Empty"/> for an anonymous request
    /// or a member, so a caller never has to null-check before asking whether a library is covered.
    /// </summary>
    Task<LibraryScope> GetCurrentScopeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// A named user's scope. Used when acting on behalf of someone else, for example when a super
    /// administrator reviews what an administrator can reach.
    /// </summary>
    Task<LibraryScope> GetScopeForAsync(
        Guid userId, UserRole role, CancellationToken cancellationToken = default);
}
