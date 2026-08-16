using Astrolabe.Domain.Features.Catalog.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Catalog.ValueObjects;

/// <summary>
/// A book's ISBN, normalised.
///
/// Hyphens and spaces are stripped at construction, so "978-0-14-103614-4" and "9780141036144" are
/// the same value. Without that, BR-CAT-003's uniqueness rule would be satisfied by two spellings of
/// one book.
/// </summary>
public sealed record Isbn
{
    private Isbn(string value) => Value = value;

    public string Value { get; }

    public static Result<Isbn> Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Result.Failure<Isbn>(CatalogErrors.IsbnRequired);
        }

        var normalised = new string(raw.Where(char.IsAsciiDigit).ToArray());

        // Length is checked on the digits, not on the input: the separators are presentation, and
        // rejecting a correctly hyphenated ISBN for its hyphens would be a rule nobody expects.
        if (normalised.Length is not (10 or 13))
        {
            return Result.Failure<Isbn>(CatalogErrors.IsbnInvalid);
        }

        return Result.Success(new Isbn(normalised));
    }

    public override string ToString() => Value;
}
