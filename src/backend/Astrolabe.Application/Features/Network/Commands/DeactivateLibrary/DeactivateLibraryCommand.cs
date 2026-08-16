using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Network.Commands.DeactivateLibrary;

/// <summary>
/// Withdraws a branch from member-facing surfaces while preserving its history.
/// Implements BR-NET-005 and BR-NET-008.
/// </summary>
public sealed record DeactivateLibraryCommand(Guid LibraryId) : ICommand;
