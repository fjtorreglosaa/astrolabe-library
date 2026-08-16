using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Network.Commands.RevokeAdmin;

/// <summary>
/// Removes a staff user's authority. Implements BR-NET-012 and BR-NET-016.
/// </summary>
public sealed record RevokeAdminCommand(Guid UserId) : ICommand;
