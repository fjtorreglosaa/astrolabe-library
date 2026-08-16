# Astrolabe Books

A membership platform for a network of physical libraries. Members reserve and buy physical books
across the libraries their plan reaches; library staff run the branches assigned to them.

**Status:** PLAN-001 Stage 0 complete — the shell runs end to end. No feature is implemented yet.

---

## Quick start

Requirements: Docker with Compose v2, and nothing else. The .NET SDK and Node are only needed to work
on the code outside containers.

```bash
cp .env.example .env
# Set POSTGRES_PASSWORD, for example: openssl rand -base64 24
docker compose up -d
```

| Service | URL | Notes |
|---|---|---|
| Web | http://localhost:5173 | React single-page application |
| API | http://localhost:5080 | ASP.NET Core |
| Swagger | http://localhost:5080/swagger | Development only |
| Health — liveness | http://localhost:5080/health/live | Process is up |
| Health — readiness | http://localhost:5080/health/ready | Process is up **and** the database is reachable |
| PostgreSQL | localhost:5432 | Credentials come from `.env` |

Stop with `docker compose down`, or `docker compose down -v` to also drop the database volume.

---

## Architecture

Clean Architecture with CQRS. Dependencies point inward and are enforced by a test, not by convention
alone.

```text
Astrolabe.Domain          entities, value objects, domain events, repository interfaces
      ↑                   zero external NuGet packages
Astrolabe.Application     commands, queries, handlers, DTOs — references Domain only
      ↑
Astrolabe.Infrastructure  EF Core, repositories, external clients — references Application + Domain
      ↑
Astrolabe.Presentation    controllers, middleware, composition root
```

### Conventions that are not negotiable

| Rule | Why |
|---|---|
| Commands return `Task<Result>`, queries return `Task<Result<T>>` | Expected business failures are values, not exceptions |
| Validation runs **inside handlers** | No pipeline behaviors, no `LoggingBehavior` — SDD+ §9.1 and Rule 4 |
| Inject `ISender`, not `IMediator` | Unless `Publish()` is genuinely needed |
| Money is an **integer number of cents** | `BR-GLOBAL-001`. Never `decimal`, `float`, or `double` |
| Timestamps are UTC | `BR-GLOBAL-002` |
| EF Core uses Fluent API configurations | No data annotations on domain entities |
| Migrations apply through an explicit startup step | Never `EnsureCreated` |

`Astrolabe.Domain.Tests/ArchitectureTests.cs` fails the build if the Domain layer grows a dependency.
`Astrolabe.Application.Tests/DependencyInjectionTests.cs` fails the build if a pipeline behavior is
ever registered.

---

## Repository layout

```text
src/backend/          four projects, one per layer
src/frontend/         astrolabe-web — Vite, React, TypeScript, Material UI
tests/backend/        one test project per layer
docker/               Dockerfile per image, plus the nginx config
sdd_strategy/         SDD+ specifications, agent rules, project contract
docs/design/          the approved UI prototype — the product authority
```

### Where the truth lives

| Question | Answer |
|---|---|
| What should this screen do? | `docs/design/` — the prototype has the final word |
| Why is the code shaped this way? | `sdd_strategy/specs/global_tech_spec.md` §4, and each domain's `technical.md` §4 |
| What are the business rules? | Each domain's `business.md`, as numbered `BR-*` entries |
| What is being built next? | `sdd_strategy/specs/plans/PLAN-001_astrolabe-books-mvp.md` |
| How does the process work? | `SDD_PLIUS_STRATEGY.md`, and `GUIDELINES.md` for architecture |

---

## Development

### Backend

```bash
dotnet build
dotnet test
dotnet run --project src/backend/Astrolabe.Presentation
```

Set `ConnectionStrings__Database` when running outside Docker.

Adding a migration:

```bash
dotnet ef migrations add <Name> \
  --project src/backend/Astrolabe.Infrastructure \
  --startup-project src/backend/Astrolabe.Presentation \
  --output-dir Persistence/Migrations
```

Every migration must have a verified down-migration before it is merged.

### Frontend

```bash
cd src/frontend/astrolabe-web
npm install
npm run dev          # http://localhost:5173
npm test
npm run typecheck
```

### Package versions

All NuGet versions are declared in `Directory.Packages.props` and nowhere else. Individual `.csproj`
files reference packages without a version. `NuGet.config` pins the restore source so a developer's
machine-level feeds cannot leak into the build.

---

## Testing

| Layer | Minimum coverage |
|---|---|
| Domain | 90% |
| Application | 80% |
| Infrastructure | 70% |
| Presentation | 70% |
| Frontend | 80% |

Every `BR-*` business rule must have at least one unit test. No real HTTP calls or database
connections in unit tests; each EF in-memory test gets a unique database name.

Test naming: `{Method}_{Condition}_{ExpectedResult}`.

---

## Environment variables

Configured through `.env`, which is git-ignored. See `.env.example` for the full list.

| Variable | Purpose |
|---|---|
| `POSTGRES_PASSWORD` | Required. The stack refuses to start without it |
| `POSTGRES_DB`, `POSTGRES_USER` | Database name and role |
| `API_PORT`, `WEB_PORT`, `DB_PORT` | Host port mappings |
| `ASPNETCORE_ENVIRONMENT` | `Development` enables Swagger and verbose logging |
| `MAILGUN_API_KEY`, `MAILGUN_DOMAIN`, `MAILGUN_FROM_ADDRESS` | Required. Transactional email |
| `ANTHROPIC_API_KEY`, `OPENAI_API_KEY` | Not needed before Stage 7 |

Secrets never appear in source, images, or logs.

---

## Contributing

Trunk-based development. `main` is the trunk; work happens on short-lived branches behind pull
requests. Commit format:

```text
{type}({task-id}): {description}
```

where type is one of `feat`, `fix`, `test`, `refactor`, `docs`, `chore` — for example
`feat(IDN-001): add RegisterCommand handler`. One task per commit.

No implementation begins without a specification that authorises it. Before starting a task, apply
the Freshness Protocol described in `sdd_strategy/README.md`.
