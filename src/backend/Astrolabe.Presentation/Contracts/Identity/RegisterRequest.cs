using Astrolabe.Domain.Features.Membership.Enums;

namespace Astrolabe.Presentation.Contracts.Identity;

/// <summary>
/// Public registration.
/// </summary>
/// <param name="Plan">
/// A <see cref="PlanTier"/> and not a role. What a visitor buys and what authority they hold are
/// different questions, and binding a role from an anonymous request body is how the second one ends
/// up answered by the caller.
/// </param>
public sealed record RegisterRequest(
    string Email, string Password, string FullName, Guid CountryId, Guid CityId, PlanTier Plan);
