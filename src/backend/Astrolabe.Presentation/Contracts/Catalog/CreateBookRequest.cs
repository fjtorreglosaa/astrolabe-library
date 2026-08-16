using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Membership.Enums;

namespace Astrolabe.Presentation.Contracts.Catalog;

/// <summary>The body of a book creation, matching the prototype's three-step wizard.</summary>
public sealed record CreateBookRequest(
    string Isbn,
    string Title,
    string Author,
    string? Publisher,
    Genre Genre,
    PlanTier Tier,
    int RetailPriceCents,
    string? CoverUrl,
    IReadOnlyList<CopyAllocationRequest> Copies);

/// <summary>How many volumes one library receives.</summary>
