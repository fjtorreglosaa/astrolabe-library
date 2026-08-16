namespace Astrolabe.Presentation.Contracts.Catalog;

/// <summary>
/// Where the cover now lives, or null when it was removed.
///
/// Null is an answer rather than an absence: a book without a cover is drawn with its generated
/// tint, so the caller needs to be told that happened rather than left to infer it from a 204.
/// </summary>
public sealed record CoverResponse(string? CoverUrl);
