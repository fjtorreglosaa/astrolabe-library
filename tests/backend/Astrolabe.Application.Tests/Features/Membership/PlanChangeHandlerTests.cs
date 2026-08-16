using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Features.Membership.Commands.CancelScheduledPlanChange;
using Astrolabe.Application.Features.Membership.Commands.ChangePlan;
using Astrolabe.Application.Features.Membership.Queries.QuotePlanChange;
using Astrolabe.Application.Tests.TestSupport;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Membership.Entities;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Membership.Errors;
using FluentAssertions;
using Moq;

namespace Astrolabe.Application.Tests.Features.Membership;

/// <summary>
/// Covers the plan change handlers: BR-MBR-013 to BR-MBR-021.
///
/// The point of these tests is the handler's own decisions — direction, persistence and the shape of
/// the answer. The arithmetic itself is the aggregate's and is covered there; repeating it here
/// would only pin it twice and make one of the two the wrong place to fix it.
/// </summary>
[TestFixture]
public sealed class PlanChangeHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid MemberId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private MembershipUnitOfWorkMock _membership = null!;
    private Mock<ICurrentUser> _currentUser = null!;

    private static CancellationToken Ct => TestContext.CurrentContext.CancellationToken;

    [SetUp]
    public void SetUp()
    {
        _membership = new MembershipUnitOfWorkMock();
        _currentUser = new Mock<ICurrentUser>();
        _currentUser.SetupGet(u => u.UserId).Returns(MemberId);
        _currentUser.SetupGet(u => u.Role).Returns(UserRole.Member);
    }

    private void TheMemberIsOn(PlanTier plan, DateTimeOffset? startedAt = null)
    {
        var subscription = Subscription.Start(MemberId, plan, startedAt ?? Now);
        subscription.ClearDomainEvents();

        _membership.Subscriptions
            .Setup(r => r.GetActiveForMemberAsync(MemberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
    }

    private void TheMemberHasNoSubscription() =>
        _membership.Subscriptions
            .Setup(r => r.GetActiveForMemberAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

    private ChangePlanCommandHandler ChangeHandler() =>
        new(_membership.Object, _currentUser.Object, new FixedClock(Now));

    private CancelScheduledPlanChangeCommandHandler CancelHandler() =>
        new(_membership.Object, _currentUser.Object, new FixedClock(Now));

    private QuotePlanChangeQueryHandler QuoteHandler() =>
        new(_membership.Object, _currentUser.Object, new FixedClock(Now));

    // ---------- Direction ----------

    [Test]
    public async Task MovingUp_AppliesImmediatelyAndReportsTheChargedAmount()
    {
        TheMemberIsOn(PlanTier.Plus);

        var result = await ChangeHandler().Handle(new ChangePlanCommand(PlanTier.Max), Ct);

        result.IsSuccess.Should().BeTrue();
        result.Value.Plan.Should().Be("Max");
        result.Value.AppliedImmediately.Should().BeTrue();
        result.Value.AmountChargedCents.Should().BeGreaterThan(0);
        _membership.Saved.Should().Be(1);
    }

    [Test]
    public async Task MovingDown_SchedulesAndLeavesTheMemberOnWhatTheyPaidFor()
    {
        // BR-MBR-016. The reported plan is the one in force, not the one requested — reporting the
        // target would tell the member they had already lost what they are still entitled to.
        TheMemberIsOn(PlanTier.Max);

        var result = await ChangeHandler().Handle(new ChangePlanCommand(PlanTier.Basic), Ct);

        result.IsSuccess.Should().BeTrue();
        result.Value.Plan.Should().Be("Max");
        result.Value.AppliedImmediately.Should().BeFalse();
        result.Value.AmountChargedCents.Should().Be(0);
        result.Value.EffectiveOn.Should().BeAfter(Now);
    }

    [Test]
    public async Task MovingToTheSamePlan_IsRefusedAndCommitsNothing()
    {
        TheMemberIsOn(PlanTier.Plus);

        var result = await ChangeHandler().Handle(new ChangePlanCommand(PlanTier.Plus), Ct);

        result.Error.Should().Be(MembershipErrors.AlreadyOnThatPlan);
        _membership.Saved.Should().Be(0, "a refused change must not reach the database");
    }

    // ---------- Guards ----------

    [Test]
    public async Task AnAnonymousCaller_IsRefusedWithoutTouchingPersistence()
    {
        _currentUser.SetupGet(u => u.UserId).Returns((Guid?)null);

        var result = await ChangeHandler().Handle(new ChangePlanCommand(PlanTier.Max), Ct);

        result.IsFailure.Should().BeTrue();
        _membership.Subscriptions.Verify(
            r => r.GetActiveForMemberAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task AMemberWithoutASubscription_IsRefused()
    {
        TheMemberHasNoSubscription();

        var result = await ChangeHandler().Handle(new ChangePlanCommand(PlanTier.Max), Ct);

        result.Error.Should().Be(MembershipErrors.SubscriptionNotFound);
    }

    // ---------- Cancelling ----------

    [Test]
    public async Task CancellingAPendingChange_CommitsOnce()
    {
        TheMemberIsOn(PlanTier.Max);
        await ChangeHandler().Handle(new ChangePlanCommand(PlanTier.Basic), Ct);

        var result = await CancelHandler().Handle(new CancelScheduledPlanChangeCommand(), Ct);

        result.IsSuccess.Should().BeTrue();
        _membership.Saved.Should().Be(2);
    }

    [Test]
    public async Task CancellingWithNothingPending_IsRefused()
    {
        TheMemberIsOn(PlanTier.Plus);

        var result = await CancelHandler().Handle(new CancelScheduledPlanChangeCommand(), Ct);

        result.Error.Should().Be(MembershipErrors.NoScheduledChange);
        _membership.Saved.Should().Be(0);
    }

    // ---------- Quoting, BR-MBR-020 ----------

    [Test]
    public async Task AQuoteChangesNothing()
    {
        // The modal asks repeatedly as the member compares plans; a quote that wrote would charge
        // them for looking.
        TheMemberIsOn(PlanTier.Plus);

        await QuoteHandler().Handle(new QuotePlanChangeQuery(PlanTier.Max), Ct);

        _membership.Saved.Should().Be(0);
    }

    [Test]
    public async Task ADowngradeQuote_ListsWhatTheMemberLoses()
    {
        TheMemberIsOn(PlanTier.Max);

        var result = await QuoteHandler().Handle(new QuotePlanChangeQuery(PlanTier.Basic), Ct);

        result.Value.Direction.Should().Be("downgrade");
        result.Value.AmountDueCents.Should().Be(0);
        result.Value.WhatYouLose.Should().Equal(
            "RewardPoints", "HomeLibraryAndBasicCatalog", "Recommendations");
    }

    [Test]
    public async Task AnUpgradeQuote_ListsNothingLostAndAnAmountDue()
    {
        TheMemberIsOn(PlanTier.Basic);

        var result = await QuoteHandler().Handle(new QuotePlanChangeQuery(PlanTier.Max), Ct);

        result.Value.Direction.Should().Be("upgrade");
        result.Value.WhatYouLose.Should().BeEmpty();
        result.Value.AmountDueCents.Should().BeGreaterThan(0);
    }

}
