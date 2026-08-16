namespace Astrolabe.Application.Contracts.Catalog;

/// <summary>The bytes of a cover and what they are. Returned by the file endpoint, never by a listing.</summary>
public sealed record BookCoverDto(string ContentType, byte[] Content, DateTimeOffset UploadedAt);
