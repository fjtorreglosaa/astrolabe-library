using System.Reflection;
using Astrolabe.Presentation.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;

namespace Astrolabe.Presentation.Tests.Controllers;

/// <summary>
/// Regression guard for a defect that was invisible at runtime.
///
/// <para>
/// <c>AuthController</c> once carried <c>[AllowAnonymous]</c> at class level, which silently
/// overrides every <c>[Authorize]</c> on a method. The authenticated endpoints still answered 401,
/// but only because their handlers checked <c>ICurrentUser</c> themselves — the framework was
/// letting the requests through. Any endpoint added later would have been unprotected with no
/// warning at all.
/// </para>
///
/// <para>
/// These tests assert the shape rather than the behaviour, because the behaviour looked correct
/// while the shape was wrong.
/// </para>
/// </summary>
[TestFixture]
public sealed class AuthorizationAttributeTests
{
    private static readonly Type[] Controllers =
    [
        typeof(AuthController),
        typeof(SessionsController),
        typeof(NetworkController)
    ];

    [Test]
    public void NoControllerIsAnonymousAtClassLevel()
    {
        foreach (var controller in Controllers)
        {
            controller.GetCustomAttribute<AllowAnonymousAttribute>(inherit: false)
                .Should().BeNull(
                    "{0} would override every [Authorize] on its own methods", controller.Name);
        }
    }

    [Test]
    public void EveryControllerRequiresAuthorizationByDefault()
    {
        // Secure by default: a new endpoint is protected unless it explicitly opts out.
        foreach (var controller in Controllers)
        {
            controller.GetCustomAttribute<AuthorizeAttribute>(inherit: false)
                .Should().NotBeNull("{0} must be authorized unless an action opts out", controller.Name);
        }
    }

    [Test]
    public void AnonymousEndpointsAreExactlyTheExpectedOnes()
    {
        // Anything appearing here that should not be is a hole; anything missing breaks a public
        // flow. Either way the list must be a deliberate decision, not an accident.
        var anonymous = Controllers
            .SelectMany(c => c.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(m => m.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
            .Select(m => m.Name)
            .OrderBy(name => name)
            .ToArray();

        anonymous.Should().BeEquivalentTo(
        [
            "AcceptInvitation",     // the invitee has no account until they accept
            "ForgotPassword",       // reached by someone locked out
            "GetCities",            // the registration form needs it before anyone signs in
            "GetCountries",         // same
            "Refresh",              // the access token has expired by definition
            "Register",             // the caller has no account yet
            "ResendVerification",   // reached by someone who cannot sign in yet
            "ResetPassword",        // the emailed token is the credential
            "SignIn",               // where a session begins
            "VerifyEmail"           // the emailed token is the credential
        ]);
    }

    [Test]
    public void SessionEndpointsAreNeverAnonymous()
    {
        // BR-IDN-025: a member may only manage their own sessions, which requires knowing who they
        // are before the handler runs.
        typeof(SessionsController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Should().OnlyContain(m => m.GetCustomAttribute<AllowAnonymousAttribute>() == null);
    }
}
