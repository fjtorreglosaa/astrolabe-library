# Astrolabe Books — Claude Instructions

**Methodology:** SDD+ v1.0
**Project:** Astrolabe Books — a membership platform for a network of physical libraries

You are a software development assistant for this project. It follows **SDD+** (Specification and
Domain-Driven Development Plus), defined in `SDD_PLIUS_STRATEGY.md`. You are a **controlled executor of
the specifications, not a decision-maker**.

---

## Before responding to any development request

1. Read `sdd_strategy/README.md`.
2. Read `sdd_strategy/specs/global_spec.md` and `sdd_strategy/specs/global_tech_spec.md`.
3. Confirm which **domain** the task belongs to. If it is unclear, ask.
4. Read that domain's three spec files.
5. Apply the **Freshness Protocol** below.
6. Do not suggest or write code for anything not described in a spec.

## Freshness Protocol

Check the `Last reviewed` date on every spec file related to the task. A spec is stale after
**7 calendar days**.

| Stale files | Action |
|---|---|
| 0 | Proceed immediately |
| 1 to 3 | Update each against the current code, set `Last reviewed` to today, set `Reviewed by` to `AI Agent — Claude — {date}`, note it in the domain's Completion Log, then proceed |
| 4 or more | **Stop.** Create a `GLOBAL-*` task in `global_task_spec.md` and tell the user a full human review is required |

You may update: review dates, factually incorrect descriptions, missing entries for things that clearly
exist in the code, and task status that the code clearly demonstrates.

You may **not** update: business rules, architecture decisions, task priorities, or anything requiring
business judgment. When you find one of these, add an agent review note at the top of the spec and raise
it with the user.

---

## Authority precedence

| # | Source | Authority over |
|---|---|---|
| 1 | **The prototype** in `docs/design/` | Product behaviour, UI, copy, business rules |
| 2 | **`SDD_PLIUS_STRATEGY.md`** | Process, structure, spec format, agent rules |
| 3 | **`GUIDELINES.md`** | Architecture and stack where the two above are silent |

**Never invent product behaviour.** The real rules, exact interface copy, and complete seed data are in
`docs/design/prototype.source.js`. `docs/design/README.md` explains where to find what.

---

## Architecture rules

```text
Domain         → no external project references, zero external NuGet packages
Application    → Domain only
Infrastructure → Application + Domain
Presentation   → Application + Infrastructure
Tests          → only the project under test
```

- Commands: `ICommand` → `ICommandHandler<T>` → `Task<Result>`
- Queries: `IQuery<T>` → `IQueryHandler<TQ,TR>` → `Task<Result<T>>`
- **Validate inside handlers. No pipeline behaviors. No `LoggingBehavior` class.**
- Inject `ISender`, not `IMediator`, unless `Publish()` is genuinely needed
- `CancellationToken` is always the last parameter and is propagated to every async call
- The presentation project is `Astrolabe.Presentation`, not `Astrolabe.Api`
- EF Core: Fluent API configurations only, no data annotations on domain entities
- Never `EnsureCreated`. Migrations are applied through a controlled startup step

## File and folder organisation

**One public type per file**, named after the type. Never group interfaces or entities together.

Each layer segregates by kind, and the namespace matches the folder:

```text
Domain/Abstractions/Repositories/   IRepository<TEntity>
Domain/Features/{Domain}/Entities|ValueObjects|Events|Errors|Repositories/
Application/Abstractions/           interfaces only
Application/Contracts/              DTOs, messages, results
Infrastructure/Persistence/Configurations|Repositories|Seeding/
Presentation/Controllers|Middleware|Options|Extensions/
```

## Feature folders

A feature contains exactly three kinds — **Commands, Queries and Events**, the three message kinds
of CQRS. There is no `Services`, `Helpers`, `Utils` or `Common` folder inside a feature. Shared
logic is a concern under `Shared/`, or an event handler.


Application and Infrastructure group every domain under `Features/`. A new domain gets a folder
there and nowhere else — never directly under `Application/` or `Infrastructure/`.

```text
Application/Abstractions/            interfaces only
Application/Contracts/               DTOs, messages, results
Application/Features/{Domain}/Commands|Queries|Events/
Application/Shared/{Concern}/        cross-cutting policy, e.g. Mail. Never inside a feature
Infrastructure/Features/{Domain}/    domain-specific implementations
Infrastructure/Persistence/Configurations|Repositories/{Domain}/
Infrastructure/Integrations/{Provider}/   external providers: Mail, Ai, Payments
```

## Unit of work

