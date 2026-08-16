namespace Astrolabe.Application.Contracts.Identity;

/// <summary>
/// The two halves of an authenticated session, returned exactly once at sign-in and at each refresh.
///
/// <see cref="RefreshToken"/> is the only time the plaintext exists outside the client: only its
/// hash is persisted (BR-IDN-016). The presentation layer places it in an HttpOnly cookie and must
/// never put it in a response body.
/// </summary>
public sealed record TokenPair(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    Guid SessionId);
