# Astrolabe Books

A membership platform for a network of physical libraries. Members reserve and buy physical books
across the libraries their plan reaches; library staff run the branches assigned to them.

**Status:** PLAN-001 stages 0–9 implemented — identity, network, membership, catalog, reservations,
billing, store, recommendations, support and notifications, plus the administration surfaces. Two
items remain open: the Stage 8 test coverage gap (`GLOBAL-024`, `GLOBAL-025`) and interface parity
with the prototype (`GLOBAL-026`). See
`sdd_strategy/specs/plans/PLAN-001_astrolabe-books-mvp.md`.

---

## Run it

### Requirements

**Docker Desktop, and nothing else.** No .NET SDK, no Node, no PostgreSQL client — the whole stack
builds and runs inside containers. You need a Docker with the `docker compose` subcommand (Compose
v2 or later, not the old `docker-compose` script).

The first build downloads the .NET SDK and Node images and every package: budget around 10 minutes
and ~4 GB. Later builds reuse the layer cache and take seconds.

### Three steps

```bash
git clone https://github.com/fjtorreglosaa/astrolabe-library.git
cd astrolabe-library
cp .env.example .env
```

Open `.env` and set the five values Compose requires. **No real Mailgun account is needed** — the
API checks these are present and well-formed, not that they work, and nothing you need in order to
explore the app sends an email. This is enough:

```dotenv
POSTGRES_PASSWORD=change-me-locally
JWT_SIGNING_KEY=at-least-thirty-two-characters-long-change-me
MAILGUN_API_KEY=dummy-key-not-real
MAILGUN_DOMAIN=sandbox.example.org
MAILGUN_FROM_ADDRESS=no-reply@sandbox.example.org
```

Everything else in `.env.example` has a working default. Then:

```bash
docker compose up -d --build
```

The stack is ready when `docker compose ps` shows all three services `healthy`. `web` starts last on
purpose: it waits for `api`, which waits for the database to accept connections.

### What happens on the first start

The API applies migrations and runs the seeders as an explicit startup step — you do not run
anything by hand. On an empty database, `docker compose logs api` shows:

```text
[INF] Applying 16 pending migration(s): 20260816003536_InitialCreate, ...
[INF] Migrations applied successfully.
[INF] Network seeded: 6 countries, 18 cities, 35 libraries inserted.
[WRN] Seeded 3 demo account(s) with a shared, publicly known password.
[WRN] Seeded 11 demo directory user(s) with a shared, publicly known password.
[INF] Seeded 12 book(s) into the catalogue.
[INF] Now listening on: http://[::]:8080
```

Two things there look alarming and are not. The `[ERR] Failed executing DbCommand ... FROM
astrolabe.__migrations_history` lines at the very top are EF Core checking which migrations have run
before the history table exists — expected on a first start, gone on every subsequent one. The two
`[WRN]` lines are a deliberate warning that development-only accounts were created.

Seeders are idempotent: they insert only what is missing, so they run on every start and do nothing
after the first.

### Sign in

Three demo accounts are seeded from the approved prototype, all sharing one password:

| Email | Password | What you see |
|---|---|---|
| `fjtorreglosaa@gmail.com` | `Testing1234*` | A member on the Plus plan, based in New York |
| `admin@astrolabe.co` | `Testing1234*` | An administrator managing the Midtown and Harlem libraries |
| `super@astrolabe.co` | `Testing1234*` | A super administrator, unrestricted across the network |

They are pre-verified, so you never need to receive a confirmation email to sign in.

Alongside them the seed creates **11 directory members** — Alice Nakamura, Tomás Iriarte, Grace
Abbott and others — deliberately spread across plans, cities and statuses (active, blocked, pending
verification, deleted) so the administrator's user directory has something real to show. They share
the same password.

**Development only.** The seeders refuse to run unless `ASPNETCORE_ENVIRONMENT` is `Development`,
because that password is public knowledge in this repository.

### Where things run

