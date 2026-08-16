using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Membership.Errors;
using Astrolabe.Domain.Features.Membership.Events;
using Astrolabe.Domain.Features.Membership.ValueObjects;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Membership.Entities;

/// <summary>
/// A member's plan over a billing period.
///
/// <para>
/// Holds both the current plan and any pending change. Splitting them across two aggregates would
/// let a downgrade be scheduled against a plan that changed in between, which is exactly the
/// ambiguity BR-MBR-019 forbids.
/// </para>
/// </summary>
public sealed class Subscription : AggregateRoot
{
    private Subscription()
    {
    }

    private Subscription(Guid id, Guid memberId, PlanTier plan, BillingCycle cycle, DateTimeOffset now)
        : base(id)
    {
        MemberId = memberId;
        Plan = plan;
        Cycle = cycle;
        StartedAt = now;

        Raise(new SubscriptionStarted(Guid.NewGuid(), now, memberId, plan));
    }

    public Guid MemberId { get; private set; }

    public PlanTier Plan { get; private set; }

    public BillingCycle Cycle { get; private set; } = null!;

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? EndedAt { get; private set; }

    /// <summary>A downgrade awaiting the renewal date. Null when nothing is pending.</summary>
    public ScheduledPlanChange? ScheduledChange { get; private set; }

    /// <summary>Reset on renewal, so the limit in BR-MBR-011 is per cycle rather than rolling.</summary>
    public int CityChangesThisCycle { get; private set; }

    public bool IsActive => EndedAt is null;

    public static Subscription Start(Guid memberId, PlanTier plan, DateTimeOffset now) =>
        new(Guid.NewGuid(), memberId, plan, BillingCycle.StartingAt(now), now);

    // ---------- Quoting ----------

    /// <summary>
    /// What a change would cost, without applying it. BR-MBR-020 requires the member to see the
    /// amount and what they lose before confirming, so the quote must be obtainable on its own.
    /// </summary>
    public Result<ProrationQuote> QuoteChange(PlanTier target, DateTimeOffset now)
    {
        if (!IsActive)
        {
            return Result.Failure<ProrationQuote>(MembershipErrors.SubscriptionEnded);
        }

        if (target == Plan)
        {
            return Result.Failure<ProrationQuote>(MembershipErrors.AlreadyOnThatPlan);
        }

        // Direction is decided by rank, never by price, so a future price change cannot silently
        // turn an upgrade into a downgrade.
        return Result.Success(target.IsHigherThan(Plan)
            ? ProrationQuote.ForUpgrade(
                Plan, target,
                PlanCatalog.For(Plan).MonthlyPrice,
                PlanCatalog.For(target).MonthlyPrice,
                Cycle.DaysRemainingAt(now),
                Cycle.TotalDays,
                now)
            : ProrationQuote.ForScheduledDowngrade(Plan, target, Cycle.RenewsOn));
    }

    // ---------- Changing plan ----------

    /// <summary>
    /// Moves up a plan immediately. Implements BR-MBR-013 to BR-MBR-015.
    /// </summary>
    public Result<ProrationQuote> Upgrade(PlanTier target, bool hasPaymentMethod, DateTimeOffset now)
    {
        var quote = QuoteChange(target, now);

        if (quote.IsFailure)
        {
            return quote;
        }

        if (!target.IsHigherThan(Plan))
        {
            return Result.Failure<ProrationQuote>(MembershipErrors.AlreadyOnThatPlan);
        }

        if (PlanCatalog.For(target).MonthlyPrice.Cents > 0 && !hasPaymentMethod)
        {
            return Result.Failure<ProrationQuote>(MembershipErrors.PaymentMethodRequired);
        }

        var previous = Plan;

        // A pending downgrade cannot survive an upgrade: keeping both would leave the member's
        // future plan ambiguous, which is the edge case recorded in membership.business.md §5.
        ScheduledChange = null;
        Plan = target;

        Raise(new PlanUpgraded(Guid.NewGuid(), now, MemberId, previous, target, quote.Value.AmountDue));

        return quote;
    }

