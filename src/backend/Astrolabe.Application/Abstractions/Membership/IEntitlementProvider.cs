using Astrolabe.Domain.Features.Membership.ValueObjects;

namespace Astrolabe.Application.Abstractions.Membership;

/// <summary>
/// Resolves what a member's plan entitles them to. The single entry point for the plan table; no
/// other domain reads a subscription directly.
/// </summary>
public interface IEntitlementProvider
{
    /// <summary>
    /// The calling member's entitlement. Returns <see cref="MemberEntitlement.None"/> for an
    /// anonymous caller or for staff, so a caller never has to null-check before asking what is
    /// covered.
    /// </summary>
    Task<MemberEntitlement> GetForCurrentMemberAsync(CancellationToken cancellationToken = default);

    /// <summary>A named member's entitlement, for the rare case of acting on someone else's behalf.</summary>
    Task<MemberEntitlement> GetForMemberAsync(
        Guid memberId, CancellationToken cancellationToken = default);
}
