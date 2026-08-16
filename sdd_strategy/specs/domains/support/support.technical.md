# Support — Technical Specification

**Last reviewed:** 2026-08-16
**Reviewed by:** AI Agent — Claude — 2026-08-16
**Version:** 1 — authored at the start of PLAN-001 Stage 9
**Implements:** `BR-SUP-001` to `BR-SUP-012`

---

## 1. Domain Model

### `Ticket` — aggregate root

```csharp
public sealed class Ticket : AggregateRoot
{
    public string Reference { get; }              // TCK-NNNN, BR-SUP-001
    public Guid MemberId { get; }
    public TicketCategory Category { get; }
    public Guid LibraryId { get; }                // BR-SUP-009
    public string Subject { get; }
    public TicketStatus Status { get; }
    public Guid? AgentUserId { get; }
    public int? Rating { get; }                   // BR-SUP-006
    public string? Review { get; }
    public IReadOnlyList<TicketMessage> Messages { get; }

    public static Result<Ticket> Open(...);
    public Result Assign(Guid agentUserId, DateTimeOffset now);   // BR-SUP-003
    public Result Reply(Guid authorId, TicketAuthor author, string text, DateTimeOffset now);
    public Result Resolve(DateTimeOffset now);
    public Result Reopen(DateTimeOffset now);                     // BR-SUP-007
    public Result Rate(int stars, string? review);                // BR-SUP-005
}
```

The conversation is **owned**, not a separate aggregate: a message has no life without its ticket and
is never queried alone, and the transition rules read the message list to decide.

### Enumerations

`TicketStatus` — `Created`, `InReview`, `Resolved`.
`TicketCategory` — the prototype's five, verbatim.
`TicketAuthor` — `Member`, `Agent`.

---

## 2. Application Layer

```text
OpenTicketCommand        → Result<TicketDto>    BR-SUP-001, BR-SUP-009
ReplyToTicketCommand     → Result               BR-SUP-008, BR-SUP-011, BR-SUP-012
AssignTicketCommand      → Result               BR-SUP-003, BR-SUP-010
ResolveTicketCommand     → Result               BR-SUP-002
ReopenTicketCommand      → Result               BR-SUP-007
RateTicketCommand        → Result               BR-SUP-005, BR-SUP-006
GetMyTicketsQuery        → Result<Paged<...>>   BR-SUP-004
GetTicketQuery           → Result<TicketDto>    BR-SUP-004
SearchTicketsQuery       → Result<Paged<...>>   BR-SUP-010  (staff)
```

---

## 3. Architecture Decision Log

| Decision | Choice | Rationale | Alternatives rejected |
|---|---|---|---|
| Messages owned by the ticket | Owned collection | A message has no meaning alone and is never queried without its ticket, and the transitions read the list to decide. A separate aggregate would need a transaction across two to append one line | A `Message` aggregate — rejected: two aggregates for one conversation |
| Reference format | `TCK-` plus a sequence | Transcribed from the prototype, and it is what a member quotes on the phone. A GUID is unreadable aloud | A GUID as the public identifier — rejected: unusable in the one place it is most needed |
| Reopening clears the rating | Yes, by `BR-SUP-007` | The rating answers "did we help", and reopening says the answer was no. Keeping a five-star rating on a reopened ticket would report satisfaction that was withdrawn | Keeping it — rejected: it makes the metric lie |
| Notifying on reply | A domain event handler in `notifications` | `BR-SUP-012` is a consequence, not a step. A reply must not fail because a notification did | Raising it inline — rejected: couples an answer to a message about it |
| Staff scope | `ILibraryScopeProvider`, by the ticket's library | `BR-SUP-010`, and it reuses the seam every other domain uses rather than inventing a second definition of scope | Scoping by the member's city — rejected: a ticket is about a library, and the rule says so |
