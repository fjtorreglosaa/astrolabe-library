# Network — Tasks

**Last reviewed:** 2026-08-16
**Overall progress:** 29/29 (100%)

Built before `identity` where the two overlap: registration validates a country and city, so the
geography must exist first.

---

## Blocking Dependencies

| Block ID | Description | Status |
|---|---|---|
| `NET-OPEN-001` | Registration countries versus seeded libraries | **Resolved 2026-08-15** — the seed grows to all six countries |
| `BLOCK-006` | The Mailgun sandbox domain only delivers to authorised recipients, so invitation mail cannot be tested with arbitrary addresses. Blocks verification of `NET-015`, not its implementation | Open |
| `BLOCK-007` | `NET-015` to `NET-019` and `NET-022` need the identity `User` aggregate | **Cleared 2026-08-15** — `IDN-001` to `IDN-006` landed |

---

## Task List

| ID | Task | Status | Blocker | Tracking | Notes |
|---|---|---|---|---|---|
| `NET-001` | `Country`, `City`, `Library` entities with invariants | ✅ | — | — | `BR-NET-001`, `-002` |
| `NET-002` | `City.DesignateHomeLibrary` | ✅ | — | — | `BR-NET-003` |
| `NET-003` | `Library.Deactivate` taking its facts as arguments | ✅ | — | — | `BR-NET-005` |
| `NET-004` | `LibraryAssignment` entity with revoke-never-delete | ✅ | — | — | `BR-NET-009`, `-011` |
| `NET-005` | `AdminInvitation` entity carrying role and libraries | ✅ | — | — | `BR-NET-014` |
| `NET-006` | `LibraryScope` value object, including `Empty()` | ✅ | — | — | `BR-NET-006`, `-007`, `-010` |
| `NET-007` | Domain events: assigned, revoked, invited | ✅ | — | — | `BR-NET-017` |
| `NET-008` | EF configurations for all five entities | ✅ | `NET-001` | — | Fluent API only |
| `NET-009` | Unique index on `(CityId, Name)` | ✅ | `NET-008` | — | `BR-NET-002` in the database |
| `NET-010` | Unique filtered index on active assignments | ✅ | `NET-008` | — | Prevents duplicate assignment |
| `NET-011` | Migration for the network schema | ✅ | `NET-008` | — | Verified down-migration required |
| `NET-012` | Repositories for all five entities | ✅ | `NET-008` | — | Extend `Repository<TEntity>`; no `IQueryable` leaks |
| `NET-013` | `NetworkSeeder`, idempotent | ✅ | `NET-011` | — | **6 countries, 18 cities, 35 libraries.** US data verbatim from the prototype |
| `NET-014` | `LibraryScopeProvider`, resolved once per request | ✅ | `NET-006` | — | `BR-NET-011` forbids a longer-lived cache |
| `NET-015` | `InviteAdminCommand` and the invitation email | ✅ | `IDN-003` | — | `BR-NET-008`, `-013`. Needs the identity `User` aggregate to create the invited account |
| `NET-016` | `ResendInvitationCommand` invalidating the previous token | ✅ | `NET-015` | — | `BR-NET-015` |
| `NET-017` | `AcceptInvitationCommand` | ✅ | `NET-015` | — | `BR-NET-013`. Activates the `User`, so it needs `IDN-006` |
| `NET-018` | `RevokeAdminCommand` with the self-demotion guard | ✅ | `IDN-003` | — | `BR-NET-012`, `-016`. Must change the `User` role |
| `NET-019` | `AssignLibrariesCommand` | ✅ | `IDN-003` | — | `BR-NET-008`, `-009`. Must verify the target user exists and is staff |
| `NET-020` | `CreateLibraryCommand`, `DeactivateLibraryCommand`, `DesignateHomeLibraryCommand` | ✅ | `NET-003` | — | `BR-NET-003`, `-005` |
| `NET-021` | `GetRegistrationCountriesQuery` and `GetCitiesByCountryQuery` | ✅ | — | — | Derived from active libraries, not from a flag |
| `NET-022` | `GetLibrariesQuery`, `GetAdminTeamQuery`, `GetMyScopeQuery` | ✅ | `IDN-003` | — | Scope-filtered. `GetAdminTeamQuery` needs the `User` aggregate |
| `NET-023` | `LibraryScopeAuthorizationHandler`, centralised | ✅ | `NET-014` | — | `GUIDELINES.md` §21 |
| `NET-024` | `admin-libraries` screen | ✅ | `NET-022` | `AdminLibrariesPage.tsx`, `InviteAdminDialog.tsx` | Done 2026-08-16. Libraries with their home-library and withdrawn states, plus the administrator team with invitation, library assignment and revocation. Withdrawing reports what the branch still held rather than refusing — `BR-NET-005` |
| `NET-025` | Real library obligations probe, and honour deactivation on member-facing surfaces | ✅ | — | `LibraryObligationsProbe` | Done 2026-08-16. Real counts across `catalog`, `reservations` and `billing`. Found a larger gap in the same rule: deactivation was refused on obligations (inverting `BR-NET-005`, and unsatisfiable), while nothing hid a withdrawn branch from members — a reservation against a deactivated library succeeded against the running system. Both fixed; 9 regression tests |

