# Membership — Business Specification

**Last reviewed:** 2026-08-16
**Reviewed by:** AI Agent — Claude — 2026-08-16
**Version:** 1
**Ring:** MVP

---

## 1. Purpose

Membership owns subscription plans and what each one entitles a member to: which libraries they may
borrow from, which titles they may reach, what discount they get on a purchase, whether they earn
reward points, and whether they see AI recommendations. It answers *"what is this member entitled
to, and until when"*.

It does **not** decide whether a specific copy is reservable — that is `catalog`, which asks this
domain for the entitlement and applies it. Keeping the entitlement here and the decision there is
what stops the plan rules being reimplemented in four places.

---

## 2. Glossary

| Term | Definition |
|---|---|
| **Plan** | A subscription tier: Basic, Plus or Max. Held on the subscription, which is its **only** authority. Distinct from the member's role, which carries authority and never a tier |
| **Subscription** | A member's holding of a plan over a period. Exactly one is active; the rest are history |
| **Billing cycle** | The monthly period a subscription is paid for, from its start date to its renewal date |
| **Anchor day** | The day of the month a cycle renews on, fixed at subscription start. Anniversary billing, not a calendar day shared by everyone |
| **Renewal date** | When the current cycle ends and the next begins. Scheduled changes take effect here |
| **Home library** | The single library a Basic member may borrow from, derived from their city of residence |
| **Reach** | The set of libraries a plan allows borrowing from |
| **Upgrade** | A move to a higher-ranked plan. Takes effect immediately |
| **Downgrade** | A move to a lower-ranked plan. **Takes effect at the renewal date, not immediately** |
| **Scheduled change** | A downgrade that has been requested but not yet applied. Cancellable until it lands |
| **Proration** | Charging only for the days remaining in the cycle, crediting the days already paid |

Plan rank is `Basic < Plus < Max`. Direction of a change is decided by rank, never by price.

---

## 3. Business Rules

### Entitlements

| ID | Rule |
|---|---|
| `BR-MBR-001` | A member holds exactly one active subscription at any moment, with a full history of previous ones |
| `BR-MBR-002` | Basic costs $0.00, Plus costs $6.99 per month, Max costs $12.99 per month |
| `BR-MBR-003` | Basic may borrow only at the member's home library, and only titles of `Basic` tier |
| `BR-MBR-004` | Plus may borrow at every library in the member's city of residence, with no tier restriction |
| `BR-MBR-005` | Max may borrow at every library in the network, with no tier restriction |
| `BR-MBR-006` | Every plan may **browse** the entire network. Reach restricts borrowing, never discovery |
| `BR-MBR-007` | Purchase discount is 0% on Basic, 10% on Plus limited to books in the member's city, and 15% on Max in any city |
| `BR-MBR-008` | Only Max earns and redeems reward points |
| `BR-MBR-009` | Only Plus and Max see AI recommendations. Basic never sees that surface |

### Home library and residence

| ID | Rule |
|---|---|
| `BR-MBR-010` | A member's home library is derived automatically from their city of residence and is that city's designated home library |
| `BR-MBR-011` | A member may change their city of residence **once per billing cycle**. The limit exists to stop a Plus member rotating cities to obtain Max reach; tying it to the cycle avoids inventing a separate duration |
| `BR-MBR-012` | Changing city recalculates reach and home library immediately, and never affects reservations already in progress |

### Changing plan

| ID | Rule |
|---|---|
| `BR-MBR-013` | An **upgrade takes effect immediately**, and the member gains the new entitlements at once |
| `BR-MBR-014` | An upgrade is **prorated**: the member is charged for the new plan over the days remaining in the cycle, credited for the days of the current plan already paid, and the amount due is never less than zero. A member must never pay twice for the same period |
| `BR-MBR-015` | An upgrade to a paid plan requires a stored payment method |
| `BR-MBR-016` | A **downgrade is scheduled, not applied**: the current plan stays active until the renewal date, and the new plan starts then |
| `BR-MBR-017` | A downgrade charges nothing and refunds nothing. The member keeps what they already paid for |
| `BR-MBR-018` | A member may cancel a scheduled downgrade at any time before its renewal date, keeping their current plan |
| `BR-MBR-019` | A member has at most one scheduled change outstanding. Requesting another replaces it |
| `BR-MBR-020` | Before a downgrade is confirmed, the member must be shown exactly what they lose: reward points ceasing to accrue and to be redeemable when leaving Max, borrowing narrowing to the home library and Basic catalogue when moving to Basic, and AI recommendations turning off when moving to Basic |
| `BR-MBR-021` | The renewal date does not move when a plan changes. A cycle runs its course regardless |
| `BR-MBR-025` | A billing cycle is anchored to the day of the month the subscription started, not to a calendar day shared by every member |
| `BR-MBR-026` | When the anchor day does not exist in the renewal month, the cycle renews on the **last day of that month**. A subscription started on the 31st renews on 28 or 29 February, and returns to the 31st in a month that has one |

### Effects on other domains

| ID | Rule |
|---|---|
| `BR-MBR-022` | Reservations already in progress are never invalidated by a plan change, in either direction |
| `BR-MBR-023` | A member whose reach narrows may keep the reservations they hold, but may not create new ones outside the narrowed reach |
| `BR-MBR-024` | Reward points already earned survive a downgrade, but may only be redeemed while the active plan is Max |

