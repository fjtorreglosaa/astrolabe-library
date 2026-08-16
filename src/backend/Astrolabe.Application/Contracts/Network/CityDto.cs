namespace Astrolabe.Application.Contracts.Network;

/// <summary>A city as offered on the registration form.</summary>
public sealed record CityDto(Guid Id, Guid CountryId, string Name, Guid? HomeLibraryId);
