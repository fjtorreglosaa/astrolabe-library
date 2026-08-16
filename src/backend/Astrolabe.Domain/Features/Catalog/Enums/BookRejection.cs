namespace Astrolabe.Domain.Features.Catalog.Enums;

/// <summary>
/// The single reason shown on a book card, chosen from the verdicts of all its copies. Implements
/// BR-CAT-011 to BR-CAT-014.
/// </summary>
public enum BookRejection
{
    /// <summary>"All copies out" — no copy anywhere has stock.</summary>
    AllCopiesOut = 0,

    /// <summary>
    /// "Not in Basic plan" — takes precedence over every other reason. Naming a library instead
    /// would imply a different library would help, which for a tier mismatch is untrue.
    /// </summary>
    NotInBasicPlan = 1,

    /// <summary>"Home library only" — stock exists, but only away from a Basic member's branch.</summary>
    HomeLibraryOnly = 2,

    /// <summary>"Not in {city}" — stock exists, but only outside a Plus member's city.</summary>
    NotInCity = 3,

    /// <summary>
    /// "Unavailable" — stock exists and no reach rule explains the refusal. Unreachable for a Max
    /// member by construction, and kept so an unforeseen combination degrades to a plain refusal
    /// rather than to a claim that would be wrong.
    /// </summary>
    Unavailable = 4
}
