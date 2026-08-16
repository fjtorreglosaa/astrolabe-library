# Identity — Tasks

**Last reviewed:** 2026-08-15
**Overall progress:** 42/42 (100%)

Built after `network`, because registration validates a country and city that must already exist.

---

## Blocking Dependencies

| Block ID | Description | Status |
|---|---|---|
| `BLOCK-004` | The *Devices and sessions* screen does not exist in the prototype. Blocks acceptance of `IDN-035`, not its implementation | Open |
| `BLOCK-006` | The Mailgun sandbox domain only delivers to recipients authorised in the dashboard. **Registration cannot be tested end to end with arbitrary addresses.** Blocks verification of the registration email only | Open |
| `NET-013` | The network seed must exist before registration can resolve a country and city | **Cleared** — seeded and verified |

---

## Task List

### Domain

| ID | Task | Status | Blocker | Tracking | Notes |
|---|---|---|---|---|---|
| `IDN-001` | `Email`, `PasswordHash`, `RefreshTokenHash`, `DeviceDescriptor` value objects | ✅ | — | — | `PasswordHash` must have no `ToString` override |
| `IDN-002` | `UserRole`, `UserStatus`, `DeviceType`, `SessionRevocationReason` enumerations | ✅ | — | — | |
| `IDN-003` | `User` aggregate with `Register` and `Invite` | ✅ | `IDN-001` | — | `BR-IDN-001`, `-003`, `-006` |
| `IDN-004` | `User.EnsureCanSignIn` — the single sign-in gate | ✅ | `IDN-003` | — | `BR-IDN-028`. Unverified, blocked, deleted and locked are indistinguishable |
| `IDN-005` | Lockout: `RecordFailedSignIn`, `RecordSuccessfulSignIn` | ✅ | `IDN-003` | — | `BR-IDN-011` |
| `IDN-006` | `User` lifecycle: `Verify`, `Block`, `Restore`, `Delete`, `ChangePassword` | ✅ | `IDN-003` | — | `BR-IDN-007`, `-008` |
| `IDN-007` | `EmailVerificationToken` and `PasswordResetToken` entities | ✅ | `IDN-001` | — | `BR-IDN-004`, `-012`. Single-use, hashed |
| `IDN-008` | `UserSession` aggregate with `Start`, `Revoke`, `Touch`, `IsActive` | ✅ | `IDN-001` | — | `BR-IDN-020`, `-021` |
| `IDN-009` | **`UserSession.Rotate` — rotation and reuse detection together** | ✅ | `IDN-008` | — | `BR-IDN-017`, `-018`. Highest-risk unit in the domain |
| `IDN-010` | `RefreshToken` entity with chain links | ✅ | `IDN-008` | — | `BR-IDN-015`, `-016` |
| `IDN-011` | `AuditEntry` entity | ✅ | — | — | `BR-IDN-033`. Must never hold a token or password |
| `IDN-012` | Domain events: registered, verified, password changed, reuse detected, session revoked | ✅ | `IDN-009` | — | |

### Infrastructure

| ID | Task | Status | Blocker | Tracking | Notes |
|---|---|---|---|---|---|
| `IDN-013` | EF configurations for user, session, tokens, audit | ✅ | `IDN-003` | — | Fluent API only |
| `IDN-014` | **Unique filtered index on email, excluding deleted accounts** | ✅ | `IDN-013` | — | `BR-IDN-002`. The application check alone races |
| `IDN-015` | Token hashes as `bytea`. No plaintext column may exist | ✅ | `IDN-013` | — | `BR-IDN-016` |
| `IDN-016` | `RowVersion` on `UserSession` for optimistic concurrency | ✅ | `IDN-013` | — | Simultaneous rotations must not both win |
| `IDN-017` | Migration for the identity schema | ✅ | `IDN-013` | — | Verified down-migration required |
| `IDN-018` | `UserRepository`, `UserSessionRepository`, `AuditRepository` | ✅ | `IDN-013` | — | No `IQueryable` or `DbContext` leaks |
| `IDN-019` | `AspNetIdentityPasswordHasher` | ✅ | `IDN-001` | — | `PasswordHasher<T>` only, not the full Identity stack |
| `IDN-020` | `JwtTokenGenerator` and `JwtOptions`, validated on start | ✅ | — | — | `BR-IDN-014`. 15 minutes, `sid` claim |
| `IDN-021` | `InMemorySessionRevocationCache` behind `ISessionRevocationCache` | ✅ | — | — | Interface is the seam for Redis later |
| `IDN-022` | `UserAgentDeviceParser` | ✅ | `IDN-001` | — | Cosmetic label only. Never authorizes |
| `IDN-023` | `CurrentUser` reading subject, role and `sid` from claims | ✅ | — | — | Delivered early during `NET-014`, which needed it to resolve library scope |
| `IDN-024` | Demo account seeding | ✅ | `IDN-017`, `NET-013` | — | The three prototype accounts |

