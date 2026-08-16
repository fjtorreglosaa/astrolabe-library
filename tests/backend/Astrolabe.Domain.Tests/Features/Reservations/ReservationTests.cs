using Astrolabe.Domain.Features.Reservations.Entities;
using Astrolabe.Domain.Features.Reservations.Enums;
using Astrolabe.Domain.Features.Reservations.Errors;
using Astrolabe.Domain.Features.Reservations.Events;
using Astrolabe.Domain.Primitives;
using FluentAssertions;

namespace Astrolabe.Domain.Tests.Features.Reservations;

/// <summary>
/// Covers the reservation aggregate: BR-RSV-001 to BR-RSV-020.
///
/// The return is the part worth guarding. It is two acts by two people, and most of these tests
/// exist to keep the middle state from collapsing — a copy must not be back on the shelf because the
/// member said they posted it.
/// </summary>
[TestFixture]
public sealed class ReservationTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid MemberId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Stranger = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private static readonly Guid BookId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid CopyId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid LibraryId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static Reservation AConfirmedReservation(
        DeliveryMethod delivery = DeliveryMethod.Collection, DateTimeOffset? at = null)
    {
        var reservation = Reservation.Confirm(
            MemberId, BookId, CopyId, LibraryId, delivery, idempotencyKey: null, at ?? Now);
        reservation.ClearDomainEvents();
        return reservation;
    }

    // ---------- Confirming ----------

    [Test]
    public void Confirming_SetsTheDueDateFourteenDaysOut()
    {
        // AC-RSV-001.
        var reservation = Reservation.Confirm(
            MemberId, BookId, CopyId, LibraryId, DeliveryMethod.Collection, null, Now);

        reservation.Period.StartedOn.Should().Be(Now);
        reservation.Period.DueOn.Should().Be(Now.AddDays(14));
        reservation.Status.Should().Be(ReservationStatus.Reserved);
        reservation.IsActive.Should().BeTrue();
    }

    [Test]
    public void Confirming_RaisesTheEventCarryingTheDueDate()
    {
        var reservation = Reservation.Confirm(
            MemberId, BookId, CopyId, LibraryId, DeliveryMethod.Collection, null, Now);

        reservation.DomainEvents.OfType<ReservationConfirmed>().Single()
            .DueOn.Should().Be(Now.AddDays(14));
    }

    [Test]
    public void HomeDelivery_CostsThreeNinetyNineAndCollectionIsFree()
    {
        // AC-RSV-003.
        Reservation.FeeFor(DeliveryMethod.HomeDelivery).Cents.Should().Be(399);
        Reservation.FeeFor(DeliveryMethod.Collection).Should().Be(Money.Zero);

        AConfirmedReservation(DeliveryMethod.HomeDelivery).DeliveryFee.Cents.Should().Be(399);
        AConfirmedReservation(DeliveryMethod.Collection).DeliveryFee.Should().Be(Money.Zero);
    }

    [Test]
    public void AnIdempotencyKeyIsStoredTrimmed_AndBlankBecomesNull()
    {
        Reservation.Confirm(MemberId, BookId, CopyId, LibraryId, DeliveryMethod.Collection, "  k-1  ", Now)
            .IdempotencyKey.Should().Be("k-1");
        Reservation.Confirm(MemberId, BookId, CopyId, LibraryId, DeliveryMethod.Collection, "   ", Now)
            .IdempotencyKey.Should().BeNull("a blank key must not create a row that matches every blank");
    }

    // ---------- Overdue, BR-RSV-010 ----------

    [Test]
    public void OverdueIsReportedFromTheDate_WithNoJobHavingRun()
    {
        // AC-RSV-011. Nothing mutates the reservation; the answer comes from the clock.
        var reservation = AConfirmedReservation();

        reservation.IsOverdueAt(Now.AddDays(14)).Should().BeFalse("the due moment is not yet past");
        reservation.IsOverdueAt(Now.AddDays(15)).Should().BeTrue();
        reservation.Status.Should().Be(ReservationStatus.Reserved, "overdue is never a stored state");
    }

    [Test]
    public void DaysLate_CountsDaysStarted()
    {
        // AC-RSV-014, BR-RSV-024. billing charges per day, so truncating would let every member be
        // up to a day late for free, every time. An off-by-one here misprices the whole network.
        var reservation = AConfirmedReservation();

        reservation.DaysLateAt(Now.AddDays(14).AddMinutes(30)).Should().Be(1, "a started day counts");
        reservation.DaysLateAt(Now.AddDays(17)).Should().Be(3, "exactly three days is three");
        reservation.DaysLateAt(Now.AddDays(17).AddMinutes(1)).Should().Be(4, "the fourth day has begun");
    }

    [Test]
    public void DaysLate_IsZeroBeforeTheDueDate_NeverNegative()
    {
        // A negative would flow straight into billing as a credit.
        var reservation = AConfirmedReservation();

        reservation.DaysLateAt(Now).Should().Be(0);
        reservation.DaysLateAt(Now.AddDays(13)).Should().Be(0);
    }

    [Test]
    public void AReturnedReservationReportsTheLatenessItHadWhenTheLibraryTookIt()
    {
        // The member stops being responsible when the library holds the copy, not when somebody
        // gets round to pricing the fine.
        var reservation = AConfirmedReservation();
        reservation.CheckIn(Now.AddDays(17));

        reservation.DaysLateAt(Now.AddDays(90)).Should().Be(3);
    }

    // ---------- The member's half, BR-RSV-013 to BR-RSV-015 ----------

    [Test]
    public void TheRightCode_MovesToInTransitAndNotToReturned()
    {
        // AC-RSV-008. This is the whole point of the two-step return.
        var reservation = AConfirmedReservation();

        var result = reservation.BeginReturn(
            MemberId, ReturnMethod.CourierPickup, reservation.HandoverCode.Value, Now.AddDays(3));

        result.IsSuccess.Should().BeTrue();
        reservation.Status.Should().Be(ReservationStatus.InTransit);
        reservation.Status.Should().NotBe(ReservationStatus.Returned);
        reservation.HandedOverAt.Should().Be(Now.AddDays(3));
        reservation.DomainEvents.Should().ContainSingle(e => e is ReturnStarted);
    }

    [Test]
    public void AWrongCode_ChangesNothingAtAll()
    {
        // AC-RSV-007. Checked before any state moves, so a failed attempt cannot leave the
        // reservation half-way into a return.
        var reservation = AConfirmedReservation();

        var result = reservation.BeginReturn(MemberId, ReturnMethod.CourierPickup, "PU-0000", Now);

        result.Error.Should().Be(ReservationErrors.InvalidHandoverCode);
        reservation.Status.Should().Be(ReservationStatus.Reserved);
        reservation.HandedOverAt.Should().BeNull();
        reservation.ReturnMethod.Should().BeNull();
        reservation.DomainEvents.Should().BeEmpty();
    }

    [Test]
    public void TheCodeIsAcceptedWithStrayCaseAndSpacing()
    {
        // The member is copying something read aloud. Rejecting a trailing space would be theatre.
        var reservation = AConfirmedReservation();
        var code = reservation.HandoverCode.Value;

        reservation.BeginReturn(MemberId, ReturnMethod.LibraryDropOff, $"  {code.ToLowerInvariant()} ", Now)
            .IsSuccess.Should().BeTrue();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void AnEmptyCodeIsRefused(string? typed)
    {
        AConfirmedReservation()
            .BeginReturn(MemberId, ReturnMethod.CourierPickup, typed, Now)
            .Error.Should().Be(ReservationErrors.InvalidHandoverCode);
    }

    [Test]
    public void AnotherMemberCannotStartTheReturn_EvenWithTheRightCode()
    {
        // The code is read aloud in public. It is proof of a handover, never authority.
        var reservation = AConfirmedReservation();

        reservation.BeginReturn(Stranger, ReturnMethod.CourierPickup, reservation.HandoverCode.Value, Now)
            .Error.Should().Be(ReservationErrors.NotYours);
        reservation.Status.Should().Be(ReservationStatus.Reserved);
    }

    [Test]
    public void StartingAReturnTwice_IsRefused()
    {
        var reservation = AConfirmedReservation();
        var code = reservation.HandoverCode.Value;
        reservation.BeginReturn(MemberId, ReturnMethod.CourierPickup, code, Now);

        reservation.BeginReturn(MemberId, ReturnMethod.CourierPickup, code, Now)
            .Error.Should().Be(ReservationErrors.AlreadyInTransit);
    }

    // ---------- The library's half, BR-RSV-016 to BR-RSV-020 ----------

    [Test]
    public void OnlyCheckInCompletesTheLoan()
    {
        // AC-RSV-009.
        var reservation = AConfirmedReservation();
        reservation.BeginReturn(MemberId, ReturnMethod.CourierPickup, reservation.HandoverCode.Value, Now);

        var result = reservation.CheckIn(Now.AddDays(4));

        result.Value.Should().BeTrue("the copy goes back on the shelf");
        reservation.Status.Should().Be(ReservationStatus.Returned);
        reservation.CheckedInAt.Should().Be(Now.AddDays(4));
        reservation.IsActive.Should().BeFalse();
    }

    [Test]
    public void ACopyCanBeCheckedInWithoutTheMemberEverConfirmingAHandover()
    {
        // A member who walks into the library and puts the book on the desk never touches the app.
        // Refusing that would strand the copy.
        var reservation = AConfirmedReservation();

        reservation.CheckIn(Now.AddDays(2)).Value.Should().BeTrue();
        reservation.Status.Should().Be(ReservationStatus.Returned);
    }

    [Test]
    public void ASecondCheckIn_IsANoOpAndDoesNotRestoreASecondCopy()
    {
        // BR-RSV-019. Two members of staff scanning one parcel must not invent a volume.
        var reservation = AConfirmedReservation();
        reservation.CheckIn(Now.AddDays(2));

        var second = reservation.CheckIn(Now.AddDays(2));

        second.IsSuccess.Should().BeTrue("a repeated check-in is not an error");
        second.Value.Should().BeFalse("but it must not put another copy back");
    }

    [Test]
    public void CheckingInLate_RecordsTheDaysAndChargesNothing()
    {
        // AC-RSV-012. The number travels; the price is billing's.
        var reservation = AConfirmedReservation();

        reservation.CheckIn(Now.AddDays(17));

        reservation.DaysLateAtCheckIn.Should().Be(3);
        reservation.DomainEvents.OfType<ReservationReturned>().Single()
            .DaysLate.Should().Be(3);
    }

    [Test]
    public void CheckingInOnTime_RecordsNoLateness()
    {
        var reservation = AConfirmedReservation();

        reservation.CheckIn(Now.AddDays(10));

        reservation.DaysLateAtCheckIn.Should().Be(0);
        reservation.DomainEvents.OfType<ReservationReturned>().Single().DaysLate.Should().Be(0);
    }

    [Test]
    public void ACancelledReservationCannotBeReturnedOrCheckedIn()
    {
        var reservation = AConfirmedReservation();
        reservation.Cancel();

        reservation.BeginReturn(MemberId, ReturnMethod.CourierPickup, reservation.HandoverCode.Value, Now)
            .Error.Should().Be(ReservationErrors.NotReturnable);
        reservation.CheckIn(Now).Error.Should().Be(ReservationErrors.NotReturnable);
    }

    // ---------- The handover code ----------

    [Test]
    public void TheHandoverCodeIsStableForTheLifeOfTheReservation()
    {
        // The courier may read it out days after it was issued.
        var reservation = AConfirmedReservation();

        reservation.HandoverCode.Value.Should().Be(reservation.HandoverCode.Value);
        reservation.HandoverCode.Value.Should().MatchRegex(@"^PU-\d{4}$");
    }

    [Test]
    public void TwoReservationsDoNotShareACode()
    {
        var codes = Enumerable.Range(0, 50)
            .Select(_ => AConfirmedReservation().HandoverCode.Value)
            .Distinct()
            .Count();

        // Four digits over fifty draws will collide sometimes; what matters is that the code is not
        // a constant. It is proof of a handover, not a secret — see BR-RSV-013.
        codes.Should().BeGreaterThan(40);
    }
}
