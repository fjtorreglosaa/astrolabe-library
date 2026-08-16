using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Catalog.Errors;

public static class CatalogErrors
{
    public static readonly Error BookNotFound =
        Error.NotFound("Catalog.BookNotFound", "That book does not exist.");

    public static readonly Error IsbnRequired =
        Error.Validation("Catalog.IsbnRequired", "An ISBN is required.");

    public static readonly Error IsbnInvalid =
        Error.Validation("Catalog.IsbnInvalid", "An ISBN must have 10 or 13 digits.");

    public static readonly Error IsbnAlreadyExists =
        Error.Conflict("Catalog.IsbnAlreadyExists", "A book with that ISBN is already in the catalogue.");

    public static readonly Error TitleRequired =
        Error.Validation("Catalog.TitleRequired", "A title is required.");

    public static readonly Error AuthorRequired =
        Error.Validation("Catalog.AuthorRequired", "An author is required.");

    public static readonly Error PriceInvalid =
        Error.Validation("Catalog.PriceInvalid", "A retail price cannot be negative.");

    public static readonly Error CopyQuantityInvalid =
        Error.Validation("Catalog.CopyQuantityInvalid", "A copy count must be greater than zero.");

    public static readonly Error RatingOutOfRange =
        Error.Validation("Catalog.RatingOutOfRange", "A rating must be between 1 and 5 stars.");

    /// <summary>
    /// Guards BR-CAT-021. Naming both states matters: "cannot publish a book that is already in the
    /// catalogue" is actionable, where a bare "invalid transition" leaves the caller guessing.
    /// </summary>
    public static Error InvalidTransition(string from, string to) =>
        Error.Conflict(
            "Catalog.InvalidTransition",
            $"A book in {from} cannot move to {to}.");

    public static readonly Error RepairReasonRequired =
        Error.Validation("Catalog.RepairReasonRequired", "A repair needs a stated reason.");

    public static readonly Error RemovalReasonRequired =
        Error.Validation("Catalog.RemovalReasonRequired", "A removal needs a stated reason.");

    public static readonly Error NoCopiesAvailable =
        Error.Conflict("Catalog.NoCopiesAvailable", "There are no copies available at that library.");

    public static readonly Error CopyNotFound =
        Error.NotFound("Catalog.CopyNotFound", "That library holds no copies of this book.");

    public static readonly Error ReviewNotFound =
        Error.NotFound("Catalog.ReviewNotFound", "That review does not exist.");

    public static readonly Error ReviewNotYours =
        Error.Authorization("Catalog.ReviewNotYours", "You can only change your own review.");
}
