using Astrolabe.Domain.Features.Identity.Entities;

namespace Astrolabe.Application.Abstractions.Identity;

/// <summary>Issues the two halves of a token pair.</summary>
public interface ITokenGenerator
{
    /// <summary>
    /// A signed JWT valid for a short window, carrying the subject, role and session identifier
    /// (BR-IDN-014).
    /// </summary>
    string CreateAccessToken(User user, Guid sessionId);

    /// <summary>
    /// A cryptographically random opaque secret (BR-IDN-015). Returned in plaintext exactly once,
    /// to be handed to the client; only its hash is ever persisted.
    /// </summary>
    string CreateRefreshToken();

    TimeSpan AccessTokenLifetime { get; }

    TimeSpan RefreshTokenLifetime { get; }
}
