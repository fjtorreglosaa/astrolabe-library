using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Identity;

namespace Astrolabe.Application.Features.Identity.Commands.RefreshToken;

/// <summary>
/// Exchanges a refresh token for a new pair. Implements BR-IDN-017, BR-IDN-018 and BR-IDN-019.
/// </summary>
public sealed record RefreshTokenCommand(string RefreshToken, string? IpAddress) : ICommand<TokenPair>;
