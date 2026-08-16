# Astrolabe Books — Global Tasks

**Last reviewed:** 2026-08-15
**Overall progress:** 12/18 (67%)

Cross-domain tasks, infrastructure tasks, domain split evaluations, and plan execution tasks. Tasks
belonging to a single domain live in that domain's `tasks.md`.

---

## Blocking Dependencies

| Block ID | Description | Status |
|---|---|---|
| `BLOCK-001` | `PLAN-001` is in Draft. No implementation may begin until it is explicitly approved in writing, per SDD+ §13.2 step 3 | **Cleared 2026-08-15** — approved in writing |
| `BLOCK-002` | `BR-STR-007` — the reward point redemption cap is undefined. The prototype shows a balance but never implements redemption, so arbitration does not apply. **Scope narrowed 2026-08-16: it blocks redemption only.** Earning is specified and built, so no value is lost while the question is open — the balance simply accumulates | Open |
| `BLOCK-003` | Anthropic and OpenAI API keys are not available. Blocks `recommendations` implementation, not its specs | Open |
| `BLOCK-006` | The Mailgun **sandbox domain only delivers to recipients authorised in the Mailgun dashboard**. Registration cannot be tested with arbitrary addresses until a verified domain is configured, or the test addresses are authorised | Open |
| `BLOCK-004` | The *Devices and sessions* screen does not exist in the prototype and needs a design review before acceptance. Blocks acceptance of `IDN` session tasks, not their implementation | Resolved 2026-08-16 (`GLOBAL-011`) |
| `BLOCK-005` | MediatR 14.x is RPL-1.5 or commercial, not Apache-2.0 | **Cleared 2026-08-15** — pinned to 12.5.0, verified Apache-2.0 |

---

## Task List

