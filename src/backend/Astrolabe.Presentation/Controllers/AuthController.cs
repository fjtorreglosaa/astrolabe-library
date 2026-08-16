using Astrolabe.Application.Contracts.Identity;
using Astrolabe.Application.Features.Identity.Commands.ChangePassword;
using Astrolabe.Application.Features.Identity.Commands.ForgotPassword;
using Astrolabe.Application.Features.Identity.Commands.RefreshToken;
using Astrolabe.Application.Features.Identity.Commands.Register;
using Astrolabe.Application.Features.Identity.Commands.ResendVerification;
using Astrolabe.Application.Features.Identity.Commands.ResetPassword;
using Astrolabe.Application.Features.Identity.Commands.SignIn;
using Astrolabe.Application.Features.Identity.Commands.SignOut;
using Astrolabe.Application.Features.Identity.Commands.VerifyEmail;
using Astrolabe.Application.Features.Identity.Queries.GetCurrentUser;
using Astrolabe.Domain.Features.Identity.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Astrolabe.Presentation.Controllers;

/// <summary>
/// Registration, verification, sign-in, refresh, sign-out and password recovery.
///
/// The controller stays thin: it binds, dispatches, and converts a <c>Result</c> into a response.
/// Its one extra job is cookie handling for the refresh token, which is an HTTP concern and belongs
/// nowhere else.
/// </summary>
[Route("api/v1/auth")]
[Authorize]
public sealed class AuthController(ISender sender) : ApiControllerBase(sender)
{
    /// <summary>
    /// Cookie carrying the refresh token. Path-scoped to the refresh endpoint so it is not attached
    /// to every request, which shrinks where it can leak.
    /// </summary>
    private const string RefreshCookieName = "astrolabe_refresh";

    private const string RefreshCookiePath = "/api/v1/auth";

    // Public by definition: the caller has no account yet.
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new RegisterCommand(
                request.Email, request.Password, request.FullName,
                request.CountryId, request.CityId, request.Plan),
            cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    // The token is the proof; the account cannot sign in until it is used.
    [AllowAnonymous]
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(TokenRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new VerifyEmailCommand(request.Token), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    // Reached by someone who cannot sign in yet.
    [AllowAnonymous]
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification(
        EmailRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ResendVerificationCommand(request.Email), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    // Obviously anonymous: this is where a session begins.
    [AllowAnonymous]
    [HttpPost("sign-in")]
    [ProducesResponseType<AccessTokenResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SignIn(SignInRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new SignInCommand(
                request.Email, request.Password,
                Request.Headers.UserAgent.ToString(), request.DeviceId, ClientIpAddress()),
            cancellationToken);

        if (result.IsFailure)
        {
            return HandleFailure(result);
        }

        return TokenPairResponse(result.Value);
    }

    // The access token has expired by definition; the cookie is the credential.
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType<AccessTokenResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        // Read from the cookie, never from the body: a refresh token in a body would end up in
        // logs, proxies and browser history.
        var refreshToken = Request.Cookies[RefreshCookieName] ?? string.Empty;

        var result = await Sender.Send(
            new RefreshTokenCommand(refreshToken, ClientIpAddress()), cancellationToken);

        if (result.IsFailure)
        {
            // The session is gone, so the cookie is worthless. Clearing it stops the client
            // retrying forever with a token that will never work again.
            Response.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = RefreshCookiePath });

            return HandleFailure(result);
        }

        return TokenPairResponse(result.Value);
    }

    [HttpPost("sign-out")]
    [Authorize]
    public async Task<IActionResult> SignOutCurrentSession(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new SignOutCommand(), cancellationToken);

        Response.Cookies.Delete(RefreshCookieName, new CookieOptions { Path = RefreshCookiePath });

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    // Reached by someone locked out of their account.
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(EmailRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new ForgotPasswordCommand(request.Email), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    // The emailed token is the credential.
    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new ResetPasswordCommand(request.Token, request.NewPassword), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(
            new ChangePasswordCommand(request.CurrentPassword, request.NewPassword), cancellationToken);

        return result.IsSuccess ? NoContent() : HandleFailure(result);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<CurrentUserDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetCurrentUserQuery(), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    /// <summary>
    /// Returns the access token in the body and the refresh token in an HttpOnly cookie.
    ///
    /// The refresh token never appears in the response body: script that can read it can steal it,
    /// and the whole point of the cookie is that script cannot.
    /// </summary>
    private IActionResult TokenPairResponse(TokenPair pair)
    {
        Response.Cookies.Append(RefreshCookieName, pair.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !HttpContext.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase),
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
            Expires = pair.RefreshTokenExpiresAt
        });

        return Ok(new AccessTokenResponse(pair.AccessToken, pair.AccessTokenExpiresAt));
    }

    private string? ClientIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();
}

public sealed record RegisterRequest(
    string Email, string Password, string FullName, Guid CountryId, Guid CityId, UserRole Plan);

public sealed record SignInRequest(string Email, string Password, string? DeviceId);

public sealed record TokenRequest(string Token);

public sealed record EmailRequest(string Email);

public sealed record ResetPasswordRequest(string Token, string NewPassword);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>Carries only the access token. The refresh token travels in a cookie.</summary>
public sealed record AccessTokenResponse(string AccessToken, DateTimeOffset ExpiresAt);
