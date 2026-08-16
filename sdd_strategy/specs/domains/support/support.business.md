# Support — Business Specification

**Last reviewed:** 2026-08-15
**Reviewed by:** Francisco Torregrosa
**Version:** 0 — placeholder, authored during PLAN-001 Stage 9
**Ring:** Phase 2

> **PLACEHOLDER.** This file carries every required section with guidance on what belongs in each.
> It is filled in at the start of PLAN-001 Stage 9, before any implementation in this domain.
> The product authority is the prototype in `docs/design/` — read `prototype.source.js` for the real
> rules, exact copy, and seed data. Do not invent product behaviour.


---

## 1. Purpose

Owns member support requests and the conversation around them: raising a ticket, assigning it to a staff member, answering it, resolving it, and rating the service. It answers "what went wrong for this member, and who is fixing it".

---

## 2. Glossary

Terms specific to this domain. Where a term means something different here than in
`global_spec.md`, that difference must be stated explicitly.

| Term | Definition |
|---|---|
| **Ticket** | A support request raised by a member against a library |
| **Category** | The classification chosen when the ticket is raised |
| **Agent** | The staff member handling a ticket. **In `recommendations`, "agent" means a prompt template. The two meanings are unrelated** |
| **Owner** | The agent currently assigned to a ticket |
| **Service rating** | The member's score and optional written review of how the ticket was handled |

---

## 3. Business Rules

Numbered `BR-SUP-{NNN}`. Each rule must be a complete, unambiguous, independently testable
statement. Use "must", never "should". A rule that does not fit in one sentence is probably two rules.
**An ID never changes**, even when the rule text does.

| ID | Rule |
|---|---|
| `BR-SUP-001` | *To be authored.* |

Rules this domain is expected to define:

- The available categories
- The ticket state machine, including reopening
- Who may assign, answer, and resolve a ticket
- That staff only see tickets for libraries in their scope
- When a member may rate the service, and what the rating attaches to
- What notifications each transition produces

---

## 4. Acceptance Criteria

Numbered `AC-SUP-{NNN}`, each mapping to one or more business rules. These drive test definition.

| ID | Criterion | Covers |
|---|---|---|
| `AC-SUP-001` | *To be authored.* | `BR-SUP-001` |

---

## 5. Edge Cases

Non-obvious scenarios and their expected behaviour. This section is where most defects are prevented.

| Scenario | Expected behaviour |
|---|---|
| *To be authored.* | |

---

## 6. Out of Scope

Explicitly **not** handled by this domain. This section is as important as what the domain does handle —
ambiguity about boundaries is the most common source of domain conflicts.

- Live chat and telephone support
- Service level agreements and escalation timers
- A public knowledge base beyond the static questions shown before ticket creation

---

## 7. Prototype Reference

Screens: `support` (Help and support) and `admin-support` (Support tickets)

Read `docs/design/prototype.source.js` for the authoritative rules, copy, and seed data.
Read `docs/design/prototype.text-outline.txt` to locate a screen or string quickly.
