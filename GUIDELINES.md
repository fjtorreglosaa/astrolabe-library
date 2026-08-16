# Astrolabe Books — Library Management Platform

## 1. Document Purpose

This document defines the initial product scope, software architecture, engineering standards, infrastructure strategy, testing strategy, security requirements, and delivery process for Astrolabe Books, a multi-library membership platform.

The product is defined by the approved UI prototype stored in `docs/design/`. **Where this document and the prototype disagree, the prototype prevails**, and this document must be corrected to match it.

The application will be implemented with AI-assisted software development using Claude Code.

The development process will follow an SDD+ approach in which requirements, architecture, engineering rules, specifications, implementation tasks, validation, and acceptance criteria are defined before or alongside implementation.

This document is the architectural source of truth for the project.

Implementation decisions must follow this specification unless an Architecture Decision Record explicitly documents a justified exception.

---

# 2. Product Objective

Astrolabe Books is a membership platform for a network of physical libraries.

A member registers with an email address, chooses a subscription plan, and — according to that plan — reserves books from the libraries their plan reaches, buys books from an integrated store with plan-based discounts, and accumulates redeemable reward points.

The network is operated by library administrators, each responsible for the specific libraries a super administrator assigns to them.

The product must demonstrate:

* Clean software architecture.
* Strong separation of concerns.
* Maintainable backend and frontend code.
* Secure authentication, session management, and authorization.
* Automated testing.
* Infrastructure as Code.
* Automated CI/CD.
* Cloud deployment.
* Good UI/UX practices.
* Realistic library workflows.
* Effective use of AI-assisted software engineering.

The application must not look like a simple CRUD coding exercise.

The final product must be usable by a real library network.

---

# 3. Core Functional Scope

## 3.1 Library Network

The platform operates a network of physical libraries.

* A **country** contains **cities**.
* A **city** contains one or more **libraries** (branches).
* A **library** holds **copies** of books.

Members register with a country and a city of residence. A member's city determines the reach of the Basic and Plus plans.

Seed network: New York (Midtown, Harlem), Chicago (Loop, Pilsen), Austin (Mueller). The registration country catalogue also includes Canada, the United Kingdom, Mexico, Colombia, and Spain.

## 3.2 Membership Plans

Every member holds exactly one active plan. Plans are the central business rule of the product: they govern catalogue access, borrowing reach, purchase discounts, reward points, and AI recommendations.

| Capability | Basic | Plus | Max |
|---|---|---|---|
| Monthly price | $0.00 | $6.99 | $12.99 |
| Browse the catalogue | Entire network | Entire network | Entire network |
| Reserve at | **Home library only** | Every library in the member's city | Every library in the network |
| Accessible titles | Titles with `Basic` tier only | Full catalogue | Full catalogue |
| Purchase discount | 0% | **10%**, on books in the member's city | **15%**, on books in any city |
| Reward points | No | No | **Earns and redeems** |
| AI recommendations | **Never** | Yes | Yes |

Rules:

* A member's **home library** is assigned automatically from their city of residence.
* Every plan may **browse** the entire network. Reach restricts borrowing, never discovery.
* An **upgrade takes effect immediately** and is **prorated**: the member is charged for the new plan over the days left in the cycle, credited for the days already paid, and never pays twice for the same period. The amount due is never negative.
* A **downgrade is scheduled, not applied**. The current plan runs until the renewal date and the new plan starts then. Nothing is charged and nothing is refunded. The member may cancel it until it lands.
* Before a downgrade is confirmed the member must be shown what they lose: points ceasing to accrue and to be redeemable when leaving Max, and borrowing narrowing plus AI turning off when moving to Basic.
* A billing cycle is anchored to the day of the month the subscription started — anniversary billing, not a calendar day shared by everyone. When that day does not exist in the renewal month, the cycle renews on the last day of that month and returns to its anchor afterwards.
* A member may change their city of residence once per billing cycle.
* Every change is recorded in a subscription history.
* A plan change must never retroactively invalidate reservations already in progress, in either direction.
* Reward points already earned are not destroyed by a downgrade, but they may only be redeemed while the active plan is Max.

## 3.3 Book Catalogue

The system must allow authorized users to create, edit, publish, repair, delete, restore, and view books, and must allow all users to list, search, filter, sort, and paginate them.

Book metadata must include at least:

* Id.
* ISBN.
* Title.
* Author.
* Description.
* Publisher.
* Publication year.
* Genre/category.
* Language.
* Page count.
* Cover image, with a generated colour tint as fallback.
* **Plan tier** (`Basic`, `Plus`, `Max`).
* Retail price.
* Rating.
* Copies per library.
* Created date.
* Updated date.

Each book carries its **own plan tier**, independent of any member's plan. A book is reservable only when the book's tier is within the member's plan, the copy is located within the member's plan reach, and stock is available.

### Book lifecycle

```text
draft → catalog → repair → catalog
                        ↘ deleted → catalog (restore)
```

* `draft` — created but not yet visible to members.
* `catalog` — published and reservable.
* `repair` — temporarily withdrawn, with a typed reason and an expected return date.
* `deleted` — withdrawn from the collection, with a typed reason, and restorable.

Repair reasons: damaged spine, water damage, missing pages, rebinding, cover replacement, other.
Removal reasons: donated, damaged beyond repair, lost by member, withdrawn from collection, other.

Every lifecycle transition must write an audit entry.

Book creation uses a three-step wizard: book details, copies and pricing, review.

## 3.4 Fines and Payments

* A late return accrues **$0.35 per day, per title**, capped at **$9.00 per title**.
* Fines are payable by card inside the application, or in cash or by card at a library desk.
* A desk payment is requested as a **payment code**, valid for **72 hours**, which a library administrator validates or rejects.
* An administrator may also record a payment taken at the desk without a prior code.
* Stored payment methods must retain only the card brand, last four digits, expiry, and cardholder name.
* Every payment produces a receipt number.

## 3.5 Store

Members may purchase books in addition to reserving them. Reserving is included in the plan and carries no cost; purchasing does.

* The plan discount applies **per order line**, so the city rule of the Plus plan remains auditable.
* Fulfilment is either collection at a library (free) or shipping (3–5 days, additional charge).
* Reward points accrue at **one cent per $1.50 spent** after discount, for Max members only, and are redeemable against future purchases.
* All point movements must be recorded in an immutable ledger. Balances are never mutated directly.

## 3.6 Support

* Members open support tickets in one of five categories: payments and fines, reservations and returns, catalogue and availability, account and plan, something is broken.
* A ticket moves through `created` → `in review` → `resolved`, and may be reopened.
* Staff assign a ticket to themselves, reply, and resolve it. The member is notified at every step.
* On closure the member rates the service and may leave a written review, attributed to the agent who handled it.
* Staff only see tickets belonging to libraries within their assigned scope.

## 3.7 Notifications

