# PLAN-001 — Astrolabe Books MVP

**Created:** 2026-08-15
**Status:** In Progress
**Approved by:** Francisco Torregrosa on 2026-08-15
**Recommended model:** Claude Opus 5 for spec authoring and domain modelling; Claude Sonnet 5 for
mechanical task execution once specs are frozen.
**SDD+ version:** 1.0

---

## Context

Astrolabe Books is a membership platform for a network of physical libraries. The product is defined by
an approved UI prototype (`docs/design/`) and an architectural document (`GUIDELINES.md`).

This plan is required under SDD+ §13.1: the change affects **ten domains**, requires infrastructure
(PostgreSQL, SMTP capture, container composition), and constitutes the initial build of the entire system.

Scope of this plan: **application only** — backend, frontend, containerized database, and Docker
composition. Cloud infrastructure, Terraform, and CI/CD are explicitly excluded and will require their
own plan.

---

## Current state

The repository contains documentation and a prototype. **No source code exists.**

| Artifact | State |
|---|---|
| `SDD_PLIUS_STRATEGY.md` | Complete — SDD+ methodology v1.0 |
| `GUIDELINES.md` | Complete — architectural source of truth, corrected against the prototype |
| `Astrolabe Books.html` | Complete — approved UI prototype |
| `docs/design/` | Decoded prototype source, styles, and text outline |
| `ssd_strategy/` | **Does not exist** — created by TASK 1 of this plan |
| `src/`, `tests/`, `docker/` | **Do not exist** |

### Authority precedence

Three documents can conflict. The proposed order of precedence, **pending approval**:

1. **The prototype** — product behaviour, UI, copy, and business rules. Confirmed by the product owner.
2. **`SDD_PLIUS_STRATEGY.md`** — process, repository structure, spec format, and agent rules.
   The document states: *"When in doubt, this document wins."*
3. **`GUIDELINES.md`** — project-specific architecture and stack decisions, where the two above are silent.

---

## Proposed change

Build the MVP through **nine stages**, each a complete SDD+ cycle: specs written and reviewed before any
implementation, tasks tracked in the relevant `tasks.md`, and the Freshness Protocol applied before every
task execution.

### Domain map

Domain names are drawn from business language, per SDD+ §6.1.

| Domain | Task prefix | Responsibility | Ring |
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

### Growth threshold pre-assessment

Per SDD+ §6.2, three domains are projected to approach split thresholds during spec authoring. This is
recorded now so the trigger is not a surprise later:

| Domain | Projected risk | Likely split |
|---|---|---|
| `catalog` | **> 20 business rules.** Access policy, search, lifecycle, and reviews are four distinct rule clusters | `catalog` / `reviews` |
| `billing` | **> 15 commands + queries.** Fines, card payments, and desk payments barely share entities | `fines` / `payments` |
| `identity` | **> 8 aggregates.** Identity plus session management plus device registry | `identity` / `sessions` |

No split is performed pre-emptively. If a threshold is breached while writing specs, the agent creates the
evaluation task in `global_task_spec.md` and waits for explicit human approval, per SDD+ §6.2 and Rule 7.

---

## Impact analysis

**Domains affected:** all ten. This plan creates every one of them.

**External systems affected:** none in this plan. The MVP runs entirely in local containers.

**Deferred to a future plan:** Azure Container Apps, Azure Database for PostgreSQL, Key Vault, Container
Registry, Terraform, Azure Pipelines, Application Insights, and a real payment gateway.

**Interfaces reserved for later:** `IPaymentProvider` (simulated in the MVP) and `IAiProvider`
(Anthropic and OpenAI, plus a most-borrowed fallback).

---

## Task breakdown

Each stage below produces the SDD+ artifacts listed, then implements against them. **No stage begins
implementation until its three spec files exist, are reviewed, and carry today's `Last reviewed` date.**

Task IDs follow the SDD+ `{DOM}-{NNN}` convention and are tracked in each domain's `tasks.md`.
Cross-domain and infrastructure tasks use `GLOBAL-{NNN}` in `global_task_spec.md`.

---

### Stage 0 — SDD+ bootstrap and scaffolding

