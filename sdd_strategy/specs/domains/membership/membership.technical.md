# Membership — Technical Specification

**Last reviewed:** 2026-08-16
**Reviewed by:** AI Agent — Claude — 2026-08-16
**Version:** 1
**Implements:** `BR-MBR-001` to `BR-MBR-026`

---

## 1. Domain Model

### Subscription — aggregate root

```csharp
public sealed class Subscription : AggregateRoot
{
    public Guid MemberId { get; private set; }
    public PlanTier Plan { get; private set; }
    public BillingCycle Cycle { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public ScheduledPlanChange? ScheduledChange { get; private set; }
    public int CityChangesThisCycle { get; private set; }

    public bool IsActive => EndedAt is null;

    public static Subscription Start(Guid memberId, PlanTier plan, DateTimeOffset now);

    public Result<ProrationQuote> QuoteChange(PlanTier target, DateTimeOffset now);
    public Result<ProrationQuote> Upgrade(PlanTier target, bool hasPaymentMethod, DateTimeOffset now);
    public Result ScheduleDowngrade(PlanTier target, DateTimeOffset now);
    public Result CancelScheduledChange(DateTimeOffset now);
    public Result<PlanTier?> ApplyDueChange(DateTimeOffset now);

    public Result RecordCityChange();
    public void Renew();
}
```

`CancelScheduledChange` takes the clock because it raises an event that must be stamped; `Renew`
takes none, because the next cycle starts at the current renewal date and never at "now" — passing a
clock would let a subscription renewed late drift off its anchor day.

One aggregate holds both the current plan and the pending one. Splitting them would let a downgrade
be scheduled against a plan that changed in between, which is exactly the ambiguity `BR-MBR-019`
forbids.

### BillingCycle — value object

```csharp
public sealed record BillingCycle
{
    public DateTimeOffset StartedOn { get; }
    public DateTimeOffset RenewsOn { get; }
    /// <summary>Day of month the cycle is anchored to, remembered across short months.</summary>
    public int AnchorDay { get; }

    public int TotalDays { get; }
    public int DaysRemainingAt(DateTimeOffset now);
    public bool IsDueAt(DateTimeOffset now);

    public static BillingCycle StartingAt(DateTimeOffset start);
    public BillingCycle Next();
}
```

`AnchorDay` is kept separately from `RenewsOn` on purpose. A cycle anchored on the 31st that renews
on 28 February must return to the 31st in March; storing only the renewal date would lose the anchor
and quietly walk the billing day backwards month after month.

### ProrationQuote — value object

```csharp
public sealed record ProrationQuote(
    PlanTier From, PlanTier To, Money Charge, Money Credit, Money AmountDue, DateTimeOffset EffectiveOn);
```

Implements `BR-MBR-014`. All three amounts are `Money`, so the arithmetic stays in integer cents:

```text
charge = round(targetMonthlyCents × daysRemaining / totalDays)
credit = round(currentMonthlyCents × daysRemaining / totalDays)
due    = max(charge − credit, 0)
```

`due` is floored at zero rather than producing a credit note. The prototype charges nothing on a
downgrade and never refunds, so a negative due has no meaning in this product.

### ScheduledPlanChange — value object

```csharp
public sealed record ScheduledPlanChange(PlanTier Target, DateTimeOffset EffectiveOn, DateTimeOffset RequestedAt);
```

### MemberEntitlement — the published contract

```csharp
public sealed record MemberEntitlement(
    PlanTier MaxTier,
    ReachKind Reach,
    Guid? CityId,
    Guid? HomeLibraryId,
    int DiscountPercent,
    bool EarnsPoints,
    bool SeesRecommendations);
```

What every other domain consumes. `catalog` compares `MaxTier` and `Reach`; `store` reads
`DiscountPercent` and `EarnsPoints`; `recommendations` reads `SeesRecommendations`. Publishing one
record keeps the plan table in a single place instead of a `switch` in four domains.

### Domain events

