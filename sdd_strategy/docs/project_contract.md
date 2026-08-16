# Astrolabe Books — Project Contract

**SDD+ version:** 1.0
**Contract version:** 0 — draft, no implementation exists yet
**Last updated:** 2026-08-15
**Project type:** .NET Clean Architecture REST API with a React single-page frontend

> **DRAFT.** No source code exists. Every section below states its intent so that a consuming project
> knows what to expect, and is finalised in PLAN-001 Stage 8 once the API is real. Anything marked
> *to be authored* must not be relied upon.

---

## Identity

**What this project does:** Astrolabe Books runs a network of physical libraries as a subscription
service. Members reserve and buy physical books across the libraries their plan reaches; library staff
manage catalogue, loans, payments, and support for the branches assigned to them.

**Primary consumers:** the Astrolabe Books web frontend (`src/frontend/astrolabe-web`). No external
consumer exists in this phase.

**Primary dependencies:**

| Dependency | Purpose | Phase |
|---|---|---|
| PostgreSQL 16 | Primary persistence | MVP |
| Mailgun | Verification and password-reset mail, over the Mailgun HTTP API | MVP |
| Anthropic API | Recommendation generation, credentials owned per library | MVP Stage 7 |
| OpenAI API | Recommendation generation, credentials owned per library | MVP Stage 7 |

---

## Exposed Interfaces

A REST API over HTTPS, documented with OpenAPI and served through Swagger in development.

**Base URL:** `http://localhost:5080` locally. No deployed environment exists in this phase.

Endpoint groups, one per domain:

| Group | Domain | Purpose |
|---|---|---|
| `/api/v1/auth` | `identity` | Registration, verification, sign-in, refresh, sign-out, password recovery |
| `/api/v1/sessions` | `identity` | Session and device listing and revocation |
| `/api/v1/plans` | `membership` | Plan catalogue and plan changes |
| `/api/v1/libraries` | `network` | Countries, cities, libraries, administrator assignment |
| `/api/v1/books` | `catalog` | Search, detail, lifecycle management, reviews |
| `/api/v1/reservations` | `reservations` | Reserve, deliver, return, check in |
| `/api/v1/fines`, `/api/v1/payments` | `billing` | Fines, payment methods, charges, desk payments |
| `/api/v1/orders` | `store` | Purchases, discounts, reward points |
| `/api/v1/recommendations` | `recommendations` | Recommendations and per-library AI configuration |
| `/api/v1/tickets` | `support` | Support tickets — Phase 2 |
| `/api/v1/notifications` | `notifications` | Notification centre — Phase 2 |
| `/health/live`, `/health/ready` | — | Liveness and readiness probes |

Exact routes, verbs, and payloads: *to be authored in Stage 8, generated from OpenAPI.*

---

## Authentication

**Type:** Bearer token, JWT.

- **Access token** — signed JWT, 15 minute lifetime, sent as `Authorization: Bearer {token}`. Carries a
  `sid` session claim that the API validates against revoked sessions on every request.
- **Refresh token** — opaque, 30 day lifetime, delivered in an `HttpOnly; Secure; SameSite=Strict` cookie
  scoped to the refresh endpoint. Rotated on every use. Presenting an already-rotated token revokes the
  entire session chain.

**Obtaining credentials:** register through `/api/v1/auth`, then verify the emailed link. An unverified
account cannot sign in.

Clients must hold the access token **in memory**, never in `localStorage`, and must serialise refresh
calls so only one runs at a time.

---

## Data Contracts

Conventions binding on every payload:

| Convention | Rule |
|---|---|
| Money | Always an **integer number of cents**. `1299` means $12.99. Never a decimal or float |
| Timestamps | ISO 8601 in **UTC**. Localisation is the client's responsibility |
| Identifiers | GUID |
| Currency | USD only |
| Errors | `application/problem+json` per RFC 7807, carrying a correlation identifier. Never a stack trace |
| Paging | Every list endpoint is paginated. Unbounded result sets are not returned |

Key schemas: *to be authored in Stage 8. Link to the generated OpenAPI document.*

---

## Integration Guide

1. Read `sdd_strategy/README.md` for the domain map and authority precedence.
2. Read `sdd_strategy/specs/global_spec.md` for the authoritative glossary. Note in particular that
   **tier** is a property of a book and **plan** is a property of a member, and that **agent** means
   different things in `support` and in `recommendations`.
3. Bring the system up with `docker compose up`. Seed data and three demo accounts are created
   automatically.
4. Register or sign in through `/api/v1/auth` to obtain a token pair.
5. Call endpoints with the bearer access token. Refresh before expiry, one call at a time.

---

## Versioning and Stability

**Stable** — the URL version prefix, authentication scheme, error format, money-as-cents convention, and
UTC timestamps. Breaking changes to these are announced in advance.

**Internal, may change without notice** — everything, until Contract version 1 is published at the end of
PLAN-001 Stage 8. This project has no external consumers yet.

---

## Known Limitations

- No deployed environment. The system runs locally under Docker Compose only.
- Payment is **simulated**. There is no real settlement, refund, or chargeback.
- USD only. No multi-currency support.
- Search is relational and `LIKE`-based. No full-text or semantic search.
- The revoked-session cache is in-process, so the API is effectively single-instance in this phase.
- No rate limits are published yet beyond authentication endpoints.
- `support` and `notifications` are Phase 2 and not implemented in the MVP.

---

## Contact

**Responsible:** Francisco Torregrosa
**Source of truth:** this repository. Product behaviour is defined by the prototype in `docs/design/`.