`IUnitOfWork` lives in `Domain/Abstractions/Persistence` and exposes `SaveChangesAsync` plus
`ExecuteInTransactionAsync`.

**Every bounded context has its own unit of work** exposing only that context's repositories:

```csharp
public interface IIdentityUnitOfWork : IUnitOfWork
{
    IUserRepository Users { get; }
    IUserSessionRepository Sessions { get; }
}
```

**Handlers depend on the unit of work, never on repositories directly.** Constructors stay small
while a handler in one context still cannot reach another context's repositories.

Every repository reachable from a unit of work shares one change tracker, so a single
`SaveChangesAsync` commits their staged work atomically. Repositories never call `SaveChanges`.

## Domain events

Aggregates raise events; `SaveChangesAsync` dispatches them **after** the commit succeeds.

Anything that must happen whenever a state change occurs, regardless of which handler caused it,
belongs in an event handler rather than in each caller. Cache eviction on session revocation is the
reference example: four rules end a session and every one must evict, so the event drives it and no
caller can forget.

A reaction runs after the commit, so it may be retried or lost. **Never put a step the business
outcome depends on in an event handler.**

**Never inject `IDbContextFactory` into a request-scoped handler.** Each call returns a new context
with its own change tracker, which silently breaks the unit of work: repositories would stage work
into contexts that are never saved. The factory is only for work outside a request scope.

## Repository pattern

Generic operations live on `IRepository<TEntity>`. Every concrete contract extends it and adds only
domain-specific capabilities:

```csharp
public interface IReservationRepository : IRepository<Reservation>
```

Implementations extend the base `Repository<TEntity>` in Infrastructure. Never redeclare a generic
method on a concrete interface. `IQueryable`, `DbContext` and EF tracking concepts never leave
Infrastructure.

## Project rules

- **Money is always an integer number of cents.** Never `decimal`, `float`, or `double`, in any layer,
  including DTOs and the database
- Timestamps are persisted in UTC
- The prototype does **not** use Material UI. It is a visual and behavioural reference, never reusable
  code. Rebuild every screen on the Material UI theme in `GUIDELINES.md` §38.1
- All repository content is written in **English**: code, comments, specs, docs, commit messages, UI copy
- Trunk-based development. `main` is the trunk. Never propose `develop`, `staging`, or `production`
  branches, long-lived feature branches, or environment-specific builds
- No infrastructure work in this phase. Azure, Terraform, and CI/CD require their own plan

## Coverage

Domain 90% · Application 80% · Infrastructure 70% · Presentation 70% · Frontend 80%

Every `BR-*` business rule has at least one unit test. No real HTTP calls or database connections in unit
tests. Each EF InMemory test gets a unique database name.

## Security

**No card number ever enters this system.** `PaymentMethod` holds brand, last four digits, expiry and
cardholder only; the factory refuses anything but four digits rather than truncating, and the column
is `character(4)`. There is no field, no DTO and no endpoint that could carry a full number. Never
add one — a tokenising provider returns exactly these details and nothing more is needed.

Secrets, API keys, passwords, and connection strings never appear in committed files. Passwords are always
hashed. API error responses never include stack traces. AI provider keys are encrypted at rest and are
never returned by any API response.

---

## Verify against the running system, not only the tests

A green test suite is not evidence that a feature works. Before marking a task done, exercise it
against the running composition: sign in, call the endpoint, read the response.

Stage 2 shipped two defects that every unit test passed — a value converter that made catalogue
search fail at run time, and a seeder projection that crashed the API at startup. Both were found
with `curl`, neither with `dotnet test`.

Watch in particular:

- anything that queries **through a value object** — see the persistence rule below. Stage 2 shipped
  three of these: catalogue search, the rating average, and ordering by price
- anything that binds an **enumeration from a request body**
- anything a **migration or seeder** touches, including the down-migration
- any **date rendered to a member**, which must be checked in a zone other than the server's
- the **frontend build**, with `npm run build` — not `tsc --noEmit`. The build script is
  `tsc -b && vite build`, and project-reference mode applies stricter settings than a bare
  `--noEmit`: a `noUncheckedIndexedAccess` error slipped through that way, the Docker build failed,
  and `docker compose up -d` went on serving the previous image without a word

When a defect is found this way, add the regression test that would have caught it, then fix it.

**A container that reports `healthy` is not evidence that it runs your code.** `docker compose up -d
--build` leaves the previous image in place when the build fails, and the old container passes its
health check happily. After rebuilding a service, confirm the image was actually replaced — check
`docker inspect <name> --format '{{.State.StartedAt}}'`, or grep the served asset for a string only
the new code contains.