| ID | Task | Status | Blocker | Tracking | Notes |
|---|---|---|---|---|---|
| `GLOBAL-001` | Approve authority precedence: prototype → SDD+ → GUIDELINES | ✅ | — | — | Approved 2026-08-15. Recorded in `README.md` and `global_tech_spec.md` §4 |
| `GLOBAL-002` | Resolve methodology conflicts G1 to G6 between SDD+ and GUIDELINES | ✅ | — | — | Resolved 2026-08-15. All six recorded in `global_tech_spec.md` §4 |
| `GLOBAL-003` | Bootstrap the `sdd_strategy/` structure | ✅ | — | — | Completed 2026-08-15. SDD+ §15.1 checklist passes in full: 33 spec files carry today's `Last reviewed`, 10 domains × 3 files, no source code created |
| `GLOBAL-004` | Correct `GUIDELINES.md` §73 — ADRs move into domain `technical.md` | ✅ | — | — | Done 2026-08-15. §73 now points at `global_tech_spec.md` §4 and domain `technical.md` §4, and requires rejected alternatives plus a superseded-decisions move |
| `GLOBAL-005` | Correct `GUIDELINES.md` §56 — per-layer coverage thresholds | ✅ | — | — | Done 2026-08-15. Domain 90, Application 80, Infrastructure 70, Presentation 70, frontend 80. Adds the every-`BR-*`-has-a-test rule |
| `GLOBAL-006` | Correct `GUIDELINES.md` §10 — presentation project renamed `Astrolabe.Presentation` | ✅ | — | — | Done 2026-08-15. All `LibraryManagement.*` names replaced with `Astrolabe.*`. §8 also updated to show `sdd_strategy/` |
| `GLOBAL-007` | Propose a frontend stack section for `SDD_PLIUS_STRATEGY.md` §9.2 | ⬜ | — | — | SDD+ §9 documents .NET only. Recorded meanwhile in `global_tech_spec.md` §2 |
| `GLOBAL-008` | Approve container-backed integration tests as an exception to EF InMemory | ⬜ | — | — | Migrations and PostgreSQL behaviour cannot be validated on InMemory. See `global_tech_spec.md` §5 |
| `GLOBAL-009` | Define `BR-STR-007` — reward point redemption cap | ✅ | `BLOCK-002` | `RewardRedemptionPolicy`, `STR-017` | **Resolved 2026-08-16.** Points cover up to 50% of the book total after the plan discount, excluding delivery; floor of 100 point-cents; one point-cent to the cent. Built as a tender rather than a discount, so the order total is unchanged and the ledger carries the full charge against two payments. The part paid in points earns nothing. `BR-STR-008` deliberately untouched — spending still needs an active Max plan, and rewriting that under cover of a cap question would have been a product decision smuggled in as a technical one. Reasoning in `store.business.md` §9 |
| `GLOBAL-021` | **Resolve: are points earned on the pre- or post-discount total?** | ✅ | — | `RewardPointsPolicy` unchanged | **Resolved 2026-08-16: post-discount, as `BR-STR-006` already said.** Earning on the list price would pay the discount twice — the member is credited for money they did not spend, and the more a plan discounts the more it inflates the reward. `PLAN-001`'s acceptance example ($150 at 15% accruing $1.00) is an arithmetic slip and has been corrected to $0.85. Renumbered from `GLOBAL-010`, which was already taken by the Stage 0 scaffolding |
| `GLOBAL-010` | Scaffold the .NET solution, frontend, and Docker composition | ✅ | — | — | Completed 2026-08-15. Four services healthy under `docker compose up`; 53 tests green |
| `GLOBAL-013` | Decide the MediatR licence position | ✅ | — | — | Resolved 2026-08-15. Pinned to 12.5.0 in `Directory.Packages.props`, licence verified Apache-2.0 from the package manifest. FluentAssertions pinned to 7.2.0 for the same reason |
| `GLOBAL-015` | Evaluate domain split: `identity` | ✅ | — | — | **Deferred by written approval 2026-08-15.** Threshold breached at 33 business rules (limit 20). Revisit after Stage 1 ships. The `identity` / `sessions` boundary is recorded below so the split stays cheap |
| `GLOBAL-014` | Replace the email system with Mailgun | ✅ | — | — | Done 2026-08-15. `IEmailSender` in Application, `MailgunEmailSender` in Infrastructure via RestSharp. Mailpit removed from compose. Verified with a real send: HTTP 200, queued |
| `GLOBAL-011` | Design the *Devices and sessions* screen | ✅ | `BLOCK-004` | `DevicesAndSessionsPage.tsx` | **Reviewed and settled 2026-08-16.** Shaped as a security screen rather than a settings one: current device pinned first, sign-in date beside last-seen, destructive action on the row. The review found a live defect — the device icon was matched against numeric codes while the API sends enum *names*, so every row had been rendering the generic fallback since Stage 1. TypeScript could not catch it because the declared type said `number` and was untrue. Fixed with named types on both `DeviceType` and `RevocationScope`; 6 regression tests |
| `GLOBAL-012` | Full Freshness Protocol sweep before MVP acceptance | ⬜ | — | — | `PLAN-001` Stage 8 |
| `GLOBAL-018` | **Evaluate domain split: `catalog`** | ✅ | — | `catalog` kept whole | **Resolved 2026-08-16: kept undivided, as a documented exception to SDD+ §6.2.** The threshold is a smell detector and here it reports a false positive — see the reasoning below. Revisit if the rule count passes 35 or a second aggregate root appears |
| `GLOBAL-020` | Promote audit to its own bounded context | ✅ | — | `Domain/Features/Audit/`, `IAuditUnitOfWork` | Done 2026-08-16. `AuditEntry` and `IAuditRepository` moved out of `identity`. Five `network` handlers no longer reach into `IIdentityUnitOfWork` for one row; `catalog` can write BR-CAT-025 entries without knowing identity exists. Rule 24 |
| `GLOBAL-022` | **Make the create endpoints answer one shape** | ✅ | — | `CreatedResourceResponse` | **Resolved 2026-08-16.** All three now answer `{ id }` through one named record. A record rather than an anonymous object so it reaches the generated schema, and an object rather than a bare identifier so a field can be added later without breaking every caller. The frontend gained a matching `CreatedResource` type so the next screen cannot get it wrong either. Creates that return a whole resource — `PlaceOrder` answers an order — are deliberately untouched: that is a different thing, not an inconsistency |
| `GLOBAL-019` | **Remove the plan tiers from `UserRole`** | ✅ | — | `UserRole` = `Member`/`Admin`/`SuperAdmin`; `SeparateRoleFromPlan` migration | **Resolved 2026-08-16: tiers removed.** `Subscription.Plan` is now the sole authority for what a member bought and `UserRole` carries authority only. `MirrorPlanOntoUserRoleHandler` deleted, `User.ChangePlan` deleted, `UserRegistered` carries the chosen `PlanTier`, `/auth/me` returns `plan`, and the sidebar's `requiresPaidPlan` reads the plan instead of the role. 35 references touched; 548 backend and 100 frontend tests green; migration applied, reverted and reapplied against the running database. See the resolution below |
| `GLOBAL-016` | Adopt `Features/` in all three layers, plus RULE 17 and RULE 20 | ✅ | — | — | Done 2026-08-15. Domain, Application and Infrastructure now share one shape. A feature holds only Commands, Queries and Events |
| `GLOBAL-017` | Unit of work per bounded context, transactions, and domain event dispatch | ✅ | — | — | Done 2026-08-15. Handler dependencies cut from 93 to 55 (41%). Nine domain events were being raised and none dispatched; the dispatcher removed `SessionRevoker` entirely |

