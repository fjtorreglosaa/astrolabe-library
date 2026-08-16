# Reservations — Tasks

**Last reviewed:** 2026-08-16
**Overall progress:** 20/20 (100%)

The whole of PLAN-001 Stage 3. Depends on Stage 2: the access verdict and the copy both come from
`catalog`.

---

## Blocking Dependencies

| Block ID | Description | Status |
|---|---|---|
| `CAT-006` | `CatalogAccessPolicy` must exist before a reservation can be refused for reach | Resolved 2026-08-16 |
| `CAT-003` | `BookCopy.Take` and its concurrency token must exist before stock can move | Resolved 2026-08-16 |

---

## Task List

| ID | Task | Status | Blocker | Tracking | Notes |
|---|---|---|---|---|---|
| `RSV-001` | `ReservationStatus`, `DeliveryMethod`, `ReturnMethod` enumerations | ✅ | — | 3 enumerations | No `Overdue` member, by design |
| `RSV-002` | `LoanPeriod` value object, 14 days, lateness counted as days started | ✅ | — | `LoanPeriod.cs` | `BR-RSV-001`, `-010` |
| `RSV-003` | `HandoverCode` value object | ✅ | — | `HandoverCode.cs` | `BR-RSV-013`, `-014` |
| `RSV-004` | `ReservationErrors` | ✅ | — | `ReservationErrors.cs` | Reasons are typed, never strings |
| `RSV-005` | `Reservation` aggregate with `Confirm` | ✅ | `RSV-002` | `Reservation.Confirm` | `BR-RSV-001` to `-005` |
| `RSV-006` | `BeginReturn` with code matching | ✅ | `RSV-005` | `BeginReturn` | `BR-RSV-013` to `-015` |
| `RSV-007` | `CheckIn`, idempotent, recording days late | ✅ | `RSV-006` | `CheckIn`, returns whether stock moved | `BR-RSV-016` to `-020` |
| `RSV-008` | Domain events for confirmed, return started, returned | ✅ | `RSV-007` | 3 domain events | `ReservationReturned` carries `DaysLate` |
| `RSV-009` | `IReservationRepository` and `IReservationUnitOfWork` | ✅ | `RSV-005` | `ReservationRepository`, `ReservationUnitOfWork` | Exposes the book repository too — stock and record are one fact |
| `RSV-010` | EF configuration and migration | ✅ | `RSV-009` | `AddReservationsDomain` | Owned `LoanPeriod`, complex `Money`. Down-migration verified |
| `RSV-011` | Unique index on `(member_id, idempotency_key)` | ✅ | `RSV-010` | Filtered unique index | `BR-RSV-008` in the database |
| `RSV-012` | `ConfirmReservationCommand` with the concurrency path | ✅ | `RSV-009` | `ConfirmReservationCommandHandler` | **The stage's highest-risk unit** |
| `RSV-013` | `BeginReturnCommand` and `CheckInReservationCommand` | ✅ | `RSV-012` | 2 commands, separate authority | Separate commands, separate authority |
| `RSV-014` | `GetMyReservationsQuery` and `QuoteReservationQuery` | ✅ | `RSV-012` | 2 queries | No member identifier in the contract |
| `RSV-015` | `GetMyDashboardQuery` | ✅ | `RSV-014` | `GetMyDashboardQuery` | Stat cards and active reservations |
| `RSV-016` | `GetLibraryReservationsQuery` scoped to assigned libraries | ✅ | `RSV-014` | `GetLibraryReservationsQuery` | `BR-RSV-022` |
| `RSV-017` | `ReservationsController` and `AdminReservationsController` | ✅ | `RSV-016` | 2 controllers | Staff routes separate, as in `catalog` |
| `RSV-018` | Reservation modal: copy selection, delivery, fee, due date | ✅ | `RSV-017` | `ReserveDialog.tsx` | Opened from the catalogue |
| `RSV-019` | `loans` screen with the handover code modal | ✅ | `RSV-018` | `LoansPage.tsx`, `HandoverDialog.tsx` | Copy from the prototype |
| `RSV-020` | `home` dashboard: stat cards and active reservations | ✅ | `RSV-015` | `HomePage.tsx` | Replaces the placeholder |

### Status values

⬜ Not started · 🔄 In progress · ✅ Done · ❌ Removed · 🔴 Blocked

---

## Test Obligations

| Test | Covers |
|---|---|
| **Two simultaneous confirmations of the last copy: one succeeds, one is refused, the count lands at zero** | `AC-RSV-002` — mandatory per the plan |
| Confirmation sets the due date 14 days out and takes exactly one copy | `AC-RSV-001` |
| A replayed idempotency key returns the first reservation and takes no second copy | `AC-RSV-006` |
| Reserving the same copy twice while active is refused | `AC-RSV-005` |
| A copy the member's plan forbids is refused with the reason `catalog` gives | `AC-RSV-004` |
| Home delivery quotes $3.99 and collection $0.00 | `AC-RSV-003` |
| A wrong handover code changes nothing | `AC-RSV-007` |
| The right code moves to `InTransit` and not to `Returned` | `AC-RSV-008` |
| Only check-in returns the copy to the shelf | `AC-RSV-009` |
| A librarian of another library is refused the check-in | `AC-RSV-010` |
| Overdue is reported from the date with no job having run | `AC-RSV-011` |
| Days late counts days started and never goes negative | `BR-RSV-024` |
| A second check-in is a no-op and does not restore a second copy | `BR-RSV-019` |
| A member's query returns only their own reservations | `AC-RSV-013` |

---

## Completion Log

| Date | Task ID | Completed by | Notes |
|---|---|---|---|
| 2026-08-16 | `RSV-001` to `RSV-008` | AI Agent — Claude | Domain model and the two-step return. 24 domain tests |
| 2026-08-16 | `RSV-009` to `RSV-011` | AI Agent — Claude | Owned `LoanPeriod`, complex `Money`, filtered unique index on the idempotency key |
| 2026-08-16 | `RSV-012` to `RSV-017` | AI Agent — Claude | 15 application tests. **`AC-RSV-002` verified against the running system: ten simultaneous races for the last copy, ten correct outcomes, no negative stock** |
| 2026-08-16 | `RSV-012` | AI Agent — Claude | **Defect caught by a test.** The refusal named the branch the member had asked for — "Basic borrows at Loop only" while refusing them at Loop. It now names the member's own home library |
| 2026-08-16 | `RSV-018` to `RSV-020` | AI Agent — Claude | Reserve modal, loans table with the handover modal, home dashboard. 18 frontend tests |

---

## Progress Summary

| Layer | Tasks | Done |
|---|---|---|
| Domain | `RSV-001` to `RSV-008` | 8/8 |
| Infrastructure | `RSV-009` to `RSV-011` | 3/3 |
| Application | `RSV-012` to `RSV-016` | 5/5 |
| Presentation | `RSV-017` | 1/1 |
| Frontend | `RSV-018` to `RSV-020` | 3/3 |
