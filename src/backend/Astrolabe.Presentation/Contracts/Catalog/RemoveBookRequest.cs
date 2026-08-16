using Astrolabe.Domain.Features.Catalog.Enums;

namespace Astrolabe.Presentation.Contracts.Catalog;

/// <summary>The body of a removal. BR-CAT-024 makes the reason mandatory.</summary>
public sealed record RemoveBookRequest(RemovalReason Reason, string? Notes);
