# Recommendations — Tasks

**Last reviewed:** 2026-08-16
**Overall progress:** 18/18 (100%)

PLAN-001 Stage 7. Depends on Stages 2 and 3 — it needs a real catalogue and real reading history.

---

## Blocking Dependencies

| Block ID | Description | Status |
|---|---|---|
| `BLOCK-007` | No provider credential is available for a live end-to-end call. Every test mocks the vendor with WireMock.Net, and a real key is needed only to exercise `BR-REC-008` against an actual provider | **Open** — does not block implementation |
| `GLOBAL-023` | `BR-REC-011` requires a rate limit and names no figure | **Open** — built at one hour, the proposed figure. Changing it is a constant and a test |

---

## Task List

| ID | Task | Status | Blocker | Tracking | Notes |
|---|---|---|---|---|---|
| `REC-001` | `AiProvider`, `RecommendationSource` enumerations | ✅ | — | — | Two providers only, per the prototype |
| `REC-002` | `EncryptedSecret` value object with no path to plaintext | ✅ | — | — | `BR-REC-004`. The leak must be unrepresentable, not merely forbidden |
| `REC-003` | `LibraryAiConfiguration` aggregate | ✅ | `REC-002` | — | `BR-REC-001`, `-008`, `-012` |
| `REC-004` | `RecommendationItem` refusing an empty reason | ✅ | — | — | `BR-REC-010` |
| `REC-005` | `RecommendationSet` aggregate with expiry | ✅ | `REC-004` | — | `BR-REC-006` |
| `REC-006` | `RecommendationAccessPolicy` — who may see the surface | ✅ | `REC-003` | — | `BR-REC-002`, `BR-REC-003` |
| `REC-007` | Repositories and `IRecommendationsUnitOfWork` | ✅ | `REC-005` | — | |
| `REC-008` | `ISecretProtector` and its Data Protection implementation | ✅ | `REC-002` | — | `BR-REC-004` |
| `REC-009` | `IReadingProfileBuilder` — the anonymised payload | ✅ | — | — | `BR-REC-005`. One builder, so the rule has one place to live |
| `REC-010` | `IAiRecommendationProvider` and the two vendor clients | ✅ | `REC-009` | — | RestSharp, short timeout |
| `REC-011` | `IFallbackRecommender` — the most-borrowed ranking | ✅ | — | — | `BR-REC-003`. A catalogue query, so it survives a provider outage |
| `REC-012` | `ConfigureLibraryAiCommand` with verification | ✅ | `REC-010` | — | `BR-REC-008`, `BR-REC-013` |
| `REC-013` | `DisableLibraryAiCommand` | ✅ | `REC-003` | — | `BR-REC-012` |
| `REC-014` | `GetMyRecommendationsQuery` | ✅ | `REC-011` | — | `BR-REC-002`, `-003`, `-006`, `-007` |
| `REC-015` | `RegenerateRecommendationsCommand` with the rate limit | ✅ | `REC-014` | `GLOBAL-023` | `BR-REC-011` |
| `REC-016` | `GetLibraryAiStatusQuery` | ✅ | `REC-003` | — | `BR-REC-004` — the DTO carries no credential in any form |
| `REC-017` | `RecommendationsController` and `AdminRecommendationsController` | ✅ | `REC-016` | — | |
| `REC-018` | `ai` screen and the per-library configuration panel | ✅ | `REC-017` | — | Copy transcribed from the prototype |

### Status values

⬜ Not started
🔄 In progress
✅ Done
❌ Removed / not applicable (reason required in Notes)
🔴 Blocked (blocker ID required)

### Tracking reference format

`{PLATFORM} #{ID} — {URL}`. No external tracker is configured, so the column reads `—`.

---

## Completion Log

| Date | Task ID | Completed by | Notes |
|---|---|---|---|
| 2026-08-16 | `REC-008` to `REC-018` | AI Agent — Claude | **Stage 7 complete.** Verified against the running system with a real outbound provider call: a bogus key is refused by Anthropic, the library stays `verified=false, enabled=false`, and the stored column holds ciphertext rather than the key. A member whose city has no connected library gets four fallback suggestions with reasons and no error (`BR-REC-003`), and the staff panel's response contains no credential-shaped field at all (`AC-REC-004`). A library outside an administrator's scope answers 403. **Adding the Data Protection package pulled in a critical-severity advisory** — caught by the build, fixed by pinning 10.0.11, and confirmed clear with `dotnet list package --vulnerable --include-transitive` |
| 2026-08-16 | `REC-001` to `REC-007` | AI Agent — Claude | **Domain layer complete.** `EncryptedSecret` has no path back to plaintext — no `Value`, no accessor, and a `ToString` that redacts — so `BR-REC-004` is unrepresentable rather than merely forbidden, which is the difference between a rule and a promise. `IsVerified` and `IsEnabled` are two flags because a provider outage and a library's own decision are different facts, and staff need to know which one to act on. The fallback path has no `Result` at all, deliberately: it is where every other path goes when it fails. 27 tests |
| 2026-08-16 | — | AI Agent — Claude | **Specifications authored**, per SDD+ Principle 2 and PLAN-001 Stage 7. 13 business rules, 10 acceptance criteria, 9 edge cases and a six-entry decision log, transcribed from the prototype's `ai` and `settings → AI recommendations per library` screens. Two questions raised rather than answered: the match percentage has no stated derivation, and `BR-REC-011` names no rate limit figure |

---

## Progress Summary

| Layer | Done | Total |
|---|---|---|
| Domain | 6 | 6 |
| Application | 5 | 5 |
| Infrastructure | 5 | 5 |
| Presentation | 1 | 1 |
| Frontend | 1 | 1 |
| **Total** | **18** | **18** |
