using System.Text;
using Astrolabe.Application.Abstractions.Recommendations;
using Astrolabe.Domain.Features.Recommendations.ValueObjects;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace Astrolabe.Infrastructure.Features.Recommendations;

/// <summary>
/// Encrypts and decrypts provider credentials with ASP.NET Core Data Protection. BR-REC-004.
///
/// <para>
/// The purpose string is fixed and specific. A payload protected for recommendations cannot be
/// unprotected by any other purpose in the application, so a bug elsewhere cannot become a way to
/// read a provider key.
/// </para>
/// <para>
/// The key ring rotates, and the version travels with the ciphertext so an older row stays readable.
/// Data Protection handles that itself; the version is stored anyway, because the day the ring is
/// replaced wholesale somebody needs to know which rows they have just lost.
/// </para>
/// </summary>
public sealed class DataProtectionSecretProtector : ISecretProtector
{
    private const string Purpose = "Astrolabe.Recommendations.ProviderCredential.v1";

    private readonly IDataProtector _protector;
    private readonly ILogger<DataProtectionSecretProtector> _logger;

    public DataProtectionSecretProtector(
        IDataProtectionProvider provider, ILogger<DataProtectionSecretProtector> logger)
    {
        _protector = provider.CreateProtector(Purpose);
        _logger = logger;
    }

    public EncryptedSecret Protect(string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);

        var cipherText = _protector.Protect(Encoding.UTF8.GetBytes(plaintext));

        // Create only fails on an empty payload or a blank version, neither of which is reachable
        // here — so the value is taken directly rather than propagating a Result nobody can act on.
        return EncryptedSecret.Create(cipherText, Purpose).Value;
    }

    public string? Unprotect(EncryptedSecret secret)
    {
        ArgumentNullException.ThrowIfNull(secret);

        try
        {
            return Encoding.UTF8.GetString(_protector.Unprotect(secret.CipherText));
        }
        catch (Exception exception)
        {
            // Null rather than a throw: the caller is on a member-facing path where BR-REC-007
            // forbids an error, and an unreadable credential is exactly the case where it must fall
            // back. Logged without the ciphertext, which would be a smaller leak but a leak.
            _logger.LogError(
                exception,
                "A provider credential encrypted with key version {KeyVersion} could not be read.",
                secret.KeyVersion);

            return null;
        }
    }
}
