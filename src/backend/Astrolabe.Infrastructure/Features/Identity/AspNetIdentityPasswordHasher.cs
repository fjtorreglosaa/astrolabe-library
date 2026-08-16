using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Microsoft.AspNetCore.Identity;

namespace Astrolabe.Infrastructure.Features.Identity;

/// <summary>
/// Hashes and verifies passwords with ASP.NET Core Identity's PBKDF2-HMAC-SHA256 implementation.
///
/// <para>
/// Only the hasher is used, not the whole Identity stack. Taking the full framework would drag
/// <c>IdentityUser</c> and <c>IdentityDbContext</c> into the model, and <c>IdentityUser</c> cannot
/// satisfy the Domain layer's zero-dependency rule. This satisfies the algorithm GUIDELINES.md §6.3
/// requires while the user store stays ours.
/// </para>
///
/// <para>
/// The generic argument is <see cref="object"/> because the hasher never inspects the user; it only
/// exists to satisfy the type parameter.
/// </para>
/// </summary>
public sealed class AspNetIdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    private static readonly object HashingSubject = new();

    public PasswordHash Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        return PasswordHash.FromHashedValue(_hasher.HashPassword(HashingSubject, password));
    }

    /// <summary>
    /// Verifies a candidate password. Returns false rather than throwing for a malformed stored
    /// hash: a corrupt row must fail authentication, not crash the sign-in endpoint.
    /// </summary>
    public bool Verify(string password, PasswordHash hash)
    {
        ArgumentNullException.ThrowIfNull(hash);

        if (string.IsNullOrEmpty(password))
        {
            return false;
        }

        try
        {
            var outcome = _hasher.VerifyHashedPassword(HashingSubject, hash.Value, password);

            // SuccessRehashNeeded means the stored hash used older parameters. It is still a
            // correct password, so it must authenticate; upgrading the hash is a separate concern.
            return outcome is PasswordVerificationResult.Success
                or PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