| Service | URL | Notes |
|---|---|---|
| Web | http://localhost:5173 | React single-page application |
| API | http://localhost:5080 | ASP.NET Core |
| Swagger | http://localhost:5080/swagger | Development only |
| Health — liveness | http://localhost:5080/health/live | Process is up |
| Health — readiness | http://localhost:5080/health/ready | Process is up **and** the database is reachable |
| PostgreSQL | localhost:5432 | Credentials come from `.env` |

Change `WEB_PORT`, `API_PORT` or `DB_PORT` in `.env` if a port is taken. The web image is built
against `API_PORT`, so after changing it you must rebuild, not just restart.

Stop with `docker compose down`, or `docker compose down -v` to also drop the database volume and
start over from an empty schema.

### What will not work without real credentials

Neither of these blocks you from using the application.

| Feature | Needs | Without it |
|---|---|---|
| Confirmation, password reset and notification emails | A real Mailgun account | The request succeeds, the email is never delivered |
| AI recommendations | An `ANTHROPIC_API_KEY` or `OPENAI_API_KEY`, configured per library | Generation is refused; nothing else changes |

**Stuck?** [`GETTING_STARTED.md`](GETTING_STARTED.md) has the long-form walkthrough and a
troubleshooting section covering port clashes, options-validation failures, and the case where a
failed rebuild silently leaves the old container running.

---

## How the app works

Two facts about a user are deliberately kept separate: their **role**, which says what they are
authorised to do, and their **plan**, which says what they bought. Roles are `Member`, `Admin` and
`SuperAdmin`. Plans are `Basic`, `Plus` and `Max`, and live on the subscription.

### Plans

| Plan | Price | Borrowing reach | Book tiers | Purchase discount | Reward points | AI recommendations |
|---|---|---|---|---|---|---|
| Basic | $0.00 | Home library only | `Basic` titles only | — | — | — |
| Plus | $6.99/mo | Every library in the member's city | All | 10%, books in their city | — | Yes |
| Max | $12.99/mo | The whole network | All | 15%, any city | Yes | Yes |

Every plan may **browse** the entire network. Reach restricts borrowing, never discovery. A home
library is derived automatically from the member's city of residence. Upgrades take effect
immediately; downgrades wait for the renewal date.

### The main flow

A **book** is the bibliographic work; a **copy** is a physical instance belonging to one library, and
a copy is what gets reserved. A member browses the catalogue, reserves a copy from a library their
plan reaches, and returns it. A late return or a lost copy accrues a **fine**. Fines, payments and
reward points are never mutated in place — they are entries in an immutable ledger. Payments can be
made online or as a **desk payment** in cash or card at a counter, identified by a payment code an
administrator validates.

Separately, members can **buy** books from the store at their plan's discount, and Max members earn
and redeem reward points (one point = one cent). Plus and Max members get AI-generated
recommendations. Members raise support **tickets** against a library; staff handle them.

### Staff

An administrator acts only on the libraries a super administrator has explicitly assigned to them —
that boundary is enforced in the backend, and the frontend guards exist only to improve the
experience. Only a super administrator appoints administrators. Every administrative operation
writes an audit entry recording user, action, entity and timestamp.

### Rules that hold everywhere

Money is always an integer number of cents, never a floating point type, in any layer including the
database. Timestamps are persisted in UTC and localised only in the client. A member holds exactly
one active plan at a time, with full history. Operations with financial or inventory effects accept
an idempotency key and never apply twice. The interface language is English.

The authoritative glossary and the cross-domain rules are in `sdd_strategy/specs/global_spec.md`;
each domain's numbered `BR-*` rules are in its own `business.md`.

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

