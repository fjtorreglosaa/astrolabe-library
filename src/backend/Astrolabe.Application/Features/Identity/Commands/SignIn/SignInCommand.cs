using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Identity;

namespace Astrolabe.Application.Features.Identity.Commands.SignIn;

/// <summary>
/// Authenticates and opens a session. Implements BR-IDN-011, BR-IDN-014, BR-IDN-020 and BR-IDN-028.
/// </summary>
public sealed record SignInCommand(
    string Email,
    string Password,
    string? UserAgent,
    string? ClientDeviceId,
    string? IpAddress) : ICommand<TokenPair>;
