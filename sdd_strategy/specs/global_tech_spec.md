# Astrolabe Books — Global Technical Specification

**Last reviewed:** 2026-08-15
**Reviewed by:** Francisco Torregrosa
**Version:** 1
**Implements:** `BR-GLOBAL-001` to `BR-GLOBAL-010`

---

## 1. Solution structure

```text
/
├── src/
│   ├── backend/
│   │   ├── Astrolabe.Domain/          ← ZERO external NuGet packages
│   │   │   ├── Abstractions/          ← Entity, AggregateRoot, IDomainEvent
│   │   │   │   └── Persistence/       ← IRepository<T>, IUnitOfWork
│   │   │   ├── Primitives/            ← Result, Error, Money, PagedResult
│   │   │   └── Features/{Domain}/     ← Entities, ValueObjects, Events, Errors, Repositories
│   │   ├── Astrolabe.Application/     ← references Domain only
│   │   │   ├── Abstractions/          ← ports: Messaging, Identity, Mail, Network, Events
│   │   │   ├── Contracts/             ← DTOs, messages, results
│   │   │   ├── Shared/{Concern}/      ← cross-cutting policy, e.g. Mail
│   │   │   └── Features/{Domain}/     ← Commands, Queries, Events
│   │   ├── Astrolabe.Infrastructure/  ← references Application + Domain
│   │   │   ├── Persistence/           ← DbContext, Configurations, Repositories, Migrations, Seeding
│   │   │   ├── Integrations/          ← external providers: Mail, later Ai and Payments
│   │   │   ├── Features/{Domain}/     ← domain-specific implementations
│   │   │   └── Time/
│   │   └── Astrolabe.Presentation/    ← references Application + Infrastructure
│   └── frontend/
│       └── astrolabe-web/
│
├── tests/
│   ├── backend/
│   │   ├── Astrolabe.Domain.Tests/
│   │   ├── Astrolabe.Application.Tests/
│   │   ├── Astrolabe.Infrastructure.Tests/
│   │   └── Astrolabe.Presentation.Tests/
│   └── frontend/
│
├── docker/
│   ├── api/Dockerfile
│   └── web/Dockerfile
│
├── docs/design/                       ← approved UI prototype
├── sdd_strategy/                      ← SDD+ specs, docs, and agent rules
├── docker-compose.yml
├── .env.example
├── GUIDELINES.md
├── SDD_PLIUS_STRATEGY.md
├── README.md
└── CLAUDE.md
```

### Dependency rules — strictly enforced

```text
Domain         → no references to any other project layer
Application    → Domain only
Infrastructure → Application + Domain
Presentation   → Application + Infrastructure
Tests.*        → only the project under test
```

Any reference violating these rules must be rejected by the agent and flagged to the developer.

---

## 2. Technology stack

### Backend

| Concern | Choice |
|---|---|
| Language and runtime | C#, .NET 10 |
| API | ASP.NET Core Web API |
| ORM | Entity Framework Core, Code First, Fluent API only |
| Database | PostgreSQL 16 |
| Dispatch | MediatR, injected as `ISender` |
| Validation | Inside handlers. **No FluentValidation pipeline behaviors** |
| Logging | Serilog, structured, using built-in tooling. **No `LoggingBehavior` class** |
| Configuration | Options Pattern — `IOptions<T>`, `IOptionsSnapshot<T>`, `IOptionsMonitor<T>` |
| API documentation | OpenAPI and Swagger in development |

### Frontend

SDD+ §9 documents the .NET stack only. This section is the project-level record of the frontend stack,
pending a formal §9.2 addition to the methodology document.

| Concern | Choice |
|---|---|
| Build tool | Vite |
| Framework | React 19, TypeScript in strict mode |
| Components | Material UI v6 |
| Server state | TanStack Query |
| Client state | Zustand, for genuinely global non-server state only |
| HTTP | Axios, with dedicated API services per feature |
| Routing | React Router |
| Forms | React Hook Form with Zod |
| Testing | Jest and React Testing Library |

### Runtime and local environment

| Concern | Choice |
|---|---|
| Containers | Docker, multi-stage builds |
| Orchestration | Docker Compose — web, api, db |
| Database image | `postgres:16-alpine`, persistent volume |
| Transactional email | Mailgun HTTP API, via RestSharp, behind `IEmailSender` |

---

## 3. CQRS conventions

```csharp
// Command — write operation, always returns Result
public sealed record CreateReservationCommand(Guid MemberId, Guid CopyId, DeliveryMode Delivery)
    : ICommand;

public sealed class CreateReservationCommandHandler : ICommandHandler<CreateReservationCommand>
{
    public async Task<Result> Handle(CreateReservationCommand request, CancellationToken ct)
    {
        // 1. Validate inside the handler — no pipeline behaviors
        // 2. Business logic
        return Result.Success();
    }
}

// Query — read operation, returns Result<T>
public sealed record GetReservationQuery(Guid ReservationId) : IQuery<ReservationDto>;
```

**Rules**

