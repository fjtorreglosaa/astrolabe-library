namespace Astrolabe.Domain.Features.Catalog.Enums;

/// <summary>Where a book sits in its lifecycle. Implements BR-CAT-021.</summary>
public enum BookStatus
{
    /// <summary>Created but not yet published. Visible to staff only, and never reservable.</summary>
    Draft = 0,

    /// <summary>In the collection and visible to members.</summary>
    Catalog = 1,

    /// <summary>Withdrawn for repair. Loans already running are unaffected.</summary>
    Repair = 2,

    /// <summary>Removed from the collection. Restorable, and its reviews survive.</summary>
    Deleted = 3
}
