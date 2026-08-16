using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Identity.ValueObjects;

/// <summary>
/// A validated email address, normalised to lower case and trimmed.
///
/// Normalisation happens at construction because BR-IDN-002 makes the address unique: without it,
/// "Ada@Example.com" and "ada@example.com" would be two accounts, and the unique index would not
/// catch it.
/// </summary>
public sealed record Email
{
    public const int MaxLength = 254;

    private Email(string value) => Value = value;

    public string Value { get; }

    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Email>(IdentityErrors.EmailRequired);
        }

        var normalised = value.Trim().ToLowerInvariant();

        if (normalised.Length > MaxLength || !IsWellFormed(normalised))
        {
            return Result.Failure<Email>(IdentityErrors.EmailInvalid);
        }

        return Result.Success(new Email(normalised));
    }

    /// <summary>
    /// Deliberately permissive. The only address proven to exist is one that receives the
    /// verification email, so rejecting unusual but legal addresses here would lock out real people
    /// for no security gain.
    /// </summary>
    private static bool IsWellFormed(string value)
    {
        var at = value.IndexOf('@', StringComparison.Ordinal);

        if (at <= 0 || at != value.LastIndexOf('@'))
        {
            return false;
        }

        var domain = value[(at + 1)..];

        return domain.Length >= 3
            && domain.Contains('.', StringComparison.Ordinal)
            && !domain.StartsWith('.')
            && !domain.EndsWith('.')
            && !value.Contains(' ', StringComparison.Ordinal);
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}
