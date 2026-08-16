using Astrolabe.Domain.Features.Billing.Entities;
using Astrolabe.Domain.Features.Billing.Enums;
using Astrolabe.Domain.Features.Billing.Errors;
using Astrolabe.Domain.Features.Billing.Events;
using Astrolabe.Domain.Primitives;
using FluentAssertions;

namespace Astrolabe.Domain.Tests.Features.Billing;

/// <summary>
/// Covers the billing entities: BR-BIL-003 to BR-BIL-006 and BR-BIL-009 to BR-BIL-021.
///
/// Two things get the most attention here. A payment method must be <em>incapable</em> of holding a
/// card number, and a desk code must settle nothing until a librarian says they took the money.
/// </summary>
[TestFixture]
public sealed class BillingEntityTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid MemberId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ReservationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid LibraryId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static Fine AFine(int daysLate = 20)
    {
        var fine = Fine.Assess(
            MemberId, ReservationId, LibraryId, "The Savage Detectives", daysLate, Now)!;
        fine.ClearDomainEvents();
        return fine;
    }

    // ---------- Fine ----------

    [Test]
    public void AssessingALateReturn_FreezesTheAmount()
    {
        // BR-BIL-003. Priced once from the days reservations froze at check-in.
        var fine = Fine.Assess(MemberId, ReservationId, LibraryId, "The Savage Detectives", 20, Now)!;

        fine.Amount.Cents.Should().Be(700);
        fine.DaysLate.Should().Be(20);
        fine.Status.Should().Be(FineStatus.Outstanding);
        fine.DomainEvents.Should().ContainSingle(e => e is FineAssessed);
    }

    [TestCase(0)]
    [TestCase(-3)]
    public void AnOnTimeReturn_ProducesNoFineAtAll(int daysLate)
    {
        // AC-BIL-004. Not a fine of zero: a statement full of lines saying nothing happened is
        // worse than no line.
        Fine.Assess(MemberId, ReservationId, LibraryId, "Sapiens", daysLate, Now).Should().BeNull();
    }

    [Test]
    public void TheTitleIsCopied_SoAStatementSurvivesTheCatalogue()
    {
        // A book removed from the catalogue must not turn a line of somebody's bill into a blank.
        var fine = AFine();

        fine.BookTitle.Should().Be("The Savage Detectives");
    }

    [Test]
    public void AFineWithoutATitle_StillReadsAsSomething()
    {
        Fine.Assess(MemberId, ReservationId, LibraryId, "   ", 5, Now)!
            .BookTitle.Should().Be("Unknown title");
    }

    [Test]
    public void SettlingAFine_IsIdempotent()
    {
        // A repeated payment must not write a second ledger entry.
        var fine = AFine();

        fine.Settle(Now).Value.Should().BeTrue();
        fine.Settle(Now).Value.Should().BeFalse("nothing moved the second time");
        fine.Status.Should().Be(FineStatus.Paid);
        fine.DomainEvents.OfType<FinePaid>().Should().ContainSingle();
    }

    [Test]
    public void HoldingAFineForADeskCode_LeavesItOwed()
    {
        // BR-BIL-017. Nobody has paid. The debt stands; it is simply promised to a counter.
        var fine = AFine();

        fine.Hold(Guid.NewGuid()).IsSuccess.Should().BeTrue();

        fine.Status.Should().Be(FineStatus.AwaitingValidation);
        fine.IsOwed.Should().BeTrue();
        fine.IsOutstanding.Should().BeFalse("it can no longer be paid by card");
    }

    [Test]
    public void AFineAwaitingValidation_CannotBeHeldASecondTime()
    {
        // BR-BIL-021. Two open codes for one debt is two chances to pay for one book.
        var fine = AFine();
        fine.Hold(Guid.NewGuid());

        fine.Hold(Guid.NewGuid()).Error.Should().Be(BillingErrors.FineAwaitingValidation);
    }

    [Test]
    public void APaidFine_CannotBeHeldAtAll()
    {
        var fine = AFine();
        fine.Settle(Now);

        fine.Hold(Guid.NewGuid()).Error.Should().Be(BillingErrors.FineAlreadyPaid);
    }

    [Test]
    public void ReleasingAHeldFine_ReturnsItToOutstanding()
    {
        // BR-BIL-019 and BR-BIL-020: a rejected or expired code forgives nothing.
        var fine = AFine();
        fine.Hold(Guid.NewGuid());

        fine.Release();

        fine.Status.Should().Be(FineStatus.Outstanding);
        fine.DeskPaymentId.Should().BeNull();
    }

    [Test]
    public void ReleasingAPaidFine_DoesNotResurrectTheDebt()
    {
        var fine = AFine();
        fine.Settle(Now);

        fine.Release();

        fine.Status.Should().Be(FineStatus.Paid);
    }

    // ---------- PaymentMethod, BR-BIL-006 ----------

    [Test]
    public void AValidCardIsStoredWithItsDisplayDetailsOnly()
    {
        var card = PaymentMethod.Create(
            MemberId, CardBrand.Visa, "4242", "09/28", "Francisco Torreglosa", isPrimary: true).Value;

        card.Last4.Should().Be("4242");
        card.DisplayName.Should().Be("Visa •••• 4242");
    }

    [TestCase("4242424242424242")]
    [TestCase("378282246310005")]
    [TestCase("42424")]
    public void AFullCardNumberIsRefused_NotTruncated(string number)
    {
        // AC-BIL-008. The distinction matters: truncating would mean the number crossed the wire,
        // reached this process and sat in memory before being trimmed. Refusing tells the caller to
        // stop sending it. The system must be incapable of storing one, not merely disinclined.
        var result = PaymentMethod.Create(
            MemberId, CardBrand.Visa, number, "09/28", "Francisco Torreglosa", false);

        result.Error.Should().Be(BillingErrors.CardDetailsInvalid);
    }

    [TestCase("424")]
    [TestCase("")]
    [TestCase(null)]
    [TestCase("42a2")]
    [TestCase(" 4242 ")]
    public void AnythingButExactlyFourDigitsIsRefused(string? last4)
    {
        PaymentMethod.Create(MemberId, CardBrand.Visa, last4, "09/28", "A Holder", false)
            .Error.Should().Be(BillingErrors.CardDetailsInvalid);
    }

    [TestCase("13/28")]
    [TestCase("00/28")]
    [TestCase("9/28")]
    [TestCase("09-28")]
    [TestCase("09/2028")]
    public void AnImpossibleExpiryIsRefused(string expiry)
    {
        PaymentMethod.Create(MemberId, CardBrand.Visa, "4242", expiry, "A Holder", false)
            .Error.Should().Be(BillingErrors.ExpiryInvalid);
    }

    [Test]
    public void ACardWithoutAHolderIsRefused()
    {
        PaymentMethod.Create(MemberId, CardBrand.Visa, "4242", "09/28", "  ", false)
            .Error.Should().Be(BillingErrors.CardholderRequired);
    }

    [Test]
    public void ThePaymentMethodTypeExposesNoFieldThatCouldHoldACardNumber()
    {
        // The guard above stops a number arriving. This one stops a field being added later that
        // could hold one — a property named for a card number would fail here on the day it appears.
        var suspicious = typeof(PaymentMethod)
            .GetProperties()
            .Select(property => property.Name.ToLowerInvariant())
            .Where(name =>
                (name.Contains("number") || name.Contains("pan") || name.Contains("cvv")
                 || name.Contains("cvc") || name.Contains("securitycode"))
                && !name.Contains("last4"))
            .ToList();

        suspicious.Should().BeEmpty();
    }

    // ---------- DeskPayment, BR-BIL-004 and BR-BIL-018 to BR-BIL-020 ----------

    private static DeskPayment ACode(DateTimeOffset? at = null)
    {
        var payment = DeskPayment.Issue(
            MemberId, LibraryId, Money.FromCents(700), [Guid.NewGuid()], at ?? Now);
        payment.ClearDomainEvents();
        return payment;
    }

    [Test]
    public void ACodeIsValidForSeventyTwoHours()
    {
        // BR-BIL-004.
        var payment = ACode();

        payment.ExpiresAt.Should().Be(Now.AddHours(72));
        payment.Code.Value.Should().MatchRegex(@"^MP-\d{5}$");
    }

    [Test]
    public void ACodeAtSeventyOneHoursValidates_AndOneAtSeventyThreeDoesNot()
    {
        // AC-BIL-009.
        ACode().Validate(Now.AddHours(71)).IsSuccess.Should().BeTrue();
        ACode().Validate(Now.AddHours(73)).Error.Should().Be(BillingErrors.DeskPaymentExpired);
    }

    [Test]
    public void AnExpiredCodeCannotBeRejectedEither()
    {
        // BR-BIL-020. A librarian must not be able to resolve a code that ran out while the member
        // stood in the queue.
        ACode().Reject("Member never came", Now.AddHours(80))
            .Error.Should().Be(BillingErrors.DeskPaymentExpired);
    }

    [Test]
    public void ValidatingTwice_IsRefused()
    {
        // Two administrators reaching for the same code: one wins, the other is told so, and no
        // second payment is recorded.
        var payment = ACode();
        payment.Validate(Now.AddHours(1));

        payment.Validate(Now.AddHours(2)).Error.Should().Be(BillingErrors.DeskPaymentAlreadyResolved);
    }

    [Test]
    public void RejectingWithoutAReasonIsRefused()
    {
        // BR-BIL-019. A rejection puts a debt back on somebody's account.
        var payment = ACode();

        payment.Reject("   ", Now.AddHours(1)).Error.Should().Be(BillingErrors.RejectionReasonRequired);
        payment.Status.Should().Be(DeskPaymentStatus.Pending);
    }

    [Test]
    public void RejectionRecordsTheReason()
    {
        var payment = ACode();

        payment.Reject("  Member never came to the desk  ", Now.AddHours(1))
            .IsSuccess.Should().BeTrue();

        payment.RejectionReason.Should().Be("Member never came to the desk");
        payment.DomainEvents.Should().ContainSingle(e => e is DeskPaymentRejected);
    }

    [Test]
    public void ExpiryIsDerived_NotSwept()
    {
        // A job that failed would leave stale codes looking valid at a counter, which is money.
        var payment = ACode();

        payment.IsExpiredAt(Now.AddHours(71)).Should().BeFalse();
        payment.IsExpiredAt(Now.AddHours(73)).Should().BeTrue();
        payment.Status.Should().Be(DeskPaymentStatus.Pending, "nothing wrote the state");
    }

    [Test]
    public void AResolvedCodeIsNeverReportedExpired()
    {
        var payment = ACode();
        payment.Validate(Now.AddHours(1));

        payment.IsExpiredAt(Now.AddHours(500)).Should().BeFalse();
    }

    // ---------- LedgerEntry, BR-BIL-011 and BR-BIL-012 ----------

    [Test]
    public void AChargeIsNegativeAndAPaymentIsPositive()
    {
        // Signed so a balance is a plain sum rather than a case analysis.
        LedgerEntry.Charge(MemberId, Money.FromCents(700), "Late fine", null, null, Now)
            .Amount.Cents.Should().Be(-700);
        LedgerEntry.Payment(MemberId, Money.FromCents(700), "Card payment", null, Now)
            .Amount.Cents.Should().Be(700);
        LedgerEntry.Credit(MemberId, Money.FromCents(500), "Correction", Now)
            .Amount.Cents.Should().Be(500);
    }

    [Test]
    public void TheSignIsNormalised_WhicheverWayTheCallerPassesIt()
    {
        // A caller passing an already-negative charge must not flip it back to a credit.
        LedgerEntry.Charge(MemberId, Money.FromCents(-700), "Late fine", null, null, Now)
            .Amount.Cents.Should().Be(-700);
        LedgerEntry.Payment(MemberId, Money.FromCents(-700), "Card payment", null, Now)
            .Amount.Cents.Should().Be(700);
    }

    [Test]
    public void ALedgerEntryExposesNoWayToChangeItself()
    {
        // BR-BIL-012. A ledger that can be edited is not a ledger. Enforced by the type, not by a
        // convention somebody has to keep.
        var mutators = typeof(LedgerEntry)
            .GetMethods()
            .Where(method => method.IsPublic && !method.IsStatic)
            .Where(method => method.Name.StartsWith("Set") || method.Name is "Update" or "Delete"
                             or "Edit" or "Adjust")
            .ToList();

        mutators.Should().BeEmpty();

        typeof(LedgerEntry).GetProperties()
            .Where(property => property.SetMethod?.IsPublic == true)
            .Should().BeEmpty("nothing outside the entry may write to it");
    }

    [Test]
    public void ABalanceIsTheSumOfTheEntries_AndPayingLeavesTheChargeStanding()
    {
        // AC-BIL-006. The charge is not removed when it is paid; both movements remain visible.
        var entries = new[]
        {
            LedgerEntry.Charge(MemberId, Money.FromCents(700), "Late fine", null, null, Now),
            LedgerEntry.Charge(MemberId, Money.FromCents(385), "Late fine", null, null, Now),
            LedgerEntry.Payment(MemberId, Money.FromCents(700), "Card payment", null, Now),
        };

        entries.Sum(entry => entry.Amount.Cents).Should().Be(-385);
        entries.Count(entry => entry.Kind == LedgerEntryKind.Charge).Should().Be(2);
    }
}
