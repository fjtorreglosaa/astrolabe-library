# Notifications — Tasks

**Last reviewed:** 2026-08-16
**Overall progress:** 12/12 (100%)

PLAN-001 Stage 9. Depends on Stages 3 and 4 — every notification reacts to an event those produce.

---

## Blocking Dependencies

| Block ID | Description | Status |
|---|---|---|
| — | None | — |

---

## Task List

| ID | Task | Status | Blocker | Tracking | Notes |
|---|---|---|---|---|---|
| `NTF-001` | `NotificationKind`, `NotificationFamily` enumerations | ✅ | — | — | Eight kinds, five families |
| `NTF-002` | `NotificationFamilies` map | ✅ | `NTF-001` | — | `BR-NTF-002`. Total, so a new kind cannot fall through a default |
| `NTF-003` | `Notification` aggregate | ✅ | `NTF-002` | — | `BR-NTF-001`, `-006`, `-009` |
| `NTF-004` | `NotificationPreference` aggregate | ✅ | `NTF-001` | — | Absence means "on" |
| `NTF-005` | Repositories and `INotificationsUnitOfWork` | ✅ | `NTF-004` | — | Clearing is filtered inside the repository |
| `NTF-006` | `INotificationRaiser` and its implementation | ✅ | `NTF-005` | — | `BR-NTF-003`, `BR-NTF-005`. The one place a notification is created |
| `NTF-007` | Five domain event handlers | ✅ | `NTF-006` | — | Fines, payments, desk codes, returns |
| `NTF-008` | `GetMyNotificationsQuery` | ✅ | `NTF-005` | — | `BR-NTF-010` — the count is counted |
| `NTF-009` | `MarkNotificationsReadCommand` | ✅ | `NTF-008` | — | `BR-NTF-006`, `BR-NTF-007` |
| `NTF-010` | `ClearNotificationsCommand`, `SetNotificationPreferenceCommand` | ✅ | `NTF-009` | — | `BR-NTF-008`, `BR-NTF-002` |
| `NTF-011` | `NotificationsController` | ✅ | `NTF-010` | — | No route accepts another member's identifier |
| `NTF-012` | The bell and the settings screen | ✅ | `NTF-011` | — | Five switches for eight kinds |

### Status values

⬜ Not started · 🔄 In progress · ✅ Done · ❌ Removed · 🔴 Blocked

---

## Completion Log

| Date | Task ID | Completed by | Notes |
|---|---|---|---|
| 2026-08-16 | `NTF-001` to `NTF-012` | AI Agent — Claude | **Specifications authored and domain built.** Verified against the running system with a real domain event: beginning a return produced one `Transit` notification with its route, muting `Returns` then stopped the check-in producing another (`BR-NTF-003`), and the already-delivered one survived the mute (`BR-NTF-004`) — both rules in one sequence. Marking read twice is idempotent, another member's notification answers 403, and clearing empties it. The raiser swallows its own failures on purpose: every caller is an event handler reacting to something already committed, and a message about a reservation must not be able to cost somebody the reservation |

---

## Progress Summary

| Layer | Done | Total |
|---|---|---|
| Domain | 4 | 4 |
| Application | 4 | 4 |
| Infrastructure | 2 | 2 |
| Presentation | 1 | 1 |
| Frontend | 1 | 1 |
| **Total** | **12** | **12** |
