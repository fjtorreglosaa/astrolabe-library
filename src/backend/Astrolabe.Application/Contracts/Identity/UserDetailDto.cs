using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Membership.Enums;

namespace Astrolabe.Application.Contracts.Identity;

/// <summary>
/// The user detail panel. Rows and statistics transcribed from the prototype: email, role, home
/// library, city, member since, last activity — then active reservations, outstanding fines,
/// purchases and on-time returns.
/// </summary>
/// <param name="LastActiveAt">
/// The most recent session activity, or null for an account that has never signed in. The prototype
/// renders that case as "Never".
/// </param>
/// <param name="OnTimeReturnPercent">
/// Null when the member has returned nothing yet — the prototype shows an em dash rather than 0%,
/// which would read as a bad record instead of no record.
/// </param>
/// <param name="AdministrationBlockedReason">
/// Why the actions are unavailable, or null when they are available. The prototype is explicit that
/// a disabled control must say why.
/// </param>
public sealed record UserDetailDto(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    PlanTier? Plan,
    UserStatus Status,
    string? CityName,
    string? HomeLibraryName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastActiveAt,
    int ActiveReservations,
    int OutstandingFineCents,
    int Purchases,
    int? OnTimeReturnPercent,
    bool CanAdminister,
    string? AdministrationBlockedReason);
