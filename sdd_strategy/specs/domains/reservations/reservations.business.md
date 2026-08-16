# Reservations — Business Specification

**Last reviewed:** 2026-08-15
**Reviewed by:** Francisco Torregrosa
**Version:** 0 — placeholder, authored during PLAN-001 Stage 3
**Ring:** MVP

> **PLACEHOLDER.** This file carries every required section with guidance on what belongs in each.
> It is filled in at the start of PLAN-001 Stage 3, before any implementation in this domain.
> The product authority is the prototype in `docs/design/` — read `prototype.source.js` for the real
> rules, exact copy, and seed data. Do not invent product behaviour.


---

## 1. Purpose

Owns the loan cycle: reserving a copy, delivering it, tracking the term, and taking it back. It answers "who has what, until when, and what state is it in".

---

## 2. Glossary

Terms specific to this domain. Where a term means something different here than in
`global_spec.md`, that difference must be stated explicitly.

| Term | Definition |
|---|---|
| **Reservation** | The link between a member and a copy for a fixed term. The product term for a loan - never "check-out" |
| **Term** | The fixed period a reservation runs before it becomes overdue |
| **Delivery mode** | How a reserved copy reaches the member: collection at the library, or home delivery |
| **Return mode** | How a copy goes back: courier pickup, or drop-off at the desk |
| **Handover code** | The code a courier presents to confirm a collection |
| **Check-in** | The staff action that completes a return and returns the copy to stock |

---

## 3. Business Rules

Numbered `BR-RSV-{NNN}`. Each rule must be a complete, unambiguous, independently testable
statement. Use "must", never "should". A rule that does not fit in one sentence is probably two rules.
**An ID never changes**, even when the rule text does.

| ID | Rule |
|---|---|
| `BR-RSV-001` | *To be authored.* |

Rules this domain is expected to define:

- The loan term length and how the due date is derived
- That a reservation targets one specific copy at one specific library
- Delivery and return modes, and which carry a charge
- The state machine and every legal transition
- That a return completes only when staff check the copy in
- That the available copy count may never go negative
- That a member may not hold two active reservations of the same copy
- How simultaneous attempts on the last copy are resolved

---

## 4. Acceptance Criteria

Numbered `AC-RSV-{NNN}`, each mapping to one or more business rules. These drive test definition.

| ID | Criterion | Covers |
|---|---|---|
| `AC-RSV-001` | *To be authored.* | `BR-RSV-001` |

---

## 5. Edge Cases

Non-obvious scenarios and their expected behaviour. This section is where most defects are prevented.

| Scenario | Expected behaviour |
|---|---|
| *To be authored.* | |

---

## 6. Out of Scope

Explicitly **not** handled by this domain. This section is as important as what the domain does handle —
ambiguity about boundaries is the most common source of domain conflicts.

- Deciding whether the member may reserve at all - that belongs to `catalog`
- Charging the delivery fee and accruing late fines - that belongs to `billing`
- Holds queues on out-of-stock titles
- Inter-library transfer of copies

---

## 7. Prototype Reference

Screens: `loans` (Book Reservations), the reservation confirmation modal, the courier modal, and the `home` dashboard

Read `docs/design/prototype.source.js` for the authoritative rules, copy, and seed data.
Read `docs/design/prototype.text-outline.txt` to locate a screen or string quickly.
