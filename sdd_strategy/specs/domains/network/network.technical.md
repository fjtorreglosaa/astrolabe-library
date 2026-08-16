# Network — Technical Specification

**Last reviewed:** 2026-08-16
**Reviewed by:** AI Agent — Claude — 2026-08-16
**Version:** 1
**Implements:** `BR-NET-001` to `BR-NET-017`

---

## 1. Domain Model

Three reference entities and one authority service. The geography is small, near-static, and read on
almost every request, which shapes every decision below.

### Country, City, Library

```csharp
public sealed class Country : Entity
{
    public string Name { get; private set; }
    public string IsoCode { get; private set; }
    public bool IsAvailableForRegistration { get; private set; }
}

public sealed class City : Entity
{
    public Guid CountryId { get; private set; }
    public string Name { get; private set; }
    public Guid? HomeLibraryId { get; private set; }   // BR-NET-003

    public Result DesignateHomeLibrary(Library library);
}

public sealed class Library : Entity
{
    public Guid CityId { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }

    public Result Deactivate(bool isCityHomeLibrary);   // BR-NET-005
}
```

`Library.Deactivate` takes the two facts it needs as arguments rather than reaching for repositories.
The aggregate stays a pure decision; the handler gathers the facts. That is what keeps the rule
testable without a database.

### LibraryAssignment

```csharp
public sealed class LibraryAssignment : Entity
{
    public Guid UserId { get; private set; }
    public Guid LibraryId { get; private set; }
    public Guid GrantedByUserId { get; private set; }
    public DateTimeOffset GrantedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsActive => RevokedAt is null;
    public void Revoke(DateTimeOffset now);
}
```

Assignments are revoked, never deleted, so `BR-NET-017` has something to audit against.

### AdminInvitation

```csharp
public sealed class AdminInvitation : Entity
{
    public Guid UserId { get; private set; }
    public UserRole Role { get; private set; }
    public IReadOnlyList<Guid> LibraryIds { get; private set; }
    public InvitationTokenHash TokenHash { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public Result Accept(DateTimeOffset now);
}
```

The invitation carries its own role and libraries. That is what makes the edge case in
`network.business.md` work: an invitation stays valid even if its sender is later revoked.

### LibraryScope — the authority

```csharp
public sealed record LibraryScope
{
    public bool IsUnrestricted { get; }              // super administrator
    public IReadOnlySet<Guid> LibraryIds { get; }

    public bool Covers(Guid libraryId);
    public bool CoversAll(IEnumerable<Guid> libraryIds);

    public static LibraryScope Unrestricted();
    public static LibraryScope Of(IEnumerable<Guid> libraryIds);
    public static LibraryScope Empty();              // administrator with no assignments
}
```

This is the single answer to "may this staff user act here". `billing`, `catalog`, `reservations`,
and `support` all consume it. `LibraryScope.Empty()` is a first-class value so `BR-NET-010` — an
unassigned administrator sees empty lists, not an error — falls out of the type rather than needing a
null check at every call site.

### Domain events

| Event | Raised when | Consumed by |
|---|---|---|
| `LibraryAssigned` | An assignment is granted | Audit, and scope cache eviction |
| `LibraryAssignmentRevoked` | An assignment is revoked | Audit, and scope cache eviction |
| `AdminInvited` | An invitation is created | Triggers the invitation email |

---

## 2. Application Layer

### Commands

| Name | Input | Output | Rule |
|---|---|---|---|
| `InviteAdminCommand` | email, fullName, role, libraryIds, message | `Result<Guid>` | `BR-NET-008`, `-013`, `-014` |
| `ResendInvitationCommand` | invitationId | `Result` | `BR-NET-015` |
| `AcceptInvitationCommand` | token | `Result` | `BR-NET-013` |
| `RevokeAdminCommand` | userId | `Result` | `BR-NET-012`, `-016` |
| `AssignLibrariesCommand` | userId, libraryIds | `Result` | `BR-NET-006`, `-008`, `-009`, `-011` |
| `CreateLibraryCommand` | cityId, name | `Result<Guid>` | `BR-NET-001`, `-002` |
| `DeactivateLibraryCommand` | libraryId | `Result<LibraryObligations>` | `BR-NET-005` |
| `DesignateHomeLibraryCommand` | cityId, libraryId | `Result` | `BR-NET-003` |

