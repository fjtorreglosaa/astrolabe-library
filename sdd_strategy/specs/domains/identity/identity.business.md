# Identity — Business Specification

**Last reviewed:** 2026-08-16
**Reviewed by:** AI Agent — Claude — 2026-08-16
**Version:** 1
**Ring:** MVP

> **Growth watch:** projected to exceed 8 aggregates. Likely split is `identity` / `sessions`.
> No split without written approval, per SDD+ §6.2 and RULE 7.

---

## 1. Purpose

Identity owns who a user is and how they prove it: registration, email verification, sign-in, token
issuance and rotation, session and device management, account lifecycle, and role assignment. It
answers *"who is making this request, and is that identity still valid"*.

It deliberately does **not** decide what that identity is allowed to do with books, money, or
libraries. It establishes the identity and the role; every other domain decides what that role means
in its own terms.

---

## 2. Glossary

| Term | Definition |
|---|---|
| **Account** | A user record with an email address and a password, in one of four states: `active`, `pending verification`, `blocked`, `deleted` |
| **Session** | The revocable unit of authentication, bound to one device. Identified by the `sid` claim carried in the access token |
| **Device** | A label used to group and name sessions in the interface. **Never an authorization credential** |
| **Access token** | A short-lived signed JWT proving identity on each request |
| **Refresh token** | A long-lived opaque secret used to obtain a new token pair within the same session |
| **Token chain** | The sequence of refresh tokens issued within one session. Reuse of an already-rotated token invalidates the whole chain |
| **Rotation** | Issuing a new refresh token and invalidating the presented one, on every refresh |
| **Reuse** | Presentation of a refresh token that has already been rotated. Treated as evidence of theft |
| **Verification token** | A single-use secret emailed to confirm ownership of an email address |
| **Recovery token** | A single-use secret emailed to allow a password reset |
| **Role** | `Member`, `Admin`, or `Super Admin`. What a user may **do**. It carries no plan: what a member bought lives on their subscription, and `identity` does not read it |
| **Invitation** | How a staff account is created. The account exists as `Invited` until the recipient confirms |

> In `recommendations`, *agent* means a prompt template; in `support`, a staff member. Neither meaning
> applies here.

---

## 3. Business Rules

### Registration and account lifecycle

| ID | Rule |
|---|---|
| `BR-IDN-001` | An account is created in `pending verification` and **must not be able to sign in** until it is verified |
| `BR-IDN-002` | An email address must be unique across all non-deleted accounts |
| `BR-IDN-003` | Registration must capture email, password, full name, country, and city of residence |
| `BR-IDN-004` | A verification token must be single-use, stored only as a hash, and valid for 24 hours |
| `BR-IDN-005` | Requesting a new verification email must invalidate any previously issued verification token for that account |
| `BR-IDN-006` | A member account may only be created through public registration; a staff account may only be created through invitation by a super administrator |
| `BR-IDN-007` | A blocked account must not be able to sign in, and its active sessions must be revoked at the moment of blocking |
| `BR-IDN-008` | A deleted account must not be able to sign in and must not be returned by member-facing queries, but its historical records must be preserved |

### Passwords

| ID | Rule |
|---|---|
| `BR-IDN-009` | A password must be at least 12 characters. No composition rules and no forced rotation are imposed, per NIST SP 800-63B |
| `BR-IDN-010` | A password must only ever be persisted as a salted hash. It must never be logged, returned, or stored in plain text |
| `BR-IDN-011` | An account must lock for 15 minutes after 5 failed sign-in attempts within a 15-minute window |
| `BR-IDN-012` | A recovery token must be single-use, stored only as a hash, and valid for 1 hour |
| `BR-IDN-013` | Changing or resetting a password must revoke **every session except the one performing the change** |

### Tokens

| ID | Rule |
|---|---|
| `BR-IDN-014` | An access token must be a signed JWT valid for 15 minutes, carrying at least the subject, role, and `sid` session claim |
| `BR-IDN-015` | A refresh token must be opaque, at least 256 bits of cryptographic randomness, and valid for 30 days |
| `BR-IDN-016` | A refresh token must be persisted **only as a SHA-256 hash**. The plaintext must never be stored |
| `BR-IDN-017` | Every refresh must rotate the token: the presented token is invalidated and a new one is issued **within the same session** |
| `BR-IDN-018` | Presenting an already-rotated refresh token must revoke the **entire session**, invalidating every token in its chain, and must record a security event |
| `BR-IDN-019` | An expired or revoked refresh token must be rejected without revealing which of the two it was |

### Sessions and devices

| ID | Rule |
|---|---|
| `BR-IDN-020` | Every successful sign-in must create exactly one session |
| `BR-IDN-021` | A session must record device name, device type, IP address, approximate location, creation time, last-seen time, and expiry |
| `BR-IDN-022` | A device identifier is a **label only**. It must never be used to authorize a request |
| `BR-IDN-023` | A revoked session must be rejected on the **next request**, without waiting for the access token to expire |
| `BR-IDN-024` | A member must be able to revoke one named session, several selected sessions, every session except the current one, or every session including the current one |
| `BR-IDN-025` | A member must only ever see and revoke their own sessions |
| `BR-IDN-026` | The session a request originates from must be identifiable to the client, so the interface can mark it as "this device" |
| `BR-IDN-027` | Signing out must revoke the current session only |

