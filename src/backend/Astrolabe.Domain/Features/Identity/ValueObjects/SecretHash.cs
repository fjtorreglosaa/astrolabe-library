using System.Security.Cryptography;
using System.Text;

namespace Astrolabe.Domain.Features.Identity.ValueObjects;

/// <summary>
/// The SHA-256 of a high-entropy secret: a refresh token, a verification token, or a recovery token.
///
/// <para>
/// A fast hash is correct here, unlike for passwords. Key stretching protects low-entropy secrets a
/// human chose; these are 256 bits of cryptographic randomness, so stretching buys nothing and would
/// add latency to every refresh. What matters is that a database leak yields no usable token, which
/// SHA-256 already guarantees at this entropy.
/// </para>
///
/// <para>Comparison is constant-time, so a timing side channel cannot reveal a partial match.</para>
/// </summary>
public sealed class SecretHash : IEquatable<SecretHash>
{
    public const int ByteLength = 32;

    private readonly byte[] _value;

    private SecretHash(byte[] value) => _value = value;

    public IReadOnlyList<byte> Value => _value;

    public byte[] ToByteArray() => [.. _value];

    /// <summary>Hashes a plaintext secret. The plaintext is never retained.</summary>
    public static SecretHash FromPlaintext(string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);
        return new SecretHash(SHA256.HashData(Encoding.UTF8.GetBytes(plaintext)));
    }

    /// <summary>Rehydrates a stored hash. Used by the persistence layer only.</summary>
    public static SecretHash FromStoredValue(byte[] storedValue)
    {
        ArgumentNullException.ThrowIfNull(storedValue);

        if (storedValue.Length != ByteLength)
        {
            throw new ArgumentException(
                $"A secret hash must be exactly {ByteLength} bytes.", nameof(storedValue));
        }

        return new SecretHash([.. storedValue]);
    }

    public bool Equals(SecretHash? other) =>
        other is not null && CryptographicOperations.FixedTimeEquals(_value, other._value);

    public override bool Equals(object? obj) => Equals(obj as SecretHash);

    /// <summary>
    /// Derived from the leading bytes only. Enough to distribute across buckets, while equality
    /// still runs the full constant-time comparison.
    /// </summary>
    public override int GetHashCode() => BitConverter.ToInt32(_value, 0);

    public override string ToString() => "[redacted]";
}