### Queries

| Name | Input | Output | Rule |
|---|---|---|---|
| `GetRegistrationCountriesQuery` | — | `Result<IReadOnlyList<CountryDto>>` | `BR-NET-004` |
| `GetCitiesByCountryQuery` | countryId | `Result<IReadOnlyList<CityDto>>` | `BR-NET-004` |
| `GetLibrariesQuery` | cityId? | `Result<IReadOnlyList<LibraryDto>>` | `BR-NET-006` |
| `GetAdminTeamQuery` | — | `Result<IReadOnlyList<AdminDto>>` | `BR-NET-007`, `-008` |
| `GetMyScopeQuery` | — | `Result<LibraryScopeDto>` | `BR-NET-006`, `-010` |

Thirteen operations. Under the SDD+ §6.2 threshold, with headroom.

`GetRegistrationCountriesQuery` returns only countries that have at least one city with at least one
active library — the mechanism that keeps `BR-NET-004` true no matter what the seed data contains.
See `NET-OPEN-001` below.

---

## 3. Infrastructure

| Concern | Implementation |
|---|---|
| Persistence | One repository per entity, each extending the generic `Repository<TEntity>` base |
| EF configuration | One configuration class per entity, Fluent API only |
| Scope resolution | `LibraryScopeProvider`, resolving the caller's scope once per request |
| Authorization | `LibraryScopeAuthorizationHandler`, an ASP.NET Core authorization handler |
| Seed data | `NetworkSeeder`, idempotent, applied after migrations |

### Scope resolution and caching

Scope is resolved **once per request** and held for that request's lifetime, never longer.
`BR-NET-011` requires a revoked assignment to take effect on the next request; a cache outliving the
request would break exactly that. The cost is one indexed query per staff request, which is
acceptable at 35 libraries.

Authorization is centralised in `LibraryScopeAuthorizationHandler` rather than repeated in
controllers, as `GUIDELINES.md` §21 requires. A handler asks the scope; it never asks the database
what an administrator may do.

### Layout

Per RULE 15, the domain segregates by kind and the namespace matches the folder:

```text
Domain/Network/Entities/        Country, City, Library, LibraryAssignment, AdminInvitation
Domain/Network/ValueObjects/    LibraryScope
Domain/Network/Events/          LibraryAssigned, LibraryAssignmentRevoked, AdminInvited
Domain/Network/Errors/          NetworkErrors
Domain/Network/Repositories/    ICountryRepository, ICityRepository, ILibraryRepository,
                                ILibraryAssignmentRepository, IAdminInvitationRepository
```

Each repository contract extends `IRepository<TEntity>` and declares only what is specific to its
entity. Generic operations — by identifier, by predicate, paged, counted, staged for insertion — are
never redeclared.

### Persistence notes

- `Library.Name` carries a unique index on `(CityId, Name)`, enforcing `BR-NET-002` in the database.
- `City.HomeLibraryId` is nullable in the schema but never null in practice once seeded. It cannot be
  non-nullable because a city and its libraries are inserted in the same transaction.
- `LibraryAssignment` carries a unique filtered index on `(UserId, LibraryId)` where `RevokedAt IS NULL`,
  so the same library cannot be assigned twice to one administrator.
- Deactivation is a flag. Nothing in this domain is ever hard-deleted.

---

## 4. Architecture Decision Log

