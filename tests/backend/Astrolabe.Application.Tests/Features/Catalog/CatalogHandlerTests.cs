using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Membership;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Features.Catalog.Commands.PublishReview;
using Astrolabe.Application.Features.Catalog.Commands.RemoveBook;
using Astrolabe.Application.Features.Catalog.Commands.SendBookToRepair;
using Astrolabe.Application.Features.Catalog.Queries.GetBookDetail;
using Astrolabe.Application.Features.Catalog.Queries.SearchBooks;
using Astrolabe.Application.Shared.Catalog;
using Astrolabe.Application.Tests.TestSupport;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Catalog.Errors;
using Astrolabe.Domain.Features.Catalog.ValueObjects;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Membership.ValueObjects;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Primitives;
using FluentAssertions;
using Moq;

namespace Astrolabe.Application.Tests.Features.Catalog;

/// <summary>
/// Covers the catalog handlers: BR-CAT-016, BR-CAT-020, BR-CAT-022, BR-CAT-025 and BR-CAT-027.
///
/// The access rule itself is exercised exhaustively against the policy in the domain tests. What is
/// tested here is what the handlers add on top: who may call them, which books they will admit to
/// seeing, and that a lifecycle change writes the trail BR-CAT-025 requires.
/// </summary>
[TestFixture]
public sealed class CatalogHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid MemberId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid CityId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid LibraryId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private CatalogUnitOfWorkMock _catalog = null!;
    private AuditUnitOfWorkMock _audit = null!;
    private Mock<ICurrentUser> _currentUser = null!;
    private Mock<IEntitlementProvider> _entitlements = null!;
    private Mock<ILibraryLocationProvider> _locations = null!;

    private static CancellationToken Ct => TestContext.CurrentContext.CancellationToken;

    [SetUp]
    public void SetUp()
    {
        _catalog = new CatalogUnitOfWorkMock();
        _audit = new AuditUnitOfWorkMock();

        _currentUser = new Mock<ICurrentUser>();
        _currentUser.SetupGet(u => u.UserId).Returns(MemberId);
        _currentUser.SetupGet(u => u.Role).Returns(UserRole.Admin);

        _entitlements = new Mock<IEntitlementProvider>();
        _entitlements.Setup(e => e.GetForCurrentMemberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(PlanCatalog.EntitlementFor(PlanTier.Max, CityId, LibraryId));

        _locations = new Mock<ILibraryLocationProvider>();
        _locations.Setup(l => l.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, BookProjection.LibraryLocation>
            {
                [LibraryId] = new(LibraryId, "Midtown", CityId, "New York", IsActive: true)
            });
    }

    private static Book ABook(BookStatus status = BookStatus.Catalog)
    {
        var book = Book.CreateDraft(
            Isbn.Create("9780553383806").Value, "The House of the Spirits", "Isabel Allende",
            null, Genre.Fiction, PlanTier.Basic, Money.FromUnits(18), null, Now).Value;

        book.AddCopies(LibraryId, 3);

        if (status is not BookStatus.Draft)
        {
            book.Publish(Now);
        }

        if (status is BookStatus.Repair)
        {
            book.SendToRepair(RepairReason.WaterDamage, null, null, Now);
        }

        if (status is BookStatus.Deleted)
        {
            book.Remove(RemovalReason.Donated, null, Now);
        }

        book.ClearDomainEvents();
        return book;
    }

    private void TheCatalogHolds(Book book)
    {
        _catalog.Books
            .Setup(r => r.GetWithCopiesAsync(book.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);
        _catalog.Books
            .Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);
    }

    // ---------- Search, BR-CAT-020 ----------

    [Test]
    public async Task AMemberSearch_AsksOnlyForBooksInTheCatalogue()
    {
        // The status is not optional on the repository, so this asserts the handler passes the one
        // value that keeps drafts and removed books away from members.
        _catalog.Books
            .Setup(r => r.SearchAsync(It.IsAny<string?>(), It.IsAny<Genre?>(), It.IsAny<BookStatus?>(),
                It.IsAny<BookSortKey>(), It.IsAny<SortDirection>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PagedResult<Book>.Empty(1, 20));

        await new SearchBooksQueryHandler(_catalog.Object, _entitlements.Object, _locations.Object)
            .Handle(new SearchBooksQuery(null, null), Ct);

        _catalog.Books.Verify(r => r.SearchAsync(
            It.IsAny<string?>(), It.IsAny<Genre?>(), BookStatus.Catalog,
            It.IsAny<BookSortKey>(), It.IsAny<SortDirection>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AMemberSearch_ResolvesTheEntitlementOncePerPageRatherThanPerBook()
    {
        _catalog.Books
            .Setup(r => r.SearchAsync(It.IsAny<string?>(), It.IsAny<Genre?>(), It.IsAny<BookStatus?>(),
                It.IsAny<BookSortKey>(), It.IsAny<SortDirection>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PagedResult<Book>.Create([ABook(), ABook(), ABook()], 1, 20, 3));

        await new SearchBooksQueryHandler(_catalog.Object, _entitlements.Object, _locations.Object)
            .Handle(new SearchBooksQuery(null, null), Ct);

        _entitlements.Verify(e => e.GetForCurrentMemberAsync(It.IsAny<CancellationToken>()), Times.Once);
        _locations.Verify(l => l.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Detail, BR-CAT-016 and BR-CAT-020 ----------

    [Test]
    public async Task ABookTheMemberCannotReserve_StillOpens()
    {
        // BR-CAT-016: reach restricts borrowing, never discovery.
        _entitlements.Setup(e => e.GetForCurrentMemberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(PlanCatalog.EntitlementFor(PlanTier.Basic, CityId, Guid.NewGuid()));

        var book = ABook();
        TheCatalogHolds(book);

        var result = await new GetBookDetailQueryHandler(
                _catalog.Object, _entitlements.Object, _locations.Object)
            .Handle(new GetBookDetailQuery(book.Id), Ct);

        result.IsSuccess.Should().BeTrue();
        result.Value.CanReserve.Should().BeFalse();
        result.Value.Badge.Should().NotBeNull();
        result.Value.Copies.Should().ContainSingle();
    }

    [TestCase(BookStatus.Draft)]
    [TestCase(BookStatus.Repair)]
    [TestCase(BookStatus.Deleted)]
    public async Task ABookOutsideTheCatalogue_IsNotFoundForAMember(BookStatus status)
    {
        // Answering "not found" rather than "not visible" avoids confirming the book exists at all.
        var book = ABook(status);
        TheCatalogHolds(book);

        var result = await new GetBookDetailQueryHandler(
                _catalog.Object, _entitlements.Object, _locations.Object)
            .Handle(new GetBookDetailQuery(book.Id), Ct);

        result.Error.Should().Be(CatalogErrors.BookNotFound);
    }

    // ---------- Lifecycle and the audit trail, BR-CAT-025 ----------

    [Test]
    public async Task SendingABookToRepair_WritesAnAuditEntryCarryingTheReason()
    {
        var book = ABook();
        TheCatalogHolds(book);

        var result = await new SendBookToRepairCommandHandler(
                _catalog.Object, _audit.Object, _currentUser.Object, new FixedClock(Now))
            .Handle(new SendBookToRepairCommand(
                book.Id, RepairReason.MissingPages, null, "Pages 40-58"), Ct);

        result.IsSuccess.Should().BeTrue();

        _audit.Entries.Verify(r => r.AddAsync(
            It.Is<Domain.Features.Audit.Entities.AuditEntry>(entry =>
                entry.Action == "catalog.book_sent_to_repair"
                && entry.ActorUserId == MemberId
                && entry.Detail!.Contains("MissingPages")
                && entry.Detail!.Contains("Pages 40-58")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RemovingABook_WritesAnAuditEntry()
    {
        var book = ABook();
        TheCatalogHolds(book);

        await new RemoveBookCommandHandler(
                _catalog.Object, _audit.Object, _currentUser.Object, new FixedClock(Now))
            .Handle(new RemoveBookCommand(book.Id, RemovalReason.LostByMember, null), Ct);

        _audit.Entries.Verify(r => r.AddAsync(
            It.Is<Domain.Features.Audit.Entities.AuditEntry>(entry =>
                entry.Action == "catalog.book_removed" && entry.Detail == "LostByMember"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ARefusedTransition_WritesNoAuditEntry()
    {
        // A trail that records attempts as if they were changes is worse than no trail.
        var book = ABook(BookStatus.Draft);
        TheCatalogHolds(book);

        var result = await new SendBookToRepairCommandHandler(
                _catalog.Object, _audit.Object, _currentUser.Object, new FixedClock(Now))
            .Handle(new SendBookToRepairCommand(book.Id, RepairReason.Rebinding, null, null), Ct);

        result.IsFailure.Should().BeTrue();
        _audit.Entries.Verify(r => r.AddAsync(
            It.IsAny<Domain.Features.Audit.Entities.AuditEntry>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _catalog.Saved.Should().Be(0);
    }

    [Test]
    public async Task AMemberCannotMoveABookThroughItsLifecycle()
    {
        _currentUser.SetupGet(u => u.Role).Returns(UserRole.Member);
        var book = ABook();
        TheCatalogHolds(book);

        var result = await new RemoveBookCommandHandler(
                _catalog.Object, _audit.Object, _currentUser.Object, new FixedClock(Now))
            .Handle(new RemoveBookCommand(book.Id, RemovalReason.Donated, null), Ct);

        result.Error.Should().Be(NetworkErrors.StaffRequired);
    }

    // ---------- Reviews, BR-CAT-027 ----------

    [Test]
    public async Task ReviewingABookTwice_EditsRatherThanCreatingASecond()
    {
        var book = ABook();
        TheCatalogHolds(book);

        var existing = Review.Publish(book.Id, MemberId, StarRating.Create(5).Value, "Loved it", Now);
        existing.ClearDomainEvents();

        _catalog.Reviews
            .Setup(r => r.GetByMemberAndBookAsync(MemberId, book.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await new PublishReviewCommandHandler(
                _catalog.Object, _currentUser.Object, new FixedClock(Now))
            .Handle(new PublishReviewCommand(book.Id, 2, "Changed my mind"), Ct);

        result.IsSuccess.Should().BeTrue();
        existing.Rating.Stars.Should().Be(2);
        existing.Comment.Should().Be("Changed my mind");
        _catalog.Reviews.Verify(
            r => r.AddAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task AReviewOutsideTheStarScale_IsRefusedBeforeTheBookIsEvenLoaded()
    {
        var result = await new PublishReviewCommandHandler(
                _catalog.Object, _currentUser.Object, new FixedClock(Now))
            .Handle(new PublishReviewCommand(Guid.NewGuid(), 9, null), Ct);

        result.Error.Should().Be(CatalogErrors.RatingOutOfRange);
        _catalog.Books.Verify(
            r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ABookOutsideTheCatalogue_CannotBeReviewed()
    {
        var book = ABook(BookStatus.Deleted);
        TheCatalogHolds(book);

        var result = await new PublishReviewCommandHandler(
                _catalog.Object, _currentUser.Object, new FixedClock(Now))
            .Handle(new PublishReviewCommand(book.Id, 4, null), Ct);

        result.Error.Should().Be(CatalogErrors.BookNotFound);
    }

}
