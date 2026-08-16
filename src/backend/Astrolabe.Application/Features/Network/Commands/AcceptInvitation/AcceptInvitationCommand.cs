using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Network.Commands.AcceptInvitation;

/// <summary>
/// Confirms a staff invitation and sets the initial password. Implements BR-NET-013 and BR-NET-014.
/// </summary>
public sealed record AcceptInvitationCommand(string Token, string Password) : ICommand;
