namespace Astrolabe.Presentation.Contracts.Catalog;

/// <summary>How many copies of a new book one library receives.</summary>
public sealed record CopyAllocationRequest(Guid LibraryId, int Quantity);
