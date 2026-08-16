# Membership — Tasks

**Last reviewed:** 2026-08-16
**Overall progress:** 18/18 (100%)

Built before `catalog`, which consumes `MemberEntitlement`.

---

## Blocking Dependencies

| Block ID | Description | Status |
|---|---|---|
| — | None. `identity` and `network` are complete | — |

---

## Task List

| ID | Task | Status | Blocker | Tracking | Notes |
|---|---|---|---|---|---|
| `MBR-001` | `PlanTier`, `ReachKind` enumerations, ordered so tier comparison is a comparison | ✅ | — | `PlanTier.cs`, `ReachKind.cs` | Shared with `catalog` |
| `MBR-002` | `BillingCycle` value object with a remembered anchor day | ✅ | — | `BillingCycle.cs`, 10 tests | `BR-MBR-025`, `-026` |
| `MBR-003` | `ProrationQuote` value object, integer cents throughout | ✅ | `MBR-001` | `ProrationQuote.cs` | `BR-MBR-014` |
| `MBR-004` | `MemberEntitlement`, the record every other domain consumes | ✅ | `MBR-001` | `MemberEntitlement.cs` | |
| `MBR-005` | `PlanCatalog`: prices, reach, discount, points, recommendations per plan | ✅ | `MBR-001` | `PlanDefinition.cs` | `BR-MBR-002` to `-009` |
| `MBR-006` | `Subscription` aggregate with `Start` and `Renew` | ✅ | `MBR-002` | `Subscription.cs` | `BR-MBR-001` |
| `MBR-007` | `Subscription.Upgrade`, immediate and prorated | ✅ | `MBR-006` | `Subscription.Upgrade` | `BR-MBR-013` to `-015` |
| `MBR-008` | `Subscription.ScheduleDowngrade` and `CancelScheduledChange` | ✅ | `MBR-006` | `ScheduleDowngrade`, `CancelScheduledChange` | `BR-MBR-016` to `-019` |
| `MBR-009` | `Subscription.ApplyDueChange`, idempotent | ✅ | `MBR-008` | `ApplyDueChange`, rolls whole cycles | `BR-MBR-021` |
| `MBR-010` | `RecordCityChange` with the per-cycle limit | ✅ | `MBR-006` | `RecordCityChange` | `BR-MBR-011` |
| `MBR-011` | Domain events for started, upgraded, scheduled, cancelled, applied | ✅ | `MBR-009` | 5 domain events | |
| `MBR-012` | EF configuration, owned types, and migration | ✅ | `MBR-006` | `SubscriptionConfiguration`, `AddMembershipSubscriptions` | Verified down-migration |
| `MBR-013` | `ISubscriptionRepository` and `IMembershipUnitOfWork` | ✅ | `MBR-012` | `SubscriptionRepository`, `MembershipUnitOfWork` | RULE 16, RULE 18 |
| `MBR-014` | `EntitlementProvider`, scoped, applying a due change on read | ✅ | `MBR-013` | `EntitlementProvider`, scoped | |
| `MBR-015` | `ChangePlanCommand` and `CancelScheduledPlanChangeCommand` | ✅ | `MBR-014` | `ChangePlanCommand`, `CancelScheduledPlanChangeCommand` | |
| `MBR-016` | `ChangeCityOfResidenceCommand` | ✅ | `MBR-014` | `ChangeCityOfResidenceCommand` | `BR-MBR-012` |
| `MBR-017` | `GetMyMembershipQuery`, `GetPlanComparisonQuery`, `QuotePlanChangeQuery` | ✅ | `MBR-014` | 3 queries | `BR-MBR-020` |
| `MBR-018` | `MembershipController` and the Membership settings screen | ✅ | `MBR-017` | `MembershipController`, `MembershipPage` | Plan comparison and change modal |

### Status values

⬜ Not started · 🔄 In progress · ✅ Done · ❌ Removed · 🔴 Blocked

---

## Test Obligations

| Test | Covers |
|---|---|
| Upgrading mid-cycle charges the difference and never a negative amount | `AC-MBR-001`, `-002` |
| A downgrade leaves the plan untouched until the renewal date | `AC-MBR-004` |
| A downgrade charges nothing | `AC-MBR-005` |
| Cancelling a scheduled change restores no-outstanding-change | `AC-MBR-006` |
| A second scheduled change replaces the first | `AC-MBR-007` |
| An upgrade cancels a pending downgrade | Edge case, §5 |
| A cycle anchored on the 31st renews on 28 or 29 February and returns to the 31st | `AC-MBR-012`, `-013` |
| A second city change in one cycle is refused | `AC-MBR-014` |
| `ApplyDueChange` is idempotent | `BR-MBR-021` |

---

## Completion Log

| Date | Task ID | Completed by | Notes |
|---|---|---|---|
| 2026-08-15 | `MBR-001` to `MBR-005` | AI Agent — Claude | Plan table, billing cycle and proration. 37 domain tests |
| 2026-08-15 | `MBR-006` to `MBR-011` | AI Agent — Claude | `Subscription` aggregate and its 5 events |
| 2026-08-15 | `MBR-012`, `MBR-013` | AI Agent — Claude | Owned `BillingCycle` and `ScheduledPlanChange`. Down-migration reverted and reapplied against the running database on 2026-08-16, not merely generated |
| 2026-08-15 | `MBR-014` to `MBR-018` | AI Agent — Claude | Entitlement provider, 2 commands, 3 queries, controller and Membership screen |
| 2026-08-16 | `MBR-009` | AI Agent — Claude | **Reopened and completed.** The read path existed but `ApplyDuePlanChangesJob` did not, so BR-MBR-021 was half-implemented and `GetDueForRenewalAsync` was dead code. Verified by backdating a renewal and restarting: the sweep applied the downgrade, cleared the pending change and preserved the anchor day |
| 2026-08-16 | `MBR-015` to `MBR-017` | AI Agent — Claude | 10 application-layer handler tests added; the layer had none for this domain |

---

## Progress Summary

| Layer | Done | Total |
|---|---|---|
| Domain | 0 | 11 |
| Infrastructure | 0 | 3 |
| Application | 0 | 3 |
| Presentation and frontend | 0 | 1 |
| **Total** | **0** | **18** |
