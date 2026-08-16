using Astrolabe.Domain.Features.Identity.ValueObjects;

namespace Astrolabe.Application.Abstractions.Identity;

/// <summary>
/// Derives the device label shown on the sessions screen. Display only — BR-IDN-022 forbids using
/// any device attribute to authorize a request.
/// </summary>
public interface IDeviceParser
{
    DeviceDescriptor Parse(string? userAgent, string? clientDeviceId);
}
