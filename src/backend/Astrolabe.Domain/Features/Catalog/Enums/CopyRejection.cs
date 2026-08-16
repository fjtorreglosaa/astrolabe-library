namespace Astrolabe.Domain.Features.Catalog.Enums;

/// <summary>
/// Why one copy cannot be reserved. Implements BR-CAT-015.
///
/// An enumeration, not a sentence: the wording is fixed by the prototype and must be identical in
/// every surface, so the text is resolved once at the presentation edge and the domain carries only
/// the reason. Two call sites formatting a string would drift on the first edit.
/// </summary>
public enum CopyRejection
{
    /// <summary>"All copies out" — this library holds none available.</summary>
    OutOfStock = 0,

    /// <summary>"Not in Basic catalog" — the book's tier is above the member's plan.</summary>
    NotInBasicCatalog = 1,

    /// <summary>"Basic borrows at {library} only" — stock exists, but not at the home library.</summary>
    HomeLibraryOnly = 2,

    /// <summary>"Outside {city}" — the copy sits in another city.</summary>
    OutsideCity = 3
}
