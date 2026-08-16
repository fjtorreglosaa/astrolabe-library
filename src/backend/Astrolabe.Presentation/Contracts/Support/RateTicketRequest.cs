namespace Astrolabe.Presentation.Contracts.Support;

/// <summary>One to five stars, and optionally a few words.</summary>
public sealed record RateTicketRequest(int Stars, string? Review);
