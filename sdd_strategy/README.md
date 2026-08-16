# Astrolabe Books — SDD+ Strategy

**Methodology:** SDD+ v1.0
**Project type:** .NET Clean Architecture + React SPA
**Last strategy review:** 2026-08-15

---

## What this project does

Astrolabe Books is a membership platform for a network of physical libraries. A member registers with an
email address, chooses a subscription plan, and — according to that plan — reserves books from the
libraries their plan reaches, buys books from an integrated store with plan-based discounts, and
accumulates redeemable reward points. Library administrators run the libraries assigned to them, and a
super administrator runs the whole network.

## Technology stack

- **Backend:** C#, .NET 10, ASP.NET Core Web API, Entity Framework Core, MediatR, PostgreSQL
- **Frontend:** React, TypeScript, Material UI, TanStack Query, Axios, Zustand
- **Testing:** NUnit 4.x, Moq, FluentAssertions, WireMock.Net (backend); Jest, React Testing Library (frontend)
- **Runtime:** Docker, Docker Compose, `postgres:16-alpine`
- **Email:** Mailgun HTTP API via RestSharp

Full rationale for every choice is in `specs/global_tech_spec.md`.

## Authority precedence

Three documents can conflict. This is the approved order — **higher wins**:

| # | Source | Authority over |
|---|---|---|
| 1 | **The prototype** (`docs/design/`) | Product behaviour, UI, copy, and business rules |
| 2 | **`SDD_PLIUS_STRATEGY.md`** | Process, repository structure, spec format, agent rules |
| 3 | **`GUIDELINES.md`** | Architecture and stack decisions where the two above are silent |

When a lower source is overridden, it must be corrected rather than left contradictory.

## Domain map

| Domain | Prefix | Responsibility | Ring |
|---|---|---|---|
| `identity` | `IDN` | Registration, verification, sign-in, tokens, sessions, devices, roles | MVP |
| `membership` | `MBR` | Plans, subscriptions, plan changes | MVP |
| `network` | `NET` | Countries, cities, libraries, administrator assignment and scope | MVP |
| `catalog` | `CAT` | Books, copies, genres, search, access policy, lifecycle, reviews | MVP |
| `reservations` | `RSV` | Reservations, delivery, returns, courier handover | MVP |
| `billing` | `BIL` | Fines, payment methods, charges, desk payments, receipts | MVP |
| `store` | `STR` | Purchases, discounts, reward points, wallet | MVP |
| `recommendations` | `REC` | Per-library AI configuration and recommendation generation | MVP |
| `support` | `SUP` | Tickets, conversation, service rating | Phase 2 |
| `notifications` | `NTF` | Notification centre and preferences | Phase 2 |

### Domains under growth watch

Three domains are projected to approach the SDD+ §6.2 split thresholds. No split happens without written
human approval.

| Domain | Projected trigger | Likely split |
|---|---|---|
| `catalog` | More than 20 business rules | `catalog` / `reviews` |
| `billing` | More than 15 commands and queries combined | `fines` / `payments` |
| `identity` | More than 8 aggregates | `identity` / `sessions` |

## Agent operating rules

Before taking any action, read:

1. This file
2. `specs/global_spec.md`
3. `specs/global_tech_spec.md`
4. The relevant domain spec files for this task
5. Apply the **Freshness Protocol** (SDD+ methodology Section 7)

The Freshness Protocol in one line: a spec reviewed more than **7 calendar days** ago is stale. Zero stale
specs means proceed. One to three stale means update them first. Four or more stale means stop, create a
`GLOBAL-*` task, and notify the user.

Agent rules files: `.cursorrules` for Cursor, `copilot-instructions.md` for GitHub Copilot, and
`CLAUDE.md` at the repository root for Claude.

## Key constraints

These extend or override the SDD+ defaults for this project.

- **The prototype is the product authority.** `docs/design/prototype.source.js` holds the real business
  rules, copy, and seed data. Do not invent product behaviour — read it there.
- **The prototype does not use Material UI.** It is a visual and behavioural reference, never reusable
  code. Rebuild every screen on the Material UI theme defined in `GUIDELINES.md` §38.1.
- **The presentation project is named `Astrolabe.Presentation`**, per SDD+ §9.1.
- **No pipeline behaviors.** Validation runs inside handlers. There is no `LoggingBehavior` class.
- **Money is always an integer number of cents.** Never a floating point type, anywhere.
- **Coverage minimums:** Domain 90%, Application 80%, Infrastructure 70%, Presentation 70%, frontend 80%.
- **The interface language is English.** Repository content is written in English.
- **No infrastructure work in this phase.** Azure, Terraform, and CI/CD require their own plan.

## Current plan

`specs/plans/PLAN-001_astrolabe-books-mvp.md` — **Draft, awaiting approval.**