**Objective.** Establish the SDD+ structure and a runnable skeleton, before any business rule is written.

**SDD+ artifacts produced**

- `ssd_strategy/README.md` — agent entry point, per SDD+ §4.3.
- `ssd_strategy/.cursorrules` — all nine standard rules, per SDD+ §10.3.
- `ssd_strategy/copilot-instructions.md` and a copy at `.github/copilot-instructions.md`.
- `CLAUDE.md` at the repository root, carrying the Claude system prompt of SDD+ §10.5 plus the
  project-specific rules of `GUIDELINES.md` §74.
- `ssd_strategy/specs/global_spec.md` — project-wide business context and the authoritative glossary.
- `ssd_strategy/specs/global_tech_spec.md` — stack decisions and their rationale, including the
  **frontend stack**, which SDD+ §9 does not yet cover.
- `ssd_strategy/specs/global_task_spec.md` — the `GLOBAL-*` backlog.
- `ssd_strategy/specs/plans/` containing this file.
- `ssd_strategy/docs/project_contract.md` — per SDD+ §8.2.
- Ten domain folders, each with three placeholder spec files carrying required sections and
  `Last reviewed` set.

**Implementation — backend**

- .NET 10 solution with four source projects and four test projects.
- **Dependency rules strictly enforced** per SDD+ §9.1: Domain has zero external NuGet dependencies;
  Application references Domain only; Infrastructure references Application and Domain; Presentation
  references Application and Infrastructure.
- Cross-cutting core: `Result` and `Result<T>`, a typed `Error` hierarchy, global exception middleware
  with correlation identifiers, the Options Pattern, structured Serilog logging, and health endpoints.
- MediatR wired with `ISender` injection. **No pipeline behaviors and no `LoggingBehavior` class** —
  validation runs inside handlers and logging uses the built-in tooling, per SDD+ Rule 4 and §10.3.
- `AstrolabeDbContext` with Fluent API configurations in
  `Infrastructure/Persistence/Configurations/`, auto-discovered via `ApplyConfigurationsFromAssembly`.
  **No data annotations on domain entities.**
- `IUnitOfWork` and an empty initial migration.
- OpenAPI and Swagger in development.

**Implementation — frontend**

- Vite, React, and TypeScript in strict mode.
- A Material UI theme derived from the prototype tokens in `GUIDELINES.md` §38.1, with light and dark
  modes and the three required fonts.
- Feature-based structure per `GUIDELINES.md` §30, with `AuthLayout` and `AppLayout`
  (navbar, sidebar, footer, quick-action FAB).
- Axios client with base interceptors, TanStack Query for server state, Zustand for UI state.
- Shared components: paginated table, loading, empty and error states, confirmation dialog, snackbar, and
  forms built with React Hook Form and Zod.

**Implementation — Docker**

- Multi-stage `docker/api/Dockerfile` and `docker/web/Dockerfile`.
- `docker-compose.yml` with three services — web, api, `postgres:16-alpine` — plus
  `.env.example`, a persistent volume, and chained health checks.
- Migrations applied through a controlled startup step. **Never `EnsureCreated`**, per `GUIDELINES.md` §16.

**Testing.** A smoke test per project and a container test verifying the API responds and applies
migrations. Coverage tooling configured on both sides. Test naming follows
`{Method}_{Condition}_{ExpectedResult}` per SDD+ §9.1.

**Acceptance.** `docker compose up` brings up four services; the web shell loads; `/health/ready` is
green; Swagger is reachable; the post-bootstrap checklist of SDD+ §15.1 passes in full.

**Estimate.** 6 sessions — 4 for scaffolding, 2 for the SDD+ structure. **No dependencies.**

---

### Stage 1 — `identity` and `network`

**Objective.** Registration, email verification, sign-in, role-based authorization with library scope, and
multi-device session management.

**Spec artifacts.** `identity.business.md`, `identity.technical.md`, `identity.tasks.md`,
`network.business.md`, `network.technical.md`, `network.tasks.md`.

**Key business rules to specify** (indicative IDs)

