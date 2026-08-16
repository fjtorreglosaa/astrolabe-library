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

## Do not suggest

- Magic numbers or hardcoded configuration values
- Direct database access from controllers
- Pipeline behaviors, or a `LoggingBehavior` class
- Nullable reference type suppressions (`!`) without a comment explaining why
- Architectural patterns not described in `global_tech_spec.md`
- Secrets, API keys, or connection strings in committed files

## When in doubt

Suggest reading the relevant spec file rather than guessing.
State explicitly: "This is not in the spec — should we add it first?"
