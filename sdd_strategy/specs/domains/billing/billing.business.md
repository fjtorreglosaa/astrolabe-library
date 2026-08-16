# Billing — Business Specification

**Last reviewed:** 2026-08-16
**Reviewed by:** AI Agent — Claude — 2026-08-16
**Version:** 1
**Ring:** MVP

---

## 1. Purpose

Billing owns what a member owes and what they have paid.

It answers *"how much, for what, and has it been settled"*.

`reservations` reports that a copy came back late; this domain decides what that costs. Nothing else
in the system knows the daily rate or the cap, and nothing else is allowed to.

It is also the only domain that touches money movement, and it never mutates a balance. Every
movement is an entry in an immutable ledger, and the balance is what those entries add up to.

---

## 2. Glossary

| Term | Definition |
|---|---|
| **Fine** | What a member owes for one late title. One fine per reservation, never per day |
| **Accrual** | Growing a fine as the days pass. Stops at check-in and at the cap |
| **Ledger entry** | One immutable movement: a charge, a payment or a credit. Never edited, never deleted |
| **Balance** | The sum of a member's ledger entries. Derived, never stored |
| **Payment method** | A card the member has on file. Only its display details are held — never a card number |
| **Desk payment** | Cash or card paid at a library counter, authorised by a short code |
| **Payment code** | The `MP-…` code a member shows at the desk. Valid for 72 hours |
| **Validation** | A librarian confirming they took the money. The only thing that settles a desk payment |

> **A fine is per title, not per day.** The days are how it is calculated; the debt is one thing the
> member owes for one book.

> **No card number ever reaches this system.** The interface collects brand, last four digits, expiry
> and cardholder — the details a payment provider returns after tokenising. There is no field, no
> column and no endpoint that could hold a full number.

---

## 3. Business Rules

### Fines

| ID | Rule |
|---|---|
| `BR-BIL-001` | A late return accrues **$0.35 per day, per title** |
| `BR-BIL-002` | A fine is capped at **$9.00 per title**, so it stops growing after 26 days |
| `BR-BIL-003` | A fine stops accruing when the library checks the copy in. The days late frozen at check-in are the days it is priced on |
| `BR-BIL-009` | A copy returned on time produces no fine at all, not a fine of zero |
| `BR-BIL-010` | One reservation produces at most one fine, however many times it is assessed |

### The ledger

| ID | Rule |
|---|---|
| `BR-BIL-007` | Every monetary amount is stored as an integer number of cents |
| `BR-BIL-011` | A balance is never stored or mutated. Every movement is an immutable ledger entry, and the balance is their sum |
| `BR-BIL-012` | A ledger entry is never edited or deleted. A mistake is corrected by a compensating entry, which leaves both visible |
| `BR-BIL-013` | Every entry records what it was for, in the member's own terms, and which reservation or subscription it came from |

### Paying by card

| ID | Rule |
|---|---|
| `BR-BIL-006` | A stored payment method retains only brand, last four digits, expiry and cardholder. A full card number is never accepted, transmitted or stored |
| `BR-BIL-008` | Payment operations are idempotent — a repeated request never charges twice, and answers with the same receipt rather than an empty one |
| `BR-BIL-014` | A member may pay one fine or several at once, and the receipt covers exactly what was paid |
| `BR-BIL-015` | Paying a fine settles it immediately and writes a payment entry to the ledger |
| `BR-BIL-016` | A member may only pay their own fines, and no endpoint accepts another member's identifier |

### Paying at the desk

| ID | Rule |
|---|---|
| `BR-BIL-004` | A desk payment code is valid for **72 hours** from issue |
| `BR-BIL-005` | Only an administrator of the **owning library** may validate or reject a desk payment |
| `BR-BIL-017` | Issuing a code does not settle anything. The fines it covers are marked awaiting validation and remain owed |
| `BR-BIL-018` | Validation settles the fines the code covers and writes one payment entry |
| `BR-BIL-019` | Rejection returns the fines to unpaid and requires a stated reason |
| `BR-BIL-020` | An expired code can be neither validated nor rejected, and its fines return to unpaid |
| `BR-BIL-021` | A fine already awaiting validation cannot be paid by card or given a second code |
| `BR-BIL-022` | Issuing, validating and rejecting each write an audit entry recording who, what and when |
| `BR-BIL-023` | One payment code covers fines owed to **one library**. Fines owed to different libraries need one code each |

---

## 4. Acceptance Criteria