### Status values

⬜ Not started
🔄 In progress
✅ Done
❌ Removed / not applicable (reason required in Notes)
🔴 Blocked (blocker ID required)

### Tracking reference format

`{PLATFORM} #{ID} — {URL}`. No external tracker is configured for this project, so the column reads `—`.

---

## Domain Growth Watch

Per SDD+ §6.2. Exceeding a threshold creates an evaluation task here. **No split without written human
approval.**

| Domain | Trigger | Proposed split | Status |
|---|---|---|---|
| `identity` | **33 business rules — exceeds the limit of 20** | `identity` / `sessions` | **BREACHED — split DEFERRED by approval 2026-08-15. Revisit after Stage 1** |
| `catalog` | 31 business rules — over the limit of 20 | `catalog` / `reviews` | **Documented exception, approved 2026-08-16 (`GLOBAL-018`).** Revisit at 35 rules or a second aggregate root |
| `billing` | More than 15 commands and queries combined | `fines` / `payments` | Watching |

### `GLOBAL-015` — proposed split of `identity`

Measured against SDD+ §6.2 after authoring `identity.business.md` and `identity.technical.md`:

| Indicator | Value | Limit | Breached |
|---|---|---|---|
| Business rules | **33** | 20 | **Yes** |
| Commands and queries | 15 | 15 | No — exactly at the boundary |
| Aggregates and entities | 6 | 8 | No |
| `technical.md` lines | 265 | 600 | No |

The rules fall into two clusters that reference each other only through a user identifier:

| Proposed `identity` | Proposed `sessions` |
|---|---|
| `BR-IDN-001` to `-013` — registration, account lifecycle, passwords, recovery | `BR-IDN-014` to `-027` — tokens, rotation, reuse detection, sessions, devices, revocation |
| `BR-IDN-028` to `-031` — anti-enumeration on sign-in and recovery | |
| `BR-IDN-032` to `-033` — auditing | |
| Aggregate: `User` | Aggregate: `UserSession` |
| 11 commands and queries | 4 commands and queries |

The aggregates are already separate roots, and `BR-IDN-013` (a password change revokes other
sessions) is the only rule that crosses the boundary — expressible as a domain event `sessions`
subscribes to.

**Recommendation: defer the split until after Stage 1 ships.** The boundary is clean and the split is
cheap, but performing it now rewrites two approved specifications and 42 task identifiers before a
line of code exists. The cost of deferring is one oversized domain for one stage; the cost of
splitting now is re-approving the specifications that Stage 1 is about to be built from.

**Decision: deferred**, approved in writing on 2026-08-15. The split is not performed now. It must be
re-evaluated once Stage 1 ships, before Stage 2 adds further rules to any shared surface.

### `GLOBAL-018` — proposed split of `catalog`

Measured after authoring `catalog.business.md`:

| Indicator | Value | Limit | Breached |
|---|---|---|---|
| Business rules | **31** | 20 | **Yes** |
| Commands and queries | 13 | 15 | No |
| Aggregates and entities | 4 | 8 | No |

