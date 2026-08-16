# Catalog — Business Specification

**Last reviewed:** 2026-08-15
**Reviewed by:** Francisco Torregrosa
**Version:** 0 — placeholder, authored during PLAN-001 Stage 2
**Ring:** MVP

> **PLACEHOLDER.** This file carries every required section with guidance on what belongs in each.
> It is filled in at the start of PLAN-001 Stage 2, before any implementation in this domain.
> The product authority is the prototype in `docs/design/` — read `prototype.source.js` for the real
> rules, exact copy, and seed data. Do not invent product behaviour.
>
> **Growth watch:** projected to exceed 20 business rules. Likely split is `catalog` / `reviews`.

---

## 1. Purpose

Owns books, their physical copies, search, and the access policy that decides whether a given member may reserve a given copy. It also owns the book lifecycle and member reviews. It answers "what exists, where is it, and may this member have it".

---

## 2. Glossary

Terms specific to this domain. Where a term means something different here than in
`global_spec.md`, that difference must be stated explicitly.

| Term | Definition |
|---|---|
| **Book** | The bibliographic work. Never borrowed or sold directly |
| **Copy** | A specific physical instance of a book, belonging to exactly one library |
| **Tier** | A property of a **book**, not of a member: Basic, Plus, or Max |
| **Access policy** | The pure rule deciding whether a member may reserve a copy, and the reason when they may not |
| **Lifecycle state** | One of draft, catalog, repair, or deleted |
| **Review** | A member's star rating and optional written comment on a book |

---

## 3. Business Rules

Numbered `BR-CAT-{NNN}`. Each rule must be a complete, unambiguous, independently testable
statement. Use "must", never "should". A rule that does not fit in one sentence is probably two rules.
**An ID never changes**, even when the rule text does.

| ID | Rule |
|---|---|
| `BR-CAT-001` | *To be authored.* |

Rules this domain is expected to define:

- That every book carries its own tier, independent of any member's plan
- The access rule per plan: Basic at the home library and Basic tier only, Plus across their city, Max across the network
- That stock must be greater than zero for a copy to be reservable
- The exact rejection reason wording, which must match the prototype
- The book lifecycle states and the legal transitions between them
- Typed reasons required for repair and for removal
- That every lifecycle transition writes an audit entry
- Searchable fields and partial matching behaviour
- Review authorship, editing, removal, and how ratings aggregate

---

## 4. Acceptance Criteria

Numbered `AC-CAT-{NNN}`, each mapping to one or more business rules. These drive test definition.

| ID | Criterion | Covers |
|---|---|---|
| `AC-CAT-001` | *To be authored.* | `BR-CAT-001` |

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

- Creating the reservation itself - that belongs to `reservations`
- Selling a book - that belongs to `store`
- Which plan a member holds - that belongs to `membership`
- Full-text and semantic search

---

## 7. Prototype Reference

Screens: `catalog`, the book detail panel, and `admin-books` (Book management) with its three-step wizard

Read `docs/design/prototype.source.js` for the authoritative rules, copy, and seed data.
Read `docs/design/prototype.text-outline.txt` to locate a screen or string quickly.