| ID | Criterion | Covers |
|---|---|---|
| `AC-BIL-001` | **A book 20 days overdue produces exactly $7.00** | `BR-BIL-001` |
| `AC-BIL-002` | **At 26 days it is capped at $9.00**, and stays there at 200 days | `BR-BIL-002` |
| `AC-BIL-003` | At 25 days it is $8.75 — the cap does not bite early | `BR-BIL-002` |
| `AC-BIL-004` | A copy returned on time produces no fine record | `BR-BIL-009` |
| `AC-BIL-005` | Assessing one reservation twice leaves one fine | `BR-BIL-010` |
| `AC-BIL-006` | A balance is the sum of the entries, and paying leaves the charge entry in place | `BR-BIL-011`, `BR-BIL-012` |
| `AC-BIL-007` | Paying the same fines twice charges once and returns the same receipt both times | `BR-BIL-008` |
| `AC-BIL-008` | A stored card exposes no field that could hold a card number | `BR-BIL-006` |
| `AC-BIL-009` | A desk code issued 71 hours ago validates; one issued 73 hours ago does not | `BR-BIL-004`, `BR-BIL-020` |
| `AC-BIL-010` | An administrator of another library cannot validate the code | `BR-BIL-005` |
| `AC-BIL-011` | Issuing a code leaves the fine owed, and validation is what clears it | `BR-BIL-017`, `BR-BIL-018` |
| `AC-BIL-012` | A rejected code returns its fines to unpaid and records the reason | `BR-BIL-019` |
| `AC-BIL-013` | A fine awaiting validation cannot also be paid by card | `BR-BIL-021` |
| `AC-BIL-014` | A member requesting another member's fines receives their own | `BR-BIL-016` |
| `AC-BIL-015` | Asking for one code covering fines from two libraries is refused | `BR-BIL-023` |

---

## 5. Edge Cases

| Scenario | Expected behaviour |
|---|---|
| A copy is checked in twice | One fine. `reservations` makes the second check-in a no-op, and `BR-BIL-010` makes a second assessment one too |
| A member pays by card while a desk code is open for the same fine | Refused. `BR-BIL-021` — otherwise the librarian validates a fine that is already settled and the member pays twice |
| The desk code expires with the fine still owed | The fine returns to unpaid and the member can pay again. Nothing is forgiven by a code going stale |
| A librarian validates a code for a fine that was already cleared | Refused. The fines a code covers are checked at validation, not only at issue |
| A member is 200 days late | $9.00. The cap is a cap, not a rate change |
| A book is removed from the catalogue while its fine is unpaid | The fine stands. The member had the library's property regardless of what happened to the catalogue record |
| A member's account is deleted with fines owed | The ledger survives. A record of money owed must outlive the account that owed it |
| Two administrators validate the same code at once | One succeeds. The second finds it already validated and is told so, and no second payment entry is written |

---

## 6. Out of Scope

Explicitly **not** handled by this domain:

- Taking real money. No payment provider is integrated; a card payment is recorded, not charged
- Storing, transmitting or validating a card number. See `BR-BIL-006`
- Refunds, chargebacks, partial payments and instalments
- Deciding that a return was late — that is `reservations`, which reports the days
- Subscription charges. `membership` computes proration; a future stage may route it through this ledger
- Purchases — that is `store`, which will write to this ledger in Stage 5
- Tax, currency other than USD, and any exchange rate

---

## 7. Prototype Reference

Screens: `fines` — the outstanding total, the fine list, the payment modal with its select, confirm
and done steps — and `admin-payments`, the desk payment queue with validate and reject.

Seed data confirms the arithmetic: *The Savage Detectives* at "20 days late" is `cents: 700`, and
*Pedro Paramo* at "11 days late" is `cents: 385`. Both are exactly days × 35.

The prototype's own help text states the rule to members: *"A late return costs $0.35 a day per
title."*

---

## 8. Resolved Questions

**Where does a fine's amount live once the copy is back?** Frozen. `reservations` records the days
late at check-in, and the fine is priced from that number and never recomputed. A fine that
recalculated itself would keep growing after the book was returned, which is precisely what
`BR-BIL-003` forbids — and the bug would only appear days later, in production, on somebody's bill.

**Why is the balance not a column?** Because a stored balance is a second source of truth for
something the entries already say, and the day they disagree there is no way to tell which is right.
Summing the entries is slower and always correct. If it ever becomes too slow, the fix is a
materialised projection that can be rebuilt from the entries — not a mutable number.

**Why must a code cover only one library?** Because `BR-BIL-005` lets only the owning library's
staff validate it. A code spanning two counters could be validated at neither without somebody acting
outside their scope — and the member would be sent to a desk that is not allowed to take their money.
The constraint is a consequence of the authority rule, not a separate policy.

**Why can a desk code not settle the fine at issue?** Because nobody has paid yet. Marking the fine
settled when a code is printed would let any member clear their debt by generating a code and never
going to the library.
