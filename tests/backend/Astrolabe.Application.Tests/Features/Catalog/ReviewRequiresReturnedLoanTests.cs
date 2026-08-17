using Astrolabe.Application.Abstractions.Catalog;
using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Features.Catalog.Commands.PublishReview;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Catalog.ValueObjects;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Primitives;
using Astrolabe.Domain.Features.Catalog.Errors;
using Astrolabe.Domain.Features.Catalog.Repositories;
using FluentAssertions;
using Moq;

namespace Astrolabe.Application.Tests.Features.Catalog;

/// <summary>
/// BR-CAT-032 — a member may review a book once they have borrowed it and given it back.
/// </summary>
/// <remarks>
/// <para>
/// This corrects a rule the specification had recorded backwards. `catalog.business.md` said a
/// member may review a book they never borrowed, "the prototype places no restriction" — and the
/// prototype does: it gates its rating dialog on <c>canRate: done &amp;&amp; !isLibrarian</c> and
/// opens it from a returned loan, never from the catalogue. The dialog's own first line reads "You
/// returned this copy on {date}".
/// </para>
/// <para>
/// The rule is what makes the displayed average mean anything. A rating anybody can leave on a book
/// they never opened measures reach, not quality, and `BR-CAT-030` puts that number on every card.
/// </para>
/// </remarks>
[TestFixture]
public sealed class ReviewRequiresReturnedLoanTests
{
    private static readonly Guid MemberId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid BookId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);

    private Mock<IBorrowingHistoryProbe> _history = null!;
    private Mock<IReviewRepository> _reviews = null!;
    private Mock<ICatalogUnitOfWork> _catalog = null!;

    private static CancellationToken Ct => TestContext.CurrentContext.CancellationToken;

    [SetUp]
    public void SetUp()
    {
        var book = Book.CreateDraft(
            Isbn.Create("9780553383806").Value, "The House of the Spirits", "Isabel Allende",
            null, Genre.Fiction, PlanTier.Basic, Money.FromUnits(18), null, Now).Value;

        book.Publish(Now);

        var books = new Mock<IBookRepository>();
        books.Setup(r => r.GetByIdAsync(BookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        _reviews = new Mock<IReviewRepository>();
        _reviews.Setup(r => r.GetByMemberAndBookAsync(
                MemberId, BookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Review?)null);

        _history = new Mock<IBorrowingHistoryProbe>();

        _catalog = new Mock<ICatalogUnitOfWork>();
        _catalog.SetupGet(u => u.Books).Returns(books.Object);
        _catalog.SetupGet(u => u.Reviews).Returns(_reviews.Object);
    }

    private PublishReviewCommandHandler Handler()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(u => u.UserId).Returns(MemberId);

        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(c => c.UtcNow).Returns(Now);

        return new PublishReviewCommandHandler(
            _catalog.Object, _history.Object, currentUser.Object, clock.Object);
    }

    [Test]
    public async Task AMemberWhoNeverBorrowedItCannotReviewIt()
    {
        _history.Setup(h => h.HasReturnedAsync(MemberId, BookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await Handler().Handle(new PublishReviewCommand(BookId, 5, "Loved it"), Ct);

        result.Error.Should().Be(CatalogErrors.ReviewRequiresReturnedLoan);

        // Nothing staged, and nothing committed. A refusal that still wrote the row would be worse
        // than no rule at all.
        _reviews.Verify(
            r => r.AddAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()), Times.Never);
        _catalog.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task AMemberStillHoldingTheBookCannotReviewItYet()
    {
        // The probe answers "returned", not "borrowed". A loan that is still out is not a book the
        // member has finished, and the prototype's dialog would not open for it either.
        _history.Setup(h => h.HasReturnedAsync(MemberId, BookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await Handler().Handle(new PublishReviewCommand(BookId, 4, null), Ct);

        result.Error.Should().Be(CatalogErrors.ReviewRequiresReturnedLoan);
    }

    [Test]
    public async Task AMemberWhoReturnedItMayReviewIt()
    {
        _history.Setup(h => h.HasReturnedAsync(MemberId, BookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await Handler().Handle(new PublishReviewCommand(BookId, 5, "Loved it"), Ct);

        result.IsSuccess.Should().BeTrue();
        _reviews.Verify(
            r => r.AddAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task TheHistoryIsCheckedBeforeAnythingIsWritten()
    {
        // Ordering matters: the probe crosses a context boundary, and a handler that staged the
        // review first would leave the unit of work holding work it then declined to commit.
        _history.Setup(h => h.HasReturnedAsync(MemberId, BookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Handler().Handle(new PublishReviewCommand(BookId, 3, null), Ct);

        _history.Verify(
            h => h.HasReturnedAsync(MemberId, BookId, It.IsAny<CancellationToken>()), Times.Once);
        _reviews.Verify(
            r => r.GetByMemberAndBookAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
