# Membership — Business Specification

**Last reviewed:** 2026-08-15
**Reviewed by:** Francisco Torregrosa
**Version:** 0 — placeholder, authored during PLAN-001 Stage 2
**Ring:** MVP

> **PLACEHOLDER.** This file carries every required section with guidance on what belongs in each.
> It is filled in at the start of PLAN-001 Stage 2, before any implementation in this domain.
> The product authority is the prototype in `docs/design/` — read `prototype.source.js` for the real
> rules, exact copy, and seed data. Do not invent product behaviour.


---

## 1. Purpose

Owns subscription plans and what each one entitles a member to: catalogue reach, borrowing reach, purchase discount rate, reward point eligibility, and access to AI recommendations. It answers "what is this member entitled to".

---

## 2. Glossary

Terms specific to this domain. Where a term means something different here than in
`global_spec.md`, that difference must be stated explicitly.

| Term | Definition |
|---|---|
| **Plan** | A subscription tier: Basic, Plus, or Max |
| **Subscription** | A member's holding of a plan over a period of time. A member has exactly one active subscription and a full history |
| **Home library** | The single library a Basic member may borrow from, assigned automatically from their city of residence |
| **Reach** | The set of libraries a plan allows a member to borrow from |
| **Plan change** | A move between plans, taking effect immediately and recorded in the subscription history |

---

## 3. Business Rules

Numbered `BR-MBR-{NNN}`. Each rule must be a complete, unambiguous, independently testable
statement. Use "must", never "should". A rule that does not fit in one sentence is probably two rules.
**An ID never changes**, even when the rule text does.

| ID | Rule |
|---|---|
| `BR-MBR-001` | *To be authored.* |

Rules this domain is expected to define:

- The three plans, their prices, and their entitlements
- How the home library is derived from the city of residence
- What happens to reservations, points, and discounts on upgrade and downgrade
- Whether and how often a member may change their city of residence
- That plan changes are recorded, never overwritten

---

## 4. Acceptance Criteria

Numbered `AC-MBR-{NNN}`, each mapping to one or more business rules. These drive test definition.

| ID | Criterion | Covers |
|---|---|---|
| `AC-MBR-001` | *To be authored.* | `BR-MBR-001` |

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

- Charging for a plan - the MVP has no payment gateway, see `billing`
- Applying the discount to an order - that belongs to `store`
- Deciding whether a specific copy is reservable - that belongs to `catalog`

---

## 7. Prototype Reference

Screens: `settings → Membership` and the plan selector on `signup`

Read `docs/design/prototype.source.js` for the authoritative rules, copy, and seed data.
Read `docs/design/prototype.text-outline.txt` to locate a screen or string quickly.