* A notification centre in the header groups notifications by family: due dates, payments, returns, reservations, and support.
* Members can mark all as read, clear all, disable notifications globally, and disable individual families.
* Selecting a notification navigates to the screen it refers to.

## 3.8 Reviews

Members may rate a book with stars and an optional written review, attributed with their name and initials. Reviews are editable and removable, and aggregate into the rating shown in the catalogue.

## 3.9 AI Recommendations

Personalised recommendations are generated per library from that library's own model provider credentials. The capability is specified in section 71.

---

# 4. Reservation Management

The system must support the full reservation lifecycle. The product term is **reservation**, not the ambiguous "check-in/check-out" terminology of the original challenge.

## 4.1 Lifecycle

```text
Reserved → InTransit → Borrowed → ReturnInProgress → Returned
                          ↓
                       Overdue → Lost
```

## 4.2 Rules

* The loan period is **14 days** from confirmation.
* A reservation targets a **specific copy at a specific library**, chosen by the member from the copies their plan can reach.
* Delivery is either collection at the library (ready in 2 hours, free) or **home delivery** (24–48 hours, additional charge).
* Return is either **courier pickup**, confirmed by the courier with a handover code, or drop-off at the library desk.
* A return becomes `Returned` only when library staff check the copy in.
* Overdue reservations accrue fines as defined in section 3.4.

## 4.3 Invalid operations

The system must prevent:

* Reserving a book outside the member's plan reach.
* Reserving an unavailable copy.
* Returning a copy that is not currently borrowed.
* Creating duplicate active reservations for the same physical copy.
* Producing a negative available-copy count.

Concurrency must be handled explicitly when multiple members attempt to reserve the same last available copy. Optimistic concurrency is the required mechanism, and a race test is mandatory.

Reservation creation and payment operations must accept an idempotency key.

---

# 5. Search

Books must be searchable by:

* Title.
* Author.
* ISBN.
* Genre/category.
* Publisher.

Search should support partial matching where appropriate.

Search capabilities should be implemented in a way that allows additional filters to be introduced without redesigning the application.

---

# 6. Authentication, Sessions and Authorization

Authentication will use token-based authentication issued entirely by the backend. The frontend never signs or validates tokens.

## 6.1 Roles

There is no separate librarian role. A library administrator scoped to specific libraries fulfils that function.

### Member

One role, `Member`, whatever the plan. What a member may **do** is this list; how far each entry
reaches is decided by their plan, which lives on their subscription and never on their role.

Access to:

* Catalogue browsing and search across the whole network.
* Book details.
* Reservations, within the reach of their plan.
* Their own reservation history.
* Fines, payments, and payment methods.
* Purchases and reward points.
* Support tickets they opened.
* Own profile, plan, preferences, and sessions.
* AI recommendations, for Plus and Max only.

### Administrator

Scoped to the libraries a super administrator has explicitly assigned. Access to:

* Book and copy management within their libraries.
* Reservation and return operations within their libraries.
* Manual desk payments within their libraries.
* Member directory and member lifecycle within their libraries.
* Support tickets raised against their libraries.
* AI provider configuration for their libraries.

An administrator has no access whatsoever to libraries outside their assignment.

### Super Administrator

Unrestricted access to the entire network, and exclusively responsible for:

* Creating and revoking administrators.
* Assigning libraries to administrators.
* Granting extended powers.
* Managing countries, cities, and libraries.

## 6.2 Account lifecycle

An account is in one of four states: `active`, `pending verification`, `blocked`, `deleted`.

* Registration is public and creates an account in `pending verification`.
* **An unverified account cannot sign in.** Verification is by single-use emailed token, hashed at rest, valid for 24 hours.
* Verification and recovery mail is delivered through Mailgun, behind the `IEmailSender` abstraction. A provider failure must never leave an account in an unrecoverable state: the message can always be re-requested.
* Password recovery uses a single-use token valid for 1 hour.
* An administrator may block, restore, and delete accounts within their scope, and resend verification.
* Administrators are onboarded by invitation and appear as `Invited` until they confirm.

## 6.3 Token model

| Concern | Decision |
|---|---|
| Identity store and password hashing | ASP.NET Core Identity, PBKDF2-HMAC-SHA256 |
| Access token | Signed JWT, **15 minutes**, stateless, carries a session identifier claim (`sid`) |
| Refresh token | **Opaque, 256-bit, 30 days**, one per session |
| Refresh token at rest | **SHA-256 hash only.** Never stored in plaintext |
| Refresh token transport | Cookie: `HttpOnly`, `Secure`, `SameSite=Strict`, path-scoped to the refresh endpoint |
| Access token transport | Held in memory by the client. **Never in `localStorage`** |
| Rotation | **Mandatory on every refresh**, with reuse detection |

The API must validate token signature, expiration, issuer, audience, and required claims.

## 6.4 Session and device management

A **session** is the unit a member sees and revokes. Every successful sign-in creates one, recording:

* Session identifier, carried as the `sid` claim of the access token.
* Device identifier generated by the client and persisted locally.
* Human-readable device name derived from the user agent.
* Device type: web, mobile, tablet, desktop, unknown.
* IP address and approximate location.
* Created at, last seen at, expires at, revoked at, revocation reason.
* A flag marking the session the request originated from, so the UI can show "this device".

The device identifier is a **label, not a credential**. It groups and names sessions in the interface and must never be used to authorize anything. The identity of a session is the refresh token.

### Rotation and reuse detection

1. The client presents its refresh token.
2. The backend validates the hash, confirms the session is live, and revokes that token.
3. A new token pair is issued **within the same session**, chained to the previous one.
4. If an already-rotated token is presented, theft is assumed: the **entire session chain is revoked** and a security event is recorded.

### Revocation operations

The member must be able to:

* Sign out of the current session.
* Revoke one named session.
* Revoke several selected sessions in one operation.
* Revoke every session except the current one.
* Revoke every session, including the current one.

### Immediate revocation

Access tokens are stateless and would otherwise remain valid until expiry. Every authenticated request must therefore validate the `sid` claim against a cache of revoked sessions, backed by the database, so that **revocation takes effect immediately** rather than after the token's lifetime.

The cache is in-process for local development, behind an abstraction that allows a distributed cache to replace it once more than one instance runs.

### Session surface

Session and device management is exposed as a dedicated screen under Settings. This screen does not exist in the UI prototype and must be designed consistently with the prototype's design language.

## 6.5 Hardening

* Password policy aligned with NIST SP 800-63B: **minimum 12 characters**, no forced rotation, no arbitrary composition rules.
* Account lockout after **5 failed attempts** within a 15-minute window.
* Rate limiting on sign-in, registration, refresh, and password recovery.
* **Generic error messages** on sign-in and recovery, to prevent user enumeration.
* Constant-time sign-in response regardless of whether the account exists.
* Changing or resetting a password revokes every session except the current one.
* Security events must be audited: successful and failed sign-in, sign-out, sign-in from an unknown device, revocations, refresh token reuse, and changes to password, email, plan, or role.
* Two-factor authentication is out of scope initially, but the data model must leave room for TOTP.

