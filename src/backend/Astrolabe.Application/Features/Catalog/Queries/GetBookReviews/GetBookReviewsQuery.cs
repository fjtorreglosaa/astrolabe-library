using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Catalog;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Catalog.Queries.GetBookReviews;

/// <summary>A page of a book's reviews, newest first. Implements BR-CAT-029.</summary>
public sealed record GetBookReviewsQuery(
    Guid BookId,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<ReviewDto>>;
