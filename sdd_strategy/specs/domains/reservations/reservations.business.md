# Reservations — Business Specification

**Last reviewed:** 2026-08-16
**Reviewed by:** AI Agent — Claude — 2026-08-16
**Version:** 1
**Ring:** MVP

---

## 1. Purpose

Reservations owns the loan: taking a copy off a shelf, getting it to the member, and getting it back.

It answers *"who holds which copy, until when, and where is it now"*.

It is the only domain that moves stock. `catalog` decides whether a member **may** reserve a copy;
this domain decides whether one is **still there** when they try, and it is the place where two
members reaching for the last copy is resolved.

It records that a loan is late. It never prices that lateness — a fine is `billing`'s, and Stage 4
owns it.

---

## 2. Glossary

| Term | Definition |
|---|---|
| **Reservation** | One member holding one physical copy from one library, with a due date |
| **Loan term** | The 14 days a member has the copy before it is due back |
| **Delivery** | How the copy reaches the member: collection at the library, or home delivery |
| **Return method** | How the copy goes back: a courier collects it, or the member drops it at a desk |
| **Handover code** | The short code the courier or librarian reads out, which the member types to prove the copy changed hands |
| **In transit** | The copy has left the member and has not yet been checked in by the library |
| **Check-in** | Library staff physically receiving the copy. The only thing that completes a return |
| **Overdue** | Past the due date and not yet checked in. Derived from the clock, never stored as a state |

> **A return is not an event the member can declare.** The member can say they handed the copy over;
> only the library can say it arrived. Everything between those two facts is `InTransit`.

---

## 3. Business Rules

### Creating a reservation

| ID | Rule |
|---|---|
| `BR-RSV-001` | The loan term is 14 days from confirmation |
| `BR-RSV-002` | A reservation targets one specific copy at one specific library, chosen by the member |
| `BR-RSV-003` | Home delivery adds a $3.99 charge; collection at the library is free |
| `BR-RSV-004` | A reservation may only be created for a copy the member's plan permits, as `catalog` decides it |
| `BR-RSV-005` | Confirming a reservation decrements the available count of that copy by exactly one |
| `BR-RSV-006` | The available count must never go below zero, under any amount of concurrency |
| `BR-RSV-007` | A member may not hold two active reservations of the same physical copy |
| `BR-RSV-008` | A repeated confirmation carrying the same idempotency key returns the original reservation and never takes a second copy |

### The loan

| ID | Rule |
|---|---|
| `BR-RSV-009` | A reservation is `Reserved` from confirmation until the member hands the copy over |
| `BR-RSV-010` | A reservation is **overdue** when the due date has passed and the copy has not been checked in. This is derived from the date, never stored, so it can never disagree with the calendar |
| `BR-RSV-011` | A book withdrawn to `repair` or removed from the catalogue does not disturb reservations already in progress |
| `BR-RSV-012` | A member whose plan reach narrows keeps the reservations they hold; the narrowed reach applies only to new ones |

### Returning

| ID | Rule |
|---|---|
| `BR-RSV-013` | A return begins when the member hands the copy over and confirms it with the matching handover code |
| `BR-RSV-014` | An incorrect handover code is refused and changes nothing |
| `BR-RSV-015` | Between handover and check-in the reservation is `InTransit`, and the member can take no further action on it |
| `BR-RSV-016` | A reservation becomes `Returned` only when library staff check the copy in |
| `BR-RSV-017` | Check-in restores exactly one to the available count of the copy it came from |
| `BR-RSV-018` | Only staff of the library that owns the copy may check it in |
| `BR-RSV-019` | Checking in a copy that is already `Returned` changes nothing and is not an error |
| `BR-RSV-020` | Checking in an overdue copy records the days late, so `billing` can price it. This domain never computes a fine |
| `BR-RSV-024` | Lateness counts **days started**: any part of a day past the due moment is a whole day late. A member thirty minutes over is one day late, and one three days and a minute over is four |

### Visibility and audit

| ID | Rule |
|---|---|
| `BR-RSV-021` | A member sees only their own reservations, and no endpoint accepts another member's identifier |
| `BR-RSV-022` | Staff see the reservations of the libraries assigned to them, and a super administrator sees all |
| `BR-RSV-023` | Confirmation, handover and check-in each write an audit entry recording who, what and when |

---

## 4. Acceptance Criteria

