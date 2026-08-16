using System.Security.Claims;
using Astrolabe.Presentation.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Astrolabe.Presentation.Tests.Middleware;

/// <summary>
/// Verifies that a per-user response can never be stored by a cache.
///
/// Every authenticated endpoint answers at a URL identical for everyone — only the token differs.
/// Without an explicit directive the response carries no instruction at all, and a shared cache is
/// free to hand one member's profile, membership or catalogue verdicts to whoever asks next.
/// </summary>
[TestFixture]
public sealed class NoStoreForAuthenticatedResponsesMiddlewareTests
{
    /// <summary>
    /// Runs the callbacks the middleware registers. <see cref="DefaultHttpContext"/> accepts an
    /// <c>OnStarting</c> callback and then never calls it, so without this the test would assert
    /// against headers nothing ever wrote and pass for the wrong reason.
    /// </summary>
    private sealed class CallbackRunningResponseFeature : HttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _callbacks = [];

        public override bool HasStarted { get; }

        public override void OnStarting(Func<object, Task> callback, object state) =>
            _callbacks.Add((callback, state));

        public async Task StartAsync()
        {
            foreach (var (callback, state) in _callbacks)
            {
                await callback(state);
            }
        }
    }

    private static Task<HttpContext> InvokeAsync(bool authenticated) =>
        InvokeAsync(authenticated, alreadySetCacheControl: null);

    private static async Task<HttpContext> InvokeAsync(
        bool authenticated, string? alreadySetCacheControl)
    {
        var responseFeature = new CallbackRunningResponseFeature();
        var features = new FeatureCollection();
        features.Set<IHttpResponseFeature>(responseFeature);

        var context = new DefaultHttpContext(features);

        if (authenticated)
        {
            context.User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
                    authenticationType: "Bearer"));
        }

        var middleware = new NoStoreForAuthenticatedResponsesMiddleware(inner =>
        {
            // Stands in for a handler that has decided its own policy — the file endpoint is the
            // only one that does.
            if (alreadySetCacheControl is not null)
            {
                inner.Response.Headers.CacheControl = alreadySetCacheControl;
            }

            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        // The headers are written from OnStarting, which the real pipeline fires when the response
        // begins. Nothing has started here, so it is triggered explicitly.
        await responseFeature.StartAsync();

        return context;
    }

    [Test]
    public async Task AnAuthenticatedResponse_IsMarkedNoStore()
    {
        var context = await InvokeAsync(authenticated: true);

        context.Response.Headers.CacheControl.ToString().Should().Contain("no-store");
    }

    [Test]
    public async Task AnAuthenticatedResponse_IsMarkedPrivateSoNoSharedCacheKeepsIt()
    {
        var context = await InvokeAsync(authenticated: true);

        context.Response.Headers.CacheControl.ToString().Should().Contain("private");
    }

    [Test]
    public async Task AnAuthenticatedResponse_VariesOnTheAuthorizationHeader()
    {
        // Two readers behind one cache send different tokens for the same URL. Without this the
        // cache has no way to tell their responses apart.
        var context = await InvokeAsync(authenticated: true);

        context.Response.Headers.Vary.ToString().Should().Contain("Authorization");
    }

    [Test]
    public async Task AnAnonymousResponse_IsLeftAlone()
    {
        // The registration form's country list is the same for everyone and worth caching. Marking
        // it no-store would cost a round trip on every visit for no benefit.
        var context = await InvokeAsync(authenticated: false);

        context.Response.Headers.CacheControl.ToString().Should().BeEmpty();
        context.Response.Headers.Vary.ToString().Should().BeEmpty();
    }

    [Test]
    public async Task AnExplicitPolicySetByTheHandlerSurvives()
    {
        // The book cover. Every reader receives identical bytes, and blanket revalidation would undo
        // the whole reason the image is served separately from the listing — a page of twenty books
        // would re-fetch twenty pictures on every search.
        var context = await InvokeAsync(authenticated: true, alreadySetCacheControl: "private, max-age=86400");

        context.Response.Headers.CacheControl.ToString().Should().Be("private, max-age=86400");
    }

    [Test]
    public async Task AnExplicitPolicyStillGetsVary()
    {
        // Two readers behind one cache send different tokens for the same URL, whatever the
        // directive says. Dropping this alongside the override would be the actual disclosure.
        var context = await InvokeAsync(authenticated: true, alreadySetCacheControl: "private, max-age=86400");

        context.Response.Headers.Vary.ToString().Should().Be("Authorization");
    }

    [Test]
    public async Task AHandlerThatSaysNothingStillGetsNoStore()
    {
        // The default has to keep working, or the override would have quietly disabled the guard for
        // every endpoint rather than for the one that asked.
        var context = await InvokeAsync(authenticated: true, alreadySetCacheControl: null);

        context.Response.Headers.CacheControl.ToString().Should().Contain("no-store");
    }
}