| ID | Rule |
|---|---|
| `BR-IDN-001` | An account is created in `pending verification` and **cannot sign in** until verified |
| `BR-IDN-002` | Email verification tokens are single-use, hashed at rest, and valid for 24 hours |
| `BR-IDN-003` | Access tokens expire after 15 minutes and carry a session identifier claim |
| `BR-IDN-004` | Refresh tokens are opaque, valid 30 days, stored as SHA-256 hash only, one per session |
| `BR-IDN-005` | Every refresh rotates the token within the same session |
| `BR-IDN-006` | Presenting an already-rotated refresh token revokes the **entire session chain** |
| `BR-IDN-007` | A revoked session is rejected on the next request, without waiting for token expiry |
| `BR-IDN-008` | A member may revoke one, several, all other, or all sessions |
| `BR-IDN-009` | Changing or resetting a password revokes every session except the current one |
| `BR-IDN-010` | An account locks after 5 failed sign-in attempts within 15 minutes |
| `BR-NET-001` | An administrator may only operate on libraries explicitly assigned by a super administrator |
| `BR-NET-002` | Only a super administrator creates, assigns, and revokes administrators |

**Application layer.** `Register`, `VerifyEmail`, `ResendVerification`, `SignIn`, `RefreshToken`,
`SignOut`, `ForgotPassword`, `ResetPassword`, `ChangePassword`, `GetMySessions`, `RevokeSession`,
`RevokeSessions`, `RevokeOtherSessions`, `RevokeAllSessions`, plus the `network` commands and queries.

> **Threshold watch:** this is already 15+ commands and queries across two domains. If `identity` alone
> breaches 15, an evaluation task goes into `global_task_spec.md`.

**Frontend.** `login`, `signup` with the three-column plan selector and cascading country and city,
`verify`, `AuthProvider`, `useAuth`, `ProtectedRoute`, `RoleGuard`, an Axios interceptor with a request
queue so only one refresh runs at a time, and role-driven sidebar composition.

**New screen not present in the prototype.** *Settings → Devices and sessions*: device, location, last
access, a "this device" marker, and revocation of one, several, all others, and all. Requires design
review before acceptance.

**Testing.** Refresh token reuse detection is the critical case. Full registration → verification →
sign-in → refresh → sign-out integration cycle. Authorization matrix by role and by library scope.

**Acceptance.** The three demo accounts work. Revoking a session invalidates its access on the next
request. An unverified account cannot sign in.

**Estimate.** 9 sessions. **Depends on:** Stage 0.

---

### Stage 2 — `membership` and `catalog`

**Objective.** Plans, and the catalogue access rule that is the heart of the product.

**Spec artifacts.** Three files each for `membership` and `catalog`.

**Key business rules to specify**

| ID | Rule |
|---|---|
| `BR-MBR-001` | A member holds exactly one active plan, with a recorded subscription history |
| `BR-MBR-002` | A member's home library is assigned automatically from their city of residence |
| `BR-MBR-003` | ~~A plan change takes effect immediately~~ — **superseded**: an upgrade applies immediately and is prorated; a downgrade is scheduled to the renewal date and charges nothing. Corrected against the prototype's `planModal` on 2026-08-15. Neither invalidates reservations in progress |
| `BR-CAT-001` | Every book carries its own plan tier, independent of any member's plan |
| `BR-CAT-002` | A Basic member may only reserve `Basic` tier titles, and only at their home library |
| `BR-CAT-003` | A Plus member may reserve any title at any library in their city of residence |
| `BR-CAT-004` | A Max member may reserve any title at any library in the network |
| `BR-CAT-005` | A copy is reservable only when stock is greater than zero |
| `BR-CAT-006` | A rejected reservation must state its reason using the prototype's exact wording |
| `BR-CAT-007` | Book lifecycle transitions are `draft → catalog → repair → catalog` and `→ deleted → catalog` |
| `BR-CAT-008` | Every lifecycle transition writes an audit entry |

The access rule is implemented as a **pure domain service** with no infrastructure dependency, mirroring
the prototype's `copyState` and `bookAccess` functions exactly.