### Anti-enumeration and rate limiting

| ID | Rule |
|---|---|
| `BR-IDN-028` | Sign-in must return the same generic failure whether the account does not exist, the password is wrong, the account is unverified, or the account is blocked |
| `BR-IDN-029` | Password recovery must return the same response whether or not the address is registered |
| `BR-IDN-030` | Registration must not reveal whether an address is already registered through a distinguishable response |
| `BR-IDN-031` | Sign-in, registration, refresh, and password recovery must be rate limited per address and per client |

### Auditing

| ID | Rule |
|---|---|
| `BR-IDN-032` | The following must be audited: successful sign-in, failed sign-in, sign-out, session created from an unrecognised device, every revocation, refresh token reuse, and changes to password, email, role, or account state |
| `BR-IDN-033` | An audit entry must record actor, action, subject, timestamp, and originating IP. It must never contain a password, token, or token hash |

---

## 4. Acceptance Criteria

| ID | Criterion | Covers |
|---|---|---|
| `AC-IDN-001` | Registering creates an account that cannot sign in until the emailed link is opened | `BR-IDN-001`, `BR-IDN-004` |
| `AC-IDN-002` | A verification link opened twice succeeds the first time and fails the second | `BR-IDN-004` |
| `AC-IDN-003` | A verification link opened after 24 hours fails and offers to resend | `BR-IDN-004` |
| `AC-IDN-004` | Registering with an address already in use returns the same response as a fresh registration | `BR-IDN-030` |
| `AC-IDN-005` | Six wrong passwords in a row lock the account; the sixth response is indistinguishable from the fifth | `BR-IDN-011`, `BR-IDN-028` |
| `AC-IDN-006` | A refresh returns a new pair, and the presented refresh token stops working | `BR-IDN-017` |
| `AC-IDN-007` | Presenting a previously rotated refresh token revokes the whole session and forces re-authentication | `BR-IDN-018` |
| `AC-IDN-008` | Revoking a session causes its next authenticated request to fail, even with an unexpired access token | `BR-IDN-023` |
| `AC-IDN-009` | "Sign out everywhere else" leaves exactly one live session — the current one | `BR-IDN-024` |
| `AC-IDN-010` | Resetting a password leaves exactly one live session — the one that performed the reset | `BR-IDN-013` |
| `AC-IDN-011` | A member requesting another member's sessions receives 403 and learns nothing about them | `BR-IDN-025` |
| `AC-IDN-012` | The three demo accounts from the prototype sign in and land on their correct role surface | `BR-IDN-014` |
| `AC-IDN-013` | No response body, log line, or audit entry anywhere contains a password or a refresh token | `BR-IDN-010`, `BR-IDN-033` |

---

## 5. Edge Cases

| Scenario | Expected behaviour |
|---|---|
| Two devices refresh simultaneously with the same valid token | One wins and rotates. The other presents a token that is now rotated, which by `BR-IDN-018` revokes the session. The client must serialise refreshes to avoid this — a single-flight refresh queue is mandatory |
| A member signs in on a device they have used before | A **new** session is created. Sessions are not reused across sign-ins, even on the same device |
| An account is blocked while it holds live sessions | Every session is revoked immediately. The next request from any device fails |
| A verification email is never delivered | The account stays `pending verification` indefinitely. The member can request a new email, which invalidates the previous token |
| Mailgun rejects the verification message | Registration still succeeds and the account is created. The failure is logged and the member is told to request a new email. An email provider outage must never lose an account |
| A member resets their password from a device that is not signed in | The reset succeeds and revokes **every** session, since there is no current session to preserve |
| An access token is presented after its session expired but before the token did | Rejected. Session state wins over token expiry |
| A member deletes their account while holding active reservations | Out of scope for this domain. `identity` marks the account `deleted`; `reservations` decides what happens to the loans |
| The clock skews between issuing and validating a token | Validation allows a small tolerance. The tolerance is a technical decision recorded in `identity.technical.md` |

---

## 6. Out of Scope

Explicitly **not** handled by this domain:

- What a role is allowed to do with books, money, or libraries — each domain enforces its own rules
- Which libraries an administrator may act on — that belongs to `network`
- What a plan entitles a member to — that belongs to `membership`. `identity` only stores the role
- Two-factor authentication. The data model must leave room for TOTP, but it is not implemented
- Single sign-on and external identity providers
- Rendering the email. `identity` decides *that* a message must be sent and what it says; `IEmailSender` decides how it travels
- Notifying a member of a new sign-in through the notification centre — that belongs to `notifications`, Phase 2
- CAPTCHA and bot detection

---

## 7. Prototype Reference

Screens: `login`, `signup` (with the three-column plan selector and cascading country and city),
`verify` (*Check your inbox*), and `settings → Devices and sessions`.

**The sessions screen does not exist in the prototype.** It is the one surface in this domain with no
approved design, tracked as `BLOCK-004`. It must be designed in the prototype's visual language and
reviewed before the domain is accepted.

Demo accounts, password `Testing1234*` for all three:

| Address | Role |
|---|---|
| `fjtorreglosaa@gmail.com` | Member, Plus |
| `admin@astrolabe.co` | Admin — Midtown and Harlem |
| `super@astrolabe.co` | Super Admin |

Read `docs/design/prototype.source.js` for the exact copy of every screen and error message.
