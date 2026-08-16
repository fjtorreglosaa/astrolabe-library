using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Presentation.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Astrolabe.Presentation.Middleware;

/// <summary>
/// Rejects a request whose session has been revoked. Implements BR-IDN-023.
///
/// <para>
/// An access token is a signed JWT: once issued it cannot be recalled, so signature and expiry alone
/// would let a revoked session keep working for the rest of the token's fifteen minutes. This is the
/// check that makes "sign out everywhere" mean what the interface promises.
/// </para>
///
/// <para>
/// It runs after authentication, so the claims are populated, and before authorization, so no
/// endpoint ever sees a revoked identity. A request with no session claim passes through untouched:
/// it is anonymous, and authorization will deal with it.
/// </para>
/// </summary>
public sealed class SessionValidationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ISessionRevocationCache revocationCache)
    {
        var sessionId = context.User.FindFirst(CurrentUser.SessionIdClaimType)?.Value;

        if (Guid.TryParse(sessionId, out var parsed) && revocationCache.IsRevoked(parsed))
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "This session has ended. Please sign in again.",
                Instance = $"{context.Request.Method} {context.Request.Path}"
            };

            problem.Extensions["code"] = "identity.session_revoked";
            problem.Extensions["correlationId"] = context.TraceIdentifier;

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(problem);

            return;
        }

        await next(context);
    }
}
