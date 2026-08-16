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
    /// Withdraws the library from member-facing surfaces while preserving its history. BR-NET-005.
    ///
    /// <para>
    /// Deliberately <b>not</b> refused for copies, live reservations or unpaid fines. BR-NET-005
    /// lists those as what blocks a <em>deletion</em>, and offers deactivation as the alternative
    /// that keeps the history — so refusing on them turned the escape hatch off exactly when it was
    /// needed. It also could not converge: stock is permanent and new reservations keep arriving
    /// until the library stops taking them, which is what deactivating is for. Outstanding work is
    /// reported to the operator instead, and stays serviceable: returns and fine payments run
    /// through staff paths, which do not consult this flag.
    /// </para>
    ///
    /// The one fact it needs is passed in rather than fetched, so the rule stays a pure decision
    /// that can be tested without a database. The handler gathers the fact; the entity judges it.
    /// </summary>
    /// <param name="isCityHomeLibrary">
    /// Whether this library is its city's designated home library. BR-NET-003 requires every city to
    /// expose exactly one, and Basic members of that city may borrow nowhere else.
    /// </param>
    public Result Deactivate(bool isCityHomeLibrary)
    {
        if (!IsActive)
        {
            return Result.Failure(NetworkErrors.LibraryAlreadyInactive);
        }

        if (isCityHomeLibrary)
        {
            return Result.Failure(NetworkErrors.CannotDeactivateHomeLibrary);
        }

        IsActive = false;
        return Result.Success();
    }

    public void Reactivate() => IsActive = true;
}
