using Astrolabe.Domain.Features.Identity.Enums;

namespace Astrolabe.Application.Contracts.Network;

/// <summary>A staff user as shown on the Libraries and admins screen.</summary>
public sealed record AdminDto(
    Guid Id,
    string Email,
    string FullName,
    UserRole Role,
    UserStatus Status,
    IReadOnlyList<string> Libraries,
    DateTimeOffset Since);
