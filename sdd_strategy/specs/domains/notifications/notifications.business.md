# Notifications — Business Specification

**Last reviewed:** 2026-08-16
**Reviewed by:** AI Agent — Claude — 2026-08-16
**Version:** 1 — authored at the start of PLAN-001 Stage 9
**Ring:** Phase 2

---

## 1. Purpose

Owns the notification centre: what a member is told, whether they have read it, and which kinds they
have chosen to stop hearing about.

It answers *"what has happened to me lately, and do I still care"*. It **originates nothing** — every
notification is a reaction to something another domain already committed.

---

## 2. Glossary

| Term | Definition |
|---|---|
| **Notification** | One thing that happened to one member, with a title, a body and a place to go |
| **Kind** | What specifically happened: `Due`, `Pending`, `Paid`, `Transit`, `Returned`, `Hold`, `Desk`, `Support` |
| **Family** | The group a kind is muted by: `Due`, `Payments`, `Returns`, `Holds`, `Support`. Several kinds share one family, because a member who mutes payments means all of them |
| **Preference** | A member's decision to stop receiving one family |
| **Unread** | Delivered and not yet opened. The count on the bell |

---

## 3. Business Rules

| ID | Rule |
|---|---|
| `BR-NTF-001` | A notification must belong to exactly one member and exactly one kind |
| `BR-NTF-002` | Every kind must belong to exactly one family, and a member mutes a **family**, never a single kind |
| `BR-NTF-003` | A muted family must produce no notification at all. Nothing is delivered and silently hidden |
| `BR-NTF-004` | Muting must never affect a notification already delivered. A member silencing a family keeps what they were already sent |
| `BR-NTF-005` | A notification must be raised from a **domain event**, never from a command handler, and must never be a step the originating outcome depends on |
| `BR-NTF-006` | Marking one notification read, or all of them, must be idempotent |
| `BR-NTF-007` | A member may only read, mark and clear their **own** notifications. No endpoint accepts another member's identifier |
| `BR-NTF-008` | Clearing removes a notification from the member's view permanently. There is no undo and none is offered |
| `BR-NTF-009` | A notification must carry a route to the screen it is about, so it can be acted on rather than only read |
| `BR-NTF-010` | The unread count must be the number of unread notifications and must never be stored as a column |

---

## 4. Acceptance Criteria

| ID | Criterion | Covers |
|---|---|---|
| `AC-NTF-001` | Returning a book late produces one `Due` notification for that member and nobody else | `BR-NTF-001`, `BR-NTF-005` |
| `AC-NTF-002` | A member who mutes `Payments` receives no `Paid`, `Pending` or `Desk` notification | `BR-NTF-002`, `BR-NTF-003` |
| `AC-NTF-003` | Muting a family leaves the notifications already received untouched | `BR-NTF-004` |
| `AC-NTF-004` | Marking all read twice leaves the same state and writes nothing the second time | `BR-NTF-006` |
| `AC-NTF-005` | A member cannot mark or clear a notification belonging to somebody else | `BR-NTF-007` |
| `AC-NTF-006` | A failure while creating a notification never fails the reservation, payment or ticket that caused it | `BR-NTF-005` |

---

## 5. Edge Cases

| Scenario | Expected behaviour |
|---|---|
| A member mutes a family, then unmutes it | New notifications resume. Nothing is back-filled — they were never created, and inventing them later would be inventing history |
| An event arrives for a deleted account | No notification. A deleted member has no centre to show it in |
| The same event is dispatched twice | Two notifications. Deduplicating would need a natural key the events do not carry, and a duplicate is visibly harmless where a missing one is not |
| A member clears everything, then something happens | The new one appears. Clearing is not muting, and the two are separate decisions on purpose |
| A notification points at a screen the member can no longer reach | It still shows. The route is a convenience, and hiding the message because its destination moved would lose the message |

---

## 6. Out of Scope

- Email, push and SMS delivery. This is the **in-app centre**; `Shared/Mail` owns email
- Digests and scheduled summaries of any kind
- Staff notifications. Every rule here is about a member
- Retention and expiry. Nothing ages out in the MVP

---

## 7. Prototype Reference

The bell, its unread badge, "Mark all read", "Clear all", and the empty states — *"Notifications are
off"* when muted and *"Nothing new yet"* when simply empty. The kind-to-family mapping is transcribed
from `NOTE_KINDS`, which is the authority for it.
