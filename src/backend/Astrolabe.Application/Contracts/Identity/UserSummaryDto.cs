using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Membership.Enums;

namespace Astrolabe.Application.Contracts.Identity;

/// <summary>
/// One row of the user directory. Columns transcribed from the prototype: User, Email, Role,
/// Status, Library, Member since.
/// </summary>
/// <param name="Plan">
/// Null for staff, who hold none. A separate field from <paramref name="Role"/> since GLOBAL-019 —
/// the prototype showed one column because a member's role *was* their plan, and it no longer is.
/// </param>
/// <param name="HomeLibraryName">The branch a member borrows from, or null for staff.</param>
/// <param name="CanAdminister">
/// Whether the caller may act on this row. Decided server-side so the screen cannot offer a button
/// the API would refuse; the handler checks again before acting.
/// </param>
public sealed record UserSummaryDto(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    PlanTier? Plan,
    UserStatus Status,
    string? CityName,
    string? HomeLibraryName,
    DateTimeOffset CreatedAt,
    bool CanAdminister);