---

## Persisting value objects

Map a value object as an **owned type** (a class) or a **complex type** (a struct such as `Money`),
never with a value converter, whenever any of its members is ever read, filtered, ordered or
aggregated in a query.

A converter collapses the object into an opaque scalar, so the provider cannot see the members
inside it: `book.Isbn.Value` compiles, passes every unit test, and throws at run time. Stage 2 hit
this three times — `Isbn` broke catalogue search, `StarRating` broke the rating average, and `Money`
broke ordering by price.

---

## Audit is its own bounded context

`AuditEntry` and `IAuditUnitOfWork` live under `Domain/Features/Audit/`, not under `identity`. Four
domains append to the trail and none owns it; while it lived in identity, a network handler injected
the whole `IIdentityUnitOfWork` to write one row.

Write the entry **inside the command handler**, in the same transaction as the change it describes.
Never in a domain event handler: a reaction runs after the commit and may be lost, and a trail that
can silently skip a transition is not a trail.

---

## Authority and entitlement are two facts

A **role** says what a user may do. A **plan** says what they bought. They change for different
reasons, at different rates, decided by different people — so they are never the same field.

`UserRole` once held `Basic | Plus | Max | Admin | SuperAdmin`, which meant a member's role *was*
their subscription. Keeping the two in step needed a mirror handler, and every domain that read a
plan had to know which of the two representations was current. `GLOBAL-019` removed the tiers:
`UserRole` is `Member | Admin | SuperAdmin`, and `Subscription.Plan` is the only authority for a
plan, reached through `IEntitlementProvider`.

Before adding a field, ask what single question it answers. If the honest answer names two, it is
two fields.

## Strings that must match an enum use `nameof`

Authorization policies, claim values and anything else compared against an enum's name are written
`nameof(UserRole.Member)`, never `"Member"`. A literal compiles perfectly against a renamed enum and
fails only at run time — the same shape of defect as a value converter, and the reason `GLOBAL-019`
did not lock every member out of the API.

---

## After completing any task

1. Mark the task done in the relevant `tasks.md`.
2. Add a row to its Completion Log with the date and notes.
3. Update the Progress Summary percentage.
4. Update `global_task_spec.md` if the task was global or cross-domain.

Commit format: `{type}({task-id}): {description}` where type is one of
`feat | fix | test | refactor | docs | chore`. Example: `feat(IDN-001): add RegisterCommand handler`.
One task per commit. All existing tests pass before committing.

---

## Committing

**A stage is not finished until its work is committed**, and **you must ask before committing**.
Never commit unprompted. Present first: the build result, the test result, the file count, and proof
that no secret is staged.

Never stage `.env` or any file holding a real key, password or connection string — verify it rather
than assuming `.gitignore` covered it.

A stage-closing commit uses the plan and stage as its identifier:
`feat(PLAN-001-stage-1): identity and network`.

Pushing is a separate action and needs its own approval.

## Non-negotiables

- **No assumptions.** If something is ambiguous or not covered by the specs, ask the user and reference
  the specific spec section that is unclear. Do not assume and proceed.
- **No domain splits** without the user explicitly writing "approved" or "proceed", even when a growth
  threshold is breached. Breaching a threshold creates a task in `global_task_spec.md` — nothing more.
- **No plan execution** without explicit written approval, per SDD+ §13.2 step 3. Implicit approval is
  not accepted.

Domains currently under growth watch: `billing`, `identity`. `catalog` breached the rule limit and is
a **documented exception approved 2026-08-16** (`GLOBAL-018`) — revisit at 35 rules or a second
aggregate root, not before.

## Current state

`sdd_strategy/specs/plans/PLAN-001_astrolabe-books-mvp.md` — **Approved, In Progress.**

| Stage | Domains | State |
|---|---|---|
| 0 | Scaffolding, Docker composition | ✅ Done |
| 1 | `identity`, `network` | ✅ Done — 42/42 and 23/25 |
| 2 | `membership`, `catalog` | ✅ Done — 18/18 and 22/22 |
| 3 | `reservations` | ✅ Done — 20/20 |
| 4 | `billing` | ✅ Done — 22/22 |
| 5 | `store` | ✅ Done — 17/17 |
| 6 | Administration surfaces | ✅ Done — user directory, book management, libraries and admins |
| 7–9 | — | Not started |

Open decisions awaiting the user:

- `BLOCK-006` — the Mailgun sandbox only delivers to authorised recipients. Needs an account
  change on your side, not a decision.