| Decision | Choice | Rationale | Alternatives rejected |
|---|---|---|---|
| What blocks deactivating a library | Only being the city's home library. Copies, live loans and unpaid fines are reported | `BR-NET-005` lists those three as what blocks a *deletion* and offers deactivation as the alternative that preserves history, so refusing on them inverted it. It also could not converge — stock is permanent and new reservations keep arriving until the branch stops taking them, which is what deactivating does. The operator gets a count instead, and a warning log, because a branch withdrawn with work on it needs a human | Refusing on any obligation — rejected: unsatisfiable, so it left no way to wind a branch down. Refusing on live reservations only — rejected: same deadlock, since new ones keep arriving. A two-phase suspend-then-withdraw — rejected: invents product the prototype does not have |
| Scope as a value object | `LibraryScope`, consumed by every other domain | Puts `BR-NET-006` in exactly one place. Five domains asking the database independently would give five chances to get it wrong | Each domain querying assignments itself — rejected: duplicated rule, guaranteed drift |
| Empty scope representation | `LibraryScope.Empty()` as a real value | `BR-NET-010` says an unassigned administrator sees empty lists, not an error. A null scope would invite a null check at every call site, and one would be forgotten | Null or an exception — rejected: turns a valid state into an error path |
| Scope cache lifetime | Per request, no longer | `BR-NET-011` demands the next request reflect a revocation. Any longer-lived cache contradicts the rule | Cached per session or in memory with TTL — rejected: would leave revoked administrators acting for the TTL |
| Library removal | Deactivate, never delete | Historical reservations, fines and audit entries reference libraries. Deleting one would orphan them | Hard delete with cascade — rejected: destroys financial and audit history |
| Registration country list | Derived from active libraries, not a static flag | Makes `BR-NET-004` structurally true. A country cannot be offered into an empty branch even if someone sets a flag wrongly | Static `IsAvailableForRegistration` flag alone — rejected: it is a second source of truth that can disagree with reality. The column stays as an override to *hide* a country, never to expose an empty one |
| Invitation payload | Role and libraries carried on the invitation | Lets an invitation survive its sender being revoked, and makes the grant reviewable before acceptance | Applying role and libraries at send time — rejected: would grant access before the recipient confirms, breaking `BR-NET-013` |
| Self-demotion guard | Checked in the handler against the acting user | `BR-NET-012` needs the caller's identity, which an aggregate must not know about | Domain-level check — rejected: would force `ICurrentUser` into the Domain layer, violating RULE 3 |
| Repository contracts | Generic `IRepository<TEntity>` extended by one interface per entity | Removes the boilerplate every repository would repeat, while keeping domain-specific capability on the concrete contract. Established as RULE 16 | Five standalone interfaces — rejected: every one restated the same six methods. A single generic repository with no concrete contracts — rejected: it would push query construction into handlers |
| Predicate type | `Expression<Func<TEntity, bool>>` | Translatable to SQL by the provider. `System.Linq.Expressions` ships with the runtime, so the Domain layer keeps zero external packages | `Func<TEntity, bool>` — rejected: compiles to a delegate, forcing the whole table into memory before filtering |
| Read tracking | Generic reads return tracked entities | Returning a detached entity that a caller mutates and expects to save is silent data loss. Correctness outweighs the tracking cost at this scale | Untracked by default — rejected on that failure mode. Concrete repositories still use the untracked query for projections |
| Unbounded listing | `GetPagedAsync` with `PagedResult<T>`, page size capped at 100 | `GUIDELINES.md` §68 forbids unbounded result sets and §25 requires paging. A cap is what stops a caller defeating paging by asking for a million rows | `GetAllAsync` only — rejected: kept for bounded reference data, but it cannot be the general listing mechanism |

---

## 5. Dependencies

**This domain depends on:**

| Domain | For |
|---|---|
| `identity` | The user record an assignment attaches to, and `ICurrentUser` for the self-demotion guard |

**Domains that depend on this one:**

| Domain | For |
|---|---|
| `identity` | Validating country and city at registration |
| `membership` | Resolving the home library from the city of residence |
| `catalog` | Which library holds a copy |
| `billing`, `reservations`, `support` | `LibraryScope`, to filter staff-facing data |

**External services:** Mailgun, through `IEmailSender`, for invitation mail.

---

## 6. Known Constraints and Limitations

- Scope is resolved with a database query per staff request. Correct and fast at 35 libraries; it
  would need a cache with explicit invalidation at a thousand.
- Libraries carry no address, opening hours, or coordinates. The prototype shows a branch name only.
- There is no library hierarchy. A library belongs to a city and nothing else.
- `NET-OPEN-001` is **resolved**: the seed covers all six countries, 18 cities and 35 libraries.
  `GetRegistrationCountriesQuery` still derives its list from active libraries rather than trusting a
  flag, so the rule holds even if seed data is later trimmed.
- The fifteen non-United-States cities are invented seed data. Only the five United States libraries
  come from the approved prototype.

---

## 7. Superseded Decisions

| Decision | Superseded by | Reason | Date |
|---|---|---|---|
| — | — | None yet | — |
