namespace Astrolabe.Domain.Features.Recommendations.Enums;

/// <summary>
/// The model vendors a library may connect to. Exactly the two the prototype offers.
///
/// A closed set on purpose: `BR-REC-001` lets a library choose a provider, not invent one, and an
/// open string here would mean the client picks which HTTP call the server makes.
/// </summary>
public enum AiProvider
{
    Claude = 0,
    OpenAI = 1
}
