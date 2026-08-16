namespace Astrolabe.Domain.Features.Identity.Enums;

/// <summary>
/// The columns the user directory can be ordered by. Mirrors the prototype's sortable headers:
/// User, Email, Role, Status, Library, Member since.
/// </summary>
public enum UserSortKey
{
    /// <summary>Newest first by default — a directory is read to find who just arrived.</summary>
    CreatedAt = 0,
    FullName = 1,
    Email = 2,
    Role = 3,
    Status = 4
}