- Commands return `Task<Result>` — never `Task<Unit>` or bare `Task`
- Queries return `Task<Result<T>>`
- Validation runs **inside the handler**. There are no pipeline behavior classes
- Inject `ISender` by default. Inject `IMediator` only when `Publish()` is genuinely needed
- `CancellationToken` is always the last parameter and is propagated to every async call
- Controllers stay thin: bind, dispatch, convert `Result` to an HTTP response

---

## 4. Architecture Decision Log

| Decision | Choice | Rationale | Alternatives rejected |
|---|---|---|---|
| Authority precedence | Prototype → SDD+ → GUIDELINES | The prototype is the only artifact that describes the actual product. SDD+ claims final authority on process. GUIDELINES fills the remaining gaps | GUIDELINES first — rejected because it contradicted the approved prototype on roles, scope, and discounts |
| Presentation project name | `Astrolabe.Presentation` | SDD+ §9.1 names the layer by architectural role, not by protocol | `Astrolabe.Api` per GUIDELINES §10 — rejected in favour of SDD+ under the approved precedence. GUIDELINES §10 to be corrected |
| Persistence engine | PostgreSQL 16 | Free licence, excellent EF Core support via Npgsql, JSONB for AI metadata, runs natively on Apple Silicon | SQL Server — rejected: heavy image and emulation required on the development machines in use |
| Money representation | Integer cents | Eliminates an entire class of rounding defects in fines, per-line discounts, and point accrual, which are the most error-prone rules in the product | `decimal` — rejected: correct arithmetically but invites accidental float conversion at DTO and JSON boundaries |
| Validation location | Inside handlers | SDD+ §9.1 and Rule 4 forbid pipeline behaviors. Keeps the failure path visible in the handler that owns the rule | FluentValidation pipeline behavior — rejected, explicitly forbidden by the methodology |
| Logging | Built-in tooling with Serilog | SDD+ Rule 4 explicitly forbids a `LoggingBehavior` class | MediatR logging behavior — rejected, explicitly forbidden |
| ADR location | Domain `technical.md` §4 | SDD+ §5.2 makes `technical.md` the living ADR collection, keeping the decision next to the code it governs | Standalone `docs/adr/NNN-*.md` per GUIDELINES §73 — rejected under the approved precedence. GUIDELINES §73 to be corrected |
| Coverage thresholds | Domain 90, Application 80, Infrastructure 70, Presentation 70, frontend 80 | SDD+ §9.1 demands most where business rules live and least where tests are expensive. Frontend is not covered by SDD+, so GUIDELINES §56 applies | Flat 80/80 per GUIDELINES §56 — rejected: allows the domain layer, which holds the critical rules, to sit at 80 |
| Concurrency on copies | Optimistic, version token per copy | The contested resource is a single row and contention is rare. A race test is mandatory | Pessimistic locking — rejected: unnecessary contention cost for a rare collision |
| Payment provider | `IPaymentProvider` with a simulated implementation | The MVP needs the full purchase and fine flows without a gateway. The seam keeps a real provider a drop-in later | Stripe test mode — rejected: adds a full stage to the MVP for no product learning |
| AI credentials | Per library, encrypted at rest | The prototype defines credentials as library-owned, managed by library staff | A single platform key — rejected: contradicts the prototype |
| UI reconstruction | Rebuild on Material UI (v9 as resolved) | GUIDELINES §38 requires Material UI. The prototype uses inline styles and is not reusable | Copying prototype styles — rejected: produces an unmaintainable parallel design system |
| Frontend test runner | Jest with SWC transform | GUIDELINES §58 mandates Jest and React Testing Library. SWC keeps the suite fast while `tsc -b` type-checks separately | Vitest — rejected despite being the natural fit for Vite, because GUIDELINES governs the frontend stack and SDD+ is silent on it. Revisit via a GUIDELINES amendment, not silently |
| NuGet version management | Central Package Management in `Directory.Packages.props` | A transitive dependency pulled a second EF Core Relational version into the build. Central versions plus transitive pinning make that impossible | Per-project versions — rejected: the conflict it caused was invisible until the build warned |
| NuGet sources | `NuGet.config` with `<clear />` and source mapping | A developer machine feed leaked into restore, making builds non-reproducible and breaking CPM | Relying on the machine-level config — rejected for reproducibility |
| API container healthcheck | `curl` installed in the runtime image, probe declared in the Dockerfile | The ASP.NET runtime image ships neither curl nor wget, so the original probe could never pass. Declaring it in the Dockerfile keeps the image self-describing outside compose | Probe in compose only — rejected: the image would report no health when run directly |
| Build context | `.dockerignore` excluding `obj/`, `node_modules/`, docs and specs | Host build artefacts were copied into the image and overwrote the container's restore, failing with NETSDK1064. Excluding docs also stops a documentation edit from invalidating the layer cache | No dockerignore — rejected: it actively broke the API image build |
| Frontend state split | TanStack Query for server state, Zustand for UI state | Prevents server data being duplicated into a second cache | Zustand for everything — rejected: reimplements caching, invalidation, and refetching by hand |
| Layer organisation | Every layer groups domains under `Features/` | With ten domains, folders sitting beside `Abstractions` and `Primitives` stop distinguishing cross-cutting from business. The same shape in all three layers means one mental model | Domains at the layer root — rejected: unreadable past a handful of contexts. Established as RULE 17 |
| Unit of work scope | One per bounded context, extending `IUnitOfWork` | Cut handler dependencies from 93 to 55 (41%) and made the shared change tracker structural rather than a consequence of container wiring | A single global unit of work — rejected: `identity` would depend on `billing` and `catalog` repositories it never uses, coupling every context in one type. Repositories injected directly — rejected: measured at 37 repository parameters across 14 handlers, an excessive-parameter-list smell per GUIDELINES §42 |
| Transaction control | `ExecuteInTransactionAsync` over EF's execution strategy | A bare `BeginTransaction` breaks under connection resiliency: a retry resumes mid-transaction on a connection that no longer has one | Manual `BeginTransaction` — rejected on that failure mode. No transaction support — rejected: multi-step operations that must save twice had no way to stay atomic |
| Domain event dispatch | Collected before saving, dispatched after committing | Collecting first stops a reaction that saves again from republishing the same events; dispatching after means no reaction observes a rolled-back change | Dispatch inside the transaction — rejected: a reaction could act on a change that never landed. Manual calls in each handler — rejected: nine events were raised and none dispatched, and the manual stand-in (`SessionRevoker`) had to be remembered by four separate callers |
| Cross-cutting reactions | Event handlers, not shared services | Four rules end a session and every one must evict from the revocation cache. Driving it from the event makes it impossible to forget rather than merely documented | A shared `SessionRevoker` service — rejected once the dispatcher existed: it was a manual substitute for the event, and it put a fourth kind of folder inside a feature |
| Mail composition location | `Application/Shared/Mail`, one template class per domain | Message copy is a concern shared by every domain that sends email, and `MailOptions` must not be duplicated per domain | Inside each feature — rejected: adds a fourth folder kind to `Features/{Domain}` and copies the frontend base URL once per domain. `Utilities/` — rejected: email copy is business policy, not a technical helper |

