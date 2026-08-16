namespace Astrolabe.Application.Abstractions.Identity;

/// <summary>
/// Tracks sessions that have been revoked but whose access tokens have not yet expired.
///
/// This is what makes BR-IDN-023 true: a stateless JWT cannot be recalled, so without this check
/// "sign out everywhere" would take up to fifteen minutes to bite — which is not what the interface
/// promises the member.
/// </summary>
public interface ISessionRevocationCache
{
    bool IsRevoked(Guid sessionId);

    /// <summary>
    /// Marks a session revoked. <paramref name="until"/> need only cover the access token lifetime:
    /// past that point every token bearing the session is expired anyway.
    /// </summary>
    void Revoke(Guid sessionId, DateTimeOffset until);
}
