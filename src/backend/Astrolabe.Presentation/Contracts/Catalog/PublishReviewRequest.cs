namespace Astrolabe.Presentation.Contracts.Catalog;

/// <summary>The body of a review. The book comes from the route, and the author from the token.</summary>
public sealed record PublishReviewRequest(int Rating, string? Comment);