---

## 5. Testing conventions

| Concern | Tool |
|---|---|
| Backend unit tests | NUnit 4.x, Moq 4.x, FluentAssertions |
| EF Core integration | `Microsoft.EntityFrameworkCore.InMemory`, unique database name per test |
| HTTP client mocking | WireMock.Net |
| Test data | Builder pattern per aggregate |
| Frontend | Jest and React Testing Library |

**Naming:** `{Method}_{Condition}_{ExpectedResult}` —
for example `Handle_WhenCopyIsOutOfStock_ReturnsUnavailableFailure`.

**Coverage minimums**

| Layer | Minimum |
|---|---|
| Domain | 90% |
| Application | 80% |
| Infrastructure | 70% |
| Presentation | 70% |
| Frontend | 80% |

**Rules**

- Every `BR-*` business rule has at least one unit test
- Tests verifying a documented example carry a comment referencing the spec section
- No real HTTP calls or real database connections in unit tests
- Each EF InMemory test gets a unique database name to prevent state leakage

**Proposed exception, pending approval.** SDD+ §9.1 specifies EF InMemory. Migrations and PostgreSQL
specific behaviour cannot be validated that way. Proposal: add container-backed integration tests against
a real `postgres:16-alpine` for migrations and repository queries only, keeping InMemory for handler-level
tests. Tracked as `GLOBAL-008`.

---

## 6. Persistence conventions

- Code First with Fluent API configurations. **No data annotations on domain entities**
- All configurations in `Infrastructure/Persistence/Configurations/`, auto-discovered via
  `ApplyConfigurationsFromAssembly`
- Migrations are version controlled and applied through a controlled startup step.
  **Never `EnsureCreated`**
- Every migration must have a verified down-migration before merge
- Money columns are integer types storing cents
- Timestamps are `timestamptz`, persisted in UTC

---

## 7. Security conventions

- Access token: JWT, 15 minutes, carrying a `sid` session claim
- Refresh token: opaque, 256-bit, 30 days, stored as SHA-256 hash only, one per session
- Refresh transport: cookie with `HttpOnly`, `Secure`, `SameSite=Strict`, path-scoped to the refresh
  endpoint. Access token held in memory, never in `localStorage`
- Rotation on every refresh, with reuse detection revoking the whole session chain
- Passwords hashed with ASP.NET Core Identity, minimum 12 characters, lockout after 5 failed attempts
- Secrets supplied by environment variable. Never in source, images, pipeline files, or logs
- API errors follow `application/problem+json` and never expose stack traces
- AI provider keys encrypted at rest, never returned by any API response

---

## 8. Known constraints and limitations

- The revoked-session cache is in-process for the MVP, behind an abstraction so a distributed cache can
  replace it when more than one instance runs
- Payment is simulated. No real settlement, refunds, or chargebacks
- The system is USD only
- Search is relational `LIKE`-based. Full-text and semantic search are out of scope for this phase
- No infrastructure exists yet. Deployment requires its own plan

---

## 9. Superseded decisions

| Decision | Superseded by | Reason | Date |
|---|---|---|---|
| — | — | No decisions superseded yet | — |
