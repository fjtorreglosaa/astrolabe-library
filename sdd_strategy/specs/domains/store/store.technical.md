# Store — Technical Specification

**Last reviewed:** 2026-08-15
**Reviewed by:** Francisco Torregrosa
**Version:** 0 — placeholder, authored during PLAN-001 Stage 5
**Implements:** *to be listed once `store.business.md` defines its `BR-STR-*` rules*

> **PLACEHOLDER.** This file carries every required section with guidance on what belongs in each.
> It is authored **after** `store.business.md`, per SDD+ Principle 2 — you cannot make a good
> technical decision without understanding the business rule it implements.


---

## 1. Domain Model

Aggregates, entities, value objects, and domain events. Include **code signatures, not full
implementations** — a spec describes shape, not body. Explain why each aggregate boundary was drawn
where it is.

*To be authored.*

---

## 2. Application Layer

Every command and query this domain exposes, one entry each:

```text
Name:             {CommandName / QueryName}
Type:             Command / Query
Input:            {parameters}
Output:           Result / Result<T>
Business rule:    BR-STR-{NNN}
Handler location: {file path}
```

Conventions, per `global_tech_spec.md` §3: commands return `Task<Result>`, queries return
`Task<Result<T>>`, validation runs **inside the handler**, dispatch is through `ISender`, and
`CancellationToken` is the last parameter and is propagated everywhere.

*To be authored.*

---

## 3. Infrastructure

Repository interfaces, EF Core configurations, and external service clients, and how each connects to
the domain model. Fluent API only — no data annotations on domain entities.

*To be authored.*

---

## 4. Architecture Decision Log

Per SDD+ §5.2 this file **is** the ADR collection for this domain. Every entry must carry rejected
alternatives — a decision without them is not a decision, it is a default.

| Decision | Choice | Rationale | Alternatives rejected |
|---|---|---|---|
| *To be authored.* | | | |

---

## 5. Dependencies

**This domain depends on:** *to be authored.*

**Domains that depend on this one:** *to be authored.*

**External services:** *to be authored, with justification.*

---

## 6. Known Constraints and Limitations

Technical debt, known issues, and intentional simplifications with their justification.

*To be authored.*

---

## 7. Superseded Decisions

Decisions that changed. Record the old decision, the reason for the change, and the date.
**Never delete — always move here.**

| Decision | Superseded by | Reason | Date |
|---|---|---|---|
| — | — | None yet | — |