## 6.6 Authorization

Authorization must be enforced by the backend. Frontend authorization exists only to improve the experience and is never a security boundary.

Library scope must be enforced by a centralized authorization handler, not duplicated across controllers.

SSO may be introduced later. The architecture must allow replacing or extending local token authentication with an external identity provider without redesigning the application domain.

---

# 7. Technology Stack

## Backend

* C#.
* .NET 10.
* ASP.NET Core Web API.
* Entity Framework Core.
* PostgreSQL.
* MediatR — **pinned to the 12.x line**, the last Apache-2.0 release. Version 13 and later are
  RPL-1.5 or commercial. See `sdd_strategy/specs/global_tech_spec.md` §4.
* FluentValidation where appropriate.
* NUnit.
* Docker.

### Transactional email

* **Mailgun**, over its HTTP API, accessed with RestSharp.
* Reached exclusively through the `IEmailSender` abstraction declared in the Application layer, so no
  use case depends on the provider.
* Credentials are supplied by environment variable and never committed.
* A sandbox Mailgun domain only delivers to recipients authorised in the Mailgun dashboard. This is a
  configuration limit, not a defect, and the sender logs it as such.

## Frontend

* React.
* TypeScript.
* Material UI.
* TanStack Query.
* Axios.
* Zustand for client-side/global state when required.
* Jest.
* React Testing Library.
* Docker.

## Infrastructure

* Microsoft Azure.
* Terraform.
* Docker.
* Azure Container Apps.
* Azure Database for PostgreSQL Flexible Server.
* Azure Key Vault.
* Azure Container Registry.
* Azure Monitor / Application Insights where appropriate.

## DevOps

* Azure Repos.
* Azure Pipelines.
* YAML pipelines.
* Docker images.
* Infrastructure as Code.
* Trunk-Based Development.
* Build Once, Deploy Many.
* Manual Production deployment approval.

---

# 8. Repository Strategy

The solution will use a monorepository.

Conceptual structure:

```text
/
├── src/
│   ├── backend/
│   │   ├── Astrolabe.Domain/
│   │   ├── Astrolabe.Application/
│   │   ├── Astrolabe.Infrastructure/
│   │   └── Astrolabe.Presentation/
│   └── frontend/
│       └── astrolabe-web/
│
├── tests/
│   ├── backend/
│   └── frontend/
│
├── sdd_strategy/               ← SDD+ specs, agent rules, project contract
│   ├── README.md
│   ├── .cursorrules
│   ├── copilot-instructions.md
│   ├── specs/
│   │   ├── global_spec.md
│   │   ├── global_tech_spec.md
│   │   ├── global_task_spec.md
│   │   ├── plans/
│   │   └── domains/{domain}/{domain}.{business|technical|tasks}.md
│   └── docs/
│       └── project_contract.md
│
├── docs/
│   └── design/                 ← approved UI prototype
│
├── infrastructure/
│   └── terraform/
│
├── pipelines/
│
├── scripts/
│
├── docker/
│   ├── api/Dockerfile
│   └── web/Dockerfile
│
├── docker-compose.yml
├── .env.example
├── GUIDELINES.md
├── SDD_PLIUS_STRATEGY.md
├── README.md
└── CLAUDE.md
```

`sdd_strategy/` is the specification home, mandated by `SDD_PLIUS_STRATEGY.md` §4.1. `docs/` holds
reference material that is not a specification — currently the approved UI prototype.

`infrastructure/` and `pipelines/` are out of scope for the MVP and require their own plan.

Backend, frontend, specifications, documentation, infrastructure, and pipelines must remain clearly
separated.

---

# 9. Backend Architecture

The backend will follow Clean Architecture.

The primary architectural layers will conceptually represent:

```text
Domain
   ↑
Application
   ↑
Infrastructure
   ↑
API
```

Dependencies must point inward.

The Domain layer must not depend on infrastructure technologies.

---

# 10. Backend Projects

Projects:

```text
Astrolabe.Domain

Astrolabe.Application

Astrolabe.Infrastructure

Astrolabe.Presentation
```

Tests are separated per project:

```text
Astrolabe.Domain.Tests

Astrolabe.Application.Tests

Astrolabe.Infrastructure.Tests

Astrolabe.Presentation.Tests
```

The entry project is named **`Astrolabe.Presentation`**, not `Astrolabe.Api`, per `SDD_PLIUS_STRATEGY.md`
§9.1. The layer is named by its architectural role, not by the protocol it happens to speak. Recorded in
`sdd_strategy/specs/global_tech_spec.md` §4.

---

# 11. Domain Layer

The Domain project will contain the business model and must remain independent from infrastructure concerns.

It may contain:

* Entities.
* Value Objects.
* Domain Errors.
* Domain Events.
* Enumerations.
* Domain Services when justified.
* Domain rules.
* Repository abstractions where appropriate.

The Domain layer must not reference:

* Entity Framework.
* ASP.NET Core.
* Azure SDKs.
* PostgreSQL libraries.
* HTTP concepts.
* Controllers.
* Infrastructure implementations.

---

# 12. Application Layer

The Application layer will contain use cases.

CQRS will be used to separate commands from queries.

Examples:

```text
Books/
    Commands/
        CreateBook/
        UpdateBook/
        DeleteBook/

    Queries/
        GetBook/
        SearchBooks/

Reservations/
    Commands/
        CreateReservation/
        StartReturn/
        CheckInReturn/

    Queries/
        GetMyReservations/
        GetReservationHistory/
```

The application layer is organized into these modules:

```text
Identity/       — registration, verification, sign-in, tokens, sessions, roles
Membership/     — plans, subscriptions, plan changes
Catalog/        — books, copies, genres, search, lifecycle, reviews
Reservations/   — reservations, delivery, returns, courier handover
Billing/        — fines, payment methods, charges, desk payments, receipts
Store/          — purchases, discounts, reward points, wallet
Network/        — countries, cities, libraries, administrator assignment
Support/        — tickets, conversation, service rating
Notifications/  — notification centre and preferences
Ai/             — per-library configuration, recommendations
```

MediatR will be used to dispatch commands and queries.

Handlers should orchestrate use cases but must not contain unrelated responsibilities.

---

# 13. CQRS

Commands represent operations that modify application state.

Queries represent read operations.

Commands and queries must remain independent.

Example conceptual flow:

```text
HTTP Request
    ↓
Controller
    ↓
Command / Query
    ↓
MediatR
    ↓
Handler
    ↓
Domain / Repository
    ↓
Result
    ↓
API Response
```

CQRS must not be used as an excuse to introduce unnecessary complexity.

---

# 14. Repository Pattern

Repositories will abstract persistence behavior.

Application and Domain layers must not depend directly on Entity Framework Core.

Repository interfaces should expose domain-oriented persistence capabilities rather than generic database operations whenever possible.

Avoid leaking:

* IQueryable.
* DbContext.
* EF tracking concepts.

outside the Infrastructure layer.

A Generic Repository must not be created simply to duplicate Entity Framework functionality.

Specific repositories should exist when they represent meaningful persistence abstractions.

---

# 14.1 Persisting Value Objects

**Map a value object as an owned type, not with a value converter**, whenever any of its members is
ever read in a query.

A value converter collapses the whole object into an opaque scalar. The provider then has no view of
the members inside it, so `book.Isbn.Value` or `review.Rating.Stars` cannot be translated: the code
compiles, every unit test passes, and the query fails at run time. That is exactly what happened to
catalogue search and to the rating average in Stage 2, and it reached the running system because
nothing before it had ever queried through a converted type.

An owned type produces the same single column and keeps its members queryable, so it is the default
for a value object that is a **class**. For one that is a **struct** — `Money` is the case in this
system — use `ComplexProperty` instead, which is the same idea for value types.

A converter remains acceptable only for a value object that is never filtered, projected, ordered or
aggregated on — and even then, the cost of being wrong is a run-time failure rather than a build one.
This bit three times in Stage 2: `Isbn` broke catalogue search, `StarRating` broke the rating
average, and `Money` broke ordering by price. Each compiled, and each passed every unit test.

---

# 15. Unit of Work

A Unit of Work abstraction will control transactional persistence.

**One unit of work per bounded context.** Each extends the base `IUnitOfWork` and exposes only its
own context's repositories. Handlers depend on the unit of work, never on repositories directly:
this keeps constructors small while a handler in one context still cannot reach another context's
persistence.

`IDbContextFactory` must never be injected into a request-scoped handler. Each call returns a new
context with its own change tracker, which silently breaks the unit of work.

Responsibilities include:

* Coordinating persistence operations.
* Saving changes.
* Managing transactions where appropriate.
* Guaranteeing atomicity for multi-step business operations.

Example conceptual responsibility:

```text
IUnitOfWork
    SaveChangesAsync()
    ExecuteInTransactionAsync()

IIdentityUnitOfWork : IUnitOfWork
    Users · Sessions · Tokens

IAuditUnitOfWork : IUnitOfWork
    Entries

INetworkUnitOfWork : IUnitOfWork
    Countries · Cities · Libraries · Assignments · Invitations

IMembershipUnitOfWork : IUnitOfWork
    Subscriptions

ICatalogUnitOfWork : IUnitOfWork
    Books · Reviews
```

**Audit is its own bounded context, not part of `identity`.** Four domains append to the trail and
none of them owns it. While `AuditEntry` lived under identity, a `network` handler had to inject the
whole `IIdentityUnitOfWork` — users, sessions and tokens — to write a single row, which is exactly
the coupling a unit of work per context exists to prevent.

Because every unit of work shares one `DbContext`, a handler that stages an entry through
`IAuditUnitOfWork` and commits through its own unit of work still writes both in one transaction.
That matters: `BR-CAT-025` and `BR-NET-017` require the entry, so it must not be able to go missing
while the change it describes succeeds. **Write the audit entry inside the command handler, never in
a domain event handler** — a reaction runs after the commit and may be lost, and a trail that can
silently skip a transition is not a trail.

**A handler that genuinely spans two contexts injects both units of work and wraps them in one
transaction.** `ChangeCityOfResidenceCommandHandler` is the reference case: the city belongs to
`identity` and the per-cycle allowance to `membership`, and saving them independently could spend a
member's one move for the cycle without moving them. They share a single `DbContext`, so one
`ExecuteInTransactionAsync` covers both saves. This is the exception, not the pattern — reach for it
only when a partial write would leave the system in a state no rule describes.

The implementation will use Entity Framework Core.

---

# 16. Entity Framework Core

Entity Framework Core will be used for:

* ORM mapping.
* Persistence.
* Database migrations.
* Entity configuration.
* Transactions.
* Concurrency handling.

Entity configuration should use dedicated configuration classes rather than large `OnModelCreating` implementations.

Database migrations must be version controlled.

Production migrations must be executed through a controlled deployment process.

The application must not silently modify the production database schema on startup.

---

# 17. Result Pattern

Application operations will use an explicit Result Pattern instead of exceptions for expected business outcomes.

Conceptual result properties:

```text
Value
IsSuccess
IsFailure
Errors
StatusCode
```

Expected application failures should use Results.

Examples:

* Book not found.
* Book unavailable.
* Invalid request.
* User not authorized for an operation.
* ISBN already exists.

Unexpected technical failures should be handled through exception handling.

---

# 18. Error Model

A base `Error` abstraction will represent application errors.

Errors should contain enough structured information to allow consistent API responses.

Possible information includes:

```text
Code
Message
Type
Metadata
```

Derived error types may represent:

* Validation errors.
* Not Found errors.
* Conflict errors.
* Authentication errors.
* Authorization errors.
* Domain errors.
* Infrastructure errors where exposure is appropriate.

Error definitions should be reusable and strongly typed.

Magic strings must be avoided.

---

# 19. Exception Handling Middleware

A global exception handling middleware must:

* Capture unexpected exceptions.
* Log technical details.
* Generate correlation identifiers.
* Prevent internal implementation details from leaking.
* Return standardized API responses.
* Map known exceptions where appropriate.

Expected domain validation errors should normally use Result rather than exceptions.

---

# 20. Authentication Middleware

Authentication will use ASP.NET Core authentication mechanisms.

The application must validate:

* Token signature.
* Expiration.
* Issuer.
* Audience.
* Required claims.

Authentication concerns must remain separated from domain logic.

---

# 21. Authorization

Authorization should support:

* Roles.
* Policies.
* Claims where required.

Authorization may be applied through:

* Controller attributes.
* Endpoint metadata.
* Authorization policies.
* Custom authorization handlers where justified.

Authorization must be centralized where possible instead of duplicated across controllers.

---

# 22. API Controllers

Controllers must remain thin.

Their primary responsibilities are:

* Receive HTTP requests.
* Bind request data.
* Send commands/queries.
* Convert application Results into HTTP responses.

Controllers must not contain:

* Business logic.
* Database access.
* Complex validation.
* Infrastructure logic.

---

# 23. Base Controller

A common API controller abstraction may centralize shared API behavior.

Example responsibilities:

* Result-to-HTTP conversion.
* Current-user access.
* Standard response behavior.

The base controller must not become a miscellaneous utility class.

Inheritance must only be used for genuine shared controller behavior.

---

# 24. Validation

Input validation must be separated from domain invariants.

Request/application validation can use FluentValidation where appropriate.

Domain invariants must remain protected by the domain model.

Invalid domain states should not be representable whenever reasonably possible.

---

# 25. API Standards

The API should support:

* RESTful semantics.
* Consistent HTTP status codes.
* Pagination.
* Filtering.
* Sorting.
* Structured errors.
* OpenAPI.
* Swagger for development/testing.
* API versioning if justified.
* Correlation IDs.
* Cancellation tokens.
* Async I/O.
* Health checks.

