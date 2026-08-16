using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.ValueObjects;

namespace Astrolabe.Infrastructure.Features.Identity;

/// <summary>
/// Turns a user agent into a readable device label, such as "Chrome on macOS".
///
/// <para>
/// Hand-written rather than delegated to a parsing library. The label is cosmetic: BR-IDN-022
/// forbids any device attribute from authorizing a request, so a wrong label is a display defect,
/// never a security hole. That does not justify a dependency plus a regularly updated regex
/// database.
/// </para>
/// </summary>
public sealed class UserAgentDeviceParser : IDeviceParser
{
    private static readonly (string Token, string Name)[] Browsers =
    [
        ("Edg/", "Edge"),
        ("OPR/", "Opera"),
        ("Chrome/", "Chrome"),
        ("Firefox/", "Firefox"),
        ("Safari/", "Safari")
    ];

    private static readonly (string Token, string Name)[] Platforms =
    [
        ("iPhone", "iOS"),
        ("iPad", "iPadOS"),
        ("Android", "Android"),
        ("Mac OS X", "macOS"),
        ("Macintosh", "macOS"),
        ("Windows", "Windows"),
        ("CrOS", "ChromeOS"),
        ("Linux", "Linux")
    ];

    public DeviceDescriptor Parse(string? userAgent, string? clientDeviceId)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return DeviceDescriptor.Create(null, DeviceType.Unknown, clientDeviceId);
        }

        var browser = Browsers.FirstOrDefault(
            b => userAgent.Contains(b.Token, StringComparison.OrdinalIgnoreCase)).Name;

        var platform = Platforms.FirstOrDefault(
            p => userAgent.Contains(p.Token, StringComparison.OrdinalIgnoreCase)).Name;

        var name = (browser, platform) switch
        {
            (not null, not null) => $"{browser} on {platform}",
            (not null, null) => browser,
            (null, not null) => platform,
            _ => null
        };

        return DeviceDescriptor.Create(name, DetectType(userAgent), clientDeviceId);
    }

    private static DeviceType DetectType(string userAgent)
    {
        if (userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase))
        {
            return DeviceType.Tablet;
        }

        if (userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
        {
            return DeviceType.Mobile;
        }

        // Anything else reaching the API over HTTP is a browser on a computer.
        return DeviceType.Web;
    }
}
