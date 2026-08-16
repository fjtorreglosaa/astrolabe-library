using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Membership.Entities;
using Astrolabe.Domain.Features.Membership.Enums;

namespace Astrolabe.Domain.Features.Membership.Repositories;

/// <summary>Persistence for <see cref="Subscription"/>.</summary>
public interface ISubscriptionRepository : IRepository<Subscription>
{
    /// <summary>
    /// The member's current subscription, or null when they have none. A member has at most one
    /// active subscription at a time, which is why this returns a single entity rather than a list.
    /// </summary>
    Task<Subscription?> GetActiveForMemberAsync(
        Guid memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The active plan of each named member, for a listing.
    ///
    /// One query rather than a call per row: the user directory shows a plan on every line, and
    /// asking per member would be twenty round trips for a page of twenty. Members with no
    /// subscription are simply absent from the result rather than defaulted, so the caller can tell
    /// "no subscription" from "Basic".
    /// </summary>
    Task<IReadOnlyDictionary<Guid, PlanTier>> GetActivePlansForAsync(
        IReadOnlyCollection<Guid> memberIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Active subscriptions whose renewal date has passed, for the sweep that applies scheduled
    /// changes to members who never sign in. See BR-MBR-021.
    /// </summary>
    Task<IReadOnlyList<Subscription>> GetDueForRenewalAsync(
        DateTimeOffset asOf, int maxCount, CancellationToken cancellationToken = default);
}
