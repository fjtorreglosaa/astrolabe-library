namespace Astrolabe.Domain.Features.Identity.Enums;

/// <summary>
/// A user's role. For a member the role <em>is</em> their plan, which is why the three plan tiers
/// appear here rather than as a separate concept. See global_spec.md section 2.
/// </summary>
public enum UserRole
{
    Basic = 0,
    Plus = 1,
    Max = 2,
    Admin = 10,
    SuperAdmin = 20
}

public static class UserRoleExtensions
{
    public static bool IsMember(this UserRole role) =>
        role is UserRole.Basic or UserRole.Plus or UserRole.Max;

    public static bool IsStaff(this UserRole role) =>
        role is UserRole.Admin or UserRole.SuperAdmin;

    public static bool IsSuperAdmin(this UserRole role) => role is UserRole.SuperAdmin;
}
