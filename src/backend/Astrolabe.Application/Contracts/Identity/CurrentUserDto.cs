using Astrolabe.Domain.Features.Identity.Enums;

namespace Astrolabe.Application.Contracts.Identity;

/// <summary>The signed-in user, as the shell needs them to render.</summary>
public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    string FullName,
    UserRole Role,
    Guid? CountryId,
    Guid? CityId,
    bool IsStaff);
