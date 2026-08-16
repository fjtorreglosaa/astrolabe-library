using Astrolabe.Domain.Features.Catalog.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Catalog.ValueObjects;

/// <summary>A review's star rating: an integer from 1 to 5. Implements BR-CAT-027.</summary>
public sealed record StarRating
{
    private StarRating(int stars) => Stars = stars;

    public int Stars { get; }

    public static Result<StarRating> Create(int stars) =>
        stars is >= 1 and <= 5
            ? Result.Success(new StarRating(stars))
            : Result.Failure<StarRating>(CatalogErrors.RatingOutOfRange);
}
