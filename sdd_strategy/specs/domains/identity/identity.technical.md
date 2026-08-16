# Identity — Technical Specification

**Last reviewed:** 2026-08-15
**Reviewed by:** Francisco Torregrosa
**Version:** 1
**Implements:** `BR-IDN-001` to `BR-IDN-033`

> **Growth threshold BREACHED.** This domain carries **33 business rules** against a limit of 20
> (SDD+ §6.2). `GLOBAL-015` is raised in `global_task_spec.md` with a proposed `identity` / `sessions`
> split and a recommendation to defer it until after Stage 1. **No split without written approval.**
>
> It also sits at **15 commands and queries**, exactly on the "more than 15" boundary — the next
> operation trips that indicator too. Aggregates and entities: 6, under the limit of 8.

---

## 1. Domain Model

Two aggregate roots. `User` owns everything about who someone is; `UserSession` owns everything about
one authenticated device. They are separate roots because sessions churn constantly while the user
record is near-static — putting them in one aggregate would force loading every session to change a
name.

### User — aggregate root

```csharp
public sealed class User : Entity
{
    public Email Email { get; private set; }
    public PasswordHash PasswordHash { get; private set; }
    public string FullName { get; private set; }
    public Guid CountryId { get; private set; }
    public Guid CityId { get; private set; }
    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }
    public int FailedSignInAttempts { get; private set; }

    public static Result<User> Register(Email email, PasswordHash hash, string fullName,
                                        Guid countryId, Guid cityId, DateTimeOffset now);
    public static Result<User> Invite(Email email, UserRole role, DateTimeOffset now);

    public Result Verify(DateTimeOffset now);
    public Result Block();
    public Result Restore();
    public Result Delete();
    public Result ChangePassword(PasswordHash newHash);

    public Result EnsureCanSignIn(DateTimeOffset now);
    public void RecordFailedSignIn(DateTimeOffset now);
    public void RecordSuccessfulSignIn();
}
```

`EnsureCanSignIn` is the single place that answers "may this account authenticate". Keeping the
unverified, blocked, deleted, and locked checks together is what makes `BR-IDN-028` — one generic
failure for all four — enforceable rather than aspirational.

### UserSession — aggregate root

```csharp
public sealed class UserSession : Entity
{
    public Guid UserId { get; private set; }
    public DeviceDescriptor Device { get; private set; }
    public string IpAddress { get; private set; }
    public string? ApproximateLocation { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastSeenAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public SessionRevocationReason? RevokedReason { get; private set; }

    private readonly List<RefreshToken> _tokens = [];
    public IReadOnlyList<RefreshToken> Tokens => _tokens;

    public bool IsActive(DateTimeOffset now);

    public static UserSession Start(Guid userId, DeviceDescriptor device, string ipAddress,
                                    RefreshTokenHash hash, DateTimeOffset now, TimeSpan lifetime);

    public Result<RefreshToken> Rotate(RefreshTokenHash presented, RefreshTokenHash replacement,
                                       DateTimeOffset now);
    public void Revoke(SessionRevocationReason reason, DateTimeOffset now);
    public void Touch(DateTimeOffset now);
}
```

`Rotate` is the heart of the domain. It owns `BR-IDN-017` and `BR-IDN-018` together: presenting the
current token rotates it, presenting an already-rotated one revokes the whole session. Both outcomes
are decided inside the aggregate, never by a handler, because a handler could forget the second one.

### Entities

| Entity | Belongs to | Purpose |
|---|---|---|
| `RefreshToken` | `UserSession` | One link in the chain: hash, issued, rotated, replaced-by |
| `EmailVerificationToken` | `User` | Single-use, hashed, 24-hour expiry |
| `PasswordResetToken` | `User` | Single-use, hashed, 1-hour expiry |
| `AuditEntry` | — | Append-only security event record |

### Value objects

| Type | Invariant |
|---|---|
| `Email` | Normalised to lower case and trimmed. Rejects malformed input at construction |
| `PasswordHash` | Opaque wrapper. Has no `ToString` override and is never serialised |
| `RefreshTokenHash` | 32-byte SHA-256. Constructed only from a plaintext token, never stored plaintext |
| `DeviceDescriptor` | Display name plus `DeviceType`, derived from the user agent |

### Enumerations

`UserRole` — `Basic`, `Plus`, `Max`, `Admin`, `SuperAdmin`.
`UserStatus` — `PendingVerification`, `Active`, `Blocked`, `Deleted`, `Invited`.
`DeviceType` — `Web`, `Mobile`, `Tablet`, `Desktop`, `Unknown`.
`SessionRevocationReason` — `SignedOut`, `RevokedByUser`, `PasswordChanged`, `TokenReuseDetected`,
`AccountBlocked`, `Expired`.

### Domain events

| Event | Raised when | Consumed by |
|---|---|---|
| `UserRegistered` | Registration succeeds | Triggers the verification email |
| `UserVerified` | Verification succeeds | `membership` starts the subscription |
| `PasswordChanged` | Password changes or resets | Revokes other sessions |
| `RefreshTokenReuseDetected` | `BR-IDN-018` fires | Audit, and Phase 2 security notification |
| `SessionRevoked` | Any revocation | Evicts the session from the revocation cache |

