using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Features.Catalog.Errors;
using Astrolabe.Domain.Features.Catalog.ValueObjects;
using FluentAssertions;

namespace Astrolabe.Domain.Tests.Features.Catalog;

/// <summary>Covers the catalog value objects and the stock entity's invariants.</summary>
[TestFixture]
public sealed class CatalogValueObjectTests
{
    private static readonly Guid Midtown = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ABook = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    // ---------- Isbn, BR-CAT-003 ----------

    [Test]
    public void AnIsbnIsNormalisedSoTwoSpellingsAreOneValue()
    {
        // Without this, the uniqueness rule would be satisfied by two spellings of one book.
        var hyphenated = Isbn.Create("978-0-14-103614-4").Value;
        var plain = Isbn.Create("9780141036144").Value;

        hyphenated.Value.Should().Be("9780141036144");
        hyphenated.Should().Be(plain);
    }

    [Test]
    public void AnIsbnIgnoresSurroundingSpaces()
    {
        Isbn.Create("  9780141036144  ").Value.Value.Should().Be("9780141036144");
    }

    [TestCase("0141036141")]
    [TestCase("9780141036144")]
    public void TenAndThirteenDigitIsbnsAreBothAccepted(string raw)
    {
        Isbn.Create(raw).IsSuccess.Should().BeTrue();
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void AMissingIsbnIsRefused(string? raw)
    {
        Isbn.Create(raw).Error.Should().Be(CatalogErrors.IsbnRequired);
    }

    [TestCase("123")]
    [TestCase("97801410361444")]
    [TestCase("abcdefghij")]
    public void AnIsbnOfTheWrongLengthIsRefused(string raw)
    {
        Isbn.Create(raw).Error.Should().Be(CatalogErrors.IsbnInvalid);
    }

    // ---------- StarRating, BR-CAT-027 ----------

    [TestCase(1)]
    [TestCase(3)]
    [TestCase(5)]
    public void AStarRatingFromOneToFiveIsAccepted(int stars)
    {
        StarRating.Create(stars).Value.Stars.Should().Be(stars);
    }

    [TestCase(0)]
    [TestCase(6)]
    [TestCase(-1)]
    public void AStarRatingOutsideTheScaleIsRefused(int stars)
    {
        StarRating.Create(stars).Error.Should().Be(CatalogErrors.RatingOutOfRange);
    }

    // ---------- BookCopy ----------

    [Test]
    public void ANewHoldingHasEveryVolumeAvailable()
    {
        var copy = BookCopy.Create(ABook, Midtown, 6).Value;

        copy.TotalCount.Should().Be(6);
        copy.AvailableCount.Should().Be(6);
        copy.HasStock.Should().BeTrue();
    }

    [Test]
    public void TakingAVolumeReducesOnlyWhatIsAvailable()
    {
        var copy = BookCopy.Create(ABook, Midtown, 6).Value;

        copy.Take().IsSuccess.Should().BeTrue();

        copy.AvailableCount.Should().Be(5);
        copy.TotalCount.Should().Be(6, "the library still owns six");
    }

    [Test]
    public void TakingFromAnEmptyShelfIsRefused()
    {
        var copy = BookCopy.Create(ABook, Midtown, 1).Value;
        copy.Take();

        copy.Take().Error.Should().Be(CatalogErrors.NoCopiesAvailable);
        copy.AvailableCount.Should().Be(0, "a refused take must not push the count negative");
    }

    [Test]
    public void ReturningAVolumePutsItBack()
    {
        var copy = BookCopy.Create(ABook, Midtown, 2).Value;
        copy.Take();

        copy.Return();

        copy.AvailableCount.Should().Be(2);
    }

    [Test]
    public void ADuplicatedReturnCannotInflateTheShelf()
    {
        // A retried message must not leave the branch holding more than it owns.
        var copy = BookCopy.Create(ABook, Midtown, 2).Value;
        copy.Take();

        copy.Return();
        copy.Return();

        copy.AvailableCount.Should().Be(2);
    }

    [Test]
    public void AHoldingOfZeroVolumesIsRefused()
    {
        BookCopy.Create(ABook, Midtown, 0).Error.Should().Be(CatalogErrors.CopyQuantityInvalid);
    }
}
