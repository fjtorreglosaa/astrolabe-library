using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Membership.Enums;

namespace Astrolabe.Domain.Features.Identity.Events;

/// <summary>
/// Registration succeeded. Triggers the verification email and opens the subscription.
///
/// <para>
/// Carries the <see cref="Plan"/> the member chose, because <c>identity</c> no longer stores it —
/// the role and the plan were one field until <c>GLOBAL-019</c>, and separating them means the
/// choice has to travel to the domain that owns it.
/// </para>
/// Carries identifiers and values only, never entity references.
/// </summary>
public sealed record UserRegistered(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid UserId, string Email, string FullName, PlanTier Plan) : IDomainEvent;