The rules fall into two clusters that share only a book identifier:

| Proposed `catalog` | Proposed `reviews` |
|---|---|
| `BR-CAT-001` to `-026` — books, copies, the access rule, search, lifecycle | `BR-CAT-027` to `-031` — ratings and written reviews |
| Entities: `Book`, `BookCopy` | Entity: `Review` |
| 10 commands and queries | 3 commands and queries |

The only coupling is the aggregate rating a book displays, which `reviews` can publish as a domain
event rather than `catalog` reading review rows.

### Revised recommendation, 2026-08-16 — after building the domain

The first recommendation was "defer to Stage 6". Having now implemented `catalog`, the honest
recommendation is stronger: **do not split, and record why.**

**The threshold is a smell detector, not a rule, and here it reports a false positive.** Three
readings support that:

1. **The proposed `reviews` context is anaemic.** Five rules, one entity, three operations. A bounded
   context earns its own ubiquitous language, its own lifecycle and its own reasons to change. This
   one would have none of those — its whole job would be to hand a number back to `catalog`.
2. **The coupling is not incidental, it is the point.** `Book.AverageRating` is a stored column on
   the book, maintained by the review events, precisely because a listing shows a rating on every
   row and an aggregate query per book would be an N+1 by construction. Split them and that column
   becomes a cross-context projection with an eventual-consistency window on the busiest read in the
   product — real complexity bought to satisfy a counting rule.
3. **The count is inflated by granularity, not by scope.** `BR-CAT-011` to `-015` are five rules that
   state the wording of one decision. `BR-CAT-030` and `-031` are one rule about a rating, written
   twice. Merging those without losing meaning brings the domain to roughly 24 — still over 20, but
   nowhere near the two-domains-in-a-trenchcoat the threshold is meant to catch.

What genuinely deserves watching is a different boundary. `catalog` currently owns *what exists*,
*where it is*, *who may borrow it* and *what people thought of it*. If any of those grows — full-text
search, moderation, a holds queue — the seam to cut is **discovery** versus **collection**, not
books versus reviews.

### Resolution, approved 2026-08-16

`catalog` stays whole, as a **deliberate and documented exception** to SDD+ §6.2.

A threshold in a methodology is a prompt to look, not a verdict. Having looked, splitting here would
trade a real cost for a rule's satisfaction:

| | Keeping it whole | Splitting |
|---|---|---|
| `Book.AverageRating` | A stored column, maintained by an in-process event | A cross-context projection with an eventual-consistency window on the busiest read in the product |
| The `reviews` context | — | Five rules, one entity, three operations, whose whole job is to hand a number back |
| Spec files | 3 | 6, two of them describing almost nothing |
| What a reader must hold | One domain | Two, plus the contract between them |

**The revisit triggers**, recorded so this is a decision and not an omission:

1. The rule count passes **35**.
2. A **second aggregate root** appears that is not `Book`, `BookCopy` or `Review`.
3. Reviews grow a lifecycle of their own — moderation, reporting, appeals — at which point they have
   their own reasons to change and earn their own boundary.

**And the boundary worth watching is a different one.** `catalog` owns *what exists*, *where it is*,
*who may borrow it* and *what people thought of it*. The seam under real pressure is **discovery**
versus **collection** — full-text search, ranking and recommendation on one side; books, copies and
lifecycle on the other. If this domain is ever split, that is where the cut belongs, and splitting
books from reviews first would make that cut harder rather than easier.

**On the count itself.** Roughly seven of the thirty-one are granularity rather than scope:
`BR-CAT-011` to `BR-CAT-015` are five rules stating the wording of one decision, and `BR-CAT-030`
and `BR-CAT-031` are one rule about a rating written twice. Consolidated they would read as about
24 — still over 20, and nowhere near two domains in a trenchcoat. **They are deliberately not
renumbered:** every rule identifier is cited in code comments, tests and task files, and renumbering
a stable catalogue to flatter a count would be churn with negative value. The identifiers are an
API.

---

## Completion Log