---

## 4. Acceptance Criteria

| ID | Criterion | Covers |
|---|---|---|
| `AC-MBR-001` | A member on Plus with 28 days left who upgrades to Max is charged the Max rate for 28 days minus a credit for the 28 Plus days already paid | `BR-MBR-014` |
| `AC-MBR-002` | An upgrade never produces a negative amount due, even when the credit exceeds the charge | `BR-MBR-014` |
| `AC-MBR-003` | An upgrade grants the new entitlements on the very next request | `BR-MBR-013` |
| `AC-MBR-004` | A downgrade requested today leaves the member on their current plan until the renewal date | `BR-MBR-016` |
| `AC-MBR-005` | A downgrade charges nothing at the moment it is requested | `BR-MBR-017` |
| `AC-MBR-006` | Cancelling a scheduled downgrade restores the member to no outstanding change, still on their current plan | `BR-MBR-018` |
| `AC-MBR-007` | Requesting a second scheduled change replaces the first rather than queueing it | `BR-MBR-019` |
| `AC-MBR-008` | A Basic member is refused an upgrade with no payment method on file | `BR-MBR-015` |
| `AC-MBR-009` | A Basic member's reach is exactly one library, and it is their city's home library | `BR-MBR-003`, `BR-MBR-010` |
| `AC-MBR-010` | A Max member's reach is every active library in the network | `BR-MBR-005` |
| `AC-MBR-011` | Reservations held before a downgrade lands remain valid after it | `BR-MBR-022` |
| `AC-MBR-012` | A subscription started on 31 January renews on 28 February in a common year and on 29 February in a leap year | `BR-MBR-026` |
| `AC-MBR-013` | A cycle clamped to a short month returns to its anchor day in the next month long enough to contain it | `BR-MBR-026` |
| `AC-MBR-014` | A second city change within the same billing cycle is refused | `BR-MBR-011` |

---

## 5. Edge Cases

| Scenario | Expected behaviour |
|---|---|
| A member upgrades while a downgrade is scheduled | The scheduled downgrade is cancelled and the upgrade applies immediately. Holding both would leave the member's future plan ambiguous |
| A member downgrades to the plan they are already on | Rejected. There is nothing to schedule |
| A member on Max with points downgrades to Plus | Points survive but become unredeemable. They are not forfeited, because they were earned by spending real money |
| A scheduled downgrade reaches its renewal date while the member holds more reservations than the new plan allows | The reservations run to completion. New ones are refused until the member is within the new limits |
| A member changes city while on Basic | Their home library changes with it, and so does the only library they may borrow from |
| A member changes city twice in one cycle | The second is refused. The first already moved their reach, and oscillating within a paid period is the abuse the limit exists to stop |
| A subscription anchored on the 31st passes through February | It renews on the last day of February and returns to the 31st in March. The anchor is remembered, not overwritten by the clamp |
| A member changes city while a downgrade to Basic is scheduled | Permitted. The home library is resolved when the downgrade lands, not when it is requested |
| The renewal date passes while the system is down | The change applies on the next occasion the subscription is evaluated. A missed schedule must not be lost |
| A member upgrades on the last day of the cycle | Proration covers a single day. The amount may round to zero, which is a valid charge of nothing |

---

## 6. Out of Scope

Explicitly **not** handled by this domain:

- Taking payment. There is no gateway in the MVP; the amount is computed and recorded, and settlement
  is simulated behind `IPaymentProvider`
- Deciding whether a specific copy is reservable — that is `catalog`, which consumes the entitlement
- Applying the discount to an order — that is `store`
- Accruing or redeeming reward points — that is `store`. This domain only says whether the member is
  eligible
- Generating recommendations — that is `recommendations`. This domain only says whether the member
  may see them
- Annual plans, trials, promotional codes and refunds
- Family or shared subscriptions

---

## 7. Prototype Reference

Screens: `settings → Membership`, the plan comparison and change modal, and the plan selector on
`signup`.

The prototype's own copy for a downgrade states the rule plainly: *"Downgrades wait for the end of
the period you already paid. Nothing is charged now and nothing is refunded."* For an upgrade:
*"You only pay the difference for the days left, never twice for the same period."*

Read `docs/design/prototype.source.js`, the `planModal` block, for the exact proration arithmetic and
the wording of every line shown to the member.

---

## 8. Resolved questions

| # | Question | Resolution |
|---|---|---|
| `MBR-OPEN-001` | How often may a member change city? | **Once per billing cycle.** An earlier draft said "every 90 days", which was an invented constant. Tying the limit to a period the domain already has needs no new number, is explainable in one sentence, and blocks the actual abuse — oscillating inside a period already paid for. A genuine move is accommodated within a month, and physical collection already limits the abuse in practice |
| `MBR-OPEN-002` | What anchors a billing cycle? | **The subscription's own start date**, not a calendar day shared by everyone. A fixed day of the month would force a prorated first cycle on every new member and concentrate every renewal on one date. Anniversary billing is the industry norm, and `BR-MBR-026` settles the month-length edge by clamping to the last day while remembering the anchor |
