# Support — Tasks

**Last reviewed:** 2026-08-16
**Overall progress:** 12/12 (100%)

PLAN-001 Stage 9. Depends on Stages 3 and 4, and on `notifications` for `BR-SUP-012`.

---

## Blocking Dependencies

| Block ID | Description | Status |
|---|---|---|
| — | None | — |

---

## Task List

| ID | Task | Status | Blocker | Tracking | Notes |
|---|---|---|---|---|---|
| `SUP-001` | `TicketStatus`, `TicketCategory`, `TicketAuthor` enumerations | ✅ | — | — | The prototype's three and five, verbatim |
| `SUP-002` | `TicketMessage` entity, append only | ✅ | `SUP-001` | — | `BR-SUP-008`. No method changes anything |
| `SUP-003` | `Ticket` aggregate with its transitions | ✅ | `SUP-002` | — | `BR-SUP-002`, `-003`, `-005` to `-007`, `-011` |
| `SUP-004` | `TicketAnswered` domain event | ✅ | `SUP-003` | — | `BR-SUP-012`, raised only for an agent's reply |
| `SUP-005` | Repository and `ISupportUnitOfWork` | ✅ | `SUP-003` | — | Null scope is unrestricted; empty is nothing |
| `SUP-006` | `OpenTicketCommand` | ✅ | `SUP-005` | — | Subject and body together, or the question cannot be answered |
| `SUP-007` | `ReplyToTicketCommand` | ✅ | `SUP-006` | — | Author decided from the role, never the payload |
| `SUP-008` | `TransitionTicketCommand` | ✅ | `SUP-007` | — | One command, three transitions, one scope check |
| `SUP-009` | `RateTicketCommand` | ✅ | `SUP-008` | — | `BR-SUP-005`, `BR-SUP-006` |
| `SUP-010` | `GetTicketQuery`, `SearchTicketsQuery` | ✅ | `SUP-009` | — | One query, two audiences, no parameter for whose |
| `SUP-011` | `SupportController` | ✅ | `SUP-010` | — | One controller: the same conversation from two sides |
| `SUP-012` | `support` and `admin-support` screens | ✅ | `SUP-011` | — | One page, its audience decided by role |

### Status values

⬜ Not started · 🔄 In progress · ✅ Done · ❌ Removed · 🔴 Blocked

---

## Completion Log

| Date | Task ID | Completed by | Notes |
|---|---|---|---|
| 2026-08-16 | `SUP-001` to `SUP-012` | AI Agent — Claude | **Specifications authored and the domain built.** Verified end to end against the running system: a ticket opens as `Created`, rating before resolution answers 409, assigning moves it to `In review`, an agent reply produces a `Support` notification for the member (`BR-SUP-012`, crossing into `notifications` through the event), replying to a resolved ticket answers 409, rating after resolution succeeds, and **reopening clears both the rating and the review** (`BR-SUP-007`). The scope matrix holds: an administrator reads a Midtown ticket and answers 403 once it moves to Chicago's Loop, while the super administrator and the ticket's own member still read it |

---

## Progress Summary

| Layer | Done | Total |
|---|---|---|
| Domain | 4 | 4 |
| Application | 5 | 5 |
| Infrastructure | 1 | 1 |
| Presentation | 1 | 1 |
| Frontend | 1 | 1 |
| **Total** | **12** | **12** |
