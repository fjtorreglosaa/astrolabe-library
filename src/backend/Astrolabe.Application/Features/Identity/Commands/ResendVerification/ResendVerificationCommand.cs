using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Identity.Commands.ResendVerification;

/// <summary>Issues a fresh verification link. Implements BR-IDN-005.</summary>
public sealed record ResendVerificationCommand(string Email) : ICommand;
