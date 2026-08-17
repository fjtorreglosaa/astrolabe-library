# Getting started

How to get Astrolabe Books running on a machine that has never seen it before.

Everything here was verified on 2026-08-16 by cloning this repository from scratch into an empty
directory and following these steps literally. If a step does not work for you, it is a bug in this
document — please fix it.

> Looking for architecture, conventions or the specification process instead? See [`README.md`](README.md).

---

## 1. What you need

**Docker Desktop, and nothing else.** No .NET SDK, no Node, no PostgreSQL client. The whole stack
builds and runs inside containers.

```bash
docker --version
docker compose version
```

Any Docker with a `docker compose` subcommand — that is, Compose v2 or later, not the old
`docker-compose` script — should work. Verified on Docker 29.3.1 with Compose v5.1.0.

You only need the .NET SDK and Node if you want to run the code *outside* containers — see
[§8](#8-working-outside-docker-optional).

**Disk and time.** The first build downloads the .NET SDK image, the Node image and every package.
Budget around 10 minutes and ~4 GB. Later builds reuse the layer cache and take seconds.

---

## 2. Three steps

```bash
git clone https://github.com/fjtorreglosaa/astrolabe-library.git
cd astrolabe-library
cp .env.example .env
# now edit .env — see the next section, five values are required
docker compose up -d --build
```

The stack is ready when all three services report `healthy`:

```bash
docker compose ps
```

`web` starts last on purpose: it waits for `api` to be healthy, which in turn waits for the database
to accept connections.

---

## 3. Filling in `.env`

`.env` is git-ignored and never committed. Copy it from `.env.example` and set **five** values —
Compose refuses to start without them, by design, rather than booting into a half-configured state.

| Variable | What to put |
|---|---|
| `POSTGRES_PASSWORD` | Anything. Generate one: `openssl rand -base64 24` |
| `JWT_SIGNING_KEY` | At least 32 characters. Generate one: `openssl rand -base64 48` |
| `MAILGUN_API_KEY` | Any non-empty string is fine for local work — see below |
| `MAILGUN_DOMAIN` | Any non-empty string, e.g. `sandbox.example.org` |
| `MAILGUN_FROM_ADDRESS` | Must *look* like an email, e.g. `no-reply@sandbox.example.org` |

### You do not need a real Mailgun account

The API validates these four Mailgun settings at startup — it checks they are present and
well-formed, not that they are real. **Placeholder values let the whole application run**; only
the act of sending an email fails, and nothing you need in order to explore the app sends one.

A working `.env` for a newcomer, ready to paste:

```dotenv
POSTGRES_PASSWORD=change-me-locally
JWT_SIGNING_KEY=at-least-thirty-two-characters-long-change-me
MAILGUN_API_KEY=dummy-key-not-real
MAILGUN_DOMAIN=sandbox.example.org
MAILGUN_FROM_ADDRESS=no-reply@sandbox.example.org
```

`MAILGUN_FROM_ADDRESS` is validated as an email address, so `not-an-email` fails startup while
`no-reply@sandbox.example.org` passes. That is the only formatting constraint of the five.

Everything else in `.env.example` already has a working default and can be left alone.

---

## 4. Where things run

| What | URL |
|---|---|
| Web application | http://localhost:5173 |
| API | http://localhost:5080 |
| Swagger | http://localhost:5080/swagger |
| Health — liveness | http://localhost:5080/health/live |
| Health — readiness (includes the database) | http://localhost:5080/health/ready |
| PostgreSQL | `localhost:5432`, credentials from `.env` |

Change `WEB_PORT`, `API_PORT` or `DB_PORT` in `.env` if any of those are taken. The web container
is built against `API_PORT`, so **after changing a port you must rebuild**, not just restart:
`docker compose up -d --build`.

---

## 5. Signing in

The database is seeded automatically with the three demo accounts from the approved prototype. They
all share one password:

| Email | Password | Role |
|---|---|---|
| `fjtorreglosaa@gmail.com` | `Testing1234*` | Member, Plus plan, New York |
| `admin@astrolabe.co` | `Testing1234*` | Administrator, manages Midtown and Harlem |
| `super@astrolabe.co` | `Testing1234*` | Super administrator |

These accounts are pre-verified, so **you never need to receive a confirmation email to sign in** —
which is why the placeholder Mailgun configuration is enough.

**Development only.** The seeder refuses to run unless `ASPNETCORE_ENVIRONMENT` is `Development`,
because the password is public knowledge in this repository. Seeding these into a deployed
environment would hand a super administrator to anyone who read the source.

---

## 6. What happens on the first start

The API applies migrations and seeds reference data as an explicit startup step. On an empty
database you should see, in `docker compose logs api`:

```text
[INF] Applying 16 pending migration(s): 20260816003536_InitialCreate, ...
[INF] Migrations applied successfully.
[INF] Network seeded: 6 countries, 18 cities, 35 libraries inserted.
[WRN] Seeded 3 demo account(s) with a shared, publicly known password.
[WRN] Seeded 11 demo directory user(s) with a shared, publicly known password.
[INF] Seeded 12 book(s) into the catalogue.
[INF] Now listening on: http://[::]:8080
```

Two things in that output look alarming and are not:

- **`[ERR] Failed executing DbCommand ... FROM astrolabe.__migrations_history`**, twice, right at
  the top. EF Core is checking which migrations have already run before the history table exists.
  It is expected on a first start and disappears on every subsequent one.
- **The two `[WRN]` seed lines.** They are a deliberate warning that development-only accounts were
  created, not a failure.

Seeders are idempotent — they insert only what is missing — so they run on every start and do
nothing after the first.

### A quick check that it really works

```bash
curl -s -o /dev/null -w '%{http_code}\n' http://localhost:5080/health/ready     # expect 200

curl -s -X POST http://localhost:5080/api/v1/auth/sign-in \
  -H 'Content-Type: application/json' \
  -d '{"email":"fjtorreglosaa@gmail.com","password":"Testing1234*"}'            # expect a JWT
```

---

## 7. What will not work without real credentials

Nothing below blocks you from using the application; each one degrades on its own.

| Feature | Needs | Without it |
|---|---|---|
| Registration confirmation, password reset, notification emails | A real Mailgun account | The request succeeds, the email is never delivered |
| AI recommendations | An `ANTHROPIC_API_KEY` or `OPENAI_API_KEY`, configured per library | Generation is refused; the rest of the app is unaffected |

If you do connect a real Mailgun sandbox domain, note that it only delivers to recipients you have
authorised in the Mailgun dashboard. A rejected recipient there is a configuration limit, not a bug.

---

## 8. Working outside Docker (optional)

Only needed if you are changing code and want a fast edit-run loop.

Requirements: **.NET SDK 10.0** and **Node 24**. You still need the database, which is easiest to
keep in Docker: `docker compose up -d db`.

```bash
# Backend
dotnet build
dotnet test
dotnet run --project src/backend/Astrolabe.Presentation
```

Set `ConnectionStrings__Database` when running outside Compose, for example:

```bash
export ConnectionStrings__Database="Host=localhost;Port=5432;Database=astrolabe;Username=astrolabe;Password=<your POSTGRES_PASSWORD>"
```

```bash
# Frontend
cd src/frontend/astrolabe-web
npm install
npm run dev          # http://localhost:5173
npm test
npm run build        # tsc -b && vite build — use this, not `tsc --noEmit`
```

`npm run build` is the check that matters. It runs `tsc -b`, which applies stricter project-reference
settings than a bare `--noEmit`, and it has caught type errors that `--noEmit` let through.

---

## 9. About `NuGet.config`

Short answer: **leave it alone, it will not cause you problems.** It exists to prevent one.

It does two things: `<clear />` discards whatever NuGet feeds are configured on your machine, and it
then adds nuget.org as the only source.

Without it, a developer who has a corporate or private feed configured globally — an Azure DevOps
Artifacts feed, for instance — would silently restore this project's packages through that feed.
That makes restores non-reproducible from one machine to the next, and it can fail confusingly for
anyone whose credentials for that feed have expired, on a project that has nothing to do with it.

So the file makes the build *more* portable, not less. Every package this project uses comes from
public nuget.org. The Docker build copies it in deliberately, and the restore inside the container
depends on it.

The one situation where you would touch it: if you ever add a package that is not on nuget.org, it
needs its own `<add key=... />` entry plus a `packageSourceMapping` pattern. Until then, there is no
reason to.

Related: package **versions** are declared centrally in `Directory.Packages.props` and nowhere else.
Individual `.csproj` files reference packages without a version. Do not add one there.

---

## 10. Troubleshooting

### `error while interpolating services.db.environment: required variable POSTGRES_PASSWORD is missing`

You skipped `cp .env.example .env`, or left one of the five required values empty. See
[§3](#3-filling-in-env). The same message appears for `JWT_SIGNING_KEY`, `MAILGUN_API_KEY`,
`MAILGUN_DOMAIN` and `MAILGUN_FROM_ADDRESS`.

### `Bind for 0.0.0.0:5432 failed: port is already allocated`

Something else — very likely a local PostgreSQL — holds the port. Change `DB_PORT` in `.env`. Same
for `API_PORT` and `WEB_PORT`; rebuild after changing `API_PORT`.

### Options validation failed for `MailgunOptions`

One of the four Mailgun values is empty or malformed. `MAILGUN_FROM_ADDRESS` must parse as an email
address and `MAILGUN_BASE_URL` as a URL. See [§3](#3-filling-in-env).

### The app still behaves like the old code after a rebuild

`docker compose up -d --build` **leaves the previous image in place if the build fails**, and the old
container keeps passing its health check, so nothing looks wrong. Confirm the container was actually
replaced:

```bash
docker inspect astrolabe-api --format '{{.State.StartedAt}}'
```

If that timestamp is older than your rebuild, the build failed — scroll back through its output.

### Start over from an empty database

```bash
docker compose down -v      # -v also drops the database volume
docker compose up -d --build
```

Migrations and seeding will run again from scratch.

### Everything else

```bash
docker compose logs -f api     # or db, or web
docker compose ps              # health of each service
```