Endpoints should be predictable and consistent.

---

# 26. API Security

Backend security requirements include:

* HTTPS.
* Authentication.
* Authorization.
* Secure token handling.
* Secrets outside source control.
* Input validation.
* Rate limiting.
* Secure headers where applicable.
* Protection from over-posting.
* Safe error responses.
* Logging without sensitive information.

Secrets must never appear in:

* Source code.
* Git history.
* Docker images.
* Pipeline YAML.
* Logs.

---

# 27. Configuration

Configuration will use the .NET Options Pattern.

Configuration classes should use:

```text
IOptions<T>
IOptionsSnapshot<T>
IOptionsMonitor<T>
```

according to their lifecycle requirements.

Configuration objects should represent cohesive configuration concerns.

Raw calls to `IConfiguration` should not be spread throughout the application.

---

# 28. Logging and Observability

The backend must use structured logging.

Logs should include appropriate context such as:

* Correlation ID.
* Request information.
* User identifier when safe.
* Operation.
* Duration.
* Error information.

Sensitive information must never be logged.

Cloud observability should integrate with Azure monitoring services where appropriate.

---

# 29. Health Checks

The API will expose health endpoints.

Health checks should distinguish between:

* Application health.
* Database connectivity.
* Critical external dependencies.

Infrastructure health probes should use these endpoints.

---

# 30. Frontend Architecture

The React application will follow a Feature-Based Modular Architecture.

The objective is to organize the application around business capabilities rather than technical file types.

Example conceptual organization:

```text
src/

    app/

    features/
        auth/
        sessions/
        catalog/
        reservations/
        billing/
        store/
        support/
        notifications/
        ai/
        admin/

    shared/

    layouts/

    routes/

    infrastructure/
```

Each feature owns its internal implementation.

---

# 31. Feature Structure

A feature may contain:

```text
books/

    pages/

    components/

    hooks/

    services/

    queries/

    mutations/

    types/

    validation/

    utils/

    constants/
```

Not every feature must contain every folder.

Folders must only be introduced when they provide actual value.

---

# 32. Frontend Separation of Concerns

Frontend concerns should be separated into:

### Presentation

Material UI components and page composition.

### Server State

TanStack Query.

### Client State

Zustand where global client-side state is genuinely required.

### HTTP Communication

Axios.

### Domain/Feature Logic

Feature hooks/services.

### Routing

React Router.

### Authentication

Authentication provider/session abstraction.

### Authorization

Protected routes and permission-aware components.

---

# 33. TanStack Query

TanStack Query will manage server state.

Responsibilities include:

* Queries.
* Mutations.
* Caching.
* Cache invalidation.
* Background refetching.
* Loading states.
* Error states.

Server data should not normally be duplicated into Zustand.

---

# 34. Zustand

Zustand may manage global client-side state such as:

* Session-related UI state.
* Application preferences.
* Global UI state.
* Other non-server state.

Zustand should not become a second cache for API data already managed by TanStack Query.

Local React state should remain the default for local component concerns.

---

# 35. Axios

Axios will be the HTTP client.

Dedicated API services will exist per domain/feature.

HTTP communication must not be scattered directly throughout React components.

---

# 36. Axios Interceptors

Axios interceptors may handle cross-cutting HTTP concerns including:

* Authentication token attachment.
* Correlation headers.
* Refresh-token workflow.
* Standardized HTTP error processing.

Interceptors must not contain feature-specific business logic.

---

# 37. Authentication Frontend

Authentication responsibilities should be encapsulated behind dedicated abstractions.

Possible components include:

```text
AuthProvider

useAuth()

ProtectedRoute

PermissionGuard

RoleGuard
```

Protected routes improve UX but backend authorization remains mandatory.

---

# 38. UI Components

Material UI will provide the design foundation.

## 38.1 Design source

The approved UI prototype in `docs/design/` is the visual source of truth. It is written with inline styles and **does not use Material UI**, so it must be reconstructed as a Material UI theme rather than copied. Its tokens are binding.

| Token | Light | Dark |
|---|---|---|
| Primary | `#0E5A6E` | `#0E5A6E` |
| Background | `#F4F9FB` | `#0B1519` |
| Surface | `#FFFFFF` | `#10222A` |
| Text | `#10262E` | `#E8F3F6` |
| Muted text | `#5C7480` | `#93AFB9` |
| Border | `rgba(16,38,46,.12)` | `rgba(255,255,255,.12)` |
| Field border | `rgba(16,38,46,.26)` | `rgba(255,255,255,.28)` |

Semantic colours: success `#0F7A63` / `#0C7F70`; warning `#8A6A28` on `rgba(224,166,60,.20)`; error `#B3261E`; info `#0E5A6E`.

Typography: **Playfair Display** for brand and headings, **Plus Jakarta Sans** for interface text, **Material Symbols Outlined** for icons.

Generated cover tints: `#0E5A6E`, `#0B2E3B`, `#12766B`, `#1F5F8B`, `#0F8A7A`, `#2A6E7E`, `#164A5C`, `#3A4E7A`.

Both **light and dark themes are required**, with a selector in the header.

## 38.2 Layout

Authenticated screens use a top navbar, a collapsible left sidebar grouped into sections, a footer, and a floating quick-action button. Authentication screens use a clean layout with no navbar or sidebar.

The interface language is **English**, matching the prototype, with internationalization structured for later additions.

## 38.3 Shared components

Reusable shared components may include:

* Buttons.
* Dialogs.
* Tables.
* Search controls.
* Forms.
* Notifications.
* Loading indicators.
* Error states.
* Empty states.
* Confirmation dialogs.
* Layout components.

Feature-specific components should remain within their feature.

---

# 39. UI/UX Standards

The application should follow modern UI/UX practices.

Requirements include:

* Responsive layout.
* Consistent spacing.
* Clear navigation.
* Accessibility.
* Keyboard usability.
* Proper loading feedback.
* Empty states.
* Error states.
* Confirmation for destructive operations.
* Clear success feedback.
* Consistent typography.
* Consistent visual hierarchy.

The UI should feel like a usable product rather than a technical demo.

---

# 40. General Engineering Principles

The entire codebase must follow:

* DRY.
* Separation of Concerns.
* SOLID where applicable.
* KISS.
* YAGNI.
* Explicit dependencies.
* High cohesion.
* Low coupling.

Patterns must solve real problems.

Patterns must not be introduced only to demonstrate knowledge of patterns.

---

# 41. Separation of Concerns Definition

For this project, a concern is defined as:

> A distinct objective, responsibility, or area of behavior that the software must address.

Examples:

* Authentication.
* Authorization.
* Persistence.
* Validation.
* Logging.
* Book management.
* Borrowing.
* HTTP communication.
* UI rendering.

A class, component, service, module, or layer should not mix unrelated concerns.

SoC does not mean every method must exist in a different class.

