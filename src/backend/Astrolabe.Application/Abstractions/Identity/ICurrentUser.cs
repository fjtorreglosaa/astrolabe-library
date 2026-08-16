using Astrolabe.Domain.Features.Identity.Enums;

namespace Astrolabe.Application.Abstractions.Identity;

/// <summary>
/// Who is making the current request.
///
/// Owned by the identity domain but consumed by every other one, which is why it lives in the
/// Application layer rather than inside a domain folder. Every property is nullable: an anonymous
/// request is a valid state, not an error, and callers must decide what it means for them.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    /// <summary>The session the access token belongs to. Backs immediate revocation (BR-IDN-023).</summary>
    Guid? SessionId { get; }

    UserRole? Role { get; }

    bool IsAuthenticated { get; }
}
