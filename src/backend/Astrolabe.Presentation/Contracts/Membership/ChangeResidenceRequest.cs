namespace Astrolabe.Presentation.Contracts.Membership;

/// <summary>The body of a change of city.</summary>
public sealed record ChangeResidenceRequest(Guid CountryId, Guid CityId);