The objective is cohesive responsibilities with clear boundaries.

---

# 42. Code Quality

Code smells must be actively avoided.

Examples include:

* God classes.
* God components.
* Long methods.
* Excessive parameter lists.
* Duplicate logic.
* Boolean blindness.
* Primitive obsession.
* Magic strings.
* Magic numbers.
* Hidden side effects.
* Feature envy.
* Shotgun surgery.
* Inappropriate inheritance.
* Excessive abstraction.
* Dead code.
* Unnecessary comments explaining poor code.
* Tight coupling between layers.

Refactoring should occur when code begins violating architectural boundaries.

---

# 43. Docker

Docker will be used to provide reproducible runtime environments.

Containers will be created for at least:

* Backend API.
* Frontend application.

Local development should support Docker Compose.

Conceptual environment:

```text
Frontend
    ↓
Backend API
    ↓
PostgreSQL
```

A developer should be able to launch the local application with minimal setup.

---

# 44. Docker Compose

Docker Compose will orchestrate the local development environment.

It may contain:

```text
frontend

backend

postgres
```

Optional development infrastructure may be added when justified.

Docker Compose is intended for local development and integration testing.

Kubernetes will not be introduced unless a concrete requirement justifies its complexity.

---

# 45. Azure Cloud Architecture

Initial Azure architecture:

```text
                    Internet
                       │
                       ▼
               Frontend Container
                       │
                       ▼
                Backend API
                 Container App
                       │
             ┌─────────┴─────────┐
             ▼                   ▼
     PostgreSQL Flexible       Key Vault
          Server

             ▲
             │
       Azure Monitoring

Docker Images
      │
      ▼
Azure Container Registry

Azure DevOps
      │
      ├── Build
      ├── Test
      ├── Scan
      ├── Docker Build
      ├── Push
      └── Deploy
```

The exact networking model will be defined during infrastructure design.

---

# 46. Azure Container Apps

Azure Container Apps will be the preferred application hosting platform.

Reasons include:

* Container-native deployment.
* Managed infrastructure.
* Automatic scaling.
* Potential scale-to-zero behavior.
* Reduced operational overhead.
* Appropriate complexity for this project.

Kubernetes will not initially be used.

The application should remain container-portable so another container runtime could replace Azure Container Apps later.

---

# 47. PostgreSQL

Production persistence will use Azure Database for PostgreSQL Flexible Server.

Local development will use PostgreSQL through Docker.

Application code must remain PostgreSQL-compatible between local and cloud environments.

---

# 48. Azure Key Vault

Azure Key Vault will store sensitive configuration including:

* Database connection information.
* Authentication secrets.
* Signing keys where applicable.
* External API credentials.
* Other production secrets.

Applications should access Key Vault through managed identities where possible.

Secrets should not be copied into configuration files.

---

# 49. Azure Container Registry

Azure Container Registry will store deployable Docker images.

Images should use immutable version tags.

Example conceptual tags:

```text
library-api:<commit-sha>

library-web:<commit-sha>
```

Production deployments should reference immutable image versions rather than relying only on `latest`.

---

# 50. Azure Resource Naming Convention

All Azure resources must follow the official Microsoft Azure naming rules and restrictions for their corresponding resource type.

The project will use a consistent Azure resource naming convention across all environments.

The naming strategy should identify, where supported and appropriate:

* Resource type.
* Application/workload.
* Environment.
* Azure region.
* Instance number.

Conceptual naming convention:

```text
{resource-type}-{application}-{environment}-{region}-{instance}
```

Examples:

```text
rg-library-dev-eus-001
ca-library-dev-eus-001
ca-library-stg-eus-001
ca-library-prd-eus-001
cae-library-dev-eus-001
```

Resource abbreviations should follow Microsoft Cloud Adoption Framework recommendations whenever applicable.

Examples include:

```text
rg      Resource Group
ca      Container App
cae     Container Apps Environment
acr     Azure Container Registry
kv      Key Vault
psql    Azure Database for PostgreSQL
appi    Application Insights
log     Log Analytics Workspace
```

Azure-specific naming restrictions for individual resource types always take precedence over the general naming convention.

Terraform must centralize resource naming instead of manually duplicating naming logic throughout infrastructure definitions.

Resource names must not be independently invented inside individual Terraform resources.

Azure resources should also follow a consistent tagging strategy where supported.

Typical tags may include:

* Application.
* Environment.
* ManagedBy.
* Repository.
* Owner/responsible team when appropriate.

Terraform will be the source of truth for Azure resource naming and tagging.

---

# 51. Terraform

All cloud infrastructure must be defined as code using Terraform.

Terraform code will live inside the repository but remain isolated from application source code.

Example conceptual structure:

```text
infrastructure/
    terraform/

        modules/

        environments/
            dev/
            staging/
            production/

        providers/

        variables/

        outputs/
```

Exact structure will be defined during infrastructure specification.

---

# 52. Terraform Principles

Terraform must follow:

* Modular infrastructure.
* Minimal duplication.
* Environment isolation.
* Explicit variables.
* Explicit outputs.
* Secure secret handling.
* Remote state.
* State locking where supported.
* Predictable naming.
* Azure naming conventions.
* Resource tagging.
* Least privilege.
* Reusable modules.

Terraform source code must not contain secrets.

---

# 53. Cloud Portability

The application should avoid unnecessary Azure-specific coupling.

Backend business logic must not depend directly on Azure services.

Cloud-specific implementations should live behind infrastructure abstractions where justified.

Docker provides runtime portability.

Terraform provider resources remain cloud-specific.

Therefore portability will be achieved through separate provider implementations rather than pretending a single Terraform resource definition can transparently deploy to every cloud.

Conceptually:

```text
infrastructure/

    terraform/

        modules/

        azure/

        aws/
```

Only Azure will be implemented initially.

An AWS implementation could later provision equivalent services without requiring application redesign.

---

# 54. CI/CD

Azure DevOps will provide both CI and CD.

Pipeline configuration will use YAML stored in the repository.

The project will follow Trunk-Based Development and a Build Once, Deploy Many strategy.

A single CI process must build and validate the application.

A successful CI execution must produce immutable deployable artifacts.

Those exact artifacts must then be promoted through the different deployment environments.

The application must not be rebuilt independently for Development, Staging, or Production.

The pipeline should conceptually contain:

```text
Validate
    ↓
Build
    ↓
Test
    ↓
Coverage
    ↓
Static Analysis
    ↓
Docker Build
    ↓
Docker Push
    ↓
Deploy Development
    ↓
Deploy Staging
    ↓
Validation
    ↓
Manual Approval
    ↓
Deploy Production
```

---

# 55. Continuous Integration

CI should execute for Pull Requests and relevant trunk updates.

Backend CI:

* Restore.
* Build.
* Run unit tests.
* Generate code coverage.
* Validate minimum coverage.
* Publish test results.
* Publish coverage results.

Frontend CI:

* Install dependencies using lock file.
* Lint.
* Type check.
* Build.
* Run tests.
* Generate code coverage.
* Validate minimum coverage.
* Publish test results.
* Publish coverage results.

CI failure must prevent deployment.

For accepted changes to the trunk, CI must produce the immutable application artifacts used by all deployment environments.

The application must not be rebuilt per environment.

Docker images should use an immutable identifier associated with the source revision.

The Git commit SHA should be used as the primary immutable application version identifier.

Conceptually:

```text
library-api:<commit-sha>
library-web:<commit-sha>
```

Environment-specific configuration must be supplied at deployment/runtime.

Therefore:

```text
Development Artifact
        =
Staging Artifact
        =
Production Artifact
```

Only environment-specific configuration, secrets, infrastructure bindings, and deployment parameters may differ.

---

# 56. Test Coverage

Backend coverage minimums are defined **per layer**, per `SDD_PLIUS_STRATEGY.md` §9.1. A flat threshold
would allow the Domain layer — where the critical business rules live — to sit at the same bar as
infrastructure plumbing, which is the wrong trade.

| Layer | Minimum |
|---|---|
| Domain | **90%** |
| Application | **80%** |
| Infrastructure | **70%** |
| Presentation | **70%** |

Minimum frontend coverage:

```text
80%
```

`SDD_PLIUS_STRATEGY.md` §9 does not cover the frontend stack, so the frontend threshold is set by this
document.

The pipeline must fail if any required coverage drops below its threshold.

**Every `BR-*` business rule must have at least one unit test.**

Coverage metrics must reflect meaningful business logic rather than trivial implementation details tested
only to raise percentages.

---

# 57. Backend Testing

Primary backend unit testing framework:

```text
NUnit
```

Supporting tools may include appropriate mocking, assertion, fixture, or integration-test libraries.

Tests should follow a consistent structure such as:

```text
Arrange
Act
Assert
```

or another clearly documented convention.

Important business rules require explicit tests.

---

# 58. Frontend Testing

Primary frontend testing stack:

* Jest.
* React Testing Library.

Tests should prioritize user-visible behavior instead of implementation details.

Important coverage areas include:

* Components.
* Hooks.
* Authentication flows.
* Guards.
* Forms.
* Validation.
* Mutations.
* Error handling.
* Important user workflows.

---

# 59. Additional Testing

Where valuable, the project may include:

* Integration tests.
* API tests.
* Repository tests.
* End-to-end tests.
* Container smoke tests.

A smaller number of valuable tests is preferable to large numbers of meaningless tests.

---

# 60. CD

Continuous Delivery must deploy immutable Docker images produced by the single CI process.

Initial environments:

```text
Development

Staging

Production
```

The same application artifacts must be promoted through:

```text
Development
    ↓
Staging
    ↓
Production
```

CD must not compile or rebuild application source code.

Each environment may provide different:

* Configuration.
* Secrets.
* Connection strings.
* Scaling configuration.
* Infrastructure settings.
* External service endpoints.

These differences must be provided through deployment/runtime configuration and must not produce different application builds.

Staging deployment can occur automatically after successful CI according to the deployment strategy.

Production deployment requires explicit manual approval.

The deployment process must maintain traceability between:

* Git commit.
* CI execution.
* Docker image.
* Deployment.
* Environment.
* Production approval.

---

# 61. Production Approval

Production must never deploy automatically immediately after build completion.

Azure DevOps Environments should protect Production through an approval/check.

Only authorized users may approve Production deployment.

The pipeline should clearly identify:

* Artifact version.
* Docker image version.
* Source commit.
* Environment.
* Deployment result.

---

# 62. Secrets in CI/CD

Azure DevOps pipelines must not contain plaintext secrets.

Secrets should be obtained securely from Azure Key Vault.

Access should use secure Azure identities/service connections.

Where supported, workload identity federation or managed identity should be preferred over long-lived credentials.

---

# 63. Branching Strategy — Trunk-Based Development

The repository will follow Trunk-Based Development.

The primary integration branch is:

```text
main
```

`main` represents the trunk.

Developers must work using short-lived branches.

Examples:

```text
feature/book-search
feature/borrow-book
fix/token-refresh
chore/update-dependencies
```

Branches should exist only for the duration required to implement and validate a small change.

Changes must be integrated into `main` frequently.

Long-lived branches must be avoided.

Environment-specific branches must not be used.

The following branches must not represent environments:

```text
develop
staging
production
```

Deployment environments are deployment targets, not source-control branches.

Pull Requests are required before merging changes into `main`.

PR validation must execute before merge.

After a successful merge into `main`, the CI pipeline must produce the immutable deployable artifacts.

Those artifacts are then promoted through the deployment environments without rebuilding them.

The strategy follows:

```text
Developer
    ↓
Short-lived branch
    ↓
Pull Request
    ↓
PR Validation
    ↓
main
    ↓
CI
    ↓
Immutable Artifact
    ↓
Development
    ↓
Staging
    ↓
Manual Production Approval
    ↓
Production
```

When incomplete functionality needs to be integrated safely into the trunk, Feature Flags may be used where justified instead of creating long-lived branches.

The trunk must remain in a releasable state.

---

# 64. Pull Request Quality Gates

The `main` branch must be protected.

Direct pushes to `main` should not be allowed under normal development workflows.

A Pull Request should not be mergeable when:

* Backend build fails.
* Frontend build fails.
* Tests fail.
* Coverage is below the required threshold.
* Linting fails.
* Type checking fails.
* Required reviewers have not approved.
* Required pipeline checks fail.
* Blocking review comments remain unresolved.

PR validation should execute CI validation automatically.

A successful merge to `main` must not knowingly leave the application in a broken or non-releasable state.

---

# 65. Database Deployment

Entity Framework migrations will be version controlled.

Database schema changes should be explicitly applied during deployment.

Production migrations should:

* Be visible.
* Be traceable.
* Fail safely.
* Execute before deploying incompatible application versions where necessary.

Rollback implications must be considered for destructive migrations.

---

# 66. Observability

The deployed application should provide visibility into:

* Application errors.
* HTTP failures.
* Request latency.
* Dependency failures.
* Application health.
* Deployment version.

Azure-native observability services may be used.

Observability must not expose sensitive data.

---

# 67. Auditability

Important administrative operations should support audit information where reasonable.

Examples:

* Book created.
* Book updated.
* Book deleted.
* Book borrowed.
* Book returned.

Audit information may include:

* User.
* Action.
* Entity.
* Timestamp.

The exact audit implementation will be defined separately.

---

# 68. Performance

The application should avoid:

* N+1 database queries.
* Unbounded result sets.
* Excessive HTTP requests.
* Unnecessary React rerenders.
* Duplicate API calls.
* Large unnecessary payloads.

Backend list endpoints should support pagination.

Frontend server state should use caching appropriately.

---

# 69. Accessibility

Frontend implementation should consider accessibility from the beginning.

