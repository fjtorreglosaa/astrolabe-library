using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Recommendations.Commands.DisableLibraryAi;

/// <summary>
/// Switches a library's recommendations off. Implements BR-REC-012 and BR-REC-013.
///
/// The credential survives. A library turning this off for a month has not decided to throw away a
/// key it verified, and making them retype one is how a reversible decision becomes an irreversible
/// chore.
/// </summary>
public sealed record DisableLibraryAiCommand(Guid LibraryId) : ICommand;