    /// <summary>
    /// Requests a move down, to take effect at the renewal date. Implements BR-MBR-016 to BR-MBR-019.
    /// Nothing is charged and nothing is refunded.
    /// </summary>
    public Result ScheduleDowngrade(PlanTier target, DateTimeOffset now)
    {
        if (!IsActive)
        {
            return Result.Failure(MembershipErrors.SubscriptionEnded);
        }

        if (target == Plan)
        {
            return Result.Failure(MembershipErrors.AlreadyOnThatPlan);
        }

        if (target.IsHigherThan(Plan))
        {
            return Result.Failure(MembershipErrors.AlreadyOnThatPlan);
        }

        // BR-MBR-019: at most one outstanding change. A second request replaces the first rather
        // than queueing, so the member always has exactly one future plan.
        ScheduledChange = new ScheduledPlanChange(target, Cycle.RenewsOn, now);

        Raise(new PlanChangeScheduled(Guid.NewGuid(), now, MemberId, Plan, target, Cycle.RenewsOn));

        return Result.Success();
    }

    public Result CancelScheduledChange(DateTimeOffset now)
    {
        if (ScheduledChange is null)
        {
            return Result.Failure(MembershipErrors.NoScheduledChange);
        }

        var cancelled = ScheduledChange;
        ScheduledChange = null;

        Raise(new PlanChangeCancelled(Guid.NewGuid(), now, MemberId, cancelled.Target));

        return Result.Success();
    }

    /// <summary>
    /// Applies a scheduled change whose date has passed, and rolls the cycle forward.
    ///
    /// <para>
    /// Idempotent, and called from two places on purpose: when reading an entitlement, so a member
    /// who returns after their renewal sees the right plan at once, and from a background sweep, so
    /// a member who never signs in is still moved. Either alone leaves a gap.
    /// </para>
    /// </summary>
    /// <returns>The plan that was applied, or null when nothing was due.</returns>
    public Result<PlanTier?> ApplyDueChange(DateTimeOffset now)
    {
        if (!IsActive)
        {
            return Result.Failure<PlanTier?>(MembershipErrors.SubscriptionEnded);
        }

        if (!Cycle.IsDueAt(now))
        {
            return Result.Success<PlanTier?>(null);
        }

        PlanTier? applied = null;

        // Roll forward one cycle at a time, so a subscription untouched for months lands on the
        // right anchor day rather than jumping straight to today.
        while (Cycle.IsDueAt(now))
        {
            if (ScheduledChange is { } change && change.IsDueAt(Cycle.RenewsOn))
            {
                var previous = Plan;
                Plan = change.Target;
                applied = change.Target;
                ScheduledChange = null;

                Raise(new PlanChangeApplied(Guid.NewGuid(), Cycle.RenewsOn, MemberId, previous, change.Target));
            }

            Renew();
        }

        return Result.Success(applied);
    }

    /// <summary>
    /// Moves to the next cycle. The city-change allowance resets with it.
    ///
    /// Takes no clock: the next cycle starts at the current renewal date, never at "now", so a
    /// subscription renewed late still lands on its anchor day.
    /// </summary>
    public void Renew()
    {
        Cycle = Cycle.Next();
        CityChangesThisCycle = 0;
    }

    // ---------- Residence ----------

    /// <summary>
    /// Records a change of city, enforcing the per-cycle limit of BR-MBR-011.
    ///
    /// The limit exists to stop a Plus member rotating cities to obtain Max reach. Tying it to the
    /// billing cycle avoids inventing a separate duration, and still accommodates a genuine move
    /// within a month.
    /// </summary>
    public Result RecordCityChange()
    {
        if (!IsActive)
        {
            return Result.Failure(MembershipErrors.SubscriptionEnded);
        }

        if (CityChangesThisCycle >= 1)
        {
            return Result.Failure(MembershipErrors.CityChangeLimitReached);
        }

        CityChangesThisCycle++;

        return Result.Success();
    }

    public void End(DateTimeOffset now) => EndedAt = now;
}
