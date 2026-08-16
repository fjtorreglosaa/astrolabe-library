namespace Astrolabe.Domain.Features.Identity.ValueObjects;

/// <summary>
/// An opaque password hash.
///
/// It is a wrapper rather than a bare string so a password hash can never be passed where a
/// plaintext password is expected, and vice versa. <see cref="ToString"/> is overridden to redact:
/// BR-IDN-010 forbids a hash reaching a log, and structured loggers call ToString.
/// </summary>
public sealed class PasswordHash : IEquatable<PasswordHash>
{
    private PasswordHash(string value) => Value = value;

    /// <summary>The encoded hash, as produced by the hashing algorithm. Never a plaintext password.</summary>
    public string Value { get; }

    public static PasswordHash FromHashedValue(string hashedValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hashedValue);
        return new PasswordHash(hashedValue);
    }

    public bool Equals(PasswordHash? other) =>
        other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as PasswordHash);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    /// <summary>Redacted on purpose. A hash must never appear in a log or an error message.</summary>
    public override string ToString() => "[redacted]";
}
