# Billing — Tasks

**Last reviewed:** 2026-08-16
**Overall progress:** 22/22 (100%)

The whole of PLAN-001 Stage 4. Depends on Stage 3: the days late come from `reservations`.

---

## Blocking Dependencies

| Block ID | Description | Status |
|---|---|---|
| `RSV-007` | Check-in must freeze the days late before a fine can be priced from it | Resolved 2026-08-16 |
| `RSV-008` | `ReservationReturned` must carry `DaysLate` | Resolved 2026-08-16 |

---

## Task List

| ID | Task | Status | Blocker | Tracking | Notes |
|---|---|---|---|---|---|
| `BIL-001` | `FinePolicy`: the rate, the cap, and nothing else | ✅ | — | `FinePolicy.cs`, 12 tests | `BR-BIL-001`, `-002`. Highest-value unit |
| `BIL-002` | `FineStatus`, `LedgerEntryKind`, `DeskPaymentStatus`, `CardBrand` | ✅ | — | 4 enumerations | |
| `BIL-003` | `PaymentCode` value object, `MP-…` | ✅ | — | `PaymentCode.cs` | `BR-BIL-004` |
| `BIL-004` | `BillingErrors` | ✅ | — | `BillingErrors.cs` | Typed, never strings |
| `BIL-005` | `Fine` aggregate: assess, reserve, release, settle | ✅ | `BIL-001` | `Fine.cs` | Amount frozen at assessment |
| `BIL-006` | `LedgerEntry`, append-only by contract | ✅ | `BIL-002` | `LedgerEntry.cs` | `BR-BIL-011`, `-012` |
| `BIL-007` | `PaymentMethod` with a four-digit guard | ✅ | `BIL-002` | `PaymentMethod.cs`, `char(4)` column | **`BR-BIL-006` — must refuse a full number** |
| `BIL-008` | `DeskPayment` with 72-hour expiry | ✅ | `BIL-003` | `DeskPayment.cs` | `BR-BIL-004`, `-017` to `-020` |
| `BIL-009` | Domain events | ✅ | `BIL-008` | 5 domain events | |
| `BIL-010` | Four repository contracts and `IBillingUnitOfWork` | ✅ | `BIL-005` | 4 contracts, `BillingUnitOfWork` | No update path on the ledger |
| `BIL-011` | EF configuration and migration | ✅ | `BIL-010` | `AddBillingDomain` | Complex `Money`. Down-migration verified |
| `BIL-012` | Unique indexes: `reservation_id` and the desk code | ✅ | `BIL-011` | Unique on `reservation_id` and `code` | `BR-BIL-008`, `-010` in the database |
| `BIL-013` | `AssessFineCommand`, idempotent | ✅ | `BIL-010` | `AssessFineCommandHandler` | `BR-BIL-010` |
| `BIL-014` | `AssessFineOnReturnHandler` | ✅ | `BIL-013` | `AssessFineOnReturnHandler` | Immediate path |
| `BIL-015` | `AssessOutstandingFinesJob` | ✅ | `BIL-013` | `AssessOutstandingFinesJob` | The guarantee. Concurrency disabled, `IOptions<T>`, `ISender` |
| `BIL-016` | `PayFinesCommand`, idempotent through the fine's state | ✅ | `BIL-013` | `PayFinesCommandHandler` | `BR-BIL-008`, `-014`, `-021` |
| `BIL-017` | `IssueDeskPaymentCommand` | ✅ | `BIL-016` | `IssueDeskPaymentCommandHandler` | Settles nothing |
| `BIL-018` | `ValidateDeskPaymentCommand`, `RejectDeskPaymentCommand` | ✅ | `BIL-017` | 2 commands, library-scoped | `BR-BIL-005`, `-018` to `-020` |
| `BIL-019` | Payment method commands | ✅ | `BIL-007` | 2 commands | |
| `BIL-020` | Four queries | ✅ | `BIL-016` | 4 queries | None takes a member identifier |
| `BIL-021` | `BillingController` and `AdminPaymentsController` | ✅ | `BIL-020` | `BillingController`, `AdminPaymentsController` | Staff routes separate |
| `BIL-022` | `fines` screen with the payment modal, and `admin-payments` | ✅ | `BIL-021` | `FinesPage`, `PayFinesDialog`, `AdminPaymentsPage` | Copy from the prototype |

### Status values

⬜ Not started · 🔄 In progress · ✅ Done · ❌ Removed · 🔴 Blocked

---

## Test Obligations

| Test | Covers |
|---|---|
| **20 days overdue is exactly $7.00** | `AC-BIL-001` — stated in the plan |
| **26 days is capped at $9.00, and so is 200 days** | `AC-BIL-002` |
| 25 days is $8.75 — the cap does not bite early | `AC-BIL-003` |
| An on-time return produces no fine at all | `AC-BIL-004` |
| Assessing one reservation twice leaves one fine | `AC-BIL-005` |
| The balance is the sum of entries, and paying leaves the charge in place | `AC-BIL-006` |
| Paying the same fines twice charges once and returns the same receipt | `AC-BIL-007` |
| **A payment method refuses anything but four digits** | `AC-BIL-008` |
| A code at 71 hours validates; at 73 hours it does not | `AC-BIL-009` |
| An administrator of another library cannot validate | `AC-BIL-010` |
| Issuing a code leaves the fine owed; validation clears it | `AC-BIL-011` |
| Rejection returns the fines to unpaid with a reason | `AC-BIL-012` |
| A fine awaiting validation cannot be paid by card | `AC-BIL-013` |
| A member's query returns only their own fines | `AC-BIL-014` |

---

## Completion Log

| Date | Task ID | Completed by | Notes |
|---|---|---|---|
| 2026-08-16 | `BIL-001` to `BIL-009` | AI Agent — Claude | Domain model. 61 tests, including a full sweep of every day count up to the cap |
| 2026-08-16 | `BIL-007` | AI Agent — Claude | The four-digit guard **refuses** a full card number rather than truncating it, and the column is `character(4)` so a direct database write cannot hold one either |
| 2026-08-16 | `BIL-010` to `BIL-012` | AI Agent — Claude | The ledger repository deliberately does **not** extend `IRepository<T>`: that base offers `Update` and `Remove`, which a ledger must not have. Down-migration reverted and reapplied against the running database |
| 2026-08-16 | `BIL-013` to `BIL-020` | AI Agent — Claude | 17 application tests. Verified live: 20 days is $7.00, 25 is $8.75, 26 and 60 are $9.00 |
| 2026-08-16 | `BIL-016` | AI Agent — Claude | **Scope corrected.** The command declared an `IdempotencyKey` nothing used — a guarantee advertised and not given. Removed: the fine's own state already provides it, and the receipt now describes every requested fine that is paid so a retry answers the same rather than $0.00 |
| 2026-08-16 | `BIL-017` | AI Agent — Claude | `BR-BIL-023` added during implementation: one code covers one library, which follows from `BR-BIL-005` rather than being a new policy |
| 2026-08-16 | `BIL-022` | AI Agent — Claude | Fines screen with the three-step payment modal, and the desk queue. 11 frontend tests, several asserting that a desk code is never described as paid |

---

## Progress Summary

| Layer | Tasks | Done |
|---|---|---|
| Domain | `BIL-001` to `BIL-009` | 9/9 |
| Infrastructure | `BIL-010` to `BIL-012` | 3/3 |
| Application | `BIL-013` to `BIL-020` | 8/8 |
| Presentation | `BIL-021` | 1/1 |
| Frontend | `BIL-022` | 1/1 |
