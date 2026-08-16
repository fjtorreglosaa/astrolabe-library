using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Network.Errors;

/// <summary>
/// Reusable, strongly typed errors for the network domain. Declared once so no call site
/// invents a magic string. See GUIDELINES.md section 18.
/// </summary>
public static class NetworkErrors
{
    public static readonly Error SuperAdminRequired =
        Error.Authorization("network.super_admin_required",
            "Only a super administrator may perform this operation.");

    /// <summary>
    /// Backs BR-NET-006. Worded without naming the library: an administrator who may not act on a
    /// branch has no business learning it exists from an error message.
    /// </summary>
    public static readonly Error LibraryOutOfScope =
        Error.Authorization("network.library_out_of_scope",
            "You can only act on the libraries assigned to you.");

    public static readonly Error StaffRequired =
        Error.Authorization("network.staff_required",
            "Only library staff may perform this operation.");

    public static readonly Error CityNotFound =
        Error.NotFound("network.city_not_found", "City not found.");

    public static readonly Error LibraryNotFound =
        Error.NotFound("network.library_not_found", "Library not found.");

    public static readonly Error CountryNotFound =
        Error.NotFound("network.country_not_found", "Country not found.");

    public static readonly Error LibraryNameTakenInCity =
        Error.Conflict("network.library_name_taken_in_city",
            "A library with this name already exists in this city.");

    public static readonly Error InvitedAddressAlreadyInUse =
        Error.Conflict("network.invited_address_already_in_use",
            "An account already exists for this email address.");

    public static readonly Error AdminNotFound =
        Error.NotFound("network.admin_not_found", "Administrator not found.");

    public static readonly Error CannotRevokeYourself =
        Error.Conflict("network.cannot_revoke_yourself",
            "A super administrator cannot revoke their own role.");

    public static readonly Error NotAStaffAccount =
        Error.Validation("network.not_a_staff_account",
            "Libraries can only be assigned to a staff account.");

    public static readonly Error CountryNameRequired =
        Error.Validation("network.country_name_required", "A country must have a name.");

    public static readonly Error CountryIsoCodeInvalid =
        Error.Validation("network.country_iso_code_invalid", "A country ISO code must be two letters.");

    public static readonly Error CityNameRequired =
        Error.Validation("network.city_name_required", "A city must have a name.");

    public static readonly Error LibraryNameRequired =
        Error.Validation("network.library_name_required", "A library must have a name.");

    public static readonly Error HomeLibraryNotInCity =
        Error.Domain("network.home_library_not_in_city",
            "A city's home library must be one of its own libraries.");

    public static readonly Error HomeLibraryInactive =
        Error.Domain("network.home_library_inactive",
            "An inactive library cannot be a city's home library.");

    public static readonly Error CannotDeactivateHomeLibrary =
        Error.Conflict("network.cannot_deactivate_home_library",
            "Designate another home library for this city first.");

    public static readonly Error LibraryHasOpenObligations =
        Error.Conflict("network.library_has_open_obligations",
            "This library still holds copies, active reservations or unresolved fines.");

    public static readonly Error LibraryAlreadyInactive =
        Error.Conflict("network.library_already_inactive", "This library is already inactive.");

    public static readonly Error AssignmentAlreadyRevoked =
        Error.Conflict("network.assignment_already_revoked", "This assignment is already revoked.");

    public static readonly Error InvitationAlreadyAccepted =
        Error.Conflict("network.invitation_already_accepted", "This invitation was already accepted.");

    public static readonly Error InvitationRevoked =
        Error.Conflict("network.invitation_revoked", "This invitation was revoked.");

    public static readonly Error InvitationExpired =
        Error.Conflict("network.invitation_expired", "This invitation has expired.");

    public static readonly Error InvitationRoleInvalid =
        Error.Validation("network.invitation_role_invalid",
            "An invitation must grant a staff role.");

    public static readonly Error InvitationLibrariesRequired =
        Error.Validation("network.invitation_libraries_required",
            "An administrator invitation must name at least one library.");
}
