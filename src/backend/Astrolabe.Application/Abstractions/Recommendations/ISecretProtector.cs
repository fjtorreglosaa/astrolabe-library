using Astrolabe.Domain.Features.Recommendations.ValueObjects;

namespace Astrolabe.Application.Abstractions.Recommendations;

/// <summary>
/// Encrypts and decrypts a provider credential. The only path between plaintext and storage.
///
/// <para>
/// Both directions live behind one seam so BR-REC-004 has a single place to be true. Decryption is
/// deliberately not a method on <c>EncryptedSecret</c>: a value object that could decrypt itself
/// would put the plaintext one call away from every DTO that holds one.
/// </para>
/// </summary>
public interface ISecretProtector
{
    EncryptedSecret Protect(string plaintext);

    /// <summary>
    /// Only ever called immediately before a provider call. Returns null when the ciphertext cannot
    /// be read — a rotated-away key, most likely — so the caller falls back rather than throwing at
    /// a member.
    /// </summary>
    string? Unprotect(EncryptedSecret secret);
}
