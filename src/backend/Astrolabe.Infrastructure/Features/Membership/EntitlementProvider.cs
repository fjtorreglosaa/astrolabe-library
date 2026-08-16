using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Membership;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Membership.Repositories;
using Astrolabe.Domain.Features.Membership.ValueObjects;
using Astrolabe.Domain.Features.Network.Repositories;

namespace Astrolabe.Infrastructure.Features.Membership;

/// <summary>
/// Resolves a member's entitlement from their subscription and their geography. Implements
/// BR-MBR-010 and the read half of BR-MBR-021.
///
/// <para>
/// Registered as <b>scoped</b>, and the memoised value therefore lives exactly as long as the
/// request. A plan cannot change mid-request, so memoising is free; a longer cache would let a
/// just-applied upgrade go unnoticed, which is the same reasoning as <c>LibraryScopeProvider</c>.
/// </para>
/// </summary>
public sealed class EntitlementProvider(
    ICurrentUser currentUser,
    IMembershipUnitOfWork membership,
    IUserRepository users,
    ICityRepository cities,
    IDateTimeProvider clock) : IEntitlementProvider
{
    private MemberEntitlement? _memoisedCurrent;

    public async Task<MemberEntitlement> GetForCurrentMemberAsync(
        CancellationToken cancellationToken = default)
    {
        if (_memoisedCurrent is not null)
        {
            return _memoisedCurrent;
        }

        // Anonymous callers and staff have no membership. "Entitled to nothing" is the correct
        // answer rather than an error, so callers stay free of null checks.
        if (currentUser is not { IsAuthenticated: true, UserId: { } userId })
        {
            _memoisedCurrent = MemberEntitlement.None;
            return _memoisedCurrent;
        }

        _memoisedCurrent = await GetForMemberAsync(userId, cancellationToken);
        return _memoisedCurrent;
    }

    public async Task<MemberEntitlement> GetForMemberAsync(
        Guid memberId, CancellationToken cancellationToken = default)
    {
        var subscription = await membership.Subscriptions
            .GetActiveForMemberAsync(memberId, cancellationToken);

        if (subscription is null)
        {
            return MemberEntitlement.None;
        }

        // BR-MBR-021: a scheduled change lands the moment its date passes. Applying it on read means
        // a member returning after their renewal sees the right plan at once, without waiting for
        // the sweep. The call is idempotent, so doing it here and in the job is safe.
        var applied = subscription.ApplyDueChange(clock.UtcNow);

        if (applied.IsSuccess && applied.Value is not null)
        {
            await membership.SaveChangesAsync(cancellationToken);
        }

        var user = await users.GetByIdAsync(memberId, cancellationToken);
        var homeLibraryId = await ResolveHomeLibraryAsync(user?.CityId, cancellationToken);

        return PlanCatalog.EntitlementFor(subscription.Plan, user?.CityId, homeLibraryId);
    }

    /// <summary>
    /// BR-MBR-010: the home library is the city's designated one, never chosen by the member.
    /// Resolved here rather than stored on the user, so redesignating a city's home library moves
    /// every Basic member in it without a data migration.
    /// </summary>
    private async Task<Guid?> ResolveHomeLibraryAsync(
        Guid? cityId, CancellationToken cancellationToken)
    {
        if (cityId is not { } id)
        {
            return null;
        }

        var city = await cities.GetByIdAsync(id, cancellationToken);
        return city?.HomeLibraryId;
    }
}
