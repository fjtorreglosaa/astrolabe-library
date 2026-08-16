using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Identity.Commands.ResetPassword;

/// <summary>
/// Sets a new password from a recovery link. Implements BR-IDN-009, BR-IDN-012 and BR-IDN-013.
/// </summary>
public sealed record ResetPasswordCommand(string Token, string NewPassword) : ICommand;
