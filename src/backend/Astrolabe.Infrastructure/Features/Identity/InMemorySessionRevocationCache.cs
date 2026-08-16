using Astrolabe.Application.Abstractions.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace Astrolabe.Infrastructure.Features.Identity;

/// <summary>
/// Tracks revoked sessions in process. Implements BR-IDN-023.
///
/// <para>
/// An entry only needs to outlive the access tokens that reference it, so each one expires with the
/// session's last possible token. That keeps the cache bounded without a sweep.
/// </para>
///
/// <para>
/// <b>Single-instance only.</b> Running two API instances would let a session revoked on one keep
/// working on the other until its token expired. The interface is the seam that makes a distributed
/// cache a drop-in replacement; see global_tech_spec.md section 8.
/// </para>
/// </summary>
public sealed class InMemorySessionRevocationCache(IMemoryCache cache) : ISessionRevocationCache
{
    private const string KeyPrefix = "revoked-session:";

    public bool IsRevoked(Guid sessionId) => cache.TryGetValue(Key(sessionId), out _);

    public void Revoke(Guid sessionId, DateTimeOffset until)
    {
        // A window that has already closed means every token for the session is expired, so there
        // is nothing left to guard against.
        if (until <= DateTimeOffset.UtcNow)
        {
            return;
        }

        cache.Set(Key(sessionId), true, until);
    }

    private static string Key(Guid sessionId) => KeyPrefix + sessionId;
}
