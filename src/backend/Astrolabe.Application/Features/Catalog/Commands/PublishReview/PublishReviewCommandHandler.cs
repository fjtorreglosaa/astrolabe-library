using Astrolabe.Application.Abstractions.Catalog;
using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Features.Catalog.Errors;
using Astrolabe.Domain.Features.Catalog.Repositories;
using Astrolabe.Domain.Features.Catalog.ValueObjects;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Catalog.Commands.PublishReview;

public sealed class PublishReviewCommandHandler(
    ICatalogUnitOfWork catalog,
    IBorrowingHistoryProbe history,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<PublishReviewCommand>
{
    public async Task<Result> Handle(PublishReviewCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure(CatalogErrors.ReviewNotYours);
        }

        var rating = StarRating.Create(request.Rating);

        if (rating.IsFailure)
        {
            return Result.Failure(rating.Error);
        }

        var book = await catalog.Books.GetByIdAsync(request.BookId, cancellationToken);

        if (book is null || !book.IsVisibleToMembers)
        {
            return Result.Failure(CatalogErrors.BookNotFound);
        }

        // BR-CAT-032. The prototype opens its rating dialog from a *returned* loan — `canRate: done
        // && !isLibrarian` — and the dialog's own first line is "You returned this copy on {date}".
        // There is no path to it from the catalogue at all.
        //
        // Enforced here rather than only in the browser, because the rule is what makes the average
        // mean anything: a rating anybody can leave on a book they never opened is a number that
        // measures reach, not quality.
        if (!await history.HasReturnedAsync(memberId, request.BookId, cancellationToken))
        {
            return Result.Failure(CatalogErrors.ReviewRequiresReturnedLoan);
        }

        var existing = await catalog.Reviews.GetByMemberAndBookAsync(
            memberId, request.BookId, cancellationToken);

        if (existing is not null)
        {
            var edited = existing.Edit(memberId, rating.Value, request.Comment, clock.UtcNow);

            if (edited.IsFailure)
            {
                return edited;
            }
        }
        else
        {
            var review = Review.Publish(
                request.BookId, memberId, rating.Value, request.Comment, clock.UtcNow);

            await catalog.Reviews.AddAsync(review, cancellationToken);
        }

        // The rating itself is recomputed by the ReviewPublished handler after the commit, so the
        // stored average is never derived from a write that has not landed.
        await catalog.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
