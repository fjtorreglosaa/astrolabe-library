# SDD+ — Specification and Domain-Driven Development Plus
**Version:** 1.0  
**Author:** Francisco Torregrosa  
**Status:** General methodology — applicable to any software project

---

## Table of Contents

1. [What is SDD+](#1-what-is-sdd)
2. [Theoretical Foundations](#2-theoretical-foundations)
3. [Core Principles](#3-core-principles)
4. [Repository Structure](#4-repository-structure)
5. [The Three Spec Files](#5-the-three-spec-files)
6. [Domain Structure and Growth Rules](#6-domain-structure-and-growth-rules)
7. [The Freshness Protocol](#7-the-freshness-protocol)
8. [Documentation Standards](#8-documentation-standards)
9. [Technology Stacks](#9-technology-stacks)
10. [AI Agent Rules](#10-ai-agent-rules)
11. [External Task Tracking Integration](#11-external-task-tracking-integration)
12. [The Central Strategy Repository](#12-the-central-strategy-repository)
13. [Plans — Architecture and Infrastructure Changes](#13-plans--architecture-and-infrastructure-changes)
14. [Working with SDD+ Day to Day](#14-working-with-sdd-day-to-day)
15. [Bootstrap Prompt](#15-bootstrap-prompt)

---

## 1. What is SDD+

**SDD+** (Specification and Domain-Driven Development Plus) is a software development methodology designed for teams that use AI coding agents — Cursor, GitHub Copilot, Claude, ChatGPT, or any equivalent — as primary development tools alongside human developers.

Its central premise is simple:

> **No implementation code is written without a specification that authorises it. The AI agent operates as a controlled executor of the specs, not as a decision-maker.**

SDD+ is not a replacement for agile methodologies. It is a layer that sits on top of any delivery framework (Scrum, Kanban, SAFe) and governs how specifications are created, maintained, and consumed — by both humans and AI agents.

### 1.1 The problem it solves

| Problem | SDD+ solution |
|---|---|
| AI agents invent code that was never requested | Agents can only implement what is explicitly specified |
| Technical decisions are undocumented and tribal | Every decision lives in `technical.md` with explicit rationale |
| Business rules live only in developers' heads | Every rule lives in `business.md`, versioned and reviewable |
| Specs go stale and become misleading | The Freshness Protocol enforces mandatory review before every task |
| Integration between projects is undocumented | The `docs/` folder is a machine-readable contract between systems |
| Onboarding new developers takes weeks | The complete project context is in three files per domain |
| Architecture grows without intentional design | Growth triggers are defined and create review tasks automatically |

### 1.2 What SDD+ is NOT

- A replacement for proper engineering judgment — humans make the important decisions
- A guarantee that AI-generated code is correct
- A project management tool — it integrates with one, but does not replace it
- A methodology only for AI-assisted teams — it works equally well for fully human teams

---

## 2. Theoretical Foundations

SDD+ draws from several established disciplines. Understanding these is not required to use SDD+, but it explains why the methodology is structured as it is.

### 2.1 Specification-Driven Development (SDD)

The parent methodology. SDD establishes that software specifications should be the authoritative source of truth, not the code itself. Code is the implementation of a spec. If they diverge, the spec wins and the code must be corrected.

SDD+ extends traditional SDD by making specs machine-readable for AI agents, adding freshness enforcement, and structuring them by business domain rather than by technical feature.

### 2.2 Domain-Driven Design (DDD)

Introduced by Eric Evans in *Domain-Driven Design: Tackling Complexity in the Heart of Software* (2003). Key concepts SDD+ uses:

**Bounded Context** — A clearly defined boundary within which a specific domain model applies. In SDD+, each bounded context has its own three spec files. This prevents different teams from using the same word to mean different things.

**Ubiquitous Language** — A shared vocabulary between business and technical people, used consistently in conversation, documentation, and code. In SDD+, `business.md` defines this vocabulary for each domain.

**Aggregate** — A cluster of domain objects treated as a single unit for data changes. Aggregate design is documented in `technical.md`.

**Domain Events** — Facts about things that happened in the domain, used to communicate between bounded contexts.

### 2.3 CQRS — Command Query Responsibility Segregation

A pattern that separates read operations (Queries) from write operations (Commands). In SDD+, every application layer operation is either a Command (write, returns `Result`) or a Query (read, returns `Result<T>`). This separation makes handlers testable, predictable, and easy to document in specs.

The specific CQRS library used depends on the technology stack. SDD+ is library-agnostic at the methodology level.

### 2.4 Architecture Decision Records (ADR)

A practice documented by Michael Nygard where every significant architectural decision is recorded with its context, the decision made, and its consequences. In SDD+, the `technical.md` file for each domain serves as a living ADR collection. Every decision entry explains: what was decided, why, and what alternatives were considered and rejected.

### 2.5 Conway's Law

*"Any organisation that designs a system will produce a design whose structure is a copy of the organisation's communication structure."* — Melvin Conway, 1967.

SDD+ uses this law deliberately: domain boundaries in the specs should reflect how the business actually talks about its own problems — not how the database is structured or how services happen to be deployed. If the business says "Payments" and "Disbursements" are different things, they are different domains in the specs, even if they share a database table.

### 2.6 Freshness-First Documentation

A principle from knowledge management: documentation has a half-life. The longer since it was last verified against reality, the less reliable it is. SDD+ formalises this with the Freshness Protocol: specs not reviewed in more than seven days are treated as potentially stale and must be verified before any agent acts on them.

---

## 3. Core Principles

These seven principles govern every decision in SDD+. When in doubt about how to apply the methodology, return to these.

**Principle 1 — Spec before code**  
No implementation begins without a spec that authorises it. This applies to humans and AI agents equally.

**Principle 2 — Business first, technical second**  
`business.md` is always written before `technical.md`. You cannot make a good technical decision without understanding the business rule it implements.

**Principle 3 — Decisions have rationale**  
Every technical decision in `technical.md` must explain *why* it was made, not just *what* was decided. A decision without rationale is indistinguishable from an accident.

**Principle 4 — Freshness is mandatory**  
Stale specs are worse than no specs. Before any task is executed, the agent verifies the freshness of all related spec files. If specs are stale, they are updated before implementation begins.

**Principle 5 — Domains grow, and growth has consequences**  
When a domain exceeds the complexity threshold, it must be split. This is not optional — it creates a task automatically in `global_task_spec.md`. The agent proposes the split; the human approves it.

**Principle 6 — Documentation is a contract**  
The `docs/` folder is not for humans only. It is a machine-readable contract that other projects and systems consume. Everything in `docs/` must be accurate, current, and follow the standard format.

**Principle 7 — AI agents are controlled executors**  
Agents do not decide what to build, how to architect it, or when to change direction. They execute what the specs define. Specs are written by humans. Agents verify specs, implement tasks, and update tracking — nothing more without explicit human approval.

---

## 4. Repository Structure

### 4.1 Directory layout

Every repository following SDD+ has this structure at its root:

```
{repository-root}/
├── ssd_strategy/
│   ├── README.md                          ← Entry point — agents read this first
│   ├── .cursorrules                       ← Rules for Cursor agent
│   ├── copilot-instructions.md            ← Rules for GitHub Copilot
│   ├── specs/
│   │   ├── global_spec.md                 ← Project-wide business context and scope
│   │   ├── global_tech_spec.md            ← Technology stack and global arch decisions
│   │   ├── global_task_spec.md            ← Cross-domain tasks and global backlog
│   │   ├── plans/                         ← Architecture change plans
│   │   │   └── PLAN-{NNN}_{title}.md
│   │   └── domains/
│   │       ├── {domain_name}/
│   │       │   ├── {domain}.business.md
│   │       │   ├── {domain}.technical.md
│   │       │   └── {domain}.tasks.md
│   │       └── {large_domain}/            ← Domain split into subdomains
│   │           ├── {large_domain}.overview.md
│   │           └── subdomains/
│   │               ├── {subdomain_a}/
│   │               │   ├── {subdomain_a}.business.md
│   │               │   ├── {subdomain_a}.technical.md
│   │               │   └── {subdomain_a}.tasks.md
│   │               └── {subdomain_b}/
│   │                   ├── {subdomain_b}.business.md
│   │                   ├── {subdomain_b}.technical.md
│   │                   └── {subdomain_b}.tasks.md
│   └── docs/
│       ├── project_contract.md            ← Machine-readable contract for other projects
│       ├── api/                           ← Internal API documentation
│       ├── integrations/                  ← External service documentation
│       └── components/                   ← Shared component documentation
└── {source_code}/
```

### 4.2 File naming conventions

| File | Convention | Example |
|---|---|---|
| Domain spec files | `{domain}.{type}.md` | `payments.business.md` |
| Plan files | `PLAN-{NNN}_{kebab-title}.md` | `PLAN-001_split-payments-domain.md` |
| Cursor rules | `.cursorrules` | Always this exact name |
| Copilot rules | `copilot-instructions.md` | Always this exact name |
| Project contract | `project_contract.md` | Always this exact name |

### 4.3 The README.md — Agent entry point

The first file any agent reads before doing anything else. Required content:

```markdown
# {Project Name} — SDD+ Strategy

**Methodology:** SDD+ v{version}
**Project type:** {type}
**Last strategy review:** {date}

## What this project does
{One paragraph. Plain language. No technical jargon.}

## Technology stack
{Bullet list of core technologies}

## Domain map
{List of all current domains with one-line description each}

## Agent operating rules
Before taking any action, read:
1. This file
2. `specs/global_spec.md`
3. `specs/global_tech_spec.md`
4. The relevant domain spec files for this task
5. Apply the Freshness Protocol (see SDD+ methodology Section 7)

## Key constraints
{Any project-specific rules that override or extend the defaults}
```

---

## 5. The Three Spec Files

Every domain has exactly three spec files. Global scope also has exactly three. Always created in this order: **business first, technical second, tasks third.**

### 5.1 `{domain}.business.md` — The Business Spec

**Purpose:** Documents what the domain does, why it exists, and what rules govern it. Written in language that a non-technical product owner can read and verify.

**Who writes it:** Product owner or business analyst, reviewed by the lead developer.  
**Who reads it:** Everyone — developers, AI agents, QA, stakeholders.

**Required sections:**

```markdown
# {Domain} — Business Specification
**Last reviewed:** {date}          ← MANDATORY — used by Freshness Protocol
**Reviewed by:** {name}
**Version:** {n}

## 1. Purpose
{What this domain is responsible for. One paragraph.}

## 2. Glossary
{Every domain-specific term defined precisely.
 If a term means something different here than elsewhere, say so explicitly.}

## 3. Business Rules
{Numbered: BR-{DOMAIN}-{NNN} — {Rule description}
 Each rule must be a complete, unambiguous statement.
 Rules must be independently testable.}

## 4. Acceptance Criteria
{AC-{DOMAIN}-{NNN} — {Criterion}
 Maps to business rules. Used for test definition.}

## 5. Edge Cases
{Table of non-obvious scenarios and their expected behaviour.}

## 6. Out of Scope
{Explicit list of what this domain does NOT handle.
 This is as important as what it does handle.}
```

**Writing rules:**
- No code, no SQL, no technical implementation details
- Use "must" not "should" — a rule is a rule, not a suggestion
- Every business rule gets a unique ID (`BR-DOMAIN-NNN`) that **never changes**, even if the rule text changes — update the rule, keep the ID
- If a business rule does not fit in one sentence, it is probably two rules
- Out of Scope is mandatory — ambiguity about boundaries is the most common source of domain conflicts

### 5.2 `{domain}.technical.md` — The Technical Spec

**Purpose:** Documents how the domain is implemented and why each technical decision was made. Every decision must have explicit rationale. This file is the living Architecture Decision Record (ADR) for the domain.

**Who writes it:** Lead developer, reviewed by the team.  
**Who reads it:** Developers and AI agents only.

**Required sections:**

```markdown
# {Domain} — Technical Specification
**Last reviewed:** {date}          ← MANDATORY
**Reviewed by:** {name}
**Version:** {n}
**Implements:** [list of BR-* rules this spec covers]

## 1. Domain Model
{Aggregates, entities, value objects, domain events.
 Include code signatures — not full implementations.
 Explain why each aggregate boundary was drawn where it is.}

## 2. Application Layer
{Commands and queries defined for this domain.
 Format per entry:
   Name:             {CommandName / QueryName}
   Type:             Command / Query
   Input:            {parameters}
   Output:           Result / Result<T>
   Business rule:    BR-{DOMAIN}-{NNN}
   Handler location: {file path}}

## 3. Infrastructure
{Repository interfaces, ORM configurations, external service clients.
 Explain how each connects to the domain model.}

## 4. Architecture Decision Log
| Decision | Choice | Rationale | Alternatives rejected |
|---|---|---|---|

## 5. Dependencies
{Other domains this domain depends on.
 Other domains that depend on this one.
 External services and why.}

## 6. Known Constraints and Limitations
{Technical debt, known issues, intentional simplifications with justification.}

## 7. Superseded Decisions
{Decisions that were changed. Old decision, reason for change, date changed.
 Never delete — always move here.}
```

**Writing rules:**
- Every entry in the Architecture Decision Log must have "Alternatives rejected" — a decision without rejected alternatives is not a decision, it is a default
- Code signatures are encouraged; full implementations are not — specs describe shape, not body
- When a technical decision changes, the old entry moves to "Superseded Decisions" with the date and reason — it is never deleted

### 5.3 `{domain}.tasks.md` — The Tasks Spec

**Purpose:** Tracks every task that needs to be done, is in progress, or is complete for this domain. Single source of truth for domain-level work items.

**Who writes it:** Developers and AI agents, reviewed by the team.  
**Who reads it:** Developers, AI agents, team leads.

**Required sections:**

```markdown
# {Domain} — Tasks
**Last reviewed:** {date}          ← MANDATORY
**Overall progress:** {n}/{total} ({pct}%)

## Blocking Dependencies
| Block ID | Description | Status |
|---|---|---|

## Task List
| ID | Task | Status | Blocker | Tracking | Notes |
|---|---|---|---|---|---|
| {DOM}-{NNN} | {description} | {status} | {blocker or —} | {ref or —} | |

### Status values
⬜ Not started  
🔄 In progress  
✅ Done  
❌ Removed / not applicable (reason required in Notes)  
🔴 Blocked (blocker ID required)  

### Tracking reference format (when used)
{PLATFORM} #{ID} — {URL}

## Completion Log
| Date | Task ID | Completed by | Notes |
|---|---|---|---|

## Progress Summary
{Per-layer or per-category breakdown and overall total}
```

### 5.4 Global specs

The three global specs follow the same format as domain specs but cover the entire project:

**`global_spec.md`** — Project-wide business context, overall scope, cross-domain rules, authoritative glossary of terms used across domains.

**`global_tech_spec.md`** — Technology stack decisions, project structure, shared patterns (CQRS setup, ORM configuration, authentication strategy, testing conventions), global architecture decisions. This is where stack-wide rules live — domain `technical.md` files only document domain-specific decisions.

**`global_task_spec.md`** — Cross-domain tasks, infrastructure tasks, domain split evaluation tasks, plan execution tasks, and any task that does not belong to a single domain.

---

## 6. Domain Structure and Growth Rules

### 6.1 Starting a domain

A domain is created when a new bounded context is identified. It always starts with three files in `specs/domains/{domain_name}/`.

The domain name must come from the **business language** (ubiquitous language), not from technical concepts:
- ✅ `payments`, `disbursements`, `inventory`, `notifications`
- ❌ `payment_service_handler`, `db_manager`, `api_wrapper`

### 6.2 Growth thresholds

A domain must be evaluated for splitting when **any** of the following thresholds is exceeded:

| Indicator | Threshold | Action |
|---|---|---|
| Business rules (`BR-*` entries) | > 20 rules | Evaluate split |
| Total tasks in `tasks.md` | > 40 tasks | Evaluate split |
| Commands + Queries combined | > 15 | Evaluate split |
| Entities / Aggregates | > 8 | Evaluate split |
| `technical.md` file size | > 600 lines | Evaluate split |
| Team cognitive load | Team says "this domain is too big" | Evaluate split |

Exceeding a threshold does **not** trigger an automatic split. It triggers an automatic task in `global_task_spec.md`:

```markdown
| GLOBAL-{NNN} | Evaluate domain split: {domain_name} | ⬜ | — | — |
  Threshold exceeded: {which threshold and current value}.
  Agent must read all three domain spec files and propose subdomains.
  Human approval required before executing the split. |
```

### 6.3 The split process

**Step 1 — Analysis** (agent reads, does not write code or move files)  
Read all three spec files. Identify natural split points: groups of business rules that only reference each other, groups of commands/queries that share no entities with others, groups of tasks that are independent.

**Step 2 — Proposal** (agent creates `PLAN-{NNN}_split-{domain}.md`, waits for approval)  
Include: proposed subdomain names (from business language), which business rules go where, which technical components go where, which tasks go where, dependencies between proposed subdomains, estimated effort.

**Step 3 — Human approval**  
No code moves, no files change until explicit `"approved"` or `"proceed"` is given. Implicit approval is not accepted.

**Step 4 — Execution** (after approval)  
- Create subdomain folders and three spec files per subdomain
- Migrate content from the original domain
- Create `{domain}.overview.md` explaining the split and linking subdomains
- Update `global_spec.md` domain map
- Update `ssd_strategy/README.md` domain list
- Update cross-references in other domains

**Step 5 — Recursive growth**  
If a subdomain later exceeds a threshold, the same process repeats. More than three nesting levels is a signal that domain modelling needs to be reconsidered at the global level.

---

## 7. The Freshness Protocol

### 7.1 What is freshness?

Every spec file has a `Last reviewed` date in its header. A spec is **fresh** if reviewed within the last **7 calendar days**. A spec is **stale** if more than 7 days have passed without review.

Stale does not mean wrong. It means unverified. The agent must verify a stale spec against the current codebase before acting on it.

### 7.2 When the protocol runs

**Before every task execution — without exception.** The agent checks the `Last reviewed` date of every spec file related to the task it is about to perform.

### 7.3 Protocol decision tree

```
Before executing any task:

1. Identify all spec files related to this task
   (domain business.md, domain technical.md, domain tasks.md,
    global_spec.md, global_tech_spec.md)

2. Check Last reviewed on each. Count how many are stale (> 7 days).

   ┌── 0 stale ──────────────────────────────────────────────────────────┐
   │  Proceed with the task immediately.                                  │
   └──────────────────────────────────────────────────────────────────────┘

   ┌── 1 to 3 stale ─────────────────────────────────────────────────────┐
   │  DO NOT proceed with the original task yet.                          │
   │  For each stale spec:                                                │
   │    a. Read the current spec content                                  │
   │    b. Read the relevant source code                                  │
   │    c. Identify discrepancies                                         │
   │    d. Update the spec to reflect current reality                     │
   │    e. Update Last reviewed to today                                  │
   │    f. Update Reviewed by to "AI Agent — {agent name} — {date}"      │
   │    g. Add a note in the domain tasks.md Completion Log               │
   │  After all stale specs are updated, proceed with the original task. │
   └──────────────────────────────────────────────────────────────────────┘

   ┌── 4 or more stale ──────────────────────────────────────────────────┐
   │  DO NOT proceed with the original task.                              │
   │  Create a task in global_task_spec.md:                              │
   │                                                                      │
   │  | GLOBAL-{NNN} | Spec freshness review — {domain} | ⬜ | — | — |  │
   │  |  {count} spec files stale (> 7 days). Full human review required │
   │  |  before any implementation proceeds.                      |       │
   │                                                                      │
   │  Notify the user:                                                    │
   │  "I found {count} stale spec files. This exceeds the automatic      │
   │   update threshold. I've created GLOBAL-{NNN} in                    │
   │   global_task_spec.md. Please resolve this before I continue."      │
   └──────────────────────────────────────────────────────────────────────┘
```

### 7.4 What counts as a discrepancy?

| Type | Example |
|---|---|
| Missing in spec | Code has a new `CancelOrderCommand` not documented in `technical.md` |
| Outdated in spec | Spec says handler returns `Task<Unit>` but code returns `Task<Result>` |
| Contradicted by code | Spec says "only Active records processed" but code processes all records |
| Undocumented rule | Code enforces a validation with no corresponding `BR-*` entry |

### 7.5 What the agent updates vs. what it does not

**The agent updates:**
- `Last reviewed` date and reviewer entry
- Descriptions that are factually incorrect based on the code
- Missing entries for things that clearly exist in the code
- Status of tasks that are clearly complete based on the code

**The agent does NOT update:**
- Business rules — require human decision
- Architecture decisions — require human decision
- Task priorities, assignments, or estimates
- Anything requiring business or product judgment

When the agent finds something requiring human judgment, it adds this at the top of the spec:

```markdown
> ⚠️ AGENT REVIEW NOTE — {date} — {agent name}
> Discrepancy found during freshness review that requires a human decision:
> {precise description of the discrepancy}
> This note is removed once a human has reviewed and updated the section.
```

---

## 8. Documentation Standards

### 8.1 Purpose of the `docs/` folder

The `docs/` folder contains everything needed for external systems to understand and integrate with this project. It is a machine-readable and human-readable **contract** — not internal developer notes.

### 8.2 `project_contract.md` — The inter-project standard

This is the key to inter-project integration in the SDD+ ecosystem. Any project following SDD+ can consume another project's `project_contract.md` to understand how to integrate with it — the same way OpenAPI/Swagger describes API contracts, but at the full architectural level.

**Required format:**

```markdown
# {Project Name} — Project Contract
**SDD+ version:** {n}
**Contract version:** {n}
**Last updated:** {date}
**Project type:** {type}

## Identity
**What this project does:** {One paragraph. No jargon.}
**Primary consumers:** {Projects/systems that use this project}
**Primary dependencies:** {Projects/systems this project depends on}

## Exposed Interfaces
{For APIs: endpoint groups, base URLs, authentication method}
{For libraries: public interfaces/packages}
{For event producers: events published and their shape}

## Authentication
{Type, token format, how to obtain credentials}

## Data Contracts
{Key DTOs/schemas consumers need to know. Link to OpenAPI spec if available.}

## Integration Guide
{Step-by-step: what to read first, what to call, what to expect}

## Versioning and Stability
{What is stable — breaking changes announced in advance}
{What is internal — may change without notice}

## Known Limitations
{Rate limits, known issues, planned breaking changes}

## Contact
{Team or person responsible for this contract}
```

### 8.3 API documentation format

```markdown
# {Endpoint Group} — API Documentation
**Base URL:** {url}
**Authentication:** {method}
**Last updated:** {date}

## {HTTP Method} {/route}
**Purpose:** {one line}
**Request:** {parameters and body shape}
**Response (200):** {shape with example}
**Error responses:** {table of status → when}
**Notes:** {edge cases, caveats}
```

### 8.4 Integration documentation format

```markdown
# {Service Name} — Integration Documentation
**Service type:** External API | Message queue | Database | SDK
**Provider:** {company or team}
**Last reviewed:** {date}

## What we use it for
## Authentication
## Key operations we use
## Response handling
## Error handling and retry strategy
## Known limitations and quirks
## Environment configuration
```

---

## 9. Technology Stacks

SDD+ is **stack-agnostic at the methodology level**. The three spec files, Freshness Protocol, domain growth rules, and agent rules apply regardless of technology. The `global_tech_spec.md` for each project defines the specific stack choices and their rationale.

The following section documents the `.NET` stack as a reference implementation. Additional stacks are added to this document as they are formally adopted.

### 9.1 .NET — Clean Architecture + DDD + CQRS

#### Project structure

```
{ProjectName}.sln
├── src/
│   ├── {ProjectName}.Domain/          ← Entities, aggregates, value objects,
│   │                                     domain events, repository interfaces.
│   │                                     ZERO external NuGet dependencies.
│   ├── {ProjectName}.Application/     ← Commands, queries, handlers, DTOs.
│   │                                     References Domain only.
│   ├── {ProjectName}.Infrastructure/  ← ORM, repositories, HTTP clients,
│   │                                     external services, background jobs.
│   │                                     References Application + Domain.
│   └── {ProjectName}.Presentation/   ← Controllers, middleware, Program.cs.
│                                         References Application + Infrastructure.
└── tests/
    ├── {ProjectName}.Domain.Tests/
    ├── {ProjectName}.Application.Tests/
    ├── {ProjectName}.Infrastructure.Tests/
    └── {ProjectName}.Presentation.Tests/
```

#### Dependency rules — strictly enforced

```
Domain         → no references to any other project layer
Application    → Domain only
Infrastructure → Application + Domain
Presentation   → Application + Infrastructure
Tests.*        → only the project under test
```

Any reference that violates these rules must be rejected by the agent and flagged to the developer.

#### CQRS pattern

```csharp
// Command — write operation, always returns Result (no value on success)
public sealed record CreateOrderCommand(Guid CustomerId, List<OrderItem> Items) : ICommand;

public sealed class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand>
{
    public async Task<Result> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // 1. Validate inside the handler — no pipeline behaviors
        if (!request.Items.Any())
            return Result.Failure(Error.Validation("items.empty", "Order must have at least one item."));

        // 2. Business logic
        // ...
        return Result.Success();
    }
}

// Query — read operation, returns Result<T>
public sealed record GetOrderQuery(Guid OrderId) : IQuery<OrderDto>;

public sealed class GetOrderQueryHandler : IQueryHandler<GetOrderQuery, OrderDto>
{
    public async Task<Result<OrderDto>> Handle(GetOrderQuery request, CancellationToken ct)
    {
        var order = await _repository.FindByIdAsync(request.OrderId, ct);
        if (order is null)
            return Result.Failure<OrderDto>(Error.NotFound($"Order {request.OrderId} not found."));

        return Result.Success(order.ToDto());
    }
}

// Controller — inject ISender (not IMediator unless Publish() is needed)
[ApiController]
[Route("api/v1/orders")]
public class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(
            new CreateOrderCommand(request.CustomerId, request.Items), ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value }, null)
            : UnprocessableEntity(result.Errors);
    }
}
```

**Key rules:**
- Commands return `Task<Result>` — never `Task<Unit>` or `Task`
- Queries return `Task<Result<T>>`
- Validation runs **inside the handler** — there are no pipeline behaviors
- Inject `ISender` by default; inject `IMediator` only when `Publish()` is needed
- `CancellationToken` is always the last parameter and must be propagated to all async calls

#### EF Core

- Code First with Fluent API configurations — no data annotations on domain entities
- Separate persistence entities from domain entities when the domain is complex
- All Fluent configurations in `Infrastructure/Persistence/Configurations/`
- Auto-discovered via `ApplyConfigurationsFromAssembly`

#### Testing

| Concern | Tool |
|---|---|
| Unit tests | nUnit 4.x + Moq 4.x + FluentAssertions |
| EF Core integration | `Microsoft.EntityFrameworkCore.InMemory` |
| HTTP client mocking | WireMock.Net |
| Test data creation | Builder pattern per aggregate |

**Test method naming:** `{Method}_{Condition}_{ExpectedResult}`  
Example: `Handle_WhenItemsListIsEmpty_ReturnsValidationFailure`

**Coverage minimums:**

| Layer | Minimum |
|---|---|
| Domain | 90% |
| Application | 80% |
| Infrastructure | 70% |
| Presentation | 70% |

**Rules:**
- Every business rule (`BR-*`) in `business.md` must have at least one unit test
- Tests that verify documented examples must include a comment referencing the spec section
- No real HTTP calls or real database connections in unit tests
- Each EF InMemory test gets a unique database name to prevent state leakage

#### Background jobs

- Disable concurrent execution on all sync jobs
- Schedule, retry count, and timeout from configuration files via `IOptions<T>` — never hardcoded
- Jobs dispatch commands via `ISender` — business logic belongs in handlers, not jobs

#### Authentication

- JWT Bearer tokens for external-facing APIs
- Access tokens: short-lived (15 minutes recommended)
- Refresh tokens: longer-lived with rotation on every use
- Passwords: always hashed before persistence — never stored or logged in plain text

---

## 10. AI Agent Rules

### 10.1 Overview

Each AI agent type has its own rules file in `ssd_strategy/`. Rules are written to match the native format each agent expects, maximising the chance that the agent follows them reliably.

### 10.2 The nine standard rules

These rules are included in **every** agent rules file, regardless of project or technology stack:

| # | Rule | Description |
|---|---|---|
| 0 | Read before acting | Read `README.md`, `global_spec.md`, and `global_tech_spec.md` before any action |
| 1 | Freshness Protocol | Check `Last reviewed` on all related specs before every task |
| 2 | Spec before code | Never write code for something not in a spec. Update the spec first. |
| 3 | Dependency rules | Never add references that violate the architecture layer rules |
| 4 | CQRS rules | Commands → `Task<Result>` · Queries → `Task<Result<T>>` · Validate inside handlers |
| 5 | Update tasks | Mark ✅ and update progress percentage after every completed task |
| 6 | No assumptions | If ambiguous: ask the user. Do not assume and proceed. |
| 7 | Domain growth | When threshold is breached: create a task in `global_task_spec.md`. No domain split without explicit human approval. |
| 8 | Security | Secrets never in committed files · Passwords always hashed · No stack traces in API responses |

### 10.3 Cursor — `.cursorrules`

Location: `ssd_strategy/.cursorrules` — Cursor picks this up automatically.

```
# {Project Name} — Cursor Rules
# SDD+ Methodology v{version}

## RULE 0 — READ BEFORE ANYTHING ELSE
Before any action in this repository:
1. Read ssd_strategy/README.md
2. Read ssd_strategy/specs/global_spec.md
3. Read ssd_strategy/specs/global_tech_spec.md
4. Read the relevant domain spec files for this task
5. Apply the Freshness Protocol

## RULE 1 — FRESHNESS PROTOCOL (MANDATORY BEFORE EVERY TASK)
Check Last reviewed date on all related spec files.
0 stale: proceed immediately.
1–3 stale (> 7 days): update them before proceeding.
4+ stale: create a task in global_task_spec.md and stop. Notify the user.

## RULE 2 — SPEC BEFORE CODE
Never write implementation code for something not in a spec.
If a task requires something not in the specs, update the spec first, then implement.

## RULE 3 — ARCHITECTURE DEPENDENCY RULES
Domain        → no external project references
Application   → Domain only
Infrastructure → Application + Domain
Presentation  → Application + Infrastructure
Tests         → only the project under test
Never add a reference that violates these rules.

## RULE 4 — CQRS RULES
Commands implement ICommand, handlers return Task<Result>
Queries implement IQuery<T>, handlers return Task<Result<T>>
Inject ISender in controllers and jobs (not IMediator unless Publish() is needed)
Validate inside handlers — no pipeline behavior classes
Use built-in logging tools — do not create a LoggingBehavior class

## RULE 5 — TASK TRACKING (MANDATORY AFTER EVERY TASK)
Mark the task ✅ in the relevant tasks.md
Add a row to the Completion Log with date and notes
Update the progress percentage in the Progress Summary
Update global_task_spec.md if it was a global or cross-domain task

## RULE 6 — NO ASSUMPTIONS
If something is ambiguous or not covered by the specs:
- Do NOT assume and proceed
- Ask the user for clarification
- Reference the specific spec section that is unclear
Wait for clarification before writing any code.

## RULE 7 — DOMAIN GROWTH MONITORING
If you detect any domain exceeding these thresholds:
  - > 20 business rules
  - > 40 tasks
  - > 15 commands + queries combined
  - > 8 entities / aggregates
  - > 600 lines in technical.md
Create a task in global_task_spec.md immediately.
Do NOT split the domain without explicit human approval.

## RULE 8 — SECURITY (NON-NEGOTIABLE)
Secrets, API keys, passwords, connection strings: never in committed files.
Use environment variables or a secrets manager for development.
Passwords: always hash before persistence. Never store or log plain text.
API error responses: never include stack traces or internal exception details.

## RULE 9 — COMMIT FORMAT
{type}({task-id}): {description}
Types: feat | fix | test | refactor | docs | chore
Example: feat(PAY-001): add CreatePaymentCommand handler
One task per commit. Pass all existing tests before committing.
```

### 10.4 GitHub Copilot — `copilot-instructions.md`

Location: `.github/copilot-instructions.md` (Copilot's standard location) + copy in `ssd_strategy/`.

```markdown
# {Project Name} — GitHub Copilot Instructions
# SDD+ Methodology v{version}

You are assisting with a project that follows SDD+.
Read ssd_strategy/README.md before suggesting any code.

## Before any suggestion
1. Is this task described in a spec file? If not, suggest updating the spec first.
2. Are the related spec files fresh (reviewed within 7 days)?
3. Does this suggestion violate any architecture rule?

## Architecture rules
- Domain layer: no external dependencies of any kind
- Application layer: Commands return Task<Result>, Queries return Task<Result<T>>
- Validate inside handlers — suggest no pipeline behavior classes
- ISender for dispatch by default, not IMediator, unless Publish() is needed

## Do not suggest
- Magic numbers or hardcoded configuration values
- Direct database access from controllers
- Nullable reference type suppressions (!) without a comment explaining why
- Architectural patterns not described in global_tech_spec.md

## When in doubt
Suggest reading the relevant spec file rather than guessing.
State explicitly: "This is not in the spec — should we add it first?"
```

### 10.5 Claude — system prompt

When using Claude via `claude.ai` Projects or an API system prompt:

```
You are a software development assistant for {Project Name}.
This project follows the SDD+ methodology
(Specification and Domain-Driven Development Plus).

MANDATORY: Before responding to any development request:
1. Ask the user to confirm which domain the task belongs to
2. Remind the user to check the Last reviewed date on related spec files
3. Do not suggest code for anything not described in a spec

Architecture rules:
- Commands: ICommand → ICommandHandler<T> → Task<Result>
- Queries: IQuery<T> → IQueryHandler<TQ,TR> → Task<Result<T>>
- Validate inside handlers — no pipeline behaviors
- Inject ISender, not IMediator, unless Publish() is needed

Always ask: "Has this been added to the domain spec?" before writing code.
Always say: "Update tasks.md after completing this" when finishing an implementation.
Do not split any domain without the user explicitly writing "approved" or "proceed".
```

---

## 11. External Task Tracking Integration

### 11.1 When to use

Referencing an external tracker in a `tasks.md` file is **optional**. Use it when a corresponding work item exists in the external system and you want to link the spec task to it.

### 11.2 Standard format

The `Tracking` column in the task table uses this format when populated:

```
{PLATFORM} #{ID} — {URL}
```

| Platform | Code | Example |
|---|---|---|
| Azure DevOps | `ADO` | `ADO #4521 — https://dev.azure.com/org/project/_workitems/edit/4521` |
| Jira | `JIRA` | `JIRA #PROJ-204 — https://yourorg.atlassian.net/browse/PROJ-204` |
| Linear | `LINEAR` | `LINEAR #ENG-891 — https://linear.app/yourorg/issue/ENG-891` |
| GitHub Issues | `GH` | `GH #142 — https://github.com/yourorg/repo/issues/142` |

When no external reference exists, the `Tracking` column contains `—`.

### 11.3 Sync direction

`tasks.md` is the source of truth for **status** within the repository. The external tracker is the source of truth for **assignment, priority, and sprint allocation**. They are not automatically synchronised.

---

## 12. The Central Strategy Repository

### 12.1 Purpose

A single central repository holds the base templates, this methodology document, and the canonical versions of the agent rule files. All project repositories using SDD+ reference this central source.

### 12.2 Central repository structure

```
{name}-sdd-strategy/
├── README.md                          ← Methodology overview and quick start
├── SDD_PLUS_METHODOLOGY.md            ← This document
├── templates/
│   ├── ssd_strategy/
│   │   ├── README.md                  ← Project README template
│   │   ├── .cursorrules               ← Base rules (projects extend, do not replace)
│   │   ├── copilot-instructions.md    ← Base rules
│   │   ├── specs/
│   │   │   ├── global_spec.md         ← Template
│   │   │   ├── global_tech_spec.md    ← Template
│   │   │   ├── global_task_spec.md    ← Template
│   │   │   └── domains/
│   │   │       └── _domain_template/
│   │   │           ├── domain.business.md
│   │   │           ├── domain.technical.md
│   │   │           └── domain.tasks.md
│   │   └── docs/
│   │       └── project_contract.md    ← Template
│   └── dotnet/                        ← Stack-specific templates
│       └── Directory.Build.props
├── changelog/
│   └── CHANGELOG.md
└── examples/
    └── {example_project}/
```

### 12.3 How projects use the central repository

1. Copy `templates/ssd_strategy/` into the project root
2. Fill in all project-specific sections
3. **Extend** (do not replace) `.cursorrules` and `copilot-instructions.md` with project-specific rules
4. Record the `SDD+ version` in the project's `README.md`

When the methodology is updated: bump the version in `CHANGELOG.md`, notify teams, let each project adopt at its own pace. The `SDD+ version` field in each project's `README.md` tracks which version they are running.

---

## 13. Plans — Architecture and Infrastructure Changes

### 13.1 When a plan is needed

A Plan is required when a proposed change:
- Affects more than one domain
- Requires infrastructure changes (new database, message queue, external service)
- Involves cross-domain refactoring
- Is flagged as an architecture concern by the AI agent during spec review

### 13.2 Plan creation process

**Step 1 — Agent detects a trigger**  
Creates a draft `PLAN-{NNN}_{title}.md` in `specs/plans/` and adds a review task to `global_task_spec.md`.

**Step 2 — Agent recommends a model for complex analysis**  
For complex plans, the agent states which AI model it recommends and waits for explicit confirmation:

```
Recommended model for this analysis: {model name and version}
Reason: {why this model is appropriate for this complexity}
I will not proceed until you confirm.
```

**Step 3 — Human reviews and approves (or rejects)**  
No implementation begins without explicit `"approved"` or `"proceed"` in the conversation.

**Step 4 — Execution follows the plan**  
Tasks are added to relevant domain task files. The plan file tracks overall progress.

### 13.3 Plan file format

```markdown
# PLAN-{NNN} — {Title}
**Created:** {date}
**Status:** Draft | Approved | In Progress | Complete | Rejected
**Approved by:** {name} on {date}
**Recommended model:** {model for analysis phase}

## Context
{Why this plan is needed. What triggered it.}

## Current state
{Honest description of the current situation.}

## Proposed change
{What will be different after this plan is executed.}

## Impact analysis
{Which domains are affected. Which external systems are affected.}

## Task breakdown
{Numbered list of tasks, each referencing the spec file where the task is tracked.}

## Risks
{What could go wrong. Mitigation strategy for each risk.}

## Rollback plan
{How to undo this if it goes wrong.}

## Progress
| Task | Spec file | Status |
|---|---|---|
```

---

## 14. Working with SDD+ Day to Day

### 14.1 Starting a new task

```
1.  Developer identifies the task in tasks.md or creates it
2.  Developer opens a conversation with the AI agent
3.  Agent reads README.md and global specs (RULE 0)
4.  Agent applies the Freshness Protocol (RULE 1)
5.  If 1–3 specs are stale → agent updates them, then proceeds
    If 4+ specs are stale → agent creates a review task and stops
6.  Agent reads the relevant domain specs (business + technical + tasks)
7.  Agent implements the task following the specs exactly
8.  Agent marks the task ✅ in tasks.md (RULE 5)
9.  Agent updates the progress percentage
10. Developer reviews the implementation against the spec
11. Developer commits using the standard commit format
```

### 14.2 Adding a new domain

```
1.  Identify the bounded context name — from business language, not technical concepts
2.  Create specs/domains/{domain_name}/
3.  Write {domain}.business.md first — no code yet
4.  Have it reviewed by someone who understands the business side
5.  Write {domain}.technical.md referencing the business rules it implements
6.  Write {domain}.tasks.md with the initial task list
7.  Update global_spec.md domain map
8.  Update ssd_strategy/README.md domain list
9.  Set Last reviewed to today on all three new files
10. Implementation can now begin
```

### 14.3 When requirements change

```
1.  Update business.md first — change the rule text, keep the BR-ID
2.  If the technical implementation changes, update technical.md
    (old decision moves to "Superseded Decisions" — never deleted)
3.  Add tasks to tasks.md for the implementation work
4.  Update Last reviewed on all modified files
5.  Then implement
```

### 14.4 Common mistakes SDD+ prevents

| Mistake | How SDD+ prevents it |
|---|---|
| Agent invents an architectural pattern | RULE 6: no assumptions; RULE 2: spec before code |
| Spec not updated after a code change | Freshness Protocol catches it within 7 days |
| Two domains use the same term differently | `global_spec.md` authoritative glossary |
| Domain grows too large unnoticed | Growth thresholds create automatic review tasks |
| Technical decision made with no rationale | `technical.md` ADR format requires rationale |
| Integration breaks because a contract changed | `project_contract.md` is the versioned contract |
| New developer cannot find project context | Three files per domain contain the complete picture |

---

## 15. Bootstrap Prompt

Use this prompt verbatim with Cursor, Claude, ChatGPT, or Copilot Chat to scaffold the SDD+ structure for a new project. Replace the bracketed values before using.

```
I want you to create the base SDD+ (Specification and Domain-Driven Development Plus)
project structure for a new project. SDD+ is a methodology where specifications are
written before code, AI agents operate only within what the specs authorise, and every
domain has three dedicated spec files: business, technical, and tasks.

Project details:
  - Project name:       {PROJECT_NAME}
  - Project type:       {.NET Clean Architecture / other}
  - Repository:         {REPO_URL_OR_NAME}
  - Initial domains:    {domain_1}, {domain_2}, ...
  - Tracking system:    {ADO / JIRA / LINEAR / GH / none}
  - Today's date:       {YYYY-MM-DD}

Create this exact structure:

  ssd_strategy/
  ├── README.md
  ├── .cursorrules
  ├── copilot-instructions.md
  ├── specs/
  │   ├── global_spec.md
  │   ├── global_tech_spec.md
  │   ├── global_task_spec.md
  │   ├── plans/                    ← create .gitkeep if empty
  │   └── domains/
  │       └── {one folder per domain listed above}/
  │           ├── {domain}.business.md
  │           ├── {domain}.technical.md
  │           └── {domain}.tasks.md
  └── docs/
      ├── project_contract.md
      ├── api/                      ← .gitkeep
      ├── integrations/             ← .gitkeep
      └── components/               ← .gitkeep

Rules for file content:
  1. Every spec file must have "Last reviewed: {TODAY}" in its header
  2. Every spec file must have all required sections with clear placeholder text
     that indicates what goes in each section — do not leave sections empty
  3. .cursorrules must contain all 9 standard SDD+ rules
  4. README.md must list all domains and reference the Freshness Protocol
  5. global_tech_spec.md must document the technology stack decisions
  6. Do NOT create any source code files — only ssd_strategy/ and its contents
  7. After creating all files, show the complete directory tree and confirm
     that Last reviewed dates are set on every spec file

Today's date is: {TODAY_DATE}
```

### 15.1 Post-bootstrap verification checklist

After the agent creates the structure, verify:

- [ ] `ssd_strategy/README.md` exists and lists all domains
- [ ] `ssd_strategy/.cursorrules` contains all 9 standard rules
- [ ] `specs/global_spec.md` has `Last reviewed` = today
- [ ] `specs/global_tech_spec.md` has `Last reviewed` = today
- [ ] `specs/global_task_spec.md` has `Last reviewed` = today
- [ ] Each domain folder has exactly three files
- [ ] Each domain spec file has `Last reviewed` = today
- [ ] `docs/project_contract.md` exists with the standard format
- [ ] No source code files were created
- [ ] `specs/plans/` folder exists (with `.gitkeep` if empty)

---

*SDD+ Methodology v1.0*  
*Author: Francisco Torregrosa*  
*This document is the authoritative reference for the SDD+ methodology.*  
*When in doubt, this document wins.*