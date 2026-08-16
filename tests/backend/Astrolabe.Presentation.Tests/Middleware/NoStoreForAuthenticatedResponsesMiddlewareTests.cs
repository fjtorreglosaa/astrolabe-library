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

    private static async Task<HttpContext> InvokeAsync(bool authenticated)
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

        var middleware = new NoStoreForAuthenticatedResponsesMiddleware(_ => Task.CompletedTask);

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
}