---

## 2. Application Layer

### Commands

| Name | Input | Output | Rule |
|---|---|---|---|
| `RegisterCommand` | email, password, fullName, countryId, cityId | `Result<Guid>` | `BR-IDN-001`, `-002`, `-003`, `-030` |
| `VerifyEmailCommand` | token | `Result` | `BR-IDN-004` |
| `ResendVerificationCommand` | email | `Result` | `BR-IDN-005` |
| `SignInCommand` | email, password, deviceId, userAgent, ipAddress | `Result<TokenPair>` | `BR-IDN-011`, `-014`, `-020`, `-028` |
| `RefreshTokenCommand` | refreshToken, ipAddress | `Result<TokenPair>` | `BR-IDN-017`, `-018`, `-019` |
| `SignOutCommand` | sessionId | `Result` | `BR-IDN-027` |
| `ForgotPasswordCommand` | email | `Result` | `BR-IDN-012`, `-029` |
| `ResetPasswordCommand` | token, newPassword | `Result` | `BR-IDN-009`, `-012`, `-013` |
| `ChangePasswordCommand` | currentPassword, newPassword | `Result` | `BR-IDN-009`, `-013` |
| `RevokeSessionsCommand` | sessionIds, scope | `Result<int>` | `BR-IDN-023`, `-024`, `-025` |

`RevokeSessionsCommand` carries a `RevocationScope` of `Specified`, `AllOthers`, or `All`, rather
than existing as three near-identical commands. One handler means one place where `BR-IDN-025`
— you may only revoke your own — is enforced.

### Queries

| Name | Input | Output | Rule |
|---|---|---|---|
| `GetMySessionsQuery` | — | `Result<IReadOnlyList<SessionDto>>` | `BR-IDN-021`, `-025`, `-026` |
| `GetCurrentUserQuery` | — | `Result<CurrentUserDto>` | `BR-IDN-014` |

Handlers live in `Astrolabe.Application/Identity/{Commands,Queries}/{Name}/`. Validation runs inside
each handler; there are no pipeline behaviors.

`SessionDto` carries `IsCurrent`, computed by comparing the row against the `sid` claim of the calling
token, which is what lets the interface mark "this device" per `BR-IDN-026`.

### Cross-cutting services

```csharp
public interface ICurrentUser { Guid? UserId { get; } Guid? SessionId { get; } UserRole? Role { get; } }
public interface IPasswordHasher { PasswordHash Hash(string password); bool Verify(string password, PasswordHash hash); }
public interface ITokenGenerator { string CreateAccessToken(User user, Guid sessionId); string CreateRefreshToken(); }
public interface ISessionRevocationCache { bool IsRevoked(Guid sessionId); void Revoke(Guid sessionId, DateTimeOffset until); }
```

---

## 3. Infrastructure

| Concern | Implementation |
|---|---|
| Persistence | `UserRepository`, `UserSessionRepository`, `AuditRepository` over `AstrolabeDbContext` |
| EF configuration | `UserConfiguration`, `UserSessionConfiguration`, `RefreshTokenConfiguration` in `Persistence/Configurations`. Fluent API only |
| Password hashing | `AspNetIdentityPasswordHasher`, wrapping `Microsoft.AspNetCore.Identity.PasswordHasher<T>` |
| Token issuance | `JwtTokenGenerator` using `Microsoft.IdentityModel.JsonWebTokens`, configured by `JwtOptions` |
| Revocation cache | `InMemorySessionRevocationCache` over `IMemoryCache`, entry TTL equal to the access token lifetime |
| Device parsing | `UserAgentDeviceParser`, an internal parser with no external dependency |
| Email | `IEmailSender`, already implemented as `MailgunEmailSender` |

### Persistence notes

- `Email` is stored as a normalised string with a **unique filtered index** excluding deleted
  accounts, which is what enforces `BR-IDN-002` at the database level rather than in a race-prone
  application check.
- Token hashes are stored as `bytea`. No plaintext token column exists anywhere in the schema, so
  `BR-IDN-016` cannot be violated by a future careless write.
- `UserSession` carries a `RowVersion` for optimistic concurrency, so two simultaneous rotations
  cannot both succeed — the loser surfaces as reuse, which is the correct reading.
- Sessions are never hard-deleted. Revocation sets `RevokedAt` and `RevokedReason`, preserving the
  audit trail.

### Session validation

A middleware reads the `sid` claim on every authenticated request and rejects it when
`ISessionRevocationCache.IsRevoked` returns true, falling back to the database on a cache miss. This
is what makes `BR-IDN-023` — rejection on the next request, not at token expiry — true in practice.

---

## 4. Architecture Decision Log

