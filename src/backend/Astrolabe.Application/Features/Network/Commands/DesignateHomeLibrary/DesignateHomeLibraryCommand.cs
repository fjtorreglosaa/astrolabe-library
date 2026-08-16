using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Network.Commands.DesignateHomeLibrary;

/// <summary>
/// Sets which branch Basic members residing in a city may borrow from. Implements BR-NET-003.
/// </summary>
public sealed record DesignateHomeLibraryCommand(Guid CityId, Guid LibraryId) : ICommand;
