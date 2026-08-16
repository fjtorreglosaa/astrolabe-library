namespace Astrolabe.Domain.Features.Identity.Enums;

/// <summary>
/// The kind of device a session runs on. Display only: BR-IDN-022 forbids using any device
/// attribute to authorize a request.
/// </summary>
public enum DeviceType
{
    Unknown = 0,
    Web = 1,
    Mobile = 2,
    Tablet = 3,
    Desktop = 4
}