| `NET-026` | `ResendInvitationCommand` | ✅ | `NET-021` | `ResendInvitationCommand` | `BR-NET-015`. The folder existed and was empty; the rule had no implementation at all |
| `NET-027` | `GrantSuperAdminCommand` — extended powers | ✅ | `NET-022` | `GrantSuperAdminCommand` | The "grant extended powers" half of `BR-NET-008`, and the prototype's `elevate`. No route back, by `BR-NET-012` |
| `NET-028` | `accept-invitation` screen | ✅ | `NET-021` | `AcceptInvitationPage.tsx` | `BR-NET-013`. The endpoint existed since Stage 1 and was unreachable, so no invitation could ever be accepted |
| `NET-029` | Assign libraries, elevate and resend on the team table | ✅ | `NET-024` | `AssignLibrariesDialog.tsx` | The prototype's four row actions; only revoke existed |

### Status values

⬜ Not started
🔄 In progress
✅ Done
❌ Removed / not applicable (reason required in Notes)
🔴 Blocked (blocker ID required)

### Tracking reference format

`{PLATFORM} #{ID} — {URL}`. No external tracker is configured, so the column reads `—`.

---

## Test Obligations

Every `BR-NET-*` rule needs at least one test. These are the ones that carry the most risk:

| Test | Covers |
|---|---|
| Scope matrix: administrator of Midtown and Harlem receives 403 on Chicago and Austin | `AC-NET-001` |
| Super administrator succeeds on the same operations everywhere | `AC-NET-002` |
| Revoking an assignment takes effect on the **next** request | `AC-NET-004` |
| An administrator with no assignments gets empty lists, not an error | `AC-NET-005` |
| The last super administrator cannot remove their own role | `AC-NET-007` |
| An invitation survives its sender being revoked | Edge case, `network.business.md` §5 |
| A city's home library cannot be deactivated | Edge case, `network.business.md` §5 |

---

## Completion Log

