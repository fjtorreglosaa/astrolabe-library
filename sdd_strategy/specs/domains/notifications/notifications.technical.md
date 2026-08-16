# Notifications — Technical Specification

**Last reviewed:** 2026-08-16
**Reviewed by:** AI Agent — Claude — 2026-08-16
**Version:** 1 — authored at the start of PLAN-001 Stage 9
**Implements:** `BR-NTF-001` to `BR-NTF-010`

---

## 1. Domain Model

### `Notification` — aggregate root

```csharp
public sealed class Notification : AggregateRoot
{
    public Guid MemberId { get; private set; }
    public NotificationKind Kind { get; private set; }
    public string Title { get; private set; }
    public string Body { get; private set; }
    public string? Route { get; private set; }          // BR-NTF-009
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }
    public bool IsRead => ReadAt is not null;

    public static Result<Notification> Raise(
        Guid memberId, NotificationKind kind, string title, string body,
        string? route, DateTimeOffset now);

    public void MarkRead(DateTimeOffset now);            // BR-NTF-006, idempotent
}
```

### `NotificationPreference` — aggregate root

One row per member per **family**, and only for families they have muted. Absence means "on", so a
member who has never touched the setting has no rows and receives everything — which is what a new
member expects.

### `NotificationFamilies` — a static map

`NotificationKind` → `NotificationFamily`, transcribed from the prototype's `NOTE_KINDS`. A pure
lookup, so BR-NTF-002 is a fact about a table rather than a rule anybody enforces.

---

## 2. Application Layer

```text
GetMyNotificationsQuery   → Result<NotificationFeedDto>   BR-NTF-007, BR-NTF-010
MarkNotificationReadCommand → Result                      BR-NTF-006, BR-NTF-007
MarkAllNotificationsReadCommand → Result                  BR-NTF-006
ClearNotificationsCommand → Result                        BR-NTF-008
GetMyNotificationPreferencesQuery → Result<...>           BR-NTF-002
SetNotificationPreferenceCommand → Result                 BR-NTF-002, BR-NTF-003
```

Plus **event handlers**, which are the only thing that creates a notification: reservation due,
payment settled, desk payment pending, return in transit, return checked in, ticket answered.

`INotificationRaiser` is the seam every one of them uses, so the mute check (BR-NTF-003) lives in one
place rather than in each handler.

---

## 3. Architecture Decision Log

| Decision | Choice | Rationale | Alternatives rejected |
|---|---|---|---|
| Where notifications are created | Domain event handlers only | BR-NTF-005. A notification is a reaction, and a reaction that could fail a reservation would be a notification that cost a member their book. Nothing the business outcome depends on lives here | Creating them inside command handlers — rejected: it makes every command responsible for remembering, and couples an outcome to a message about it |
| Muting | Absence of a preference row means "on" | A new member has no rows and receives everything, which is what they expect, and unmuting is a delete rather than a flag flip | A row per member per family, seeded on registration — rejected: a write on every registration to record a default |
| Muted families | Produce nothing at all | BR-NTF-003. A notification created and hidden is one that reappears the moment somebody writes a query that forgets the filter | Creating and filtering on read — rejected for exactly that |
| Unread count | Counted, never stored | BR-NTF-010, and the same reasoning as the points balance: a stored count is a second source of truth that drifts | A denormalised column — rejected |
| One seam for raising | `INotificationRaiser` | Six event handlers need the same mute check. Six copies is five chances to forget it | Each handler checking preferences — rejected |
