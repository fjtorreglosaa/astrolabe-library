namespace Astrolabe.Presentation.Contracts.Network;

/// <summary>A new branch. Its name must be unique within its city — BR-NET-002.</summary>
public sealed record CreateLibraryRequest(Guid CityId, string Name);
