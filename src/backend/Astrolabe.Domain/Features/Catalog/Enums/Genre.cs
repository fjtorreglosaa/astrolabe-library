namespace Astrolabe.Domain.Features.Catalog.Enums;

/// <summary>
/// The prototype's own genre list. A closed set rather than free text: the catalogue filter is a
/// fixed row of chips, and a typo would create a genre nothing else can reach.
/// </summary>
public enum Genre
{
    Fiction = 0,
    Essay = 1,
    ScienceFiction = 2,
    History = 3,
    Biography = 4,
    Technical = 5
}