Requirements include:

* Semantic HTML.
* Keyboard navigation.
* Accessible forms.
* Labels.
* Focus management.
* Appropriate contrast.
* Accessible dialogs.
* Screen-reader compatible controls where practical.

Material UI accessibility capabilities should be used correctly rather than assumed automatically.

---

# 70. Responsive Design

The application must support:

* Desktop.
* Tablet.
* Mobile.

Desktop should provide the richest library-management experience.

Mobile workflows should remain usable.

---

# 71. AI Features

AI features must solve a real user problem and must not exist only as a demonstration.

The selected capability is **personalised book recommendations**, generated from a member's reading history.

## 71.1 Per-library credentials

Each library supplies **its own model provider credentials**, managed by that library's staff — never by members, and never as a single platform-wide key.

* Supported providers: **Anthropic (Claude)** and **OpenAI**, behind a single provider abstraction.
* Configuration per library: provider, model, agent, and API key.
* The configuration screen must test the connection before saving.
* Members of a connected library receive model-generated recommendations. Everywhere else, the system falls back to a most-borrowed ranking.

## 71.2 Plan gating

Recommendations are available to **Plus and Max members only**. Basic members never see this surface; they are shown an upgrade path instead.

## 71.3 Non-negotiable rules

* API keys live **exclusively in the backend**, encrypted at rest, supplied by environment variable or secret store. They must never be returned by any API response, logged, or persisted in plaintext.
* The frontend must **never** call Anthropic or OpenAI directly. Every call is proxied by the backend.
* **Data minimisation**: only aggregated, anonymised reading data is sent to the provider. Never the member's email, full name, address, or internal identifiers.
* Recommendations must be **cached**, regenerated on demand or when activity changes significantly — never on every screen render.
* Rate limiting per member on AI calls.
* Every call must record provider, model, input and output tokens, latency, and estimated cost.
* **Graceful degradation**: if the provider fails, show the last cached result with its timestamp, or the most-borrowed fallback. Never a raw error.

A test asserting that no API response can expose a stored key is mandatory.

---

# 72. Documentation

The repository must contain a complete README.

The README should eventually include:

* Project overview.
* Architecture overview.
* Technology stack.
* Requirements.
* Local setup.
* Docker instructions.
* Database instructions.
* Testing.
* Environment variables.
* Terraform instructions.
* Deployment.
* Application URLs.
* Demo users where appropriate.
* Architecture decisions.
* AI-assisted development approach.

---

# 73. Architecture Decision Records

Important technical decisions must be documented as ADRs.

**ADRs are not standalone files.** Per `SDD_PLIUS_STRATEGY.md` §5.2, the Architecture Decision Log lives
inside the spec of the thing it governs, so a decision sits next to the code and rules it constrains:

```text
sdd_strategy/
    specs/
        global_tech_spec.md                          ← section 4: project-wide decisions
        domains/
            {domain}/
                {domain}.technical.md                ← section 4: decisions local to that domain
```

A decision belongs in `global_tech_spec.md` when it binds more than one domain — stack choices, the money
representation, CQRS conventions, coverage thresholds. It belongs in a domain's `technical.md` when only
that domain is affected.

Every decision log entry must record:

* Decision.
* Choice.
* Rationale.
* **Alternatives rejected** — mandatory. A decision without rejected alternatives is not a decision, it is
  a default.

When a decision changes, the old entry **moves to the Superseded Decisions section** of the same file with
the date and reason. It is never deleted.

---

# 74. Claude Code Development Rules

Claude Code will be used as the primary AI development assistant.

Claude must follow project specifications rather than independently redesigning architecture.

A dedicated `CLAUDE.md` will eventually define:

* Project architecture.
* Coding rules.
* Allowed dependencies.
* Testing expectations.
* Commands.
* Repository structure.
* Patterns.
* Validation requirements.
* Security rules.
* Definition of Done.

Claude must respect the Trunk-Based Development strategy.

Claude must not propose:

* `develop` branches.
* `staging` branches.
* `production` branches.
* Long-lived feature branches.
* Environment-specific application builds.

Claude must assume:

```text
main = trunk
```

Application artifacts must be built once by CI and promoted through environments.

Environment differences must be handled through configuration, secrets, infrastructure, or deployment parameters.

When generating Azure infrastructure, Claude must follow the project's Azure resource naming convention and the naming restrictions applicable to each Azure resource type.

When generating Terraform, resource naming and tagging logic must be centralized and reusable.

Additional Claude skills or specialized agents may be created for specific engineering responsibilities.

Their definitions are outside the scope of this initial architecture document.

---

# 75. SDD+ Development Workflow

Development will follow the conceptual process:

```text
Requirements
      ↓
Product Scope
      ↓
Architecture
      ↓
Specifications
      ↓
Implementation Plan
      ↓
Task Breakdown
      ↓
Implementation
      ↓
Automated Validation
      ↓
Review
      ↓
Acceptance
```

Claude must not skip specification stages for significant features.

---

# 76. Definition of Done

A feature is not complete only because code has been generated.

A feature is complete when applicable requirements are satisfied:

* Requirement implemented.
* Architectural boundaries respected.
* Backend tests created.
* Frontend tests created.
* Tests passing.
* Required coverage maintained.
* Validation implemented.
* Error states handled.
* Loading states handled.
* Authorization implemented.
* Logging implemented where appropriate.
* No secrets introduced.
* No known code smells.
* UI reviewed.
* Accessibility considered.
* Documentation updated.
* CI successful.

---

# 77. Non-Goals

The project should deliberately avoid unnecessary complexity.

Unless later justified, the following are not required:

* Microservices.
* Kubernetes.
* Event-driven distributed architecture.
* Service mesh.
* Complex distributed caching.
* Multiple databases.
* Multiple repositories.
* Enterprise-scale identity infrastructure.
* Premature abstraction for unsupported cloud providers.

This project should demonstrate engineering maturity through appropriate design, not through the number of technologies used.

---

# 78. Engineering Philosophy

The architecture should optimize for:

```text
Correctness
    >
Clarity
    >
Maintainability
    >
Testability
    >
Extensibility
    >
Performance optimization
    >
Architectural sophistication
```

Complexity must be justified by product requirements.

---

# 79. Initial Success Criteria

The final application should demonstrate that:

1. The complete library workflow works.
2. A real user can understand the interface without technical knowledge.
3. Authentication and permissions work correctly.
4. The architecture is understandable.
5. Business logic is testable.
6. Infrastructure can be reproduced.
7. CI/CD is automated.
8. A single CI produces immutable artifacts.
9. The same artifacts are promoted across environments.
10. Production deployment requires approval.
11. Azure resources follow the defined naming convention.
12. Secrets are protected.
13. Monitoring exists.
14. The project can be executed locally.
15. The deployed version can be tested externally.
16. AI tooling was used within a controlled engineering process.
17. The project demonstrates production-oriented software engineering rather than only CRUD implementation.
