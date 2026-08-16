using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Domain.Features.Catalog.Events;
using Astrolabe.Domain.Features.Catalog.Repositories;
using MediatR;

namespace Astrolabe.Application.Features.Catalog.Events;

/// <summary>
/// Keeps a book's stored rating in step with its reviews. Implements BR-CAT-030 and BR-CAT-031.
///
/// <para>
/// The rating is a stored column rather than a join because a listing shows a rating on every row,
/// and an aggregate query per book would be an N+1 by construction. That choice has a cost: the
/// column has to be maintained. Doing it here, from the events, makes the maintenance structural —
/// three commands change reviews, and none of them has to remember.
/// </para>
/// <para>
/// Runs after the commit that changed the review, so the average it reads is the one that landed.
/// </para>
/// </summary>
public sealed class RecalculateBookRatingHandler(ICatalogUnitOfWork catalog)
    : INotificationHandler<DomainEventNotification<ReviewPublished>>,
      INotificationHandler<DomainEventNotification<ReviewRemoved>>
{
    public Task Handle(
        DomainEventNotification<ReviewPublished> notification, CancellationToken cancellationToken) =>
        RecalculateAsync(notification.DomainEvent.BookId, cancellationToken);

    public Task Handle(
        DomainEventNotification<ReviewRemoved> notification, CancellationToken cancellationToken) =>
        RecalculateAsync(notification.DomainEvent.BookId, cancellationToken);

    private async Task RecalculateAsync(Guid bookId, CancellationToken cancellationToken)
    {
        var book = await catalog.Books.GetByIdAsync(bookId, cancellationToken);

        // The book can be gone if it was removed in the same breath as its reviews. Nothing to
        // recompute is not an error.
        if (book is null)
        {
            return;
        }

        var (average, count) = await catalog.Reviews.GetRatingAsync(bookId, cancellationToken);

        // A count of zero clears the rating rather than storing a mean of nothing, which is what
        // keeps "no reviews" distinct from "rated zero".
        book.SetRating(average, count);

        await catalog.SaveChangesAsync(cancellationToken);
    }
}
