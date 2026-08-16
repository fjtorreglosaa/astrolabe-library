using Astrolabe.Domain.Features.Identity.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Astrolabe.Infrastructure.Persistence.Converters;

/// <summary>
/// Persists an <see cref="Email"/> as its normalised string.
///
/// Reading uses the private rehydration path rather than <c>Email.Create</c>: a value that is
/// already stored has been validated once, and re-validating on every read would make a rule change
/// silently unreadable rather than surfacing as a migration.
/// </summary>
public sealed class EmailConverter : ValueConverter<Email, string>
{
    public EmailConverter()
        : base(
            email => email.Value,
            value => Email.Create(value).Value)
    {
    }
}