**Testing.** The **exhaustive access matrix** — 3 plans × 3 tiers × in/out of city × in/out of home
library × with/without stock — is the highest-value test suite in the project. Domain layer coverage
minimum is **90%** per SDD+ §9.1.

**Frontend.** `catalog` with card and table views, genre filters, search, pagination, tint-generated
covers, plan-lock badges carrying their reason, and the detail side panel.

**Acceptance.** A Basic member sees the entire catalogue but can only reserve Basic titles at their home
library, and the interface states precisely why in every other case.

**Estimate.** 7 sessions. **Depends on:** Stage 1.

---

### Stage 3 — `reservations`

**Objective.** The complete loan cycle, with correct concurrency.

**Key business rules to specify**

| ID | Rule |
|---|---|
| `BR-RSV-001` | The loan term is 14 days from confirmation |
| `BR-RSV-002` | A reservation targets one specific copy at one specific library |
| `BR-RSV-003` | Home delivery adds a $3.99 charge; collection is free |
| `BR-RSV-004` | A return becomes `Returned` only when library staff check the copy in |
| `BR-RSV-005` | A courier return requires a matching handover code |
| `BR-RSV-006` | The available copy count must never go negative |
| `BR-RSV-007` | A member may not hold two active reservations of the same physical copy |

**Concurrency.** Optimistic concurrency with a version token on the copy. A race test firing simultaneous
requests at the last copy is **mandatory**. Reservation creation accepts an idempotency key.

**Frontend.** `loans`, the reservation confirmation modal with copy selection and delivery breakdown, the
courier modal, and the `home` dashboard with stat cards, active reservations, and preferred topics.

**Acceptance.** Two members competing for the last copy produce one reservation and one explained
rejection, never a negative stock count.

**Estimate.** 7 sessions. **Depends on:** Stage 2.

---

### Stage 4 — `billing`

**Objective.** Fine accrual, card payment, and desk payment by code.

**Key business rules to specify**

| ID | Rule |
|---|---|
| `BR-BIL-001` | A late return accrues $0.35 per day per title |
| `BR-BIL-002` | A fine is capped at $9.00 per title |
| `BR-BIL-003` | A fine stops accruing when the copy is checked in |
| `BR-BIL-004` | A desk payment code is valid for 72 hours |
| `BR-BIL-005` | Only an administrator of the owning library may validate or reject a desk payment |
| `BR-BIL-006` | Stored payment methods retain only brand, last four digits, expiry, and cardholder |
| `BR-BIL-007` | Every monetary amount is stored as an integer number of cents |
| `BR-BIL-008` | Payment operations are idempotent — a repeated request never charges twice |

**Immutable ledger.** No balance is ever mutated; every movement is an entry. A daily accrual job runs
with concurrent execution disabled, its schedule and retry count supplied through `IOptions<T>`, and it
dispatches commands via `ISender` — per SDD+ §9.1 background job rules.

**Acceptance.** A book 20 days overdue produces exactly $7.00; at 26 days it is capped at $9.00.

**Estimate.** 7 sessions. **Depends on:** Stage 3.

---

### Stage 5 — `store`

**Objective.** Book purchases with plan discounts and a reward-point wallet.

**Key business rules to specify**

| ID | Rule |
|---|---|
| `BR-STR-001` | Basic receives no purchase discount |
| `BR-STR-002` | Plus receives 10% on books held by libraries in their city of residence |
| `BR-STR-003` | Max receives 15% on books held by any library |
| `BR-STR-004` | The discount is applied per order line, never to the order total |
| `BR-STR-005` | Only Max members accrue reward points |
| `BR-STR-006` | Accrual is one point-cent per $1.50 of post-discount total, truncated downward |
| `BR-STR-007` | Redemption may cover at most 50% of an order total — **OPEN, pending approval** |
| `BR-STR-008` | Points already earned survive a downgrade but may only be redeemed on the Max plan |

**Acceptance.** A Max member spending $150 on books from another city receives a 15% discount and accrues
$1.00 of redeemable balance.

**Estimate.** 6 sessions. **Depends on:** Stage 4.

---

### Stage 6 — Administration surfaces

