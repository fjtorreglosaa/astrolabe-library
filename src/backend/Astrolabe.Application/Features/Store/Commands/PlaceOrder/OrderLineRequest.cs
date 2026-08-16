namespace Astrolabe.Application.Features.Store.Commands.PlaceOrder;

/// <summary>One book and how many of it.</summary>
public sealed record OrderLineRequest(Guid BookId, int Quantity);
