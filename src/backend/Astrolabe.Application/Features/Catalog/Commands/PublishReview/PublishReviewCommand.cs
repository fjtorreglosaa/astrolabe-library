using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Catalog.Commands.PublishReview;

/// <summary>
/// Writes or rewrites the caller's review of a book. One command for both, because BR-CAT-027 allows
/// a member only one review per book: reviewing twice edits the first rather than creating a second.
/// </summary>
public sealed record PublishReviewCommand(Guid BookId, int Rating, string? Comment) : ICommand;
