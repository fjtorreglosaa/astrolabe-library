using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Network.Commands.CreateLibrary;

/// <summary>
/// Adds a branch to a city. Implements BR-NET-001, BR-NET-002 and BR-NET-008.
/// </summary>
public sealed record CreateLibraryCommand(Guid CityId, string Name) : ICommand<Guid>;