| Decision | Choice | Rationale | Alternatives rejected |
|---|---|---|---|
| Identity framework | Use **only** `PasswordHasher<T>` from ASP.NET Core Identity, not the full stack | It gives the PBKDF2-HMAC-SHA256 hashing GUIDELINES §6.3 requires, without dragging `IdentityUser` and `IdentityDbContext` into the model | **Full ASP.NET Core Identity — rejected.** Its entity model would have to leak into or duplicate the Domain layer, and `IdentityUser` cannot satisfy the zero-dependency rule for Domain (RULE 3). GUIDELINES §6.3 is satisfied on the hashing algorithm; the store is ours |
| Aggregate boundary | `User` and `UserSession` as separate roots | Sessions change on every request while the user record is near-static. One aggregate would force loading every session to rename someone | Single `User` aggregate owning sessions — rejected on write amplification and lock contention |
| Reuse detection location | Inside `UserSession.Rotate` | Rotation and reuse are one decision. Splitting them across handler and aggregate makes it possible to implement rotation and forget revocation | Handler-level check — rejected: `BR-IDN-018` would depend on every caller remembering it |
| Revocation immediacy | `sid` claim checked against a revocation cache on every request | Stateless JWTs cannot be recalled. Without this, "sign out everywhere" would take up to 15 minutes, which is not what the interface promises | Short-lived tokens alone — rejected: leaves a 15-minute window. Database lookup per request — rejected on latency; the cache with database fallback gets both |
| Revocation cache backing | `IMemoryCache` behind `ISessionRevocationCache` | The MVP runs one API instance. The interface is the seam that makes Redis a drop-in later | Redis now — rejected as infrastructure the MVP does not have. Recorded as a known constraint |
| Refresh token storage | SHA-256 hash, `bytea`, no plaintext column | A database leak must not yield usable tokens. Refresh tokens are high-entropy random values, so a fast hash is correct — key stretching protects low-entropy secrets and would only add latency here | Plaintext — rejected outright. Argon2 or bcrypt — rejected: stretching buys nothing for a 256-bit random value and costs latency on every refresh |
| Cache eviction | Driven by the `SessionRevoked` domain event | Four rules end a session — sign-out, password change, account block, reuse detection — and every one must evict. An event makes that structural instead of something each caller must remember | A shared `SessionRevoker` service — rejected: it was a manual stand-in for the dispatcher, and it required a catch-all `Services` folder inside the feature |
| Handler dependencies | `IIdentityUnitOfWork` rather than individual repositories | `RegisterCommandHandler` carried eleven constructor parameters, four of them repositories — the excessive-parameter-list smell of GUIDELINES §42 | Individual repositories — rejected on the measurement. A global unit of work — rejected: it would expose `billing` and `catalog` repositories to identity handlers |
| Revocation command shape | One `RevokeSessionsCommand` with a scope | Three commands would mean three copies of the ownership check in `BR-IDN-025` | Separate commands per scope — rejected on duplicated authorization |
| Device parsing | Internal user-agent parser | The interface needs a readable label, nothing more. A device string is never used for authorization (`BR-IDN-022`), so precision has no security value | UAParser or similar — rejected: a dependency and a regex database for a cosmetic label |
| Approximate location | Left null in the MVP | GeoIP needs a licensed database. The field exists so the schema does not change when it arrives | Bundling a GeoIP database — rejected as licensing and size cost for a display-only field |
| Email uniqueness | Unique filtered index in PostgreSQL | An application-level check has a race between check and insert. The database is the only place the constraint holds under concurrency | Application check only — rejected: two simultaneous registrations would both pass |
| Anti-enumeration | Uniform failure and constant-time comparison in `EnsureCanSignIn` | `BR-IDN-028` demands four distinct states be indistinguishable. Centralising the check is what makes that auditable | Per-case messages — rejected: leaks account existence and state |

---

## 5. Dependencies

**This domain depends on:**

| Domain | For |
|---|---|
| `network` | Validating that the country and city given at registration exist, and resolving the home library |

**Domains that depend on this one:**

| Domain | For |
|---|---|
| All | `ICurrentUser` — who is calling |
| `membership` | The `UserVerified` event, and the role that carries the plan |
| `network` | The user record an assignment attaches to |
| `billing`, `store`, `reservations`, `catalog` | Ownership checks on member-scoped data |

**External services:** Mailgun, through `IEmailSender`, for verification and recovery mail.

---

## 6. Known Constraints and Limitations

- The revocation cache is in-process, so the API is effectively single-instance. Running two
  instances without replacing it would let a revoked session survive on the instance that did not
  handle the revocation. Tracked in `global_tech_spec.md` §8.
- `ApproximateLocation` is always null in the MVP.
- No two-factor authentication. `User` has room for a TOTP secret; nothing reads it.
- No CAPTCHA. Rate limiting is the only defence against automated registration.
- Clock skew tolerance on JWT validation is **30 seconds**, the default. Containers share the host
  clock, so no wider allowance is justified.
- Audit entries are written in the same transaction as the operation they describe. An audit write
  failure therefore rolls back the operation — deliberate, since an unaudited security change is
  worse than a failed one.

---

## 7. Superseded Decisions

| Decision | Superseded by | Reason | Date |
|---|---|---|---|
| — | — | None yet | — |
