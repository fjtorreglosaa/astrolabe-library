namespace Astrolabe.Domain.Features.Catalog.Enums;

/// <summary>
/// What a catalogue listing is ordered by. Implements BR-CAT-019.
///
/// The set is exactly the prototype's sortable columns, so a header the interface offers always has
/// a key behind it and no key exists that nothing can reach.
/// </summary>
public enum BookSortKey
{
    Title = 0,
    Author = 1,
    Genre = 2,
    Tier = 3,

    /// <summary>Total copies free across every branch, not the count the caller may reach.</summary>
    Availability = 4,

    Rating = 5,
    Price = 6
}
