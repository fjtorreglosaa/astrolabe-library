namespace Astrolabe.Presentation.Contracts.Network;

/// <summary>The branch a city's Basic members borrow from. Exactly one — BR-NET-003.</summary>
public sealed record DesignateHomeLibraryRequest(Guid LibraryId);
