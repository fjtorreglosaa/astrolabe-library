using Astrolabe.Domain.Features.Identity.Enums;

namespace Astrolabe.Application.Contracts.Identity;

/// <summary>
/// A session as shown on the devices screen.
///
/// Carries no token material of any kind — the screen needs to identify a device, not to act as one.
/// </summary>
public sealed record SessionDto(
    Guid Id,
    string DeviceName,
    DeviceType DeviceType,
    string IpAddress,
    string? ApproximateLocation,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastSeenAt,
    DateTimeOffset ExpiresAt,
    /// <summary>True for the session the request came from, so the interface can mark "this device" (BR-IDN-026).</summary>
    bool IsCurrent);