### Application

| ID | Task | Status | Blocker | Tracking | Notes |
|---|---|---|---|---|---|
| `IDN-025` | `RegisterCommand` and the verification email | ✅ | `IDN-003` | — | `BR-IDN-030`: a duplicate address must not be distinguishable |
| `IDN-026` | `VerifyEmailCommand` | ✅ | `IDN-007` | — | `BR-IDN-004` |
| `IDN-027` | `ResendVerificationCommand` invalidating the previous token | ✅ | `IDN-026` | — | `BR-IDN-005` |
| `IDN-028` | `SignInCommand` | ✅ | `IDN-004`, `IDN-020` | — | `BR-IDN-020`, `-028` |
| `IDN-029` | **`RefreshTokenCommand`** | ✅ | `IDN-009` | — | `BR-IDN-017`, `-018`, `-019` |
| `IDN-030` | `SignOutCommand` | ✅ | `IDN-008` | — | `BR-IDN-027`. Current session only |
| `IDN-031` | `ForgotPasswordCommand` | ✅ | `IDN-007` | — | `BR-IDN-029`. Uniform response |
| `IDN-032` | `ResetPasswordCommand` | ✅ | `IDN-031` | — | `BR-IDN-013` |
| `IDN-033` | `ChangePasswordCommand` | ✅ | `IDN-006` | — | `BR-IDN-013` |
| `IDN-034` | `RevokeSessionsCommand` with `Specified`, `AllOthers`, `All` | ✅ | `IDN-008` | — | One handler, one ownership check |
| `IDN-035` | `GetMySessionsQuery` with `IsCurrent` | ✅ | `IDN-008` | — | `BR-IDN-026` |
| `IDN-036` | `GetCurrentUserQuery` | ✅ | `IDN-023` | — | |

> **Growth watch.** `IDN-025` to `IDN-036` are **15 commands and queries**, exactly on the SDD+ §6.2
> threshold of "more than 15". Adding a sixteenth trips it and must create an evaluation task in
> `global_task_spec.md`. No split without written approval.

### Presentation

| ID | Task | Status | Blocker | Tracking | Notes |
|---|---|---|---|---|---|
| `IDN-037` | `AuthController` and `SessionsController` | ✅ | `IDN-036` | — | Thin. Bind, dispatch, convert `Result` |
| `IDN-038` | **Session validation middleware checking `sid` against the cache** | ✅ | `IDN-021` | — | `BR-IDN-023`. Without it, revocation waits 15 minutes |

### Frontend

| ID | Task | Status | Blocker | Tracking | Notes |
|---|---|---|---|---|---|
| `IDN-039` | `login`, `signup`, `verify` screens | ✅ | `IDN-037` | — | Copy taken verbatim from the prototype |
| `IDN-040` | `AuthProvider`, `useAuth`, `ProtectedRoute`, `RoleGuard` | ✅ | `IDN-037` | — | Backend authorization stays mandatory |
| `IDN-041` | Role-driven sidebar composition | ✅ | `IDN-040` | — | `routes/navigation.ts` already declares `visibleTo` |
| `IDN-042` | *Devices and sessions* screen | ✅ | — | — | **Not in the prototype.** Built in its visual language; **`BLOCK-004` still open — needs your design review** |

> The Axios single-flight refresh queue was delivered in Stage 0 and already satisfies the
> concurrent-refresh edge case. No task is needed for it.

### Status values

⬜ Not started
🔄 In progress
✅ Done
❌ Removed / not applicable (reason required in Notes)
🔴 Blocked (blocker ID required)

---

## Test Obligations

Every `BR-IDN-*` rule needs at least one test. These carry the most risk:

