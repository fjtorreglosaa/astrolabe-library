using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Network.Entities;

/// <summary>
/// A grouping of libraries within a country. A member's city of residence determines the reach of
/// the Basic and Plus plans. Implements BR-NET-001 and BR-NET-003.
/// </summary>
public sealed class City : Entity
{
    private City()
    {
    }

    private City(Guid id, Guid countryId, string name) : base(id)
    {
        CountryId = countryId;
        Name = name;
    }

    public Guid CountryId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// The single library a Basic member residing here may borrow from.
    ///
    /// Nullable in the schema only because a city and its libraries are inserted in one transaction;
    /// once seeded it is never null. See network.technical.md section 3.
    /// </summary>
    public Guid? HomeLibraryId { get; private set; }

    public static Result<City> Create(Guid id, Guid countryId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<City>(NetworkErrors.CityNameRequired);
        }

        return Result.Success(new City(id, countryId, name.Trim()));
    }

    /// <summary>
    /// Designates the home library. The library itself is passed rather than its identifier, so the
    /// entity can verify that it belongs to this city and is active — an identifier alone could not
    /// be checked without a repository.
    /// </summary>
    public Result DesignateHomeLibrary(Library library)
    {
        ArgumentNullException.ThrowIfNull(library);

        if (library.CityId != Id)
        {
            return Result.Failure(NetworkErrors.HomeLibraryNotInCity);
        }

        if (!library.IsActive)
        {
            return Result.Failure(NetworkErrors.HomeLibraryInactive);
        }

        HomeLibraryId = library.Id;
        return Result.Success();
    }

    public bool IsHomeLibrary(Guid libraryId) => HomeLibraryId == libraryId;
}
