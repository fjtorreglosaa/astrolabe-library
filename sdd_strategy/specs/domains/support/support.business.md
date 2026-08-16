# Support — Business Specification

**Last reviewed:** 2026-08-16
**Reviewed by:** AI Agent — Claude — 2026-08-16
**Version:** 1 — authored at the start of PLAN-001 Stage 9
**Ring:** Phase 2

---

## 1. Purpose

Owns support tickets: a member's question, the conversation that answers it, and what they thought of
the answer.

It answers *"who asked what, who is handling it, and did we help"*.

---

## 2. Glossary

| Term | Definition |
|---|---|
| **Ticket** | One member's issue, with an identifier of the form `TCK-NNNN` |
| **Agent** | The staff member handling a ticket. **In `recommendations`, "agent" is a prompt template. The two meanings are unrelated** |
| **Message** | One entry in the conversation, from the member or the agent |
| **Category** | What the ticket is about, from a closed list of five |
| **Rating** | One to five stars, given by the member when a ticket is resolved |

---

## 3. Business Rules

| ID | Rule |
|---|---|
| `BR-SUP-001` | A ticket must belong to exactly one member and carry exactly one category from the closed list |
| `BR-SUP-002` | A ticket must move only `Created → InReview → Resolved`, and may be reopened from `Resolved` back to `InReview` |
| `BR-SUP-003` | A ticket must have an agent before it enters `InReview`, and assigning one must move it there |
| `BR-SUP-004` | Only the member who opened a ticket, and staff, may read it. No other member may, by any route |
| `BR-SUP-005` | A member may rate a ticket only once it is `Resolved`, and only their own |
| `BR-SUP-006` | A rating must be one to five stars. A written review is optional and may accompany it |
| `BR-SUP-007` | Reopening must clear the rating, because the question it answered — "did we help" — is open again |
| `BR-SUP-008` | Every message must record who wrote it and when, and must never be edited or deleted |
| `BR-SUP-009` | A ticket must record the library it concerns, so it can be routed to staff who can act on it |
| `BR-SUP-010` | Staff may only see tickets for libraries within their scope. A super administrator sees all |
| `BR-SUP-011` | A resolved ticket must accept no new messages until it is reopened |
| `BR-SUP-012` | Answering a member must notify them, through `notifications` |

---

## 4. Acceptance Criteria

| ID | Criterion | Covers |
|---|---|---|
| `AC-SUP-001` | A member opens a ticket, an agent is assigned, it moves to `InReview` | `BR-SUP-002`, `BR-SUP-003` |
| `AC-SUP-002` | Another member cannot read that ticket by any route | `BR-SUP-004` |
| `AC-SUP-003` | An administrator sees tickets for their libraries and no others | `BR-SUP-010` |
| `AC-SUP-004` | Rating before resolution is refused; after it, accepted once | `BR-SUP-005` |
| `AC-SUP-005` | Reopening clears the rating and admits messages again | `BR-SUP-007`, `BR-SUP-011` |
| `AC-SUP-006` | An agent reply produces a `Support` notification for the member | `BR-SUP-012` |

---

## 5. Edge Cases

| Scenario | Expected behaviour |
|---|---|
| The member replies to their own resolved ticket | Refused by `BR-SUP-011`. Reopening is the deliberate act, and letting a reply do it silently would reopen tickets nobody meant to |
| A ticket's library is withdrawn | The ticket stays and stays readable. `BR-NET-005` preserves history, and a member's unanswered question is history |
| An agent is revoked while holding tickets | The tickets keep their agent's name. Reassignment is a separate act, and blanking the name would lose who answered |
| A member rates, then the ticket is reopened and resolved again | They may rate again. `BR-SUP-007` cleared the first, so this is a first rating of the second resolution |
| A member opens two tickets about the same thing | Both exist. Deduplicating would need judgement no rule supplies |

---

## 6. Out of Scope

- Service level agreements, response time targets and escalation
- Email replies. The conversation lives in the product
- Attachments of any kind
- Internal notes invisible to the member. Every message here is part of the conversation

---

## 7. Prototype Reference

The `support` and `admin-support` screens. Statuses `Created`, `In review` and `Resolved`, the five
categories, and the rating with its written review are transcribed from `TICKET_STATUS`,
`TICKET_CATS` and `TICKETS_SEED`.