**Objective.** Let an administrator run their libraries and the super administrator run the network.
This stage adds commands and screens to `catalog`, `identity`, and `network` rather than creating a new
domain.

**Scope.** Book management with the three-step wizard, draft and publish, repair and removal with typed
reasons and audit notes; the user directory with block, restore, delete, and resend verification; and
administrator invitation with library assignment and power granting.

**Testing.** The **scope matrix** — an administrator of Midtown and Harlem sees and touches nothing in
Chicago — and audit completeness on every administrative operation.

> **Threshold watch:** adding these commands is the most likely trigger for the `catalog` split. Expect
> a `GLOBAL-*` evaluation task here.

**Acceptance.** The demo administrator `admin@astrolabe.co` operates Midtown and Harlem and receives 403
on any operation against Chicago or Austin.

**Estimate.** 8 sessions. **Depends on:** Stages 1 and 2.

---

### Stage 7 — `recommendations`

**Objective.** Model-generated recommendations, with credentials owned per library.

**Key business rules to specify**

| ID | Rule |
|---|---|
| `BR-REC-001` | Each library supplies its own provider credentials, managed by its staff |
| `BR-REC-002` | Basic members never receive recommendations |
| `BR-REC-003` | A member of an unconnected library receives the most-borrowed fallback |
| `BR-REC-004` | API keys are encrypted at rest and never returned by any API response |
| `BR-REC-005` | Only aggregated, anonymised reading data is sent to a provider |
| `BR-REC-006` | Recommendations are cached and regenerated on demand, never per render |
| `BR-REC-007` | On provider failure the last cached result or the fallback is shown, never an error |

**Testing.** A test asserting that **no API response can expose a stored key** is mandatory. External
provider calls are mocked with WireMock.Net per SDD+ §9.1 — **no real HTTP calls in unit tests**.

**Acceptance.** A Plus member of a connected library receives model-generated recommendations with stated
reasons; the same member at an unconnected library sees the fallback, never an error.

**Estimate.** 6 sessions. **Depends on:** Stages 2 and 3 — it needs real reading history.

---

### Stage 8 — Hardening and acceptance

**Objective.** Close the MVP against the applicable success criteria of `GUIDELINES.md` §79.

- Coverage verified per layer: **Domain 90%, Application 80%, Infrastructure 70%, Presentation 70%**
  (SDD+ §9.1). Frontend threshold pending resolution of conflict G2 below.
- Every `BR-*` rule has at least one unit test, per SDD+ §9.1.
- Accessibility, responsive, performance, and security passes per `GUIDELINES.md` §68–§70.
- Complete seed data: 6 countries, 5 libraries, 12 books with per-branch stock, 14 users, three demo
  accounts.
- `ssd_strategy/docs/project_contract.md` finalised.
- Full Freshness Protocol sweep: every spec file reviewed and dated.
- A scripted acceptance walkthrough per role.

**Estimate.** 5 sessions. **Depends on:** all preceding stages.

---

### Stage 9 — `support` and `notifications` *(Phase 2)*

Tickets with conversation, agent assignment, states, reopening, and service rating; the notification
centre with families, unread counts, and per-family muting; and book reviews aggregated into the
catalogue rating.

**Estimate.** 7 sessions. **Depends on:** Stages 3 and 4.

---

## Summary

| Stage | Domains | Ring | Sessions |
|---|---|---|---|
| 0 | bootstrap | MVP | 6 |
| 1 | `identity`, `network` | MVP | 9 |
| 2 | `membership`, `catalog` | MVP | 7 |
| 3 | `reservations` | MVP | 7 |
| 4 | `billing` | MVP | 7 |
| 5 | `store` | MVP | 6 |
| 6 | administration surfaces | MVP | 8 |
| 7 | `recommendations` | MVP | 6 |
| 8 | hardening and acceptance | MVP | 5 |
| 9 | `support`, `notifications` | Phase 2 | 7 |

**MVP: 61 sessions**, roughly 30–31 focused working days. **Full prototype scope: 68 sessions.**

