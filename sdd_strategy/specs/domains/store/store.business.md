# Store — Business Specification

**Last reviewed:** 2026-08-15
**Reviewed by:** Francisco Torregrosa
**Version:** 0 — placeholder, authored during PLAN-001 Stage 5
**Ring:** MVP

> **PLACEHOLDER.** This file carries every required section with guidance on what belongs in each.
> It is filled in at the start of PLAN-001 Stage 5, before any implementation in this domain.
> The product authority is the prototype in `docs/design/` — read `prototype.source.js` for the real
> rules, exact copy, and seed data. Do not invent product behaviour.


---

## 1. Purpose

Owns buying books outright, as distinct from borrowing them: pricing, plan discounts, fulfilment, and the reward point wallet. It answers "what does this cost this member, and what did they earn by paying it".

---

## 2. Glossary

Terms specific to this domain. Where a term means something different here than in
`global_spec.md`, that difference must be stated explicitly.

| Term | Definition |
|---|---|
| **Order** | A purchase of one or more books |
| **Order line** | One book within an order. The discount is applied here, never to the order total |
| **Discount** | The percentage a plan removes from a line, subject to the plan's geographic condition |
| **Fulfilment** | How a purchased book reaches the buyer: collection, or shipping |
| **Reward point** | A unit of the wallet, worth exactly one cent |
| **Accrual** | Earning points from a purchase. In `store` this concerns points; in `billing` it concerns fines. The two must not be confused |
| **Redemption** | Spending wallet balance against an order |

---

## 3. Business Rules

Numbered `BR-STR-{NNN}`. Each rule must be a complete, unambiguous, independently testable
statement. Use "must", never "should". A rule that does not fit in one sentence is probably two rules.
**An ID never changes**, even when the rule text does.

| ID | Rule |
|---|---|
| `BR-STR-001` | *To be authored.* |

Rules this domain is expected to define:

- The discount rate per plan and the geographic condition attached to each
- That the discount applies per line so the city condition stays auditable
- Which plan accrues points, and the accrual formula and rounding direction
- The redemption cap - **currently undefined, see `BLOCK-002`**
- What happens to a wallet balance on downgrade
- Fulfilment options and which carry a charge
- That every amount is an integer number of cents
- That order creation is idempotent

---

## 4. Acceptance Criteria

Numbered `AC-STR-{NNN}`, each mapping to one or more business rules. These drive test definition.

| ID | Criterion | Covers |
|---|---|---|
| `AC-STR-001` | *To be authored.* | `BR-STR-001` |

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

- Real payment settlement - simulated behind `IPaymentProvider`
- Fines and their payment - that belongs to `billing`
- Deciding whether a member may buy a given title - the reach rule comes from `catalog`
- Physical shipping logistics and tracking

---

## 7. Prototype Reference

Screens: The Buy modal, `purchases` (My purchases), and the point wallet

Read `docs/design/prototype.source.js` for the authoritative rules, copy, and seed data.
Read `docs/design/prototype.text-outline.txt` to locate a screen or string quickly.
