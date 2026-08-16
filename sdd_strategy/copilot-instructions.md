# Astrolabe Books — GitHub Copilot Instructions
# SDD+ Methodology v1.0

You are assisting with a project that follows SDD+.
Read `sdd_strategy/README.md` before suggesting any code.

## Before any suggestion

1. Is this task described in a spec file? If not, suggest updating the spec first.
2. Are the related spec files fresh (reviewed within 7 days)?
3. Does this suggestion violate any architecture rule?

## Authority precedence

1. The prototype in `docs/design/` — product behaviour, UI, copy, business rules
2. `SDD_PLIUS_STRATEGY.md` — process, structure, spec format, agent rules
3. `GUIDELINES.md` — architecture and stack where the two above are silent

Never invent product behaviour. The real rules, copy, and seed data are in
`docs/design/prototype.source.js`.

## Architecture rules

- Domain layer: no external dependencies of any kind, zero external NuGet packages
- Application layer: Commands return `Task<Result>`, Queries return `Task<Result<T>>`
- Validate inside handlers — suggest no pipeline behavior classes
- `ISender` for dispatch by default, not `IMediator`, unless `Publish()` is needed
- `CancellationToken` is always the last parameter and is propagated to every async call
- The presentation project is `Astrolabe.Presentation`, not `Astrolabe.Api`

## Project rules

- Every monetary amount is an integer number of cents. Never decimal, float, or double for money.
- EF Core: Fluent API configurations only, no data annotations on domain entities
- The prototype does not use Material UI — never copy its inline styles into the codebase
- All repository content is written in English

## Organisation

- One public type per file, named after the type. Never group interfaces or entities together
- Namespaces match folders. Segregate by kind: `Entities/`, `ValueObjects/`, `Events/`, `Errors/`, `Repositories/`
- Generic persistence lives on `IRepository<TEntity>`; concrete contracts extend it
  (`IReservationRepository : IRepository<Reservation>`) and add only domain-specific methods

## Feature folders

- Application and Infrastructure group every domain under `Features/`
- Persistence groups configurations and repositories by domain too
- External providers live under `Infrastructure/Integrations/{Provider}/`

## Unit of work

- Each bounded context has its own unit of work: `IIdentityUnitOfWork : IUnitOfWork`
- Handlers depend on the unit of work, never on repositories directly
- Repositories never call `SaveChanges` themselves

## Domain events

- Aggregates raise events; `SaveChangesAsync` dispatches them after the commit
- Anything that must happen on every occurrence of a state change is an event handler, not a call in
  each handler
- A feature contains only Commands, Queries and Events — no Services, Helpers or Common folders

## Verify against the running system

A green test suite is not evidence that a feature works. Exercise the endpoint before calling a task
done. Two Stage 2 defects passed every unit test and were found only with `curl`: a value converter
that broke catalogue search, and a seeder projection that crashed the API at startup.

- Verify the frontend with `npm run build`, never `tsc --noEmit` — the build script is
  `tsc -b && vite build` and is stricter
- A container reporting `healthy` is not evidence it runs your code: a failed `--build` leaves the
  previous image serving. Confirm the image was replaced after a rebuild

## Persisting value objects

- Map a value object as an **owned type** (class) or **complex type** (struct, such as `Money`),
  never with a value converter, when any of its members is read, filtered, ordered or aggregated in
  a query. A converter makes `book.Isbn.Value` untranslatable — it compiles and throws at run time

## Audit

- `AuditEntry` lives under `Domain/Features/Audit/`, reached through `IAuditUnitOfWork`. Never inject
  `IIdentityUnitOfWork` just to write an audit row
- Write the entry inside the command handler, in the same transaction. Never in an event handler

## Do not suggest

- Magic numbers or hardcoded configuration values
- Direct database access from controllers
- Several public types in one file
- Generic methods redeclared on a concrete repository interface
- A domain folder placed directly under Domain/, Application/ or Infrastructure/
- A Services, Helpers, Utils or Common folder inside a feature
- A repository injected directly into a handler
- `IDbContextFactory` injected into a request-scoped handler — it breaks the unit of work
- Pipeline behaviors, or a `LoggingBehavior` class
- Nullable reference type suppressions (`!`) without a comment explaining why
- Architectural patterns not described in `global_tech_spec.md`
- Secrets, API keys, or connection strings in committed files
- A value converter on a value object whose members are filtered, projected or aggregated on
- A date rendered to a member without an explicit time zone

## Committing

- A stage is not finished until it is committed, and the user must approve the commit first
- Never stage `.env` or anything holding a real key, password or connection string
- Pushing needs its own approval

## When in doubt

Suggest reading the relevant spec file rather than guessing.
State explicitly: "This is not in the spec — should we add it first?"
