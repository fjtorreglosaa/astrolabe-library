using Astrolabe.Domain.Features.Identity.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Astrolabe.Infrastructure.Persistence.Converters;

/// <summary>
/// Persists a <see cref="SecretHash"/> as raw bytes.
///
/// Stored as <c>bytea</c> rather than text so no plaintext-shaped column ever exists for a token,
/// which is what makes BR-IDN-016 impossible to violate by a careless future write.
/// </summary>
public sealed class SecretHashConverter : ValueConverter<SecretHash, byte[]>
{
    public SecretHashConverter()
        : base(
            hash => hash.ToByteArray(),
            value => SecretHash.FromStoredValue(value))
    {
    }
}
