using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Membership.Enums;

namespace Astrolabe.Presentation.Contracts.Catalog;

/// <summary>The body of a book correction. The ISBN is absent because it identifies the work.</summary>
public sealed record UpdateBookRequest(
    string Title,
    string Author,
    string? Publisher,
    Genre Genre,
    PlanTier Tier,
    int RetailPriceCents,
    string? CoverUrl);
