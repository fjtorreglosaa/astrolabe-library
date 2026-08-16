using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Network.Commands.AssignLibraries;

/// <summary>
/// Replaces an administrator's library assignments. Implements BR-NET-008, BR-NET-009 and BR-NET-011.
/// </summary>
public sealed record AssignLibrariesCommand(Guid UserId, IReadOnlyList<Guid> LibraryIds) : ICommand;
