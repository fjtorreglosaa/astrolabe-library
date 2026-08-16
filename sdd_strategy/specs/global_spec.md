# Astrolabe Books — Global Specification

**Last reviewed:** 2026-08-16
**Reviewed by:** AI Agent — Claude — 2026-08-16
**Version:** 1

---

## 1. Purpose

Astrolabe Books is a membership platform for a network of physical libraries. It exists to let a member
discover, borrow, and buy physical books across many libraries under a single subscription, and to let
library staff run their branches without a separate back-office system.

This document holds the project-wide business context: cross-domain rules, the authoritative glossary,
and the boundaries between domains. Domain-specific rules live in each domain's `business.md`.

---

## 2. Glossary

This is the **authoritative glossary**. Where a domain uses a term differently, that domain's
`business.md` must say so explicitly.

| Term | Definition |
|---|---|
| **Member** | A user who borrows and buys. What they may reach is governed by their **plan**, which is held on their subscription and is a separate fact from their role |
| **Role** | What a user is authorised to do: `Member`, `Admin`, or `Super administrator`. A role says nothing about what anyone bought. Until `GLOBAL-019` the three plan tiers lived here too, so a member's role doubled as their plan |
| **Administrator** | A staff user who manages the specific libraries a super administrator has assigned to them. There is no separate "librarian" role |
| **Super administrator** | A staff user with unrestricted access to the whole network, and the only role that can appoint administrators |
| **Plan** | A subscription tier — Basic, Plus, or Max — governing catalogue access, borrowing reach, purchase discount, reward points, and AI recommendations. Held on `Subscription.Plan`, which is its single authority; every consumer reads it through `IEntitlementProvider` |
| **Tier** | A property **of a book**, not of a member. Values are Basic, Plus, and Max. A book is accessible when its tier is within the member's plan |
| **Library** | A physical branch belonging to a city. Holds copies |
| **Home library** | The single library a Basic member may borrow from. Assigned automatically from their city of residence |
| **City of residence** | The city a member registers in. Determines the reach of the Basic and Plus plans. Irrelevant for Max |
| **Book** | The bibliographic work. Never borrowed or sold directly |
| **Copy** | A specific physical instance of a book, belonging to exactly one library. This is what gets reserved |
| **Reservation** | The link between a member and a copy for a fixed term. The product term for a loan — never "check-out" |
| **Fine** | A charge accrued by a late return or a lost copy |
| **Desk payment** | A payment made in cash or by card at a library counter, identified by a payment code and validated by an administrator |
| **Order** | A purchase of one or more books from the store |
| **Reward point** | A unit of the Max member wallet, worth exactly one cent |
| **Ticket** | A support request raised by a member against a library |
| **Agent** | Two meanings. In `support`, the staff member handling a ticket. In `recommendations`, a prompt template with a defined objective. Each domain must disambiguate |
| **Session** | The revocable unit of authentication, bound to one device |
| **Device** | A label grouping sessions in the interface. **Never an authorization credential** |

---

## 3. Cross-domain business rules

Rules that no single domain owns. Domain rules that merely reference these live in their own
`business.md`.

| ID | Rule |
|---|---|
| `BR-GLOBAL-001` | Every monetary amount in the system is stored and computed as an integer number of cents. No floating point type may represent money in any layer |
| `BR-GLOBAL-002` | Every timestamp is persisted in UTC. Localisation happens in the client only |
| `BR-GLOBAL-003` | A member holds exactly one active plan at any moment, with a full subscription history |
| `BR-GLOBAL-004` | Authorization is enforced by the backend. Frontend guards exist only to improve the experience and are never a security boundary |
| `BR-GLOBAL-005` | An administrator may only act on libraries explicitly assigned to them. A super administrator has no such restriction |
| `BR-GLOBAL-006` | Balances are never mutated in place. Fines, payments, and reward points are recorded as entries in an immutable ledger |
| `BR-GLOBAL-007` | Operations that create financial or inventory effects accept an idempotency key and must never apply twice |
| `BR-GLOBAL-008` | Every administrative operation writes an audit entry recording user, action, entity, and timestamp |
| `BR-GLOBAL-009` | An account that has not verified its email address cannot sign in |
| `BR-GLOBAL-010` | The interface language is English |

---

## 4. Acceptance criteria

| ID | Criterion |
|---|---|
| `AC-GLOBAL-001` | No money-typed field anywhere in the codebase or database uses `decimal`, `float`, or `double`. Verified by a test |
| `AC-GLOBAL-002` | An administrator receives 403 on any operation targeting a library outside their assignment. Verified by a scope matrix test |
| `AC-GLOBAL-003` | A repeated request carrying the same idempotency key produces exactly one effect |
| `AC-GLOBAL-004` | `docker compose up` yields a usable system with seed data and the three demo accounts |
| `AC-GLOBAL-005` | The three demo accounts from the prototype sign in and land on their correct role surface |

---

## 5. Edge cases

| Scenario | Expected behaviour |
|---|---|
| A member downgrades while holding more reservations than the new plan reaches | Existing reservations are honoured until returned. New reservations are refused until the member is within the limit |
| A member downgrades from Max holding reward points | Points survive but may only be redeemed while the active plan is Max |
| A book's tier is raised above a member's plan while they hold it on loan | The active reservation is unaffected. Renewal and re-reservation are refused |
| A library is removed from an administrator's assignment mid-session | The next request is evaluated against the new scope. No cached authorization survives |
| Two members reserve the last copy simultaneously | Exactly one succeeds. The other receives an explained rejection. Stock never goes negative |
| A member's city changes | Plan reach recalculates from the new city. Active reservations are unaffected |

---

## 6. Out of scope

Explicitly **not** handled by this system in the current phase:

- Digital or audio book lending
- Inter-library transfer of copies
- Copy reservations placed on an out-of-stock title (holds queue)
- A real payment gateway — payment is simulated behind `IPaymentProvider`
- Two-factor authentication
- Single sign-on and external identity providers
- Native mobile applications
- Cloud infrastructure, Terraform, and CI/CD — these require their own plan
- Reporting, analytics dashboards, and data exports
- Multi-currency. The system is USD only

---

## 7. Product source of truth

The approved prototype in `docs/design/` defines product behaviour, UI, copy, and business rules. Where
this specification and the prototype disagree, **the prototype wins** and this specification must be
corrected.

`docs/design/prototype.source.js` contains the real rules, the exact interface copy, and the complete seed
data. `docs/design/README.md` explains where to find what.
