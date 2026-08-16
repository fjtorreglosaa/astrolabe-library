using Astrolabe.Domain.Features.Identity.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Astrolabe.Infrastructure.Persistence.Converters;

/// <summary>Persists a <see cref="PasswordHash"/> as its encoded string.</summary>
public sealed class PasswordHashConverter : ValueConverter<PasswordHash, string>
{
    public PasswordHashConverter()
        : base(
            hash => hash.Value,
            value => PasswordHash.FromHashedValue(value))
    {
    }
}
