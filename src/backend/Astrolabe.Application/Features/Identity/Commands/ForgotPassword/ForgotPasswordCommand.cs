using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Identity.Commands.ForgotPassword;

/// <summary>Starts password recovery. Implements BR-IDN-012 and BR-IDN-029.</summary>
public sealed record ForgotPasswordCommand(string Email) : ICommand;
