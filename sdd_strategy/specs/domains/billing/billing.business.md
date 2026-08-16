# Billing — Business Specification

**Last reviewed:** 2026-08-15
**Reviewed by:** Francisco Torregrosa
**Version:** 0 — placeholder, authored during PLAN-001 Stage 4
**Ring:** MVP

> **PLACEHOLDER.** This file carries every required section with guidance on what belongs in each.
> It is filled in at the start of PLAN-001 Stage 4, before any implementation in this domain.
> The product authority is the prototype in `docs/design/` — read `prototype.source.js` for the real
> rules, exact copy, and seed data. Do not invent product behaviour.
>
> **Growth watch:** projected to exceed 15 commands and queries. Likely split is `fines` / `payments`.

---

## 1. Purpose

Owns money owed and money taken: late fines, stored payment methods, card charges, desk payments validated by staff, and receipts. It answers "what does this member owe, and what have they paid".

---

## 2. Glossary

Terms specific to this domain. Where a term means something different here than in
`global_spec.md`, that difference must be stated explicitly.

| Term | Definition |
|---|---|
| **Fine** | A charge accrued by a late return or a lost copy |
| **Accrual** | The daily process that grows an outstanding fine |
| **Cap** | The maximum a single fine may reach, regardless of days elapsed |
| **Payment method** | A stored card, retaining only brand, last four digits, expiry, and cardholder |
| **Desk payment** | A payment made in cash or by card at a library counter |
| **Payment code** | The short-lived code a member presents at the desk, validated by an administrator |
| **Receipt** | The reference issued for a completed payment |
| **Ledger** | The immutable record of every financial movement |

---

## 3. Business Rules

Numbered `BR-BIL-{NNN}`. Each rule must be a complete, unambiguous, independently testable
statement. Use "must", never "should". A rule that does not fit in one sentence is probably two rules.
**An ID never changes**, even when the rule text does.

| ID | Rule |
|---|---|
| `BR-BIL-001` | *To be authored.* |

Rules this domain is expected to define:

- The daily fine rate, per what unit it applies, and its cap
- When accrual starts and when it stops
- Payment code validity period, and who may validate or reject it
- That an administrator may only act on payments for libraries in their scope
- What may and may not be stored about a card
- That every amount is an integer number of cents
- That payment operations are idempotent
- That balances are never mutated, only appended to the ledger

---

## 4. Acceptance Criteria

Numbered `AC-BIL-{NNN}`, each mapping to one or more business rules. These drive test definition.

| ID | Criterion | Covers |
|---|---|---|
| `AC-BIL-001` | *To be authored.* | `BR-BIL-001` |

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

- Real settlement, refunds, and chargebacks - payment is simulated behind `IPaymentProvider`
- Subscription charging - the MVP has no gateway
- Purchase pricing and discounts - that belongs to `store`
- Multi-currency. The system is USD only

---

## 7. Prototype Reference

Screens: `fines` (Fines and payments) and `admin-payments` (Manual payments)

Read `docs/design/prototype.source.js` for the authoritative rules, copy, and seed data.
Read `docs/design/prototype.text-outline.txt` to locate a screen or string quickly.
