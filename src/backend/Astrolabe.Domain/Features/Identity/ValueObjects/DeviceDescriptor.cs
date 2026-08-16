using Astrolabe.Domain.Features.Identity.Enums;

namespace Astrolabe.Domain.Features.Identity.ValueObjects;

/// <summary>
/// How a session is labelled in the sessions screen, for example "Chrome on macOS".
///
/// <para>
/// Purely cosmetic. BR-IDN-022 forbids any device attribute from authorizing a request, which is
/// why precision has no security value here and no user-agent parsing library is warranted. A
/// wrong label is a display defect, never a security hole.
/// </para>
/// </summary>
public sealed record DeviceDescriptor
{
    public const int MaxNameLength = 120;

    /// <summary>Shown when the user agent is absent or unrecognised.</summary>
    public static readonly DeviceDescriptor Unknown = new("Unknown device", DeviceType.Unknown, null);

    private DeviceDescriptor(string name, DeviceType type, string? clientDeviceId)
    {
        Name = name;
        Type = type;
        ClientDeviceId = clientDeviceId;
    }

    /// <summary>Human-readable label.</summary>
    public string Name { get; }

    public DeviceType Type { get; }

    /// <summary>
    /// An identifier the client generates and keeps locally. It groups sessions in the interface and
    /// nothing else — <b>never</b> treat it as a credential (BR-IDN-022).
    /// </summary>
    public string? ClientDeviceId { get; }

    public static DeviceDescriptor Create(string? name, DeviceType type, string? clientDeviceId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return clientDeviceId is null
                ? Unknown
                : new DeviceDescriptor(Unknown.Name, type, Normalise(clientDeviceId));
        }

        var trimmed = name.Trim();

        if (trimmed.Length > MaxNameLength)
        {
            trimmed = trimmed[..MaxNameLength];
        }

        return new DeviceDescriptor(trimmed, type, Normalise(clientDeviceId));
    }

    private static string? Normalise(string? clientDeviceId) =>
        string.IsNullOrWhiteSpace(clientDeviceId) ? null : clientDeviceId.Trim();

    public override string ToString() => Name;
}
