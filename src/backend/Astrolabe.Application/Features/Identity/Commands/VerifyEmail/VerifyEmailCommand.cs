using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Identity.Commands.VerifyEmail;

/// <summary>Confirms ownership of an email address. Implements BR-IDN-004.</summary>
public sealed record VerifyEmailCommand(string Token) : ICommand;
