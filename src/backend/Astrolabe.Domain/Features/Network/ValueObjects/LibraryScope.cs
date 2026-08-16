namespace Astrolabe.Domain.Features.Network.ValueObjects;

/// <summary>
/// The single authority on "may this staff user act here". Implements BR-NET-006, BR-NET-007 and
/// BR-NET-010.
///
/// Every other domain consumes this rather than querying assignments itself, so the rule exists in
/// exactly one place. <see cref="Empty"/> is a first-class value: an administrator with no
/// assignments is a valid state that yields empty lists, never an error.
/// </summary>
public sealed class LibraryScope : IEquatable<LibraryScope>
{
    private readonly HashSet<Guid> _libraryIds;

    private LibraryScope(bool isUnrestricted, HashSet<Guid> libraryIds)
    {
        IsUnrestricted = isUnrestricted;
        _libraryIds = libraryIds;
    }

    /// <summary>True for a super administrator, who never requires an assignment.</summary>
    public bool IsUnrestricted { get; }

    public IReadOnlySet<Guid> LibraryIds => _libraryIds;

    /// <summary>True when the scope grants nothing. Never true for an unrestricted scope.</summary>
    public bool IsEmpty => !IsUnrestricted && _libraryIds.Count == 0;

    public static LibraryScope Unrestricted() => new(true, []);

    public static LibraryScope Empty() => new(false, []);

    public static LibraryScope Of(IEnumerable<Guid> libraryIds)
    {
        ArgumentNullException.ThrowIfNull(libraryIds);
        return new LibraryScope(false, [.. libraryIds]);
    }

    public bool Covers(Guid libraryId) => IsUnrestricted || _libraryIds.Contains(libraryId);

    /// <summary>
    /// True only when every library is covered. An empty input is covered vacuously, which is the
    /// correct reading: an operation touching no library needs no library authority.
    /// </summary>
    public bool CoversAll(IEnumerable<Guid> libraryIds)
    {
        ArgumentNullException.ThrowIfNull(libraryIds);

        if (IsUnrestricted)
        {
            return true;
        }

        return libraryIds.All(_libraryIds.Contains);
    }

    /// <summary>True when at least one of the given libraries is covered.</summary>
    public bool CoversAny(IEnumerable<Guid> libraryIds)
    {
        ArgumentNullException.ThrowIfNull(libraryIds);

        if (IsUnrestricted)
        {
            return true;
        }

        return libraryIds.Any(_libraryIds.Contains);
    }

    /// <summary>
    /// Narrows a requested set to what this scope allows. Used by list queries so a staff user sees
    /// their own data rather than a 403 for asking too broadly.
    /// </summary>
    public IReadOnlyList<Guid> Filter(IEnumerable<Guid> libraryIds)
    {
        ArgumentNullException.ThrowIfNull(libraryIds);

        return IsUnrestricted
            ? [.. libraryIds]
            : [.. libraryIds.Where(_libraryIds.Contains)];
    }

    public bool Equals(LibraryScope? other)
    {
        if (other is null)
        {
            return false;
        }

        return IsUnrestricted == other.IsUnrestricted && _libraryIds.SetEquals(other._libraryIds);
    }

    public override bool Equals(object? obj) => Equals(obj as LibraryScope);

    public override int GetHashCode() =>
        HashCode.Combine(IsUnrestricted, _libraryIds.Count);

    public override string ToString() =>
        IsUnrestricted ? "LibraryScope(unrestricted)" : $"LibraryScope({_libraryIds.Count} libraries)";
}
