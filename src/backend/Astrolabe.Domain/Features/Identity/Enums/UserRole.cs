namespace Astrolabe.Domain.Features.Identity.Enums;

/// <summary>
/// What authority a user has in the system.
///
/// <para>
/// <b>A role is not a plan.</b> The three plan tiers used to live here, so a member's role doubled
/// as their subscription — one fact with two representations, which is a defect waiting for the day
/// they disagree. <c>Subscription.Plan</c> is the sole authority on what a member has bought, and
/// anything that needs to know asks <c>IEntitlementProvider</c>.
/// </para>
/// <para>
/// This enumeration answers a different question: what a user is allowed to *do* to the system.
/// A member borrows and buys; an administrator manages the libraries assigned to them; a super
/// administrator manages the network. None of that changes when somebody upgrades their plan, which
/// is exactly why the two do not belong in one type.
/// </para>
/// </summary>
public enum UserRole
{
    /// <summary>Somebody who borrows and buys. What they may reach is their plan's business.</summary>
    Member = 0,

    /// <summary>Staff of the libraries explicitly assigned to them. See BR-NET-006.</summary>
    Admin = 10,

    /// <summary>Unrestricted across the network. See BR-NET-007.</summary>
    SuperAdmin = 20
}

public static class UserRoleExtensions
{
    public static bool IsMember(this UserRole role) => role is UserRole.Member;

    public static bool IsStaff(this UserRole role) =>
        role is UserRole.Admin or UserRole.SuperAdmin;

    public static bool IsSuperAdmin(this UserRole role) => role is UserRole.SuperAdmin;
}