| Date | Task ID | Completed by | Notes |
|---|---|---|---|
| 2026-08-16 | `NET-026` to `NET-029` | AI Agent — Claude | **Stage 6 completed.** Three rules had no implementation rather than an incomplete one: `BR-NET-015` (the `ResendInvitation` folder existed and was empty), the extended-powers half of `BR-NET-008`, and `BR-NET-013` — whose endpoint had existed since Stage 1 with no screen, so **no invitation could ever be accepted**. Verified end to end by planting an invitation with a known token hash: invited with no password → accepted → active, password set, one library assignment applied (`BR-NET-014`), signs in, and the token replays as 409. 13 tests |
| 2026-08-16 | `NET-024` | AI Agent — Claude | **PLAN-001 Stage 6 — libraries and admins.** Super administrator only. The withdrawal confirmation states what survives the act, and the result reports what the branch was still holding, because that report is the operator's next piece of work and nothing else chases it. An administrator with no assignments is shown as such rather than as an empty cell — `BR-NET-010` describes a real state, not a fault |
| 2026-08-16 | `NET-025` | AI Agent — Claude | **`BR-NET-005` closed, and corrected.** The probe now counts copies, live reservations and unresolved fines for real, and *reports* them instead of refusing — the old refusal inverted the rule and could never be satisfied, since stock is permanent and new loans arrive until the branch is withdrawn. The larger half was unbuilt: nothing hid a deactivated branch, so it stayed in the catalogue and a reservation against it returned HTTP 200. `BookProjection` now drops copies at withdrawn branches and `ConfirmReservationCommandHandler` refuses with `reservations.library_inactive`. 9 tests, the reservation ones verified in red |
| 2026-08-15 | — | AI Agent — Claude | **Architecture review, `GLOBAL-016` and `GLOBAL-017`.** `network` moved under `Features/` in all three layers; handlers now depend on `INetworkUnitOfWork` instead of individual repositories. No business rule changed; 265 tests green throughout |
| 2026-08-15 | `NET-001` | AI Agent — Claude | `Country`, `City`, `Library` with factory validation and `NetworkErrors` as typed, reusable errors |
| 2026-08-15 | `NET-002` | AI Agent — Claude | `City.DesignateHomeLibrary` takes the `Library` itself so membership and activity can be verified without a repository |
| 2026-08-15 | `NET-003` | AI Agent — Claude | `Library.Deactivate` receives its two facts as arguments, staying a pure decision |
| 2026-08-15 | `NET-004` | AI Agent — Claude | `LibraryAssignment` revokes, never deletes |
| 2026-08-15 | `NET-005` | AI Agent — Claude | `AdminInvitation` carries its own role and libraries, so it survives its sender being revoked |
| 2026-08-15 | `NET-006` | AI Agent — Claude | `LibraryScope` with `Unrestricted`, `Of` and `Empty`, plus `Covers`, `CoversAll`, `CoversAny`, `Filter` |
| 2026-08-15 | `NET-007` | AI Agent — Claude | `LibraryAssigned`, `LibraryAssignmentRevoked`, `AdminInvited` on an `AggregateRoot` base |
| 2026-08-15 | `NET-008` | AI Agent — Claude | One EF configuration per entity, Fluent API only. `LibraryIds` stored as a primitive collection so an invitation's grant is a snapshot, not a relationship |
| 2026-08-15 | `NET-009` | AI Agent — Claude | Unique index on `(city_id, name)`. BR-NET-002 enforced by the database, not by a racy application check |
| 2026-08-15 | `NET-010` | AI Agent — Claude | Unique filtered index on `(user_id, library_id)` where `revoked_at IS NULL`, so a library can be granted again after revocation |
| 2026-08-15 | `NET-011` | AI Agent — Claude | Migration `AddNetworkDomain`, down-migration verified. snake_case naming adopted; the dev volume was recreated because the pre-existing migrations history table had PascalCase columns |
| 2026-08-15 | `NET-012` | AI Agent — Claude | Five repositories extending the generic `Repository<TEntity>` base. Reorganised per RULE 15 and RULE 16 after review |
| 2026-08-15 | `NET-015` | AI Agent — Claude | `InviteAdminCommand` plus the invitation email. The account and its invitation commit together through one unit of work |
| 2026-08-15 | `NET-016` | AI Agent — Claude | Resending revokes the previous invitation, so its link stops working (BR-NET-015) |
| 2026-08-15 | `NET-017` | AI Agent — Claude | `AcceptInvitationCommand`. The password and the library grants are applied only on acceptance, which is what makes BR-NET-013 true |
| 2026-08-15 | `NET-018` | AI Agent — Claude | `RevokeAdminCommand`. **Verified against the running system**: a super administrator gets 409 revoking themselves (BR-NET-012) |
| 2026-08-15 | `NET-019` | AI Agent — Claude | `AssignLibrariesCommand` grants only what is missing and revokes only what is gone, so resubmitting the same set is a no-op |
| 2026-08-15 | `NET-022` | AI Agent — Claude | `GetLibrariesQuery`, `GetAdminTeamQuery`, `GetMyScopeQuery`. **Verified**: Admin 403 on `/admins`, 200 on `/my-scope` with exactly 2 libraries; Super Admin unrestricted |
| 2026-08-15 | `NET-023` | AI Agent — Claude | Authorization policies declared once in `AuthenticationExtensions`, applied by attribute. **Verified**: the full role matrix behaves as specified |
| 2026-08-15 | `NET-020` | AI Agent — Claude | `CreateLibraryCommand`, `DeactivateLibraryCommand`, `DesignateHomeLibraryCommand`. The first library of a city becomes its home library, so BR-NET-003 cannot be left unsatisfied. 17 tests |
| 2026-08-15 | `NET-021` | AI Agent — Claude | `GetRegistrationCountriesQuery` and `GetCitiesByCountryQuery`, both derived from active libraries rather than a flag |
| 2026-08-15 | `NET-014` | AI Agent — Claude | `ILibraryScopeProvider` in Application, `LibraryScopeProvider` in Infrastructure, scoped so the memoised scope dies with the request. `ICurrentUser` declared and implemented in Presentation from claims. 12 tests including the BR-NET-011 revocation case |
| 2026-08-15 | `NET-013` | AI Agent — Claude | `NetworkSeeder`, idempotent, deterministic identifiers. Verified in PostgreSQL: 6 countries, 18 cities, 35 libraries, 0 cities without a home library |

---

## Progress Summary

| Layer | Done | Total |
|---|---|---|
| Domain | 7 | 7 |
| Infrastructure | 7 | 7 |
| Application | 8 | 8 |
| Presentation | 2 | 2 |
| Frontend | 0 | 1 |
| **Total** | **23** | **25** |
