namespace Astrolabe.Application.Contracts.Network;

/// <summary>A country as offered on the registration form.</summary>
public sealed record CountryDto(Guid Id, string Name, string IsoCode);
