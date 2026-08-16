using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Membership;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Features.Store.Commands.PlaceOrder;
using Astrolabe.Application.Features.Store.Queries.QuoteOrder;
using Astrolabe.Application.Shared.Catalog;
using Astrolabe.Application.Tests.TestSupport;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Billing.Entities;
using Astrolabe.Domain.Features.Billing.Enums;
using Astrolabe.Domain.Features.Billing.Errors;
using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Catalog.ValueObjects;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Membership.ValueObjects;
using Astrolabe.Domain.Features.Store.Entities;
using Astrolabe.Domain.Features.Store.Enums;
using Astrolabe.Domain.Features.Store.Errors;
using Astrolabe.Domain.Primitives;
using FluentAssertions;
using Moq;

namespace Astrolabe.Application.Tests.Features.Store;

/// <summary>
/// Covers the store handlers.
///
/// <para>
/// The most important test here is that a discount actually reaches the member. The policy was
/// correct and its unit tests passed, because they were handed copy locations directly. The
/// defect lived in the gap between the repository and the policy: books were loaded without their
/// copies, so every book looked unheld and every discount silently became zero. Nothing that tested
/// either side alone could have seen it.
/// </para>
/// </summary>
[TestFixture]
public sealed class StoreHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid MemberId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid HomeCity = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OtherCity = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Midtown = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Loop = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private StoreUnitOfWorkMock _store = null!;
    private BillingUnitOfWorkMock _billing = null!;
    private AuditUnitOfWorkMock _audit = null!;
    private Mock<ICurrentUser> _currentUser = null!;
    private Mock<IEntitlementProvider> _entitlements = null!;
    private Mock<ILibraryLocationProvider> _locations = null!;

    private static CancellationToken Ct => TestContext.CurrentContext.CancellationToken;

    [SetUp]
    public void SetUp()
    {
        _store = new StoreUnitOfWorkMock();
        _billing = new BillingUnitOfWorkMock();
        _audit = new AuditUnitOfWorkMock();

        _currentUser = new Mock<ICurrentUser>();
        _currentUser.SetupGet(u => u.UserId).Returns(MemberId);

        _entitlements = new Mock<IEntitlementProvider>();
        OnPlan(PlanTier.Max);

        _locations = new Mock<ILibraryLocationProvider>();
        _locations.Setup(l => l.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, BookProjection.LibraryLocation>
            {
                [Midtown] = new(Midtown, "Midtown", HomeCity, "New York", IsActive: true),
                [Loop] = new(Loop, "Loop", OtherCity, "Chicago", IsActive: true),
            });
    }

    private void OnPlan(PlanTier plan) =>
        _entitlements.Setup(e => e.GetForCurrentMemberAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(PlanCatalog.EntitlementFor(plan, HomeCity, Midtown));

    /// <summary>
    /// A book with real copies, returned through the method the handlers actually call. Arranging it
    /// this way is what makes the regression above visible: a stub on <c>GetByIdsAsync</c> would let
    /// the copy-less path pass.
    /// </summary>
    private Book ABook(int priceCents = 1900, params Guid[] heldAt)
    {
        var book = Book.CreateDraft(
            Isbn.Create("9781529011503").Value, "Klara and the Sun", "Kazuo Ishiguro",
            null, Genre.ScienceFiction, PlanTier.Plus, Money.FromCents(priceCents), null, Now).Value;

        foreach (var libraryId in heldAt)
        {
            book.AddCopies(libraryId, 2);
        }

        book.Publish(Now);
        book.ClearDomainEvents();

        _store.Books
            .Setup(r => r.GetByIdsWithCopiesAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(book.Id)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([book]);

        return book;
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

    private QuoteOrderQueryHandler QuoteHandler() =>
        new(_store.Object, _entitlements.Object, _locations.Object, _currentUser.Object);

    /// <summary>Gives the member a balance to spend. BR-STR-007.</summary>
    private void WithPointsBalance(int pointCents) =>
        _store.Points
            .Setup(r => r.GetBalanceAsync(MemberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pointCents);

    private PlaceOrderCommandHandler PlaceHandler() =>
        new(_store.Object, _billing.Object, _audit.Object, _entitlements.Object,
            _locations.Object, _currentUser.Object, new FixedClock(Now));

    // ---------- The discount actually reaches the member ----------

    [Test]
    public async Task AMaxMember_ActuallyReceivesFifteenPercent()
    {
        // The regression. This failed before books were loaded with their copies, and the policy's
        // own tests could not see it because they were handed the copies directly.
        var book = ABook(heldAt: Loop);

        var quote = await QuoteHandler()
            .Handle(new QuoteOrderQuery([book.Id], OrderFulfilment.Collection, PointsToRedeem: 0), Ct);

        quote.Value.Lines[0].DiscountPercent.Should().Be(15);
        quote.Value.DiscountTotalCents.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task APlusMember_ReceivesTenPercentOnABookHeldInTheirCity()
    {
        OnPlan(PlanTier.Plus);
        var book = ABook(heldAt: Midtown);

        var quote = await QuoteHandler()
            .Handle(new QuoteOrderQuery([book.Id], OrderFulfilment.Collection, PointsToRedeem: 0), Ct);

        quote.Value.Lines[0].DiscountPercent.Should().Be(10);
    }

    [Test]
    public async Task APlusMember_ReceivesNothingOnABookHeldOnlyElsewhere()
    {
        // The other half of BR-STR-002. Both halves matter: a test that only checked the first
        // would pass with a policy that always returned 10.
        OnPlan(PlanTier.Plus);
        var book = ABook(heldAt: Loop);

        var quote = await QuoteHandler()
            .Handle(new QuoteOrderQuery([book.Id], OrderFulfilment.Collection, PointsToRedeem: 0), Ct);

        quote.Value.Lines[0].DiscountPercent.Should().Be(0);
        quote.Value.DiscountNote.Should().Contain("held elsewhere");
    }

    [Test]
    public async Task ABasicMember_PaysTheListPrice()
    {
        OnPlan(PlanTier.Basic);
        var book = ABook(priceCents: 1900, heldAt: Midtown);

        var quote = await QuoteHandler()
            .Handle(new QuoteOrderQuery([book.Id], OrderFulfilment.Collection, PointsToRedeem: 0), Ct);

        quote.Value.TotalCents.Should().Be(1900);
        quote.Value.DiscountTotalCents.Should().Be(0);
    }

    [Test]
    public async Task AQuoteMatchesWhatThePurchaseCharges()
    {
        // The modal and the charge must agree to the cent, or the member is billed something other
        // than what they agreed to.
        var book = ABook(priceCents: 2600, heldAt: Midtown);
        var card = ACard();

        var quote = await QuoteHandler()
            .Handle(new QuoteOrderQuery([book.Id], OrderFulfilment.Shipping, PointsToRedeem: 0), Ct);

        var order = await PlaceHandler().Handle(new PlaceOrderCommand(
            [new OrderLineRequest(book.Id, 1)], OrderFulfilment.Shipping, card.Id, PointsToRedeem: 0, null), Ct);

        order.Value.TotalCents.Should().Be(quote.Value.TotalCents);
        order.Value.PointsEarned.Should().Be(quote.Value.PointsWouldEarn);
    }

    // ---------- Placing ----------

    [Test]
    public async Task PlacingAnOrder_WritesBothACHargeAndAPayment()
    {
        // A purchase is paid when it is placed. Writing only the charge would leave every member
        // permanently in debit by the value of everything they had ever bought.
        var book = ABook(heldAt: Midtown);
        var card = ACard();

        await PlaceHandler().Handle(new PlaceOrderCommand(
            [new OrderLineRequest(book.Id, 1)], OrderFulfilment.Collection, card.Id, PointsToRedeem: 0, null), Ct);

        _billing.Ledger.Verify(r => r.AddRangeAsync(
            It.Is<IEnumerable<LedgerEntry>>(entries =>
                entries.Count(e => e.Kind == LedgerEntryKind.Charge) == 1
                && entries.Count(e => e.Kind == LedgerEntryKind.Payment) == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Redeeming points, BR-STR-007 ----------

    [Test]
    public async Task RedeemingPoints_LeavesTheLedgerBalanced()
    {
        // The charge is the full total and the two tenders settle it between them. Netting the
        // points off the charge instead would leave the member's own statement unable to show they
        // had spent a reward at all — and netting only one side would leave them standing in debit
        // by exactly the points they had just spent.
        var book = ABook(priceCents: 4_500, heldAt: Midtown);
        var card = ACard();
        WithPointsBalance(1_000);

        await PlaceHandler().Handle(new PlaceOrderCommand(
            [new OrderLineRequest(book.Id, 1)], OrderFulfilment.Collection, card.Id,
            PointsToRedeem: 1_000, null), Ct);

        // A charge is stored negative and a tender positive, so "balanced" is literally that the
        // entries sum to zero — the member is left owing nothing and holding nothing.
        _billing.Ledger.Verify(r => r.AddRangeAsync(
            It.Is<IEnumerable<LedgerEntry>>(entries =>
                entries.Count() == 3
                && entries.Count(e => e.Kind == LedgerEntryKind.Payment) == 2
                && entries.Sum(e => e.Amount.Cents) == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RedeemingPoints_WritesANegativeMovementInTheSameCommit()
    {
        // Never in a reaction. A redemption lost after the commit would give the books away for
        // points the member still holds.
        var book = ABook(priceCents: 4_500, heldAt: Midtown);
        var card = ACard();
        WithPointsBalance(1_000);

        await PlaceHandler().Handle(new PlaceOrderCommand(
            [new OrderLineRequest(book.Id, 1)], OrderFulfilment.Collection, card.Id,
            PointsToRedeem: 1_000, null), Ct);

        _store.Points.Verify(r => r.AddAsync(
            It.Is<PointsMovement>(m => m.PointCents == -1_000), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task RedeemingMoreThanTheBalance_IsRefusedAndChargesNothing()
    {
        var book = ABook(priceCents: 4_500, heldAt: Midtown);
        var card = ACard();
        WithPointsBalance(500);

        var result = await PlaceHandler().Handle(new PlaceOrderCommand(
            [new OrderLineRequest(book.Id, 1)], OrderFulfilment.Collection, card.Id,
            PointsToRedeem: 1_000, null), Ct);

        result.Error.Should().Be(StoreErrors.RedemptionExceedsBalance);
        _store.Saved.Should().Be(0);
    }

    [Test]
    public async Task RedeemingMoreThanHalfThePurchase_IsRefused()
    {
        var book = ABook(priceCents: 4_500, heldAt: Midtown);
        var card = ACard();
        WithPointsBalance(90_000);

        var result = await PlaceHandler().Handle(new PlaceOrderCommand(
            [new OrderLineRequest(book.Id, 1)], OrderFulfilment.Collection, card.Id,
            PointsToRedeem: 3_000, null), Ct);

        result.Error.Should().Be(StoreErrors.RedemptionExceedsCap);
        _store.Saved.Should().Be(0);
    }

    [Test]
    public async Task TheQuoteAndThePurchaseAgreeOnTheAmountCharged()
    {
        // The whole reason pricing is shared. A member who is shown one figure and charged another
        // has been lied to, whichever of the two is arithmetically right.
        var book = ABook(priceCents: 4_500, heldAt: Midtown);
        var card = ACard();
        WithPointsBalance(1_000);

        var quote = await QuoteHandler().Handle(
            new QuoteOrderQuery([book.Id], OrderFulfilment.Collection, PointsToRedeem: 1_000), Ct);

        var order = await PlaceHandler().Handle(new PlaceOrderCommand(
            [new OrderLineRequest(book.Id, 1)], OrderFulfilment.Collection, card.Id,
            PointsToRedeem: 1_000, null), Ct);

        quote.Value.AmountChargedCents.Should().Be(order.Value.AmountChargedCents);
        quote.Value.PointsWouldEarn.Should().Be(order.Value.PointsEarned);
    }

    [Test]
    public async Task TheQuoteNeverOffersMoreThanThePurchaseWouldAccept()
    {
        // The control the member drags is bounded by this number, so an over-large request is
        // clamped rather than refused — a quote is not a commitment, and erroring while somebody is
        // still moving a slider would leave the modal with nothing to render.
        var book = ABook(priceCents: 4_500, heldAt: Midtown);
        WithPointsBalance(90_000);

        var quote = await QuoteHandler().Handle(
            new QuoteOrderQuery([book.Id], OrderFulfilment.Collection, PointsToRedeem: 90_000), Ct);

        // 1912, not 2250: the fixture's member is on Max, so $45 less 15% is $38.25 and half of
        // that is $19.12. The cap sits on the post-discount total on purpose — letting points go
        // first would quietly shrink what the plan discount is worth.
        quote.Value.MaxRedeemablePointCents.Should().Be(1_912);
        quote.Value.PointsRedeemed.Should().Be(1_912);
        quote.Value.PointsBalance.Should().Be(90_000);
    }

    [Test]
    public async Task ADeliveryFeeCannotBeCoveredByPoints()
    {
        // The cap is measured on the books alone, so shipping never widens it.
        var book = ABook(priceCents: 4_500, heldAt: Midtown);
        WithPointsBalance(90_000);

        var collection = await QuoteHandler().Handle(
            new QuoteOrderQuery([book.Id], OrderFulfilment.Collection, PointsToRedeem: 90_000), Ct);
        var shipped = await QuoteHandler().Handle(
            new QuoteOrderQuery([book.Id], OrderFulfilment.Shipping, PointsToRedeem: 90_000), Ct);

        shipped.Value.MaxRedeemablePointCents
            .Should().Be(collection.Value.MaxRedeemablePointCents);
    }

    [Test]
    public async Task PlacingAnOrder_LeavesEveryLibrarysStockAlone()
    {
        // AC-STR-008. A sale is a new copy; the library's shelves belong to reservations.
        var book = ABook(heldAt: Midtown);
        var before = book.CopyAt(Midtown)!.AvailableCount;
        var card = ACard();

        await PlaceHandler().Handle(new PlaceOrderCommand(
            [new OrderLineRequest(book.Id, 1)], OrderFulfilment.Collection, card.Id, PointsToRedeem: 0, null), Ct);

        book.CopyAt(Midtown)!.AvailableCount.Should().Be(before);
    }

    [Test]
    public async Task AMaxOrder_RecordsThePointsItEarned()
    {
        var book = ABook(priceCents: 15_000, heldAt: Midtown);
        var card = ACard();

        var order = await PlaceHandler().Handle(new PlaceOrderCommand(
            [new OrderLineRequest(book.Id, 1)], OrderFulfilment.Collection, card.Id, PointsToRedeem: 0, null), Ct);

        order.Value.PointsEarned.Should().Be(85);
        _store.Points.Verify(r => r.AddAsync(
            It.Is<PointsMovement>(m => m.PointCents == 85), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task APlusOrder_RecordsNoPointsMovementAtAll()
    {
        // A movement of zero would clutter the history with lines saying nothing happened.
        OnPlan(PlanTier.Plus);
        var book = ABook(heldAt: Midtown);
        var card = ACard();

        await PlaceHandler().Handle(new PlaceOrderCommand(
            [new OrderLineRequest(book.Id, 1)], OrderFulfilment.Collection, card.Id, PointsToRedeem: 0, null), Ct);

        _store.Points.Verify(
            r => r.AddAsync(It.IsAny<PointsMovement>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task AReplayedIdempotencyKey_ReturnsTheFirstOrderAndChargesNothingAgain()
    {
        // AC-STR-009.
        var book = ABook(heldAt: Midtown);
        var card = ACard();

        var first = Order.Place(
            MemberId, OrderFulfilment.Collection,
            [OrderLine.Create(book.Id, book.Title, 1, book.RetailPrice, 15).Value],
            PlanTier.Max, 0, "key-1", Now).Value;

        _store.Orders
            .Setup(r => r.GetByIdempotencyKeyAsync(MemberId, "key-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(first);

        var result = await PlaceHandler().Handle(new PlaceOrderCommand(
            [new OrderLineRequest(book.Id, 1)], OrderFulfilment.Collection, card.Id, PointsToRedeem: 0, "key-1"), Ct);

        result.Value.Id.Should().Be(first.Id);
        _store.Saved.Should().Be(0);
        _billing.Ledger.Verify(r => r.AddRangeAsync(
            It.IsAny<IEnumerable<LedgerEntry>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ABookOutsideTheCatalogue_CannotBeBought()
    {
        // AC-STR-011.
        var book = Book.CreateDraft(
            Isbn.Create("9781529011503").Value, "A draft", "Nobody", null,
            Genre.Fiction, PlanTier.Basic, Money.FromCents(999), null, Now).Value;

        _store.Books
            .Setup(r => r.GetByIdsWithCopiesAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([book]);

        var card = ACard();

        var result = await PlaceHandler().Handle(new PlaceOrderCommand(
            [new OrderLineRequest(book.Id, 1)], OrderFulfilment.Collection, card.Id, PointsToRedeem: 0, null), Ct);

        result.Error.Should().Be(StoreErrors.BookNotForSale);
        _store.Saved.Should().Be(0);
    }

    [Test]
    public async Task AnotherMembersCard_IsNotFound()
    {
        var book = ABook(heldAt: Midtown);

        var result = await PlaceHandler().Handle(new PlaceOrderCommand(
            [new OrderLineRequest(book.Id, 1)], OrderFulfilment.Collection, Guid.NewGuid(), PointsToRedeem: 0, null), Ct);

        result.Error.Should().Be(BillingErrors.PaymentMethodNotFound);
    }

    [Test]
    public async Task AnEmptyOrderIsRefused()
    {
        var result = await PlaceHandler().Handle(new PlaceOrderCommand(
            [], OrderFulfilment.Collection, Guid.NewGuid(), PointsToRedeem: 0, null), Ct);

        result.Error.Should().Be(StoreErrors.NothingToBuy);
    }

}
