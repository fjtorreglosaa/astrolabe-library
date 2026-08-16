namespace Astrolabe.Domain.Primitives;

/// <summary>
/// Classifies an <see cref="Error"/> so the presentation layer can map it to a transport status
/// without inspecting error codes. See GUIDELINES.md section 18.
/// </summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Authentication,
    Authorization,
    Domain,
    Infrastructure
}
