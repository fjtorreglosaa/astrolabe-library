using Astrolabe.Domain.Features.Recommendations.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Recommendations.ValueObjects;

/// <summary>
/// A provider credential, encrypted. Implements the storage half of BR-REC-004.
///
/// <para>
/// <b>There is no way back to plaintext from here.</b> No <c>Value</c>, no accessor, no
/// <c>ToString</c> that could be interpolated into a log line — only the ciphertext, and only for
/// <c>ISecretProtector</c> in Infrastructure to hand back. That is deliberate: "never returned by
/// any API response" is a rule a reviewer has to enforce on every DTO forever, while a type with no
/// readable value makes the leak unrepresentable, which nobody has to remember.
/// </para>
/// <para>
/// <see cref="KeyVersion"/> travels with the ciphertext so a rotated key ring can still decrypt what
/// an older one wrote. Without it a rotation would silently disconnect every library.
/// </para>
/// </summary>
public sealed class EncryptedSecret : IEquatable<EncryptedSecret>
{
    private readonly byte[] _cipherText;

    private EncryptedSecret(byte[] cipherText, string keyVersion)
    {
        _cipherText = cipherText;
        KeyVersion = keyVersion;
    }

    /// <summary>
    /// A copy, always. Handing out the array itself would let a caller mutate stored ciphertext.
    /// </summary>
    public byte[] CipherText => [.. _cipherText];

    public string KeyVersion { get; }

    public static Result<EncryptedSecret> Create(byte[] cipherText, string keyVersion)
    {
        if (cipherText is null || cipherText.Length == 0)
        {
            return Result.Failure<EncryptedSecret>(RecommendationErrors.CredentialEmpty);
        }

        if (string.IsNullOrWhiteSpace(keyVersion))
        {
            return Result.Failure<EncryptedSecret>(RecommendationErrors.CredentialKeyVersionMissing);
        }

        return Result.Success(new EncryptedSecret([.. cipherText], keyVersion.Trim()));
    }

    public bool Equals(EncryptedSecret? other) =>
        other is not null
        && KeyVersion == other.KeyVersion
        && _cipherText.AsSpan().SequenceEqual(other._cipherText);

    public override bool Equals(object? obj) => Equals(obj as EncryptedSecret);

    public override int GetHashCode() => HashCode.Combine(KeyVersion, _cipherText.Length);

    /// <summary>
    /// Says nothing. Overridden precisely so an interpolated string in a log line, an exception
    /// message or a debugger watch cannot become the way a credential escapes.
    /// </summary>
    public override string ToString() => $"EncryptedSecret({KeyVersion}, redacted)";
}
