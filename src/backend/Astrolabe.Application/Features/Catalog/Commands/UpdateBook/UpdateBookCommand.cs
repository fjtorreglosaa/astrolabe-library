using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Membership.Enums;

namespace Astrolabe.Application.Features.Catalog.Commands.UpdateBook;

/// <summary>Corrects a book's bibliographic details, tier and price. Implements BR-CAT-004.</summary>
public sealed record UpdateBookCommand(
    Guid BookId,
    string Title,
    string Author,
    string? Publisher,
    Genre Genre,
    PlanTier Tier,
    int RetailPriceCents,
    string? CoverUrl) : ICommand;
