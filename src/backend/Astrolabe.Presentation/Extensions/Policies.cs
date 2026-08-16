namespace Astrolabe.Presentation.Extensions;

/// <summary>Authorization policy names, declared once so no controller invents a string.</summary>
public static class Policies
{
    public const string MemberOnly = "MemberOnly";
    public const string StaffOnly = "StaffOnly";
    public const string SuperAdminOnly = "SuperAdminOnly";
}
