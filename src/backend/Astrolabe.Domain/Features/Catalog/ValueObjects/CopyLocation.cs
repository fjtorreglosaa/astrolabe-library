namespace Astrolabe.Domain.Features.Catalog.ValueObjects;

/// <summary>
/// Where a copy sits and how many are free, as the access policy needs it.
///
/// Carries the city alongside the library so the policy stays a pure function: resolving a library's
/// city inside it would need a repository, and the whole point of the policy is that it needs none.
/// </summary>
public sealed record CopyLocation(Guid LibraryId, Guid CityId, int AvailableCount)
{
    public bool HasStock => AvailableCount > 0;
}
