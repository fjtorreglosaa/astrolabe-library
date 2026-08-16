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
/// </summary>
public sealed class NoStoreForAuthenticatedResponsesMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            if (context.User.Identity?.IsAuthenticated is true)
            {
                context.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, private";
                context.Response.Headers.Pragma = "no-cache";

                // Two readers behind one cache send different tokens for the same URL. Without this
                // the cache cannot tell their responses apart.
                context.Response.Headers.Vary = "Authorization";
            }

            return Task.CompletedTask;
        });

        await next(context);
    }
}
