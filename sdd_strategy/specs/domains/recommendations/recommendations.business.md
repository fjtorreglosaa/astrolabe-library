# Recommendations — Business Specification

**Last reviewed:** 2026-08-16
**Reviewed by:** AI Agent — Claude — 2026-08-16
**Version:** 1 — authored at the start of PLAN-001 Stage 7
**Ring:** MVP

---

## 1. Purpose

Owns model-generated book recommendations and the per-library provider configuration that powers
them. It answers *"what should this member read next, and which credentials generated that answer"*.

The domain exists because the answer is **not** a property of the network. Two members of the same
plan, in two different cities, can get different answers or no answer at all, because their libraries
made different decisions about connecting a provider and paying for it.

---

## 2. Glossary

| Term | Definition |
|---|---|
| **Provider** | An external model vendor. The prototype offers exactly two: `Claude` and `OpenAI` |
| **Agent** | A prompt template with a defined objective and persona. **In `support`, "agent" means a staff member handling a ticket. The two meanings are unrelated** |
| **Library configuration** | The provider, credential and on/off state a specific library uses |
| **Connected** | A library holding a verified credential with recommendations switched on. The prototype's word, shown as "{provider} connected" |
| **Fallback** | The most-borrowed ranking served when no live configuration applies. Not an error and not an empty state |
| **Recommendation set** | A cached group of suggestions generated for one member at one point in time |
| **Reason** | The one-sentence justification shown beside each suggestion. The prototype always shows one |

---

## 3. Business Rules

| ID | Rule |
|---|---|
| `BR-REC-001` | Each library supplies and manages its own provider credential. A library's staff configure it; no credential is shared between libraries |
| `BR-REC-002` | A member on the **Basic** plan must never receive recommendations, and must never be shown the surface at all |
| `BR-REC-003` | A member whose city has no connected library must receive the **most-borrowed fallback**, never an error and never an empty list |
| `BR-REC-004` | A provider credential must be encrypted at rest and must **never** be returned by any API response, in whole or in part |
| `BR-REC-005` | Only aggregated, anonymised reading data may be sent to a provider. No name, no email, no member identifier, and no individual reservation record |
| `BR-REC-006` | A recommendation set must be cached and regenerated on demand or on expiry, never generated per render |
| `BR-REC-007` | When a provider fails, the last cached set must be shown; if there is none, the fallback must be. A member must never see an error on this surface |
| `BR-REC-008` | A credential must be **verified against its provider before it goes live**. An unverified credential leaves the library unconnected |
| `BR-REC-009` | Only titles with at least one copy in the catalogue may be recommended. A suggestion nobody can borrow is not a recommendation |
| `BR-REC-010` | Every suggestion must carry a stated reason. A suggestion without one must not be shown |
| `BR-REC-011` | Regeneration must be rate limited per member. A member must not be able to spend a library's credit by refreshing |
| `BR-REC-012` | Switching a library off must take effect immediately for its members, and must preserve the stored credential so it can be switched back on |
| `BR-REC-013` | Configuring a library is subject to `BR-NET-006`: a staff user may configure only libraries assigned to them |

### Which library answers for a member

`BR-REC-003` says "a member whose city has no connected library", and that is deliberate. The
prototype decides it by city, not by home library:

```js
aiLive = aiPlan && LIBRARIES.filter(l => l.city === myCity && liveLibs.indexOf(l) > -1).length > 0
```

A member is served by **any** connected library in their city. That is the rule as transcribed. It
also happens to be the kinder reading: a member should not lose recommendations because the single
branch nearest them has not paid for a key while the branch across town has.

---

## 4. Acceptance Criteria

| ID | Criterion | Covers |
|---|---|---|
| `AC-REC-001` | A Plus member of a connected library receives model-generated suggestions, each with a stated reason | `BR-REC-001`, `BR-REC-010` |
| `AC-REC-002` | The same member at an unconnected library sees the most-borrowed fallback, never an error | `BR-REC-003` |
| `AC-REC-003` | A Basic member cannot reach the surface: the entry is hidden and the endpoint refuses | `BR-REC-002` |
| `AC-REC-004` | **No API response contains a stored credential, in any field, at any endpoint** | `BR-REC-004` |
| `AC-REC-005` | The payload sent to a provider contains no name, email, identifier or individual reservation | `BR-REC-005` |
| `AC-REC-006` | Two consecutive reads return the same set without calling the provider twice | `BR-REC-006` |
| `AC-REC-007` | A provider timeout yields the previous set; with no previous set, the fallback | `BR-REC-007` |
| `AC-REC-008` | Saving an invalid credential leaves the library unconnected and says so | `BR-REC-008` |
| `AC-REC-009` | A recommended title always has at least one copy in the catalogue | `BR-REC-009` |
| `AC-REC-010` | An administrator cannot configure a library outside their scope | `BR-REC-013` |

---

## 5. Edge Cases

| Scenario | Expected behaviour |
|---|---|
| A member downgrades to Basic while holding a cached set | The surface disappears. The cached set is neither shown nor deleted — an upgrade restores it rather than paying to regenerate it |
| A library switches off while a member's set is cached | The member falls back at once. `BR-REC-012` — the switch is immediate, not on expiry |
| The provider returns a title that is not in the catalogue | That suggestion is dropped, silently. `BR-REC-009`. If every suggestion is dropped the fallback is served |
| The provider returns a suggestion with no reason | Dropped, by `BR-REC-010`. A blank justification looks like a bug to the member |
| Two libraries in a city are connected to different providers | Either may answer. The rule names no precedence, and inventing one would be product design rather than transcription |
| A member has no reading history at all | The fallback is served. There is nothing to personalise from, and a model asked to invent one produces confident noise |
| A credential is rejected by the provider *after* it went live | The library falls back on the next request, and its staff see "Not configured" so they can act |
| A member refreshes repeatedly | Rate limited by `BR-REC-011`, and the cached set is returned. Refreshing must not spend a library's money |
| The whole city's libraries switch off at once | Fallback, which is a catalogue query and needs no provider |

---

## 6. Out of Scope

- Natural-language and semantic search over the catalogue
- Automatic metadata enrichment of books
- Model training or fine-tuning of any kind
- Choosing which models exist — the catalogue of allowed models is configuration, not domain logic
- Billing a library for provider usage. The library pays its provider directly; nothing here meters it
- Recommending anything other than books — no plan, no library, no event recommendations

---

## 7. Prototype Reference

Screens: `ai` (AI recommendations) and `settings → AI recommendations per library`.

The settings screen states the whole domain in one sentence:

> *"Each library runs on its own key. Members of a connected library get model-generated picks;
> everywhere else they see the most-borrowed fallback. Plus and Max members only — Basic never sees
> this surface."*

A suggestion carries a title, an author, a **reason** and a match percentage. The configuration row
carries a provider choice, a key field and a **"Save and test"** button — the test is what
`BR-REC-008` makes normative.

---

## 8. Open Questions

**The match percentage.** The prototype shows `94% match`, `91% match` and so on. Nothing states how
it is computed, and no provider returns such a number natively. It is transcribed as **display copy
supplied by the model alongside its reason**, not as a computed score — inventing an algorithm for it
would be inventing product. If it should be a real ranking, that is a decision to record here first.

**Rate limit.** `BR-REC-011` requires one and does not name a figure. Proposed: **one regeneration per
member per hour**, with reads always served from cache. Raised as `GLOBAL-023`.