Critical path: `0 → 1 → 2 → 3 → 4 → 5 → 8`. Stage 6 may overlap with Stages 3 and 4 once Stage 2 closes.
Stage 7 may overlap with Stages 5 and 6 once Stage 3 has produced reading history.

The estimate rose from 57 to 61 sessions against the previous draft, because SDD+ requires three spec
files per domain authored and reviewed before implementation, and adds the `ssd_strategy/` bootstrap.

---

## Conflicts requiring resolution

### Product conflicts — resolved

Arbitration rule confirmed by the product owner: **the prototype has the final word.**
`GUIDELINES.md` sections 1, 2, 3, 4, 6, 12, 30, 38, and 71 were corrected accordingly.

| # | Conflict | Resolution |
|---|---|---|
| C1 | Role model | Member / Admin / Super Admin. The *Librarian* role is removed. Originally recorded as Basic / Plus / Max / Admin / Super Admin; `GLOBAL-019` separated the three plan tiers out of the role on 2026-08-16, so the tiers are now plans held on a subscription and not roles |
| C2 | Functional scope | The prototype's full scope is adopted |
| C3 | Max plan discount | 15%, not 20% |
| C4 | Multi-device sessions | Included. New screen under Settings |
| C5 | Basic home library | Assigned automatically from the city of residence |
| C7 | Payment gateway | Simulated behind `IPaymentProvider` |
| C8 | UI framework | Rebuilt on MUI v6; the prototype is a visual reference, not reusable code |
| C9 | Interface language | English, matching the prototype |

**C6 — reward point redemption cap.** The prototype shows a balance but never implements redemption, so
arbitration does not apply. Proposal: cap at 50% of the order total. **OPEN.**

### Methodology conflicts — new, requiring a decision

Reading `SDD_PLIUS_STRATEGY.md` surfaced six conflicts with `GUIDELINES.md`. Per SDD+ Rule 6, these are
raised rather than assumed.

| # | Conflict | `GUIDELINES.md` | `SDD_PLIUS_STRATEGY.md` | Recommendation |
|---|---|---|---|---|
| G1 | **ADR location** | Standalone files in `docs/adr/NNN-*.md` (§73) | `technical.md` §4 is the living ADR collection (§5.2) | Follow SDD+. One decision log per domain, plus global decisions in `global_tech_spec.md`. Correct `GUIDELINES.md` §73 |
| G2 | **Coverage thresholds** | Flat 80% backend, 80% frontend (§56) | Per layer: Domain 90%, Application 80%, Infrastructure 70%, Presentation 70% (§9.1). Frontend not covered | Follow SDD+ for backend; keep 80% for frontend from `GUIDELINES.md`. Correct §56 |
| G3 | **Pipeline behaviors** | Not addressed | **Forbidden.** Validate inside handlers; no `LoggingBehavior` class (§9.1, Rule 4) | Follow SDD+. This reverses the previous plan draft, which specified FluentValidation and logging behaviors |
| G4 | **Presentation project name** | `LibraryManagement.Api` (§10) | `{ProjectName}.Presentation` (§9.1) | **Needs your decision.** SDD+ claims final authority, but `.Api` is the more common .NET convention |
| G5 | **EF integration testing** | Integration tests permitted (§59) | `Microsoft.EntityFrameworkCore.InMemory`, unique database name per test (§9.1) | Follow SDD+ for unit-level EF tests. Real-PostgreSQL container tests remain valuable for migrations — propose adding them as an explicit exception |
| G6 | **Frontend stack governance** | Fully specified: React, TypeScript, MUI, TanStack Query, Axios, Zustand, Jest, RTL (§7, §30–§39) | Only the .NET stack is documented. §9 states additional stacks are added *"as they are formally adopted"* | Document the frontend stack in `global_tech_spec.md` for this project, and separately propose a §9.2 addition to the methodology document |

### Documentation inconsistencies in `SDD_PLIUS_STRATEGY.md`

Minor, but they will confuse an agent following the document literally:

- The directory is named `ssd_strategy/` throughout (§4.1, §12.3, §15) while the methodology is **SDD+**.
  The transposed letters look like a typo that has been carried through. This plan uses `ssd_strategy/`
  as written, so the document and the repository agree — but confirm whether you intended `sdd_strategy/`.