| Date | Task ID | Completed by | Notes |
|---|---|---|---|
| 2026-08-16 | PLAN-001 Stage 5 | AI Agent — Claude | `store` closed, 16/16. Redemption left unbuilt on `BLOCK-002`. **PLAN-001's own acceptance example disagrees with `BR-STR-006`** — see `GLOBAL-021` |
| 2026-08-16 | PLAN-001 Stage 4 | AI Agent — Claude | `billing` closed, 22/22. The plan's acceptance criterion verified live: 20 days overdue is exactly $7.00 and 26 days is capped at $9.00 |
| 2026-08-16 | PLAN-001 Stage 3 | AI Agent — Claude | `reservations` closed, 20/20. The mandatory race for the last copy was run ten times against the running system: ten correct outcomes, never a negative count |
| 2026-08-16 | `GLOBAL-020` | AI Agent — Claude | Audit promoted to its own bounded context. Five `network` handlers stopped injecting `IIdentityUnitOfWork` for a single row, and `catalog` can now satisfy BR-CAT-025 without knowing identity exists |
| 2026-08-15 | `GLOBAL-001` | Francisco Torregrosa | Precedence approved: prototype → SDD+ → GUIDELINES |
| 2026-08-15 | `GLOBAL-002` | Francisco Torregrosa | G1 ADRs in `technical.md`; G2 per-layer coverage; G3 no pipeline behaviors; G4 `.Presentation`; G5 InMemory with a proposed exception; G6 frontend stack in `global_tech_spec.md` |
| 2026-08-15 | `GLOBAL-003` | AI Agent — Claude — 2026-08-15 | Bootstrapped `sdd_strategy/`: README, agent rules for three agents, 3 global specs, 10 domains × 3 spec files, project contract. SDD+ §15.1 checklist passes |
| 2026-08-15 | `GLOBAL-004` | AI Agent — Claude — 2026-08-15 | `GUIDELINES.md` §73 rewritten — ADRs live in `technical.md` §4, never as standalone files |
| 2026-08-15 | `GLOBAL-005` | AI Agent — Claude — 2026-08-15 | `GUIDELINES.md` §56 rewritten — per-layer backend thresholds, frontend stays at 80% |
| 2026-08-15 | `GLOBAL-006` | AI Agent — Claude — 2026-08-15 | `GUIDELINES.md` §10 and §8 rewritten — `Astrolabe.Presentation`, and `sdd_strategy/` added to the repository structure |
| 2026-08-15 | `GLOBAL-016` | AI Agent — Claude — 2026-08-15 | Raised in review: domains sat beside `Abstractions`, a feature held a catch-all `Services` folder, and mail composition was inside `Features/Identity`. All three corrected; RULE 17 and RULE 20 recorded |
| 2026-08-15 | `GLOBAL-017` | AI Agent — Claude — 2026-08-15 | `IIdentityUnitOfWork` and `INetworkUnitOfWork`; `ExecuteInTransactionAsync` over EF's execution strategy; domain events collected before save and dispatched after commit. 265 tests green throughout |
| 2026-08-15 | `GLOBAL-015` | Francisco Torregrosa | Raised automatically per RULE 7 at 33 business rules. **Deferred by written approval** — revisit after Stage 1 |
| 2026-08-15 | `NET-OPEN-001` | Francisco Torregrosa | Resolved: the seed grows to all six countries offered at registration |
| 2026-08-15 | `GLOBAL-013` | AI Agent — Claude — 2026-08-15 | MediatR pinned to 12.5.0 (Apache-2.0). 43 backend tests still green after the downgrade |
| 2026-08-15 | `GLOBAL-014` | AI Agent — Claude — 2026-08-15 | Email moved to Mailgun behind `IEmailSender`. 7 new WireMock-backed tests. Docs updated in GUIDELINES §6.2 and §7, global_tech_spec, project_contract, both READMEs and PLAN-001 |
| 2026-08-15 | `GLOBAL-010` | AI Agent — Claude — 2026-08-15 | Stage 0 scaffolding: 4 backend projects + 4 test projects, Result/Error/Money primitives, CQRS abstractions, DbContext, global exception handler, health endpoints, Serilog, CPM, React shell with the prototype theme, two Dockerfiles and a 4-service compose. 43 backend + 10 frontend tests green. Verified: all endpoints 200, migration applied, CORS correct |

