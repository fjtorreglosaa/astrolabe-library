using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Network;

namespace Astrolabe.Application.Features.Network.Commands.DeactivateLibrary;

/// <summary>
/// Withdraws a branch from member-facing surfaces while preserving its history.
/// Implements BR-NET-005 and BR-NET-008.
///
/// Yields what the library still held when it was withdrawn, so the operator sees what is winding
/// down. Those facts do not refuse the withdrawal — see <see cref="LibraryObligations"/>.
/// </summary>
public sealed record DeactivateLibraryCommand(Guid LibraryId) : ICommand<LibraryObligations>;
