using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Domain.Features.Identity.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Astrolabe.Infrastructure.Features.Identity;

/// <summary>
/// Issues access and refresh tokens. Implements BR-IDN-014 and BR-IDN-015.
/// </summary>
public sealed class JwtTokenGenerator : ITokenGenerator
{
    /// <summary>Claim carrying the session identifier, checked on every request for revocation.</summary>
    public const string SessionIdClaimType = "sid";

    /// <summary>256 bits of entropy, so a refresh token cannot be guessed or brute-forced.</summary>
    private const int RefreshTokenBytes = 32;

    private readonly JwtOptions _options;
    private readonly SigningCredentials _credentials;
    private readonly JsonWebTokenHandler _handler = new();

    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        _credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(_options.AccessTokenMinutes);

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(_options.RefreshTokenDays);

    public string CreateAccessToken(User user, Guid sessionId)
    {
        ArgumentNullException.ThrowIfNull(user);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = DateTime.UtcNow.Add(AccessTokenLifetime),
            SigningCredentials = _credentials,
            Claims = new Dictionary<string, object>
            {
                [ClaimTypes.NameIdentifier] = user.Id.ToString(),
                [ClaimTypes.Role] = user.Role.ToString(),
                [SessionIdClaimType] = sessionId.ToString(),

                // The email is included so the client can render the account menu without an extra
                // round trip. Nothing else about the user goes in: a JWT is readable by anyone
                // holding it, so it carries identifiers, not personal data.
                [JwtRegisteredClaimNames.Email] = user.Email.Value,
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString()
            }
        };

        return _handler.CreateToken(descriptor);
    }

    /// <summary>
    /// Returns the plaintext refresh token exactly once. The caller hands it to the client and
    /// persists only its hash; nothing here retains it.
    /// </summary>
    public string CreateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(RefreshTokenBytes));
}
