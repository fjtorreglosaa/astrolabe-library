using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Catalog.Commands.RemoveReview;

/// <summary>Withdraws the caller's own review. Implements BR-CAT-028 and BR-CAT-031.</summary>
public sealed record RemoveReviewCommand(Guid BookId) : ICommand;
