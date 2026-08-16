using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Features.Billing.Commands.AssessFine;
using Astrolabe.Application.Features.Billing.Commands.IssueDeskPayment;
using Astrolabe.Application.Features.Billing.Commands.PayFines;
using Astrolabe.Application.Features.Billing.Commands.ValidateDeskPayment;
using Astrolabe.Application.Shared.Catalog;
using Astrolabe.Application.Tests.TestSupport;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Billing.Entities;
using Astrolabe.Domain.Features.Billing.Enums;
using Astrolabe.Domain.Features.Billing.Errors;
using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Catalog.ValueObjects;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Network.ValueObjects;
using Astrolabe.Domain.Features.Reservations.Entities;
using Astrolabe.Domain.Features.Reservations.Enums;
using Astrolabe.Domain.Primitives;
using FluentAssertions;
using Moq;

namespace Astrolabe.Application.Tests.Features.Billing;

/// <summary>
/// Covers the billing handlers.
///
/// The arithmetic is pinned against <c>FinePolicy</c> in the domain tests. What is guarded here is
/// what the handlers decide around it: that a second assessment does not double-bill, that a debt
/// promised to a counter cannot also be paid by card, and that only the owning library's staff can
/// take the money.
/// </summary>
[TestFixture]
public sealed class BillingHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid MemberId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Midtown = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Loop = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid CityId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private BillingUnitOfWorkMock _billing = null!;
    private AuditUnitOfWorkMock _audit = null!;
    private Mock<ICurrentUser> _currentUser = null!;
    private Mock<ILibraryLocationProvider> _locations = null!;
    private Mock<ILibraryScopeProvider> _scope = null!;

    private static CancellationToken Ct => TestContext.CurrentContext.CancellationToken;

    [SetUp]
    public void SetUp()
    {
        _billing = new BillingUnitOfWorkMock();
        _audit = new AuditUnitOfWorkMock();

        _currentUser = new Mock<ICurrentUser>();
        _currentUser.SetupGet(u => u.UserId).Returns(MemberId);
        _currentUser.SetupGet(u => u.Role).Returns(UserRole.Member);

        _locations = new Mock<ILibraryLocationProvider>();
        _locations.Setup(l => l.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, BookProjection.LibraryLocation>
            {
                [Midtown] = new(Midtown, "Midtown", CityId, "New York"),
                [Loop] = new(Loop, "Loop", Guid.NewGuid(), "Chicago"),
            });

        _scope = new Mock<ILibraryScopeProvider>();
        _scope.Setup(s => s.GetCurrentScopeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(LibraryScope.Unrestricted());
    }

    private static Fine AFine(int daysLate = 20, Guid? libraryId = null)
    {
        var fine = Fine.Assess(
            MemberId, Guid.NewGuid(), libraryId ?? Midtown,
            "The Savage Detectives", daysLate, Now)!;
        fine.ClearDomainEvents();
        return fine;
    }

    private void TheMemberHas(params Fine[] fines)
    {
        _billing.Fines
            .Setup(r => r.GetByIdsForMemberAsync(
                MemberId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, IReadOnlyCollection<Guid> ids, CancellationToken _) =>
                fines.Where(f => ids.Contains(f.Id)).ToList());
    }

    private PaymentMethod ACard()
    {
        var card = PaymentMethod.Create(
            MemberId, CardBrand.Visa, "4242", "09/28", "Francisco Torreglosa", true).Value;

        _billing.PaymentMethods
            .Setup(r => r.GetForMemberAsync(MemberId, card.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(card);

        return card;
    }

    // ---------- Assessing ----------

    private AssessFineCommandHandler AssessHandler(
        Mock<Domain.Features.Reservations.Repositories.IReservationRepository> reservations,
        Mock<Domain.Features.Catalog.Repositories.IBookRepository> books) =>
        new(_billing.Object, reservations.Object, books.Object, new FixedClock(Now));

    private (Mock<Domain.Features.Reservations.Repositories.IReservationRepository>,
             Mock<Domain.Features.Catalog.Repositories.IBookRepository>, Reservation)
        AReturnedReservation(int daysLate)
    {
        var book = Book.CreateDraft(
            Isbn.Create("9780312427480").Value, "The Savage Detectives", "Roberto Bolano",
            null, Genre.Fiction, PlanTier.Plus, Money.FromUnits(24), null, Now).Value;
        book.AddCopies(Midtown, 1);
        book.Publish(Now);

        var reservation = Reservation.Confirm(
            MemberId, book.Id, book.CopyAt(Midtown)!.Id, Midtown,
            DeliveryMethod.Collection, null, Now.AddDays(-(14 + daysLate)));
        reservation.CheckIn(Now);
        reservation.ClearDomainEvents();

        var reservations = new Mock<Domain.Features.Reservations.Repositories.IReservationRepository>();
        reservations.Setup(r => r.GetByIdAsync(reservation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var books = new Mock<Domain.Features.Catalog.Repositories.IBookRepository>();
        books.Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(book);

        return (reservations, books, reservation);
    }

    [Test]
    public async Task AssessingALateReturn_WritesTheFineAndItsCharge()
    {
        var (reservations, books, reservation) = AReturnedReservation(daysLate: 20);

        var result = await AssessHandler(reservations, books)
            .Handle(new AssessFineCommand(reservation.Id), Ct);

        result.Value.Should().NotBeNull();
        _billing.Fines.Verify(r => r.AddAsync(
            It.Is<Fine>(f => f.Amount.Cents == 700), It.IsAny<CancellationToken>()), Times.Once);
        _billing.Ledger.Verify(r => r.AddAsync(
            It.Is<LedgerEntry>(e => e.Kind == LedgerEntryKind.Charge && e.Amount.Cents == -700),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AssessingTheSameReservationTwice_LeavesOneFine()
    {
        // AC-BIL-005. The event handler and the daily sweep both call this, so overlapping must
        // cost one query and nothing else.
        var (reservations, books, reservation) = AReturnedReservation(daysLate: 20);
        var existing = AFine();

        _billing.Fines
            .Setup(r => r.GetByReservationAsync(reservation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await AssessHandler(reservations, books)
            .Handle(new AssessFineCommand(reservation.Id), Ct);

        result.Value.Should().Be(existing.Id);
        _billing.Fines.Verify(
            r => r.AddAsync(It.IsAny<Fine>(), It.IsAny<CancellationToken>()), Times.Never);
        _billing.Saved.Should().Be(0);
    }

    [Test]
    public async Task AnOnTimeReturn_ProducesNoFineAndNoLedgerEntry()
    {
        // AC-BIL-004.
        var (reservations, books, reservation) = AReturnedReservation(daysLate: 0);

        var result = await AssessHandler(reservations, books)
            .Handle(new AssessFineCommand(reservation.Id), Ct);

        result.Value.Should().BeNull();
        _billing.Fines.Verify(
            r => r.AddAsync(It.IsAny<Fine>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ACopyStillOnLoan_IsNotPricedAtAll()
    {
        // A fine must never be assessed before the lateness is final, or BR-BIL-003 is broken the
        // moment the copy comes back.
        var (reservations, books, reservation) = AReturnedReservation(daysLate: 20);
        var stillOut = Reservation.Confirm(
            MemberId, reservation.BookId, Guid.NewGuid(), Midtown,
            DeliveryMethod.Collection, null, Now.AddDays(-40));

        reservations.Setup(r => r.GetByIdAsync(stillOut.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stillOut);

        var result = await AssessHandler(reservations, books)
            .Handle(new AssessFineCommand(stillOut.Id), Ct);

        stillOut.Status.Should().Be(ReservationStatus.Reserved);
        result.Value.Should().BeNull();
    }

    // ---------- Paying by card ----------

    private PayFinesCommandHandler PayHandler() =>
        new(_billing.Object, _audit.Object, _currentUser.Object, new FixedClock(Now));

    [Test]
    public async Task PayingSettlesTheFineAndWritesOnePaymentEntry()
    {
        var fine = AFine();
        var card = ACard();
        TheMemberHas(fine);

        var result = await PayHandler().Handle(new PayFinesCommand([fine.Id], card.Id), Ct);

        result.Value.AmountCents.Should().Be(700);
        result.Value.PaidWith.Should().Be("Visa •••• 4242");
        fine.Status.Should().Be(FineStatus.Paid);
        _billing.Saved.Should().Be(1);
    }

    [Test]
    public async Task PayingTheSameFineTwice_ChargesOnceAndAnswersTheSame()
    {
        // AC-BIL-007. The second call reports the same receipt rather than $0.00, which would read
        // to the member as a failed payment.
        var fine = AFine();
        var card = ACard();
        TheMemberHas(fine);

        var first = await PayHandler().Handle(new PayFinesCommand([fine.Id], card.Id), Ct);
        var second = await PayHandler().Handle(new PayFinesCommand([fine.Id], card.Id), Ct);

        second.Value.AmountCents.Should().Be(first.Value.AmountCents);
        second.Value.FineCount.Should().Be(first.Value.FineCount);
        _billing.Saved.Should().Be(1, "only the first call moved anything");
    }

    [Test]
    public async Task AFineAwaitingValidation_CannotBePaidByCard()
    {
        // AC-BIL-013. Otherwise the librarian later validates a debt the card already cleared.
        var fine = AFine();
        fine.Hold(Guid.NewGuid());
        var card = ACard();
        TheMemberHas(fine);

        var result = await PayHandler().Handle(new PayFinesCommand([fine.Id], card.Id), Ct);

        result.Error.Should().Be(BillingErrors.FineAwaitingValidation);
        _billing.Saved.Should().Be(0);
    }

    [Test]
    public async Task AnotherMembersCard_IsNotFound()
    {
        var fine = AFine();
        TheMemberHas(fine);

        var result = await PayHandler().Handle(new PayFinesCommand([fine.Id], Guid.NewGuid()), Ct);

        result.Error.Should().Be(BillingErrors.PaymentMethodNotFound);
    }

    [Test]
    public async Task PayingNothing_IsRefused()
    {
        var result = await PayHandler().Handle(new PayFinesCommand([], Guid.NewGuid()), Ct);

        result.Error.Should().Be(BillingErrors.NothingToPay);
    }

    // ---------- The desk ----------

    private IssueDeskPaymentCommandHandler IssueHandler() =>
        new(_billing.Object, _audit.Object, _locations.Object, _currentUser.Object, new FixedClock(Now));

    [Test]
    public async Task IssuingACode_HoldsTheFineWithoutSettlingIt()
    {
        // AC-BIL-011. Nobody has paid.
        var fine = AFine();
        TheMemberHas(fine);

        var result = await IssueHandler().Handle(new IssueDeskPaymentCommand([fine.Id]), Ct);

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().MatchRegex(@"^MP-\d{5}$");
        fine.Status.Should().Be(FineStatus.AwaitingValidation);
        fine.IsOwed.Should().BeTrue("issuing a code settles nothing");
    }

    [Test]
    public async Task ACodeExpiresInSeventyTwoHours()
    {
        var fine = AFine();
        TheMemberHas(fine);

        var result = await IssueHandler().Handle(new IssueDeskPaymentCommand([fine.Id]), Ct);

        result.Value.ExpiresAt.Should().Be(Now.AddHours(72));
        result.Value.IsExpired.Should().BeFalse();
    }

    [Test]
    public async Task ACodeCannotSpanTwoLibraries()
    {
        // AC-BIL-015, which follows from BR-BIL-005: only the owning library's staff may validate,
        // so a code spanning two counters could be validated at neither.
        var atMidtown = AFine(libraryId: Midtown);
        var atLoop = AFine(libraryId: Loop);
        TheMemberHas(atMidtown, atLoop);

        var result = await IssueHandler()
            .Handle(new IssueDeskPaymentCommand([atMidtown.Id, atLoop.Id]), Ct);

        result.Error.Should().Be(BillingErrors.FinesSpanLibraries);
        _billing.Saved.Should().Be(0);
    }

    private ValidateDeskPaymentCommandHandler ValidateHandler() =>
        new(_billing.Object, _audit.Object, _scope.Object, _currentUser.Object, new FixedClock(Now));

    private DeskPayment ACode(Fine fine, Guid? libraryId = null)
    {
        var payment = DeskPayment.Issue(
            MemberId, libraryId ?? Midtown, fine.Amount, [fine.Id], Now);
        payment.ClearDomainEvents();
        fine.Hold(payment.Id);

        _billing.DeskPayments
            .Setup(r => r.GetByCodeAsync(payment.Code.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        _billing.Fines
            .Setup(r => r.GetByDeskPaymentAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([fine]);

        return payment;
    }

    [Test]
    public async Task ValidatingSettlesTheFinesTheCodeCovers()
    {
        // AC-BIL-011. This is the act that clears the debt.
        _currentUser.SetupGet(u => u.Role).Returns(UserRole.Admin);
        var fine = AFine();
        var payment = ACode(fine);

        var result = await ValidateHandler()
            .Handle(new ValidateDeskPaymentCommand(payment.Code.Value), Ct);

        result.IsSuccess.Should().BeTrue();
        fine.Status.Should().Be(FineStatus.Paid);
        payment.Status.Should().Be(DeskPaymentStatus.Validated);
    }

    [Test]
    public async Task AnAdministratorOfAnotherLibrary_CannotValidate()
    {
        // AC-BIL-010. A librarian takes money at their own counter.
        _currentUser.SetupGet(u => u.Role).Returns(UserRole.Admin);
        _scope.Setup(s => s.GetCurrentScopeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(LibraryScope.Of([Loop]));

        var fine = AFine(libraryId: Midtown);
        var payment = ACode(fine, Midtown);

        var result = await ValidateHandler()
            .Handle(new ValidateDeskPaymentCommand(payment.Code.Value), Ct);

        result.Error.Should().Be(BillingErrors.LibraryOutOfScope);
        fine.Status.Should().Be(FineStatus.AwaitingValidation, "a refusal must settle nothing");
    }

    [Test]
    public async Task AMember_CannotValidateTheirOwnCode()
    {
        var fine = AFine();
        var payment = ACode(fine);

        var result = await ValidateHandler()
            .Handle(new ValidateDeskPaymentCommand(payment.Code.Value), Ct);

        result.IsFailure.Should().BeTrue();
        fine.Status.Should().Be(FineStatus.AwaitingValidation);
    }

    [Test]
    public async Task AnUnknownCode_IsNotFound()
    {
        _currentUser.SetupGet(u => u.Role).Returns(UserRole.Admin);

        var result = await ValidateHandler().Handle(new ValidateDeskPaymentCommand("MP-00000"), Ct);

        result.Error.Should().Be(BillingErrors.DeskPaymentNotFound);
    }

    [Test]
    public async Task AMalformedCode_IsRefusedBeforeAnyLookup()
    {
        _currentUser.SetupGet(u => u.Role).Returns(UserRole.Admin);

        await ValidateHandler().Handle(new ValidateDeskPaymentCommand("nonsense"), Ct);

        _billing.DeskPayments.Verify(
            r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class FixedClock(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }
}
