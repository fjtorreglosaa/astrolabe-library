using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Network.Repositories;
using Astrolabe.Domain.Features.Network.ValueObjects;

namespace Astrolabe.Infrastructure.Features.Network;

/// <summary>
/// Resolves a staff user's library scope. Implements BR-NET-006, BR-NET-007 and BR-NET-010.
///
/// <para>
/// Registered as <b>scoped</b>, and the memoised value therefore lives exactly as long as the
/// request. That is deliberate: BR-NET-011 requires a revoked assignment to take effect on the very
/// next request, so any cache outliving the request would contradict the rule. Within one request
/// the scope cannot change, so memoising it there is free correctness.
/// </para>
/// </summary>
public sealed class LibraryScopeProvider(
    ICurrentUser currentUser,
    ILibraryAssignmentRepository assignments) : ILibraryScopeProvider
{
    private LibraryScope? _memoisedCurrentScope;

    public async Task<LibraryScope> GetCurrentScopeAsync(CancellationToken cancellationToken = default)
    {
        if (_memoisedCurrentScope is not null)
        {
            return _memoisedCurrentScope;
        }

        // An anonymous caller has no scope. Returning Empty rather than throwing keeps the caller
        // free of null checks: "covers nothing" is the correct answer, not an error.
        if (currentUser is not { IsAuthenticated: true, UserId: { } userId, Role: { } role })
        {
            _memoisedCurrentScope = LibraryScope.Empty();
            return _memoisedCurrentScope;
        }

        _memoisedCurrentScope = await GetScopeForAsync(userId, role, cancellationToken);
        return _memoisedCurrentScope;
    }

    public async Task<LibraryScope> GetScopeForAsync(
        Guid userId, UserRole role, CancellationToken cancellationToken = default)
    {
        // BR-NET-007: a super administrator never requires an assignment, so no query is needed.
        if (role.IsSuperAdmin())
        {
            return LibraryScope.Unrestricted();
        }

        // A member has no staff authority at all. Querying assignments for one would be a wasted
        // round trip that could only ever return nothing.
        if (!role.IsStaff())
        {
            return LibraryScope.Empty();
        }

        var libraryIds = await assignments.GetActiveLibraryIdsByUserAsync(userId, cancellationToken);

        // BR-NET-010: an administrator with no assignments is a valid state that sees empty lists.
        return libraryIds.Count == 0
            ? LibraryScope.Empty()
            : LibraryScope.Of(libraryIds);
    }
}
