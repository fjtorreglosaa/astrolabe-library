using Microsoft.Extensions.Primitives;

namespace Astrolabe.Presentation.Middleware;

/// <summary>
/// Marks every authenticated response as uncacheable.
///
/// <para>
/// Without an explicit directive, a response carries no instruction at all, and any cache between
/// the API and the reader is free to apply its own heuristics. For a per-user payload that is a
/// disclosure waiting to happen: a shared proxy can hand one member's profile, membership or
/// catalogue verdicts to the next person who asks for the same URL, because the URL is identical for
/// everyone and only the token differs.
/// </para>
/// <para>
/// Applied by whether the request was authenticated rather than by route, so a new endpoint is
/// covered the day it is written and nobody has to remember an attribute.
/// </para>
/// <para>
/// A handler that has <b>already set</b> a directive keeps it. This is a safe default, not a
/// prohibition: the endpoint knows whether its body is per-reader and the middleware does not. The
/// only override today is the book cover, where every reader receives identical bytes and blanket
/// revalidation would undo the whole reason the image is served separately from the listing.
/// <c>Vary</c> is applied either way, because two readers behind one cache still send different
/// tokens for the same URL.
/// </para>
/// </summary>
public sealed class NoStoreForAuthenticatedResponsesMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            if (context.User.Identity?.IsAuthenticated is true)
            {
                // Deliberate and specific, so a handler cannot escape this by accident — only by
                // writing the header itself.
                if (StringValues.IsNullOrEmpty(context.Response.Headers.CacheControl))
                {
                    context.Response.Headers.CacheControl =
                        "no-store, no-cache, must-revalidate, private";
                    context.Response.Headers.Pragma = "no-cache";
                }

                // Two readers behind one cache send different tokens for the same URL. Without this
                // the cache cannot tell their responses apart, whatever the directive says.
                context.Response.Headers.Vary = "Authorization";
            }

            return Task.CompletedTask;
        });

        await next(context);
    }
}