| Event | Raised when | Consumed by |
|---|---|---|
| `SubscriptionStarted` | A member registers or accepts a plan | Audit |
| `PlanUpgraded` | An upgrade applies | Audit, and `billing` once it exists |
| `PlanChangeScheduled` / `PlanChangeCancelled` | A downgrade is requested or withdrawn | Audit |
| `PlanChangeApplied` | A scheduled change lands at renewal | Audit; `store` stops point accrual |

---

## 2. Application Layer

### Commands

| Name | Input | Output | Rule |
|---|---|---|---|
| `ChangePlanCommand` | targetPlan | `Result<PlanChangeResultDto>` | `BR-MBR-013` to `-020` |
| `CancelScheduledPlanChangeCommand` | — | `Result` | `BR-MBR-018` |
| `ChangeCityOfResidenceCommand` | countryId, cityId | `Result` | `BR-MBR-011`, `-012` |

`ChangePlanCommand` covers both directions rather than splitting into upgrade and downgrade. The
member presses one button; which rules apply is decided by plan rank, and putting that decision in
two commands would let a caller pick the wrong one.

### Queries

| Name | Input | Output | Rule |
|---|---|---|---|
| `GetMyMembershipQuery` | — | `Result<MembershipDto>` | `BR-MBR-001`, `-021` |
| `GetPlanComparisonQuery` | — | `Result<IReadOnlyList<PlanOptionDto>>` | `BR-MBR-002` to `-009` |
| `QuotePlanChangeQuery` | targetPlan | `Result<PlanChangeQuoteDto>` | `BR-MBR-014`, `-020` |
| `GetMyEntitlementQuery` | — | `Result<MemberEntitlement>` | the published contract |

`QuotePlanChangeQuery` exists so the confirmation modal shows the exact amount and the exact list of
what is lost **before** the member commits. `BR-MBR-020` requires that disclosure, and computing it
in the frontend would put the money arithmetic in two places.

### Cross-cutting service

```csharp
public interface IEntitlementProvider
{
    Task<MemberEntitlement> GetForCurrentMemberAsync(CancellationToken ct = default);
    Task<MemberEntitlement> GetForMemberAsync(Guid memberId, CancellationToken ct = default);
}
```

Resolved once per request and memoised for its lifetime, exactly like `ILibraryScopeProvider`. A plan
cannot change mid-request, and a longer cache would let a just-applied upgrade go unnoticed.

---

## 3. Infrastructure

| Concern | Implementation |
|---|---|
| Persistence | `SubscriptionRepository` extending `Repository<Subscription>` |
| EF configuration | `SubscriptionConfiguration`, with `BillingCycle` and `ScheduledPlanChange` as owned types |
| Entitlement | `EntitlementProvider`, scoped |
| Due changes | `ApplyDuePlanChangesJob`, a `BackgroundService`, idempotent, applies changes whose renewal date has passed |
| Sweep configuration | `PlanRenewalOptions` — `Enabled`, `Interval` (1 h), `BatchSize` (200) |

### Applying a scheduled change

A change lands when its renewal date passes. Two mechanisms cover it, deliberately:

1. **On read.** `IEntitlementProvider` applies a due change before answering. A member who returns
   after their renewal date sees the correct plan immediately, without waiting for a job.
2. **A background job.** Sweeps subscriptions whose change is due and applies them, so a member who
   never signs in is still billed and downgraded correctly.

Both paths go through `Subscription.ApplyDueChange`, which is idempotent. Relying on the job alone
would leave a window where the member sees the old plan; relying on the read alone would never
downgrade a dormant member.

The job creates a **DI scope per tick** and resolves `IMembershipUnitOfWork` inside it, rather than
injecting `IDbContextFactory`. The unit of work is scoped, and a scope is what keeps one change
tracker per sweep; the factory would hand out a fresh context per repository and silently break it.

It runs once at startup and then on the timer, so a deployment after a missed window catches up
immediately instead of waiting an hour. A failed sweep is logged and retried on the next tick rather
than taking the host down — the work is idempotent, so nothing is double-applied.

`Enabled` exists so a test or a deterministic local run can turn the sweep off: a background job
that cannot be disabled makes every integration test race against it.

---

## 4. Architecture Decision Log

