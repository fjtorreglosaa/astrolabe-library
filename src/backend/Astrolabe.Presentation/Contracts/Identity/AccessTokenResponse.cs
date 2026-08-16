namespace Astrolabe.Presentation.Contracts.Identity;

/// <summary>Carries only the access token. The refresh token travels in a cookie.</summary>
public sealed record AccessTokenResponse(string AccessToken, DateTimeOffset ExpiresAt);
