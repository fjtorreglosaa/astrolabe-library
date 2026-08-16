using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Network.Entities;

/// <summary>
/// A top-level geographic grouping. Implements BR-NET-001.
/// </summary>
public sealed class Country : Entity
{
    private Country()
    {
    }

    private Country(Guid id, string name, string isoCode) : base(id)
    {
        Name = name;
        IsoCode = isoCode;
    }

    public string Name { get; private set; } = string.Empty;

    /// <summary>ISO 3166-1 alpha-2, upper case.</summary>
    public string IsoCode { get; private set; } = string.Empty;

    /// <summary>
    /// Allows a country to be hidden from registration. It can only ever <em>hide</em> a country:
    /// availability is derived from active libraries, so this flag can never expose an empty branch.
    /// See BR-NET-004 and the decision log in network.technical.md.
    /// </summary>
    public bool IsHiddenFromRegistration { get; private set; }

    public static Result<Country> Create(Guid id, string name, string isoCode)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Country>(NetworkErrors.CountryNameRequired);
        }

        if (string.IsNullOrWhiteSpace(isoCode) || isoCode.Trim().Length != 2)
        {
            return Result.Failure<Country>(NetworkErrors.CountryIsoCodeInvalid);
        }

        return Result.Success(new Country(id, name.Trim(), isoCode.Trim().ToUpperInvariant()));
    }

    public void HideFromRegistration() => IsHiddenFromRegistration = true;

    public void ShowInRegistration() => IsHiddenFromRegistration = false;
}
