using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Membership;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Features.Reservations.Commands.CheckInReservation;
using Astrolabe.Application.Features.Reservations.Commands.ConfirmReservation;
using Astrolabe.Application.Shared.Catalog;
using Astrolabe.Application.Tests.TestSupport;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Catalog.ValueObjects;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Membership.ValueObjects;
using Astrolabe.Domain.Features.Network.ValueObjects;
using Astrolabe.Domain.Features.Reservations.Entities;
using Astrolabe.Domain.Features.Reservations.Enums;
using Astrolabe.Domain.Features.Reservations.Errors;
using Astrolabe.Domain.Primitives;
using FluentAssertions;
using Moq;

namespace Astrolabe.Application.Tests.Features.Reservations;

/// <summary>
/// Covers the reservation handlers.
///
/// The race itself is a database concern and is exercised against the running system, not here: a
/// mock cannot lose an optimistic-concurrency check. What these tests guard is everything the
/// handler decides *before* the commit — who may take a copy, whether a replay takes a second one,
/// and whether a check-in belongs to the caller's desk at all.
/// </summary>
[TestFixture]
public sealed class ReservationHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid MemberId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CityId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Midtown = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Loop = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid OtherCity = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private ReservationUnitOfWorkMock _reservations = null!;
    private AuditUnitOfWorkMock _audit = null!;
    private Mock<ICurrentUser> _currentUser = null!;
    private Mock<IEntitlementProvider> _entitlements = null!;
    private Mock<ILibraryLocationProvider> _locations = null!;
    private Mock<ILibraryScopeProvider> _scope = null!;

    private static CancellationToken Ct => TestContext.CurrentContext.CancellationToken;

    [SetUp]
    public void SetUp()
    {
        _reservations = new ReservationUnitOfWorkMock();
        _audit = new AuditUnitOfWorkMock();

        _currentUser = new Mock<ICurrentUser>();
        _currentUser.SetupGet(u => u.UserId).Returns(MemberId);
        _currentUser.SetupGet(u => u.Role).Returns(UserRole.Member);

        _entitlements = new Mock<IEntitlementProvider>();
        OnPlan(PlanTier.Max);

        _locations = new Mock<ILibraryLocationProvider>();
        _locations.Setup(l => l.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, BookProjection.LibraryLocation>
            {
                [Midtown] = new(Midtown, "Midtown", CityId, "New York"),
                [Loop] = new(Loop, "Loop", OtherCity, "Chicago"),
            });

        _scope = new Mock<ILibraryScopeProvider>();
        _scope.Setup(s => s.GetCurrentScopeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(LibraryScope.Unrestricted());
    }

    private void OnPlan(PlanTier plan) =>
        _entitlements.Setup(e => e.GetForCurrentMemberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(PlanCatalog.EntitlementFor(plan, CityId, Midtown));

    private Book ABook(PlanTier tier = PlanTier.Basic, int atMidtown = 2, int atLoop = 1)
    {
        var book = Book.CreateDraft(
            Isbn.Create("9780553383806").Value, "The House of the Spirits", "Isabel Allende",
            null, Genre.Fiction, tier, Money.FromUnits(18), null, Now).Value;

        if (atMidtown > 0)
        {
            book.AddCopies(Midtown, atMidtown);
        }

        if (atLoop > 0)
        {
            book.AddCopies(Loop, atLoop);
        }

        book.Publish(Now);
        book.ClearDomainEvents();

        _reservations.Books
            .Setup(r => r.GetWithCopiesAsync(book.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);
        _reservations.Books
            .Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        return book;
    }

    private ConfirmReservationCommandHandler ConfirmHandler() =>
        new(_reservations.Object, _audit.Object, _entitlements.Object, _locations.Object,
            _currentUser.Object, new FixedClock(Now));

    private CheckInReservationCommandHandler CheckInHandler() =>
        new(_reservations.Object, _audit.Object, _scope.Object,
            _currentUser.Object, new FixedClock(Now));

    // ---------- Confirming ----------

    [Test]
    public async Task Confirming_TakesExactlyOneCopyAndCommitsOnce()
    {
        var book = ABook(atMidtown: 2);

        var result = await ConfirmHandler().Handle(
            new ConfirmReservationCommand(book.Id, Midtown, DeliveryMethod.Collection, null), Ct);

        result.IsSuccess.Should().BeTrue();
        book.CopyAt(Midtown)!.AvailableCount.Should().Be(1);
        _reservations.Saved.Should().Be(1);
    }

    [Test]
    public async Task Confirming_WritesAnAuditEntryInTheSameCommit()
    {
        var book = ABook();

        await ConfirmHandler().Handle(
            new ConfirmReservationCommand(book.Id, Midtown, DeliveryMethod.Collection, null), Ct);

        _audit.Entries.Verify(r => r.AddAsync(
            It.Is<Domain.Features.Audit.Entities.AuditEntry>(e => e.Action == "reservations.confirmed"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AReplayedIdempotencyKey_ReturnsTheFirstReservationAndTouchesNoStock()
    {
        // AC-RSV-006. Checked before the shelf is even read, so a retry cannot take a second copy.
        var book = ABook(atMidtown: 2);

        var first = Reservation.Confirm(
            MemberId, book.Id, book.CopyAt(Midtown)!.Id, Midtown,
            DeliveryMethod.Collection, "key-1", Now);

        _reservations.Reservations
            .Setup(r => r.GetByIdempotencyKeyAsync(MemberId, "key-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(first);

        var result = await ConfirmHandler().Handle(
            new ConfirmReservationCommand(book.Id, Midtown, DeliveryMethod.Collection, "key-1"), Ct);

        result.Value.Id.Should().Be(first.Id);
        book.CopyAt(Midtown)!.AvailableCount.Should().Be(2, "the shelf must be untouched");
        _reservations.Saved.Should().Be(0);
    }

    [Test]
    public async Task HoldingTheSameCopyAlready_IsRefused()
    {
        // AC-RSV-005.
        var book = ABook();
        _reservations.Reservations
            .Setup(r => r.HasActiveForCopyAsync(MemberId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await ConfirmHandler().Handle(
            new ConfirmReservationCommand(book.Id, Midtown, DeliveryMethod.Collection, null), Ct);

        result.Error.Should().Be(ReservationErrors.AlreadyReserved);
        _reservations.Saved.Should().Be(0);
    }

    [Test]
    public async Task ALibraryHoldingNoCopy_IsRefused()
    {
        var book = ABook(atMidtown: 1, atLoop: 0);

        var result = await ConfirmHandler().Handle(
            new ConfirmReservationCommand(book.Id, Loop, DeliveryMethod.Collection, null), Ct);

        result.Error.Should().Be(ReservationErrors.NoCopyAtLibrary);
    }

    [Test]
    public async Task AnEmptyShelf_IsRefusedWithoutTakingTheCountNegative()
    {
        var book = ABook(atMidtown: 1);
        book.CopyAt(Midtown)!.Take();

        var result = await ConfirmHandler().Handle(
            new ConfirmReservationCommand(book.Id, Midtown, DeliveryMethod.Collection, null), Ct);

        result.Error.Should().Be(ReservationErrors.CopyJustTaken);
        book.CopyAt(Midtown)!.AvailableCount.Should().Be(0);
        _reservations.Saved.Should().Be(0);
    }

    [Test]
    public async Task ABasicMember_IsRefusedACopyOutsideTheirHomeLibrary()
    {
        // AC-RSV-004. The reason comes from catalog and is reworded for the borrower, never
        // reinvented — a member told one thing on the modal and another on the button learns nothing.
        OnPlan(PlanTier.Basic);
        var book = ABook(tier: PlanTier.Basic, atMidtown: 1, atLoop: 1);

        var result = await ConfirmHandler().Handle(
            new ConfirmReservationCommand(book.Id, Loop, DeliveryMethod.Collection, null), Ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("reservations.home_library_only");
        result.Error.Message.Should().Contain("Midtown");
        book.CopyAt(Loop)!.AvailableCount.Should().Be(1, "a refusal must not move stock");
    }

    [Test]
    public async Task ABasicMember_IsRefusedAHigherTierEvenAtHome()
    {
        OnPlan(PlanTier.Basic);
        var book = ABook(tier: PlanTier.Max, atMidtown: 3);

        var result = await ConfirmHandler().Handle(
            new ConfirmReservationCommand(book.Id, Midtown, DeliveryMethod.Collection, null), Ct);

        result.Error.Code.Should().Be("reservations.not_in_basic_catalog");
    }

    [Test]
    public async Task APlusMember_IsRefusedACopyInAnotherCity()
    {
        OnPlan(PlanTier.Plus);
        var book = ABook(tier: PlanTier.Basic, atLoop: 2);

        var result = await ConfirmHandler().Handle(
            new ConfirmReservationCommand(book.Id, Loop, DeliveryMethod.Collection, null), Ct);

        result.Error.Code.Should().Be("reservations.outside_city");
        result.Error.Message.Should().Contain("Chicago");
    }

    [Test]
    public async Task AnAnonymousCaller_NeverReachesTheShelf()
    {
        _currentUser.SetupGet(u => u.UserId).Returns((Guid?)null);

        var result = await ConfirmHandler().Handle(
            new ConfirmReservationCommand(Guid.NewGuid(), Midtown, DeliveryMethod.Collection, null), Ct);

        result.IsFailure.Should().BeTrue();
        _reservations.Books.Verify(
            r => r.GetWithCopiesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- Checking in ----------

    private Reservation AnActiveReservation(Book book, Guid libraryId)
    {
        var reservation = Reservation.Confirm(
            MemberId, book.Id, book.CopyAt(libraryId)!.Id, libraryId,
            DeliveryMethod.Collection, null, Now);
        reservation.ClearDomainEvents();

        _reservations.Reservations
            .Setup(r => r.GetByIdAsync(reservation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        return reservation;
    }

    [Test]
    public async Task CheckingIn_PutsExactlyOneCopyBack()
    {
        // AC-RSV-009.
        _currentUser.SetupGet(u => u.Role).Returns(UserRole.Admin);
        var book = ABook(atMidtown: 2);
        book.CopyAt(Midtown)!.Take();
        var reservation = AnActiveReservation(book, Midtown);

        var result = await CheckInHandler().Handle(
            new CheckInReservationCommand(reservation.Id), Ct);

        result.IsSuccess.Should().BeTrue();
        reservation.Status.Should().Be(ReservationStatus.Returned);
        book.CopyAt(Midtown)!.AvailableCount.Should().Be(2);
    }

    [Test]
    public async Task ASecondCheckIn_DoesNotPutASecondCopyBack()
    {
        // BR-RSV-019. Two members of staff scanning one parcel must not invent a volume.
        _currentUser.SetupGet(u => u.Role).Returns(UserRole.Admin);
        var book = ABook(atMidtown: 2);
        book.CopyAt(Midtown)!.Take();
        var reservation = AnActiveReservation(book, Midtown);

        await CheckInHandler().Handle(new CheckInReservationCommand(reservation.Id), Ct);
        await CheckInHandler().Handle(new CheckInReservationCommand(reservation.Id), Ct);

        book.CopyAt(Midtown)!.AvailableCount.Should().Be(2);
    }

    [Test]
    public async Task AMember_CannotCheckInTheirOwnLoan()
    {
        _currentUser.SetupGet(u => u.Role).Returns(UserRole.Member);
        var book = ABook();
        var reservation = AnActiveReservation(book, Midtown);

        var result = await CheckInHandler().Handle(
            new CheckInReservationCommand(reservation.Id), Ct);

        result.IsFailure.Should().BeTrue();
        reservation.Status.Should().Be(ReservationStatus.Reserved);
    }

    [Test]
    public async Task ALibrarianOfAnotherLibrary_IsRefused()
    {
        // AC-RSV-010. A librarian receives copies at their own desk, not at somebody else's.
        _currentUser.SetupGet(u => u.Role).Returns(UserRole.Admin);
        _scope.Setup(s => s.GetCurrentScopeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(LibraryScope.Of([Loop]));

        var book = ABook(atMidtown: 2);
        book.CopyAt(Midtown)!.Take();
        var reservation = AnActiveReservation(book, Midtown);

        var result = await CheckInHandler().Handle(
            new CheckInReservationCommand(reservation.Id), Ct);

        result.Error.Should().Be(ReservationErrors.LibraryOutOfScope);
        book.CopyAt(Midtown)!.AvailableCount.Should().Be(1, "a refusal must not move stock");
    }

    [Test]
    public async Task CheckingInLate_RecordsTheDaysWithoutPricingThem()
    {
        // AC-RSV-012. billing owns the rate; this handler must not learn it.
        var book = ABook(atMidtown: 1);
        book.CopyAt(Midtown)!.Take();

        var reservation = Reservation.Confirm(
            MemberId, book.Id, book.CopyAt(Midtown)!.Id, Midtown,
            DeliveryMethod.Collection, null, Now.AddDays(-20));
        reservation.ClearDomainEvents();

        _reservations.Reservations
            .Setup(r => r.GetByIdAsync(reservation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _currentUser.SetupGet(u => u.Role).Returns(UserRole.Admin);

        await CheckInHandler().Handle(new CheckInReservationCommand(reservation.Id), Ct);

        reservation.DaysLateAtCheckIn.Should().Be(6);
    }

    private sealed class FixedClock(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }
}
