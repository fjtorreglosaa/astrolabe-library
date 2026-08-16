using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Membership.Commands.ChangeCityOfResidence;

/// <summary>
/// Moves the caller to another city, recalculating reach and home library. Implements BR-MBR-011
/// and BR-MBR-012.
/// </summary>
public sealed record ChangeCityOfResidenceCommand(Guid CountryId, Guid CityId) : ICommand;
