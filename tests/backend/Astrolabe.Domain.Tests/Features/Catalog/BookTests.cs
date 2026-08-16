using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Catalog.Errors;
using Astrolabe.Domain.Features.Catalog.Events;
using Astrolabe.Domain.Features.Catalog.ValueObjects;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Primitives;
using FluentAssertions;

namespace Astrolabe.Domain.Tests.Features.Catalog;

/// <summary>Covers the book aggregate: BR-CAT-001 to BR-CAT-005 and BR-CAT-020 to BR-CAT-026.</summary>
[TestFixture]
public sealed class BookTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Midtown = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Harlem = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static Book ADraft(PlanTier tier = PlanTier.Basic) =>
        Book.CreateDraft(
            Isbn.Create("9780141036144").Value, "Klara and the Sun", "Kazuo Ishiguro", "Faber",
            Genre.Fiction, tier, Money.FromUnits(18, 99), null, Now).Value;

    private static Book InCatalog(PlanTier tier = PlanTier.Basic)
    {
        var book = ADraft(tier);
        book.Publish(Now);
        book.ClearDomainEvents();
        return book;
    }

    // ---------- Creation ----------

    [Test]
    public void ANewBookStartsAsADraftAndIsNotVisibleToMembers()
    {
        // BR-CAT-022.
        var book = ADraft();

        book.Status.Should().Be(BookStatus.Draft);
        book.IsVisibleToMembers.Should().BeFalse();
    }

    [Test]
    public void ABookWithoutATitleOrAnAuthorIsRefused()
    {
        var isbn = Isbn.Create("9780141036144").Value;

        Book.CreateDraft(isbn, "  ", "Ishiguro", null, Genre.Fiction, PlanTier.Basic, Money.Zero, null, Now)
            .Error.Should().Be(CatalogErrors.TitleRequired);
        Book.CreateDraft(isbn, "Klara", "  ", null, Genre.Fiction, PlanTier.Basic, Money.Zero, null, Now)
            .Error.Should().Be(CatalogErrors.AuthorRequired);
    }

    [Test]
    public void ANegativePriceIsRefused()
    {
        Book.CreateDraft(
                Isbn.Create("9780141036144").Value, "Klara", "Ishiguro", null,
                Genre.Fiction, PlanTier.Basic, Money.FromCents(-1), null, Now)
            .Error.Should().Be(CatalogErrors.PriceInvalid);
    }

    [Test]
    public void BlankOptionalFieldsAreStoredAsNullRatherThanWhitespace()
    {
        // Otherwise a blank publisher renders as an empty line the interface cannot distinguish
        // from a real one.
        var book = Book.CreateDraft(
            Isbn.Create("9780141036144").Value, " Klara ", " Ishiguro ", "   ",
            Genre.Fiction, PlanTier.Basic, Money.Zero, "  ", Now).Value;

        book.Publisher.Should().BeNull();
        book.CoverUrl.Should().BeNull();
        book.Title.Should().Be("Klara");
        book.Author.Should().Be("Ishiguro");
    }

    // ---------- Lifecycle, BR-CAT-021 ----------

    [Test]
    public void PublishingADraftPutsItInTheCatalogueAndRaisesTheEvent()
    {
        var book = ADraft();

        book.Publish(Now).IsSuccess.Should().BeTrue();

        book.Status.Should().Be(BookStatus.Catalog);
        book.IsVisibleToMembers.Should().BeTrue();
        book.DomainEvents.Should().ContainSingle(e => e is BookPublished);
    }

    [Test]
    public void PublishingABookAlreadyInTheCatalogueIsRefused()
    {
        InCatalog().Publish(Now).IsFailure.Should().BeTrue();
    }

    [Test]
    public void ABookGoesToRepairAndComesBack()
    {
        var book = InCatalog();

        book.SendToRepair(RepairReason.WaterDamage, Now.AddDays(14), "Soaked", Now)
            .IsSuccess.Should().BeTrue();
        book.Status.Should().Be(BookStatus.Repair);
        book.IsVisibleToMembers.Should().BeFalse("a book in repair leaves member-facing search");

        book.ReturnFromRepair(Now.AddDays(20)).IsSuccess.Should().BeTrue();
        book.Status.Should().Be(BookStatus.Catalog);
    }

    [Test]
    public void TheRepairEventCarriesTheStatedReason()
    {
        // BR-CAT-025: the audit entry records the reason, and it cannot be recovered from the
        // entity after a second transition.
        var book = InCatalog();

        book.SendToRepair(RepairReason.MissingPages, null, "Pages 40-58", Now);

        book.DomainEvents.OfType<BookSentToRepair>().Single()
            .Reason.Should().Be(RepairReason.MissingPages);
    }

    [Test]
    public void ADraftCannotBeSentStraightToRepair()
    {
        ADraft().SendToRepair(RepairReason.Rebinding, null, null, Now)
            .IsFailure.Should().BeTrue();
    }

    [Test]
    public void ARemovedBookCanBeRestoredAndKeepsItsRating()
    {
        var book = InCatalog();
        book.SetRating(4.5m, 8);

        book.Remove(RemovalReason.Donated, null, Now).IsSuccess.Should().BeTrue();
        book.Status.Should().Be(BookStatus.Deleted);

        book.Restore(Now.AddDays(1)).IsSuccess.Should().BeTrue();
        book.Status.Should().Be(BookStatus.Catalog);
        book.AverageRating.Should().Be(4.5m, "removing a book must not silently change its score");
        book.ReviewCount.Should().Be(8);
    }

    [Test]
    public void RemovingAnAlreadyRemovedBookIsRefused()
    {
        var book = InCatalog();
        book.Remove(RemovalReason.LostByMember, null, Now);

        book.Remove(RemovalReason.Donated, null, Now).IsFailure.Should().BeTrue();
    }

    [Test]
    public void ABookInRepairCanStillBeRemoved()
    {
        // A book found to be beyond repair must not have to be returned to the shelf first.
        var book = InCatalog();
        book.SendToRepair(RepairReason.DamagedSpine, null, null, Now);

        book.Remove(RemovalReason.DamagedBeyondRepair, null, Now).IsSuccess.Should().BeTrue();
    }

    // ---------- Stock, BR-CAT-002 ----------

    [Test]
    public void AddingCopiesAtANewLibraryCreatesOneHolding()
    {
        var book = InCatalog();

        book.AddCopies(Midtown, 4).IsSuccess.Should().BeTrue();

        book.Copies.Should().ContainSingle();
        book.CopyAt(Midtown)!.TotalCount.Should().Be(4);
        book.CopyAt(Midtown)!.AvailableCount.Should().Be(4);
    }

    [Test]
    public void AddingCopiesAtTheSameLibraryMergesRatherThanDuplicating()
    {
        // Two rows for one branch would make its stock a sum nobody remembers to compute.
        var book = InCatalog();

        book.AddCopies(Midtown, 4);
        book.AddCopies(Midtown, 2);

        book.Copies.Should().ContainSingle();
        book.CopyAt(Midtown)!.TotalCount.Should().Be(6);
    }

    [Test]
    public void EachLibraryKeepsItsOwnCount()
    {
        var book = InCatalog();

        book.AddCopies(Midtown, 4);
        book.AddCopies(Harlem, 2);

        book.Copies.Should().HaveCount(2);
        book.CopyAt(Harlem)!.TotalCount.Should().Be(2);
    }

    [Test]
    public void AddingZeroOrFewerCopiesIsRefused()
    {
        var book = InCatalog();

        book.AddCopies(Midtown, 0).Error.Should().Be(CatalogErrors.CopyQuantityInvalid);
        book.AddCopies(Midtown, -1).Error.Should().Be(CatalogErrors.CopyQuantityInvalid);
        book.Copies.Should().BeEmpty();
    }

    // ---------- Rating, BR-CAT-030 ----------

    [Test]
    public void ABookWithNoReviewsReportsNoRatingRatherThanZero()
    {
        // AC-CAT-014. A zero would sort below every reviewed book and read as unanimous dislike.
        var book = InCatalog();

        book.SetRating(null, 0);

        book.AverageRating.Should().BeNull();
        book.ReviewCount.Should().Be(0);
    }

    [Test]
    public void RemovingTheLastReviewClearsTheRating()
    {
        var book = InCatalog();
        book.SetRating(5m, 1);

        book.SetRating(0m, 0);

        book.AverageRating.Should().BeNull("a count of zero means no rating, whatever average arrives");
    }
}
