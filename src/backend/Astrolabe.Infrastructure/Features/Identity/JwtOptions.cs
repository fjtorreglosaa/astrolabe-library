using System.ComponentModel.DataAnnotations;

namespace Astrolabe.Infrastructure.Features.Identity;

/// <summary>
/// Token issuance settings, validated at startup so a missing signing key stops the process rather
/// than surfacing as a failed sign-in later.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Signing key, supplied by environment variable and never committed. At least 32 bytes:
    /// HMAC-SHA256 requires a key no shorter than its output, or the signature is weakened.
    /// </summary>
    [Required(AllowEmptyStrings = false), MinLength(32)]
    public string SigningKey { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; init; } = "astrolabe";

    [Required(AllowEmptyStrings = false)]
    public string Audience { get; init; } = "astrolabe";

    /// <summary>BR-IDN-014. Short on purpose: it bounds how long a revoked session could survive.</summary>
    [Range(1, 60)]
    public int AccessTokenMinutes { get; init; } = 15;

    /// <summary>BR-IDN-015.</summary>
    [Range(1, 365)]
    public int RefreshTokenDays { get; init; } = 30;

    /// <summary>
    /// Tolerance for clock differences during validation. Containers share the host clock, so no
    /// wider allowance is justified — and a large skew extends the life of an expired token.
    /// </summary>
    [Range(0, 300)]
    public int ClockSkewSeconds { get; init; } = 30;
}
