using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Membership.Enums;

namespace Astrolabe.Application.Contracts.Identity;

/// <summary>
/// The signed-in user, as the shell needs them to render.
/// </summary>
/// <param name="Role">
/// What the user may do. Since GLOBAL-019 it says nothing about what they bought.
/// </param>
/// <param name="Plan">
/// The member's current plan, or null for staff, who hold none.
///
/// <para>
/// Carried here because the shell needs it to decide what to offer, and until GLOBAL-019 it got it
/// by reading the role — precisely the duplication that was removed. Resolved through
/// <c>IEntitlementProvider</c> rather than by reading a subscription, so this contract does not
/// become a second place that knows how a plan is stored.
/// </para>
/// </param>
public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    string FullName,
    UserRole Role,
    PlanTier? Plan,
    Guid? CountryId,
    Guid? CityId,
    bool IsStaff);
