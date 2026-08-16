# Recommendations — Business Specification

**Last reviewed:** 2026-08-15
**Reviewed by:** Francisco Torregrosa
**Version:** 0 — placeholder, authored during PLAN-001 Stage 7
**Ring:** MVP

> **PLACEHOLDER.** This file carries every required section with guidance on what belongs in each.
> It is filled in at the start of PLAN-001 Stage 7, before any implementation in this domain.
> The product authority is the prototype in `docs/design/` — read `prototype.source.js` for the real
> rules, exact copy, and seed data. Do not invent product behaviour.


---

## 1. Purpose

Owns AI-generated book recommendations and the per-library provider configuration that powers them. It answers "what should this member read next, and which credentials generated that answer".

---

## 2. Glossary

Terms specific to this domain. Where a term means something different here than in
`global_spec.md`, that difference must be stated explicitly.

| Term | Definition |
|---|---|
| **Provider** | An external model vendor: Anthropic or OpenAI |
| **Agent** | A prompt template with a defined objective and persona. **In `support`, "agent" means a staff member handling a ticket. The two meanings are unrelated** |
| **Library configuration** | The provider, model, agent, and credential a specific library uses |
| **Fallback** | The most-borrowed ranking served when no live configuration applies |
| **Recommendation set** | A cached group of suggestions generated for one member at one point in time |

---

## 3. Business Rules

Numbered `BR-REC-{NNN}`. Each rule must be a complete, unambiguous, independently testable
statement. Use "must", never "should". A rule that does not fit in one sentence is probably two rules.
**An ID never changes**, even when the rule text does.

| ID | Rule |
|---|---|
| `BR-REC-001` | *To be authored.* |

Rules this domain is expected to define:

- That each library supplies and manages its own credentials
- Which plans may see recommendations, and which never may
- What a member sees when their library has no live configuration
- That credentials are encrypted at rest and never returned by any API response
- What data may and may not be sent to a provider
- Caching and regeneration policy
- Rate limiting per member
- Behaviour when a provider fails

---

## 4. Acceptance Criteria

Numbered `AC-REC-{NNN}`, each mapping to one or more business rules. These drive test definition.

| ID | Criterion | Covers |
|---|---|---|
| `AC-REC-001` | *To be authored.* | `BR-REC-001` |

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

- Natural-language and semantic search over the catalogue
- Automatic metadata enrichment of books
- Model training or fine-tuning of any kind
- Choosing which models exist - the catalogue of allowed models is configuration, not domain logic

---

## 7. Prototype Reference

Screens: `ai` (AI recommendations) and `settings → AI recommendations per library`

Read `docs/design/prototype.source.js` for the authoritative rules, copy, and seed data.
Read `docs/design/prototype.text-outline.txt` to locate a screen or string quickly.