Only needed if you are changing code and want a fast edit-run loop. Requirements: **.NET SDK 10.0**
and **Node 24**. Keep the database in Docker with `docker compose up -d db`.

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
npm run build        # tsc -b && vite build — the check that matters
```

Use `npm run build`, not `tsc --noEmit`. It runs `tsc -b`, whose project-reference settings are
stricter, and it has caught type errors that `--noEmit` let through.

### Package versions

All NuGet versions are declared in `Directory.Packages.props` and nowhere else. Individual `.csproj`
files reference packages without a version.

`NuGet.config` clears whatever feeds are configured on your machine and pins nuget.org as the only
source. Without it, a developer with a private feed configured globally — an Azure DevOps Artifacts
feed, say — would silently restore this project through it, which makes builds non-reproducible and
fails confusingly when those unrelated credentials expire. It makes the build more portable, not
less; leave it alone unless you add a package that is not on nuget.org.

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
| `JWT_SIGNING_KEY` | Required. At least 32 characters. Signs access tokens |
| `POSTGRES_DB`, `POSTGRES_USER` | Database name and role |
| `API_PORT`, `WEB_PORT`, `DB_PORT` | Host port mappings. Rebuild after changing `API_PORT` |
| `ASPNETCORE_ENVIRONMENT` | `Development` enables Swagger, verbose logging and the demo account seed |
| `MAILGUN_API_KEY`, `MAILGUN_DOMAIN`, `MAILGUN_FROM_ADDRESS` | Required. Validated for presence and format at startup, not against Mailgun — placeholders work locally |
| `ANTHROPIC_API_KEY`, `OPENAI_API_KEY` | Optional. Without one, recommendation generation is refused and nothing else changes |

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

---

## Appendix: have an AI set it up for you

If you would rather not run the commands yourself, paste the prompt below into an AI coding
assistant that can execute shell commands in a terminal — Claude Code, Cursor, GitHub Copilot in
agent mode, or similar. Nothing in it is destructive: it clones into a new directory and starts
containers.

````text
Set up and run the Astrolabe Books project on my machine, then tell me how to open it.

Repository: https://github.com/fjtorreglosaa/astrolabe-library.git

Do this:

1. Check that Docker is installed and running (`docker compose version`). If it is not,
   stop and tell me what to install — do not try to install it yourself.
2. Clone the repository into a new directory and cd into it.
3. Copy `.env.example` to `.env` and fill in the five values Compose requires. Generate a
   real random value for POSTGRES_PASSWORD and JWT_SIGNING_KEY (`openssl rand -base64 24`
   and `openssl rand -base64 48`). For the three Mailgun values use placeholders — no real
   account is needed:
       MAILGUN_API_KEY=dummy-key-not-real
       MAILGUN_DOMAIN=sandbox.example.org
       MAILGUN_FROM_ADDRESS=no-reply@sandbox.example.org
   Leave every other variable at its default.
4. If ports 5173, 5080 or 5432 are already in use on my machine, change WEB_PORT, API_PORT
   or DB_PORT in `.env` instead of stopping whatever is using them, and tell me the new ports.
5. Run `docker compose up -d --build`. The first build takes several minutes.
6. Wait until `docker compose ps` reports all three services healthy.
7. Verify it actually works, and show me the output. Use the API_PORT from `.env` — the
   commands below assume the default 5080:
       curl -s -o /dev/null -w '%{http_code}\n' http://localhost:5080/health/ready
       curl -s -X POST http://localhost:5080/api/v1/auth/sign-in \
         -H 'Content-Type: application/json' \
         -d '{"email":"fjtorreglosaa@gmail.com","password":"Testing1234*"}'
   The first must print 200 and the second must return an access token.
8. Tell me the URL to open and the demo accounts I can sign in with.

Notes so you do not misread the logs: the API applies its migrations and runs its seeders
automatically at startup, so there is nothing to run by hand. On the first start the logs
contain two `[ERR] Failed executing DbCommand ... FROM astrolabe.__migrations_history`
lines — that is EF Core checking a history table that does not exist yet, and it is
expected. Two `[WRN]` lines about seeded accounts with a publicly known password are also
expected: those are the development demo accounts.

If a step fails, show me the actual error output rather than guessing. The repository's
README.md and GETTING_STARTED.md have a troubleshooting section.
````
