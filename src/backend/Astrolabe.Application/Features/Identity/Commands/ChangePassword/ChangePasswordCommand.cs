using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Identity.Commands.ChangePassword;

/// <summary>
/// Changes the password of the signed-in member. Implements BR-IDN-009 and BR-IDN-013.
/// </summary>
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand;