| ID | Criterion | Covers |
|---|---|---|
| `AC-RSV-001` | Confirming a reservation sets the due date 14 days out and decrements the copy by one | `BR-RSV-001`, `BR-RSV-005` |
| `AC-RSV-002` | **Two members confirming the last copy at the same instant produce one reservation and one explained rejection, and the count never goes negative** | `BR-RSV-006` |
| `AC-RSV-003` | Home delivery is quoted at $3.99 and collection at $0.00 | `BR-RSV-003` |
| `AC-RSV-004` | A Basic member cannot reserve a copy outside their home library, with the reason `catalog` gives | `BR-RSV-004` |
| `AC-RSV-005` | Reserving the same copy twice while the first is active is refused | `BR-RSV-007` |
| `AC-RSV-006` | Replaying a confirmation with the same idempotency key returns the first reservation and takes no second copy | `BR-RSV-008` |
| `AC-RSV-007` | A wrong handover code is refused and the reservation stays `Reserved` | `BR-RSV-014` |
| `AC-RSV-008` | The correct code moves the reservation to `InTransit`, and it is not yet `Returned` | `BR-RSV-013`, `BR-RSV-015` |
| `AC-RSV-009` | Only check-in makes it `Returned`, and the copy count goes back up by one | `BR-RSV-016`, `BR-RSV-017` |
| `AC-RSV-010` | A librarian of another library is refused the check-in | `BR-RSV-018` |
| `AC-RSV-011` | A reservation one day past its due date reports overdue without any job having run | `BR-RSV-010` |
| `AC-RSV-012` | Checking in three days late records three days late and charges nothing | `BR-RSV-020` |
| `AC-RSV-014` | A return thirty minutes past the due moment counts as one day late, and one exactly three days past counts as three | `BR-RSV-024` |
| `BR-RSV-025` | A member may filter their own reservations by book title or author. The match is case-insensitive and ignores surrounding whitespace, and is applied before paging |
| `AC-RSV-013` | A member requesting another member's reservations receives their own | `BR-RSV-021` |
| `AC-RSV-016` | Filtering by a title on a later page still finds it, because the filter runs before the page is taken | `BR-RSV-025` |

---

## 5. Edge Cases

| Scenario | Expected behaviour |
|---|---|
| Two members confirm the last copy simultaneously | One wins. The other is told the copy has just gone, not that it never existed. The count lands at zero, never below |
| The member confirms, then the library sends the book to repair | The loan runs to completion. `BR-RSV-011` protects it |
| The member downgrades to Basic while holding a Plus-tier loan | They keep it. Reach binds new reservations only |
| The member types the handover code before actually handing the copy over | Out of scope for the system to detect. The code is proof of a physical exchange, and the library's check-in is the fact that settles it |
| A copy is checked in twice, by two members of staff | The second is a no-op. Restoring the count twice would invent a volume the library does not own |
| The member never returns the copy | It stays overdue indefinitely. `billing` caps the fine; this domain simply keeps reporting the days |
| A reservation exists for a copy whose library is deactivated | It stands. `network` refuses to deactivate a library holding obligations, and if one slips through, the loan is still the member's to return |
| The member's account is deleted while holding a loan | The reservation survives. A record of who holds the library's property must outlive the account |

---

## 6. Out of Scope

Explicitly **not** handled by this domain:

- Pricing lateness, capping a fine, or taking payment — that is `billing`
- Whether a member's plan permits a copy — that is `catalog`, which this domain asks
- Buying a book — that is `store`
- Notifying the member — that is `notifications`, driven by this domain's events
- A holds queue for a book with no copies free. The prototype offers none: an out-of-stock book is simply not reservable
- Renewing or extending a loan
- Transferring a copy between libraries

---

## 7. Prototype Reference

Screens: the reservation modal opened from `catalog` (copy selection, delivery choice, fee breakdown,
due date), the `loans` table, the handover code modal, and the `home` dashboard's reservation card.

The state names and every member-facing sentence are transcribed from `prototype.source.js`:
`loansAll` derives the status chip, `courier` owns the handover modal, and `reserve` owns
confirmation. Where this specification and the prototype disagree, the prototype wins.

---

## 8. Resolved Questions

**How is the handover code produced?** Derived from the reservation identifier, formatted `PU-####`,
as the prototype's `pickupCode` does. It is a **proof of handover between two people present**, not a
secret: the courier reads it aloud. Making it unguessable would serve nothing, because the attacker
who could guess it still has to physically take the book — and the library's check-in is what
actually settles the return.

**Why does a part-day count as a whole day late?** Because `billing` charges per day, and the
alternative — truncating — would let every member be up to a day late for free, every time. Counting
days started is what a library desk does, and it is stated as `BR-RSV-024` rather than left implicit
in a `Math.Ceiling`, because an off-by-one here systematically overcharges or undercharges every
overdue member in the network.

**Why is `Overdue` not a state?** Because a stored state needs a job to maintain it, and the day that
job fails every overdue loan silently looks current. Deriving it from the due date means the answer
is right the moment it is asked, and `billing`'s accrual job can be late without being wrong.
