namespace Astrolabe.Presentation.Contracts.Store;

/// <summary>One book and how many of it.</summary>
public sealed record OrderLineRequestBody(Guid BookId, int Quantity);
