namespace Astrolabe.Application.Contracts.Catalog;

/// <summary>
/// A review as the detail panel renders it. Implements BR-CAT-029: attributed with the member's name
/// and initials, which the catalogue shows as an avatar.
/// </summary>
public sealed record ReviewDto(
    Guid Id,
    Guid MemberId,
    string MemberName,
    string Initials,
    int Rating,
    string? Comment,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EditedAt,
    bool IsMine);