- §12.2 names the methodology file `SDD_PLUS_METHODOLOGY.md`; this repository's copy is
  `SDD_PLIUS_STRATEGY.md`. Two different names for the same document.

---

## Risks

| Risk | Impact | Mitigation |
|---|---|---|
| The prototype does not use MUI and must be rebuilt | High — the largest frontend cost | Spend Stage 0 on a faithful MUI theme and shared components |
| The plan access rule permeates the whole product | High — an error breaks catalogue, reservations, and store | Pure domain service in Stage 2 with an exhaustive test matrix |
| Monetary arithmetic | High — cent-level errors in fines, discounts, and points | Integer cents throughout, recorded in `global_tech_spec.md` |
| Three domains projected to breach split thresholds | Medium — an unplanned split mid-stage stalls delivery | Pre-assessed above; thresholds monitored per Rule 7, split only on written approval |
| Concurrency on the last copy | Medium | Optimistic concurrency and a race test from Stage 3 |
| Third-party AI credentials | Medium | Encrypted at rest, never in API responses, explicit non-leakage test |
| Freshness Protocol overhead across 30 spec files | Medium | Stages are sequential, so only 3–6 files are in scope per task; a full sweep is scheduled in Stage 8 |

---

## Rollback plan

No stage is destructive; the MVP is greenfield. Rollback is per stage:

- **Stage 0** — remove `ssd_strategy/`, `src/`, `tests/`, `docker/`. Documentation is unaffected.
- **Stages 1–7** — each stage is a set of short-lived branches merged to trunk per `GUIDELINES.md` §63.
  Rollback is a revert of the stage's merges plus a down-migration for that stage's schema changes.
- **Schema** — every migration must have a verified down-migration before merge. Destructive migrations
  require an explicit note in the domain's `technical.md` under Known Constraints.
- **Spec files** — reverted with the code, since they live in the same repository and commit.

---

## Progress

| Task | Spec file | Status |
|---|---|---|
| GLOBAL-001 — Approve authority precedence | `global_task_spec.md` | ✅ Done |
| GLOBAL-002 — Resolve methodology conflicts G1–G6 | `global_task_spec.md` | ✅ Done |
| GLOBAL-003 — Resolve C6 point redemption cap | `global_task_spec.md` | ⬜ Not started |
| GLOBAL-004 — Bootstrap `sdd_strategy/` structure | `global_task_spec.md` | ✅ Done |
| GLOBAL-005 — Scaffold solution, frontend, and Docker composition | `global_task_spec.md` | ✅ Done — Stage 0 complete |
| Stage 1 — `identity` and `network` | `identity.tasks.md`, `network.tasks.md` | ✅ Done — 42/42 and 23/25 |
| Stage 2 — `membership` and `catalog` | `membership.tasks.md`, `catalog.tasks.md` | ✅ Done — 18/18 and 22/22, closed 2026-08-16 |
| Stage 3 — `reservations` | `reservations.tasks.md` | ✅ Done — 20/20, closed 2026-08-16 |
| Stage 4 — `billing` | `billing.tasks.md` | ✅ Done — 22/22, closed 2026-08-16 |
| Stage 5 — `store` | `store.tasks.md` | ✅ Done — 16/16, closed 2026-08-16. Redemption blocked on `BLOCK-002` |
| Stages 6–9 | Per-domain `tasks.md` | ⬜ Not started |

---

## Approval

This plan requires explicit written approval before execution, per SDD+ §13.2 Step 3. Implicit approval
is not accepted.

**Blocking questions**

1. Confirm the **authority precedence** proposed under Current state.
2. Resolve **G4** — `Astrolabe.Api` or `Astrolabe.Presentation`.
3. Resolve **C6** — reward point redemption cap.
4. Confirm the recommendation on **G1, G2, G3, G5, G6**, or override.
5. Confirm the `ssd_strategy/` folder spelling.

**Not blocking, needed later:** Anthropic and OpenAI API keys at Stage 7, supplied by environment
variable and never committed.
