using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Membership.Enums;

namespace Astrolabe.Application.Features.Catalog.Commands.CreateBookDraft;

/// <summary>
/// Creates a book as a draft, with its stock. Implements BR-CAT-003, BR-CAT-004 and BR-CAT-022.
/// The book is not published, so nothing becomes reservable until a second, deliberate step.
/// </summary>
public sealed record CreateBookDraftCommand(
    string Isbn,
    string Title,
    string Author,
    string? Publisher,
    Genre Genre,
    PlanTier Tier,
    int RetailPriceCents,
    string? CoverUrl,
    IReadOnlyList<CopyAllocation> Copies) : ICommand<Guid>;

/// <summary>How many volumes one library receives.</summary>
public sealed record CopyAllocation(Guid LibraryId, int Quantity);