| Test | Covers |
|---|---|
| **Presenting an already-rotated refresh token revokes the whole session** | `AC-IDN-007` |
| Two simultaneous refreshes: one rotates, the other is treated as reuse | `identity.business.md` §5 |
| Revoking a session fails its next request despite an unexpired access token | `AC-IDN-008` |
| "Sign out everywhere else" leaves exactly one live session | `AC-IDN-009` |
| Resetting a password leaves exactly one live session | `AC-IDN-010` |
| Six wrong passwords lock the account, indistinguishably | `AC-IDN-005` |
| Registering with an existing address is indistinguishable from a fresh registration | `AC-IDN-004` |
| A member reading another member's sessions receives 403 | `AC-IDN-011` |
| **No response, log line, or audit entry contains a password or refresh token** | `AC-IDN-013` |
| Mailgun failing during registration still creates the account | `identity.business.md` §5 |

---

## Completion Log

| Date | Task ID | Completed by | Notes |
|---|---|---|---|
| 2026-08-15 | — | AI Agent — Claude | **Architecture review, `GLOBAL-016` and `GLOBAL-017`.** `identity` moved under `Features/` in all three layers; handlers now depend on `IIdentityUnitOfWork` instead of individual repositories. No business rule changed; 265 tests green throughout |
| 2026-08-15 | `IDN-001` | AI Agent — Claude | `Email` normalised at construction, `PasswordHash` and `SecretHash` both redacting in `ToString`, `DeviceDescriptor` truncating an attacker-controlled user agent |
| 2026-08-15 | `IDN-002` | AI Agent — Claude | `UserStatus`, `DeviceType`, `SessionRevocationReason`, `SingleUseTokenPurpose` |
| 2026-08-15 | `IDN-003` | AI Agent — Claude | `User.Register` and `User.Invite`. Registering into a staff role is refused, so public signup cannot bypass BR-NET-008 |
| 2026-08-15 | `IDN-004` | AI Agent — Claude | `User.EnsureCanSignIn` — one gate, one error for all six rejection reasons |
| 2026-08-15 | `IDN-005` | AI Agent — Claude | Lockout after 5 failures in 15 minutes; an expired lock resets the counter so old failures cannot accumulate |
| 2026-08-15 | `IDN-006` | AI Agent — Claude | `Verify`, `Block`, `Restore`, `Delete`, `ChangePassword`, `AcceptInvitation`. Restore returns an unverified account to Pending, not Active |
| 2026-08-15 | `IDN-007` | AI Agent — Claude | `SingleUseToken` covers verification and recovery — identical rules, different lifetimes |
| 2026-08-15 | `IDN-008` | AI Agent — Claude | `UserSession.Start`, `Revoke` (idempotent, keeps the first reason), `Touch` (never moves backwards) |
| 2026-08-15 | `IDN-009` | AI Agent — Claude | **`UserSession.Rotate`** — rotation and reuse detection in one method. Refreshing does not extend the session lifetime |
| 2026-08-15 | `IDN-010` | AI Agent — Claude | `RefreshToken` keeps rotated links in the chain, which is what makes reuse detectable |
| 2026-08-15 | `IDN-011` | AI Agent — Claude | `AuditEntry`, append-only with no mutating members |
| 2026-08-15 | `IDN-012` | AI Agent — Claude | Six domain events, identifiers and values only |
| 2026-08-15 | `IDN-013` | AI Agent — Claude | Five EF configurations plus three value converters. The `Email` folder was renamed `Mail` across Application and Infrastructure: it collided with the `Email` value object |
| 2026-08-15 | `IDN-014` | AI Agent — Claude | **Verified in PostgreSQL**: `ix_users_email` unique `WHERE status <> 3`, so a deleted account never blocks re-registration |
| 2026-08-15 | `IDN-015` | AI Agent — Claude | **Verified in PostgreSQL**: every token hash is `bytea`. No column exists anywhere that could hold a plaintext token |
| 2026-08-15 | `IDN-016` | AI Agent — Claude | Concurrency via a shadow `xmin` property. `UseXminAsConcurrencyToken` was removed in Npgsql 10 |
| 2026-08-15 | `IDN-017` | AI Agent — Claude | Migration `AddIdentityDomain`, applied against real PostgreSQL. 11 tables total |
| 2026-08-15 | `IDN-018` | AI Agent — Claude | Four repositories extending `Repository<TEntity>`. `GetByRefreshTokenHashAsync` loads the whole chain: fetching only the live token would silently disable reuse detection |
| 2026-08-15 | `IDN-019` | AI Agent — Claude | `AspNetIdentityPasswordHasher` over `PasswordHasher<T>` only, not the full Identity stack. `SuccessRehashNeeded` still authenticates |
| 2026-08-15 | `IDN-020` | AI Agent — Claude | `JwtTokenGenerator` and `JwtOptions`, validated on start. Signing key at least 32 bytes, supplied by environment variable |
| 2026-08-15 | `IDN-021` | AI Agent — Claude | `InMemorySessionRevocationCache`; each entry expires with the session's last possible token, so the cache stays bounded without a sweep |
| 2026-08-15 | `IDN-022` | AI Agent — Claude | `UserAgentDeviceParser`, hand-written. The label is cosmetic (BR-IDN-022), so a regex database dependency is not warranted |
| 2026-08-15 | `IDN-037` | AI Agent — Claude | `AuthController` and `SessionsController`. The refresh token is read from an HttpOnly cookie and never appears in a response body |
| 2026-08-15 | `IDN-038` | AI Agent — Claude | **Session validation middleware**, between authentication and authorization. **Verified against the running system**: revoking a session made the very next request fail with a still-valid access token |
| 2026-08-15 | `IDN-039` | AI Agent — Claude | `login`, `signup` with the three-column plan selector and cascading country and city, `verify`. Copy taken from the prototype |
| 2026-08-15 | `IDN-040` | AI Agent — Claude | `AuthProvider`, `useAuth`, `ProtectedRoute`, `RoleGuard`. The user is read from `/auth/me`, not from the token, so a role changed since sign-in is reflected |
| 2026-08-15 | `IDN-041` | AI Agent — Claude | Role-driven sidebar plus the account menu. 13 tests, including that Basic never sees the AI entry |
| 2026-08-15 | `IDN-042` | AI Agent — Claude | Devices and sessions screen: revoke one, all others, or all. Ending every session signs the browser out, since it ends this one too |
| 2026-08-15 | `IDN-025` | AI Agent — Claude | `RegisterCommand`. A taken address returns the same success and mails a notice to the account holder, so the person at the keyboard learns nothing (BR-IDN-030). Committed before the email is sent: a provider outage must never lose an account |
| 2026-08-15 | `IDN-026` | AI Agent — Claude | `VerifyEmailCommand`. The entity decides spendability, so expired, consumed and superseded report identically |
| 2026-08-15 | `IDN-027` | AI Agent — Claude | `ResendVerificationCommand` invalidates outstanding links first (BR-IDN-005) |
| 2026-08-15 | `IDN-028` | AI Agent — Claude | `SignInCommand`. An unknown address still hashes the password, so existence cannot be inferred from response time |
| 2026-08-15 | `IDN-029` | AI Agent — Claude | **`RefreshTokenCommand`**. Reuse evicts from the revocation cache and audits; a session blocked meanwhile is ended rather than refreshed |
| 2026-08-15 | `IDN-030` | AI Agent — Claude | `SignOutCommand`, current session only, with cache eviction |
| 2026-08-15 | `IDN-031` | AI Agent — Claude | `ForgotPasswordCommand`. Every path returns success, per BR-IDN-029 |
| 2026-08-15 | `IDN-032` | AI Agent — Claude | `ResetPasswordCommand` revokes **every** session: a reset happens with no session to spare |
| 2026-08-15 | `IDN-033` | AI Agent — Claude | `ChangePasswordCommand` spares the current session and requires the current password |
| 2026-08-15 | `IDN-034` | AI Agent — Claude | `RevokeSessionsCommand` with a scope. BR-IDN-025 holds structurally: the candidate set is only ever the caller's own sessions |
| 2026-08-15 | `IDN-035` | AI Agent — Claude | `GetMySessionsQuery` with `IsCurrent`, plus a test asserting the DTO carries no token material |
| 2026-08-15 | `IDN-036` | AI Agent — Claude | `GetCurrentUserQuery` reads the database, not the token: a role changed since sign-in must be reflected |
| 2026-08-15 | `IDN-024` | AI Agent — Claude | `DemoAccountSeeder`, **development only**. **Verified in PostgreSQL**: three accounts active, admin scoped to exactly Midtown and Harlem |

---

## Progress Summary

| Layer | Done | Total |
|---|---|---|
| Domain | 12 | 12 |
| Infrastructure | 12 | 12 |
| Application | 12 | 12 |
| Presentation | 2 | 2 |
| Frontend | 4 | 4 |
| **Total** | **42** | **42** |