---

## Progress Summary

| Category | Done | Total |
|---|---|---|
| Governance and conflict resolution | 2 | 2 |
| Architecture corrections | 2 | 2 |
| SDD+ bootstrap | 1 | 1 |
| Stage 0 implementation | 1 | 1 |
| Document corrections | 3 | 4 |
| Domain growth evaluations | 1 | 2 |
| Open decisions | 0 | 1 |
| Licence and provider changes | 2 | 2 |
| Implementation | 0 | 2 |
| **Total** | **12** | **18** |

---

## `GLOBAL-019` — resolution, 2026-08-16

### What was wrong

`UserRole` held `Basic = 0, Plus = 1, Max = 2, Admin = 10, SuperAdmin = 20`. A member's role therefore
*was* their plan. When `membership` arrived in Stage 2 it introduced `Subscription.Plan` as the
authority, and the old field could not simply be dropped mid-stage — so `MirrorPlanOntoUserRoleHandler`
copied every plan change back onto the role.

That mirror was correct and it worked. The problem was never drift; it was that **one fact had two
representations**, and every domain added after Stage 2 was one more reader that had to know which of
the two was current. `store` was about to be the fourth.

### What was done

| Before | After |
|---|---|
| `UserRole.Basic \| Plus \| Max \| Admin \| SuperAdmin` | `UserRole.Member \| Admin \| SuperAdmin` |
| `User.Register(..., UserRole plan, ...)`, refusing a staff value at run time | `User.Register(..., PlanTier plan, ...)` — a staff value no longer type-checks |
| `User.ChangePlan(UserRole)` beside `ChangeRole(UserRole)` | `ChangePlan` deleted; only `ChangeRole` remains |
| `MirrorPlanOntoUserRoleHandler` | deleted |
| `UserRegistered(EventId, OccurredAt, UserId, Email, FullName)` | `+ PlanTier Plan` |
| `StartSubscriptionOnRegistrationHandler` read the plan back off the role | reads it off the event; the `IUserRepository` dependency and its query are gone |
| `RequireRole("Basic", "Plus", "Max")` | `RequireRole(nameof(UserRole.Member))` |
| `CurrentUserDto` had no plan | `+ PlanTier? Plan`, resolved through `IEntitlementProvider`; null for staff |
| Frontend `isVisibleTo(item, role)`, `role === 'Plus' \|\| role === 'Max'` | `isVisibleTo(item, role, plan)`; new `PlanGuard` replaces `RoleGuard allow={PaidPlanRoles}` |

### Decisions worth recording

**The plan travels on the event.** `identity` does not store what a visitor bought, so the chosen tier
has nowhere to live between the pricing screen and the subscription. Putting it on `UserRegistered` is
what lets `identity` stay ignorant of what a plan is worth.

**Policies bind with `nameof`, not literals.** Three string literals would have compiled perfectly
against a renamed enum and locked every member out at run time. This is the exact failure mode the
project has already been bitten by twice with value converters — code that compiles, passes its unit
tests, and is wrong only when it runs.

**The migration rescues before it discards.** A member on the old role 1 or 2 with no active
subscription held their plan in exactly one place, and step two of the migration overwrites it. In
practice `MembershipSeeder` guarantees this matches nothing — it is written anyway, because "should
match nothing" and "cannot lose a paid plan" are different guarantees and only the second belongs in a
migration. That rescue is also what makes `Down` genuinely reversible rather than a guess, which was
verified by reverting and reapplying against the running database.

**`MembershipSeeder` now backfills at Basic.** It used to read the tier off the role. Nothing records
what an unsubscribed member bought any more, so the free tier is the only honest answer: granting a
paid plan on a guess hands out entitlements nobody paid for, while Basic is recoverable in one screen.
`DemoAccountSeeder` therefore opens the demo member's Plus subscription itself rather than leaving it
to the backfill.

### Deployment consequence

Access tokens issued before this change carry `"Plus"` or `"Max"` in the role claim and match no
policy, so live sessions break on the first authorised request. Refresh reissues a correct token, but
any session holding only an access token is signed out. Acceptable in development; on a deployed
environment this needs either a short token lifetime window or a deliberate mass sign-out.
