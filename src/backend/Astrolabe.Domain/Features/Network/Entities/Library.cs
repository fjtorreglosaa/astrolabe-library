using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Network.Entities;

/// <summary>
/// A physical branch belonging to exactly one city. Called a "branch" in the interface.
/// Implements BR-NET-001, BR-NET-002 and BR-NET-005.
/// </summary>
public sealed class Library : Entity
{
    private Library()
    {
    }

    private Library(Guid id, Guid cityId, string name) : base(id)
    {
        CityId = cityId;
        Name = name;
        IsActive = true;
    }

    public Guid CityId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public static Result<Library> Create(Guid id, Guid cityId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Library>(NetworkErrors.LibraryNameRequired);
        }

        return Result.Success(new Library(id, cityId, name.Trim()));
    }

    /// <summary>
    /// Withdraws the library from member-facing surfaces while preserving its history.
    ///
    /// The two facts it needs are passed in rather than fetched, so the rule stays a pure decision
    /// that can be tested without a database. The handler gathers the facts; the entity judges them.
    /// </summary>
    /// <param name="isCityHomeLibrary">Whether this library is its city's designated home library.</param>
    /// <param name="hasOpenObligations">Whether it still holds copies, active reservations or unresolved fines.</param>
    public Result Deactivate(bool isCityHomeLibrary, bool hasOpenObligations)
    {
        if (!IsActive)
        {
            return Result.Failure(NetworkErrors.LibraryAlreadyInactive);
        }

        if (isCityHomeLibrary)
        {
            return Result.Failure(NetworkErrors.CannotDeactivateHomeLibrary);
        }

        if (hasOpenObligations)
        {
            return Result.Failure(NetworkErrors.LibraryHasOpenObligations);
        }

        IsActive = false;
        return Result.Success();
    }

    public void Reactivate() => IsActive = true;
}
