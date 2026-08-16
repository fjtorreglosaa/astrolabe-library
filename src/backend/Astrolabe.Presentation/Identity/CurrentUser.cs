using System.Security.Claims;
using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Domain.Features.Identity.Enums;

namespace Astrolabe.Presentation.Identity;

/// <summary>
/// Reads the caller's identity from the request's claims.
///
/// Lives in Presentation because claims are an HTTP concern: putting it in Infrastructure would drag
/// ASP.NET types into a layer that has no business knowing about requests.
///
/// Until the identity domain issues tokens, every property resolves to null, which is exactly the
/// anonymous case callers already handle.
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    /// <summary>Claim carrying the session identifier. Backs immediate revocation (BR-IDN-023).</summary>
    public const string SessionIdClaimType = "sid";

    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? UserId => ReadGuid(ClaimTypes.NameIdentifier);

    public Guid? SessionId => ReadGuid(SessionIdClaimType);

    public UserRole? Role =>
        Enum.TryParse<UserRole>(Principal?.FindFirstValue(ClaimTypes.Role), out var role)
            ? role
            : null;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    private Guid? ReadGuid(string claimType) =>
        Guid.TryParse(Principal?.FindFirstValue(claimType), out var value) ? value : null;
}
