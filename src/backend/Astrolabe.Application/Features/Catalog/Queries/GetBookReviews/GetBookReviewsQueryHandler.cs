using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Catalog;
using Astrolabe.Domain.Features.Catalog.Repositories;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Catalog.Queries.GetBookReviews;

public sealed class GetBookReviewsQueryHandler(
    ICatalogUnitOfWork catalog,
    IIdentityUnitOfWork identity,
    ICurrentUser currentUser) : IQueryHandler<GetBookReviewsQuery, PagedResult<ReviewDto>>
{
    public async Task<Result<PagedResult<ReviewDto>>> Handle(
        GetBookReviewsQuery request, CancellationToken cancellationToken)
    {
        var page = await catalog.Reviews.GetByBookAsync(
            request.BookId, request.Page, request.PageSize, cancellationToken);

        if (page.IsEmpty)
        {
            return Result.Success(page.Items.Count == 0
                ? PagedResult<ReviewDto>.Empty(page.Page, page.PageSize)
                : PagedResult<ReviewDto>.Create([], page.Page, page.PageSize, page.TotalCount));
        }

        // BR-CAT-029 attributes each review by name. The authors are fetched once for the page
        // rather than per review, which for twenty reviews is one query instead of twenty.
        var authorIds = page.Items.Select(review => review.MemberId).Distinct().ToList();

        var authors = (await identity.Users.GetByIdsAsync(
                authorIds, cancellationToken))
            .ToDictionary(user => user.Id, user => user.FullName);

        var items = page.Items.Select(review =>
        {
            // A deleted member's reviews stay visible and keep counting, so a missing author is a
            // state to render rather than an error.
            var name = authors.GetValueOrDefault(review.MemberId) ?? "Former member";

            return new ReviewDto(
                review.Id,
                review.MemberId,
                name,
                InitialsOf(name),
                review.Rating.Stars,
                review.Comment,
                review.CreatedAt,
                review.EditedAt,
                IsMine: currentUser.UserId == review.MemberId);
        }).ToList();

        return Result.Success(PagedResult<ReviewDto>.Create(
            items, page.Page, page.PageSize, page.TotalCount));
    }

    /// <summary>The catalogue shows an avatar of at most two initials, as the prototype does.</summary>
    private static string InitialsOf(string name) =>
        string.Concat(name
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(part => char.ToUpperInvariant(part[0])));
}
