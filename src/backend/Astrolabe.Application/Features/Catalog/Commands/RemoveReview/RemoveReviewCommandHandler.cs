using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Catalog.Errors;
using Astrolabe.Domain.Features.Catalog.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Catalog.Commands.RemoveReview;

public sealed class RemoveReviewCommandHandler(
    ICatalogUnitOfWork catalog,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<RemoveReviewCommand>
{
    public async Task<Result> Handle(RemoveReviewCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure(CatalogErrors.ReviewNotYours);
        }

        // Looked up by member and book rather than by review identifier: a member cannot then reach
        // someone else's review by guessing an id, whatever the entity check would have said.
        var review = await catalog.Reviews.GetByMemberAndBookAsync(
            memberId, request.BookId, cancellationToken);

        if (review is null)
        {
            return Result.Failure(CatalogErrors.ReviewNotFound);
        }

        // Raises the event before the row goes, so the rating handler still knows which book to
        // recompute.
        var removed = review.Remove(memberId, clock.UtcNow);

        if (removed.IsFailure)
        {
            return removed;
        }

        catalog.Reviews.Remove(review);

        await catalog.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