| Decision | Choice | Rationale | Alternatives rejected |
|---|---|---|---|
| Change direction | Decided by plan rank inside one command | The member presses one button. Two commands would let a caller apply upgrade rules to a downgrade | Separate `UpgradeCommand` and `DowngradeCommand` — rejected: duplicates the rank comparison and makes the wrong pairing possible |
| Downgrade timing | Scheduled to the renewal date, held on the aggregate | The prototype is explicit: *"Downgrades wait for the end of the period you already paid"* | Immediate downgrade — rejected: contradicts the prototype and would take away entitlements already paid for |
| Proration rounding | Round each side, then floor the difference at zero | Matches the prototype's arithmetic exactly, and the floor avoids inventing a refund the product does not have | Rounding only the difference — rejected: drifts from the prototype by a cent at some day counts |
| Anchor day storage | Stored separately from the renewal date | A cycle anchored on the 31st that renews on 28 February must return to the 31st. Storing only the date walks the billing day backwards | Deriving the anchor from the last renewal — rejected: loses a day every short month |
| Applying due changes | Both on read and by a job | Read alone never downgrades a dormant member; job alone leaves a window where the member sees a stale plan | Either alone — rejected on the gap each leaves |
| Entitlement shape | One published record | Keeps the plan table in one place instead of a `switch` in `catalog`, `store` and `recommendations` | Each domain reading the subscription — rejected: four copies of one rule |
| Where a member's plan lives | `Subscription.Plan`, and nowhere else. `UserRole` holds authority only (`GLOBAL-019`, resolved 2026-08-16) | The role used to double as the plan, so a mirror handler kept the two in step on every change. That handler is deleted. One fact now has one representation, which removes not only the drift but the whole class of question — no reader has to know which of the two is current, and no new plan-changing path can forget to mirror. The plan a visitor chooses at registration travels on `UserRegistered` to reach `membership`, because `identity` deliberately does not store it | Keeping the mirror — rejected: it was correct but load-bearing, and every domain added after Stage 2 would have been one more reader that could pick the wrong side. Letting each side write independently — rejected: two writers, one fact. Putting the plan in the token — rejected: a plan changes far more often than a session lives |
| Enumerations on the wire | Names, via a global `JsonStringEnumConverter` | A numeric plan or role in a payload is unreadable in a log, and reordering an enum would silently reinterpret stored requests. Adopted while wiring `ChangePlanCommand`, whose body would otherwise have needed a hand-parsed string | Per-property converters — rejected: the same decision repeated at every call site is one that will eventually be forgotten |
| Quote before commit | A dedicated query | `BR-MBR-020` requires the member to see the amount and what they lose before confirming. Computing it in the frontend would put money arithmetic in two languages | Frontend calculation — rejected outright for money |

---

## 5. Dependencies

**This domain depends on:** `identity` for the member and their city; `network` to resolve the home
library from that city.

**Domains that depend on this one:** `catalog`, `reservations`, `store` and `recommendations`, all
through `MemberEntitlement`.

---

## 6. Known Constraints and Limitations

- No payment is taken. `ProrationQuote.AmountDue` is computed and recorded; settlement is simulated.
- Monthly cycles only. No annual plans, trials or promotional pricing.
- A downgrade cannot be scheduled for a date other than the renewal date.
- `CityChangesThisCycle` resets on renewal, so the limit is per cycle rather than a rolling window.

---

## 7. Superseded Decisions

| Decision | Superseded by | Reason | Date |
|---|---|---|---|
| `PlanChangePolicy` also disclosing lost network reach and a reduced store discount | The three losses `BR-MBR-020` names | The prototype's `losing` list names exactly three, and the prototype has the final word on what the member is told. The extra disclosures were mine, not the product's, and would have put the list out of step with the screen it fills | 2026-08-15 |
| Plan changes take effect immediately | Scheduled downgrades, `BR-MBR-016` | The prototype's `planModal` schedules a downgrade to the renewal date and charges nothing. The earlier rule in `GUIDELINES.md` §3.2 was written before that block was read, and has been corrected | 2026-08-15 |
