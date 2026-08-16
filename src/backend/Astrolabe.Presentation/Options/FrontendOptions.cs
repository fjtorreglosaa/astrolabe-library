using System.ComponentModel.DataAnnotations;

namespace Astrolabe.Presentation.Options;

/// <summary>
/// Bound with the Options Pattern rather than read through IConfiguration at the point of use.
/// See GUIDELINES.md section 27.
/// </summary>
public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";

    /// <summary>Origins allowed by CORS. Never a wildcard — the API is credentialed.</summary>
    [Required, MinLength(1)]
    public string[] AllowedOrigins { get; init; } = [];
}
