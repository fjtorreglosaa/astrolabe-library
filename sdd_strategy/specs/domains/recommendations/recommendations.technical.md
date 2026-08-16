# Recommendations — Technical Specification

**Last reviewed:** 2026-08-16
**Reviewed by:** AI Agent — Claude — 2026-08-16
**Version:** 1 — authored at the start of PLAN-001 Stage 7
**Implements:** `BR-REC-001` to `BR-REC-013`

---

## 1. Domain Model

Two aggregates, and they are separate because they change for different reasons and at wildly
different rates. A library's credential changes when its staff decide something; a member's
recommendation set changes when the cache expires. Folding them together would mean loading a
credential to render a list.

### `LibraryAiConfiguration` — aggregate root

```csharp
public sealed class LibraryAiConfiguration : AggregateRoot
{
    public Guid LibraryId { get; private set; }
    public AiProvider Provider { get; private set; }

    /// BR-REC-004. Ciphertext, never plaintext, and there is deliberately no property that
    /// returns the plaintext — decryption happens in Infrastructure, at the point of use.
    public EncryptedSecret Credential { get; private set; }

    /// BR-REC-008. False until the credential has answered its provider.
    public bool IsVerified { get; private set; }

    /// BR-REC-012. Independent of the credential, so switching off preserves it.
    public bool IsEnabled { get; private set; }

    public DateTimeOffset? LastVerifiedAt { get; private set; }
    public DateTimeOffset? LastFailureAt { get; private set; }

    /// The single question every other domain asks. BR-REC-003.
    public bool IsConnected => IsEnabled && IsVerified;

    public static Result<LibraryAiConfiguration> Configure(
        Guid libraryId, AiProvider provider, EncryptedSecret credential, DateTimeOffset now);

    public void MarkVerified(DateTimeOffset now);          // BR-REC-008
    public void MarkFailed(DateTimeOffset now);            // BR-REC-007, drops IsVerified
    public Result Enable();                                // refuses while unverified
    public void Disable();                                 // BR-REC-012, keeps the credential
    public Result Replace(AiProvider provider, EncryptedSecret credential, DateTimeOffset now);
}
```

### `RecommendationSet` — aggregate root

```csharp
public sealed class RecommendationSet : AggregateRoot
{
    public Guid MemberId { get; private set; }
    public RecommendationSource Source { get; private set; }   // Model | Fallback
    public Guid? GeneratedByLibraryId { get; private set; }     // null for a fallback
    public DateTimeOffset GeneratedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public IReadOnlyList<RecommendationItem> Items { get; }

    public bool IsFresh(DateTimeOffset now) => now < ExpiresAt;

    public static Result<RecommendationSet> FromModel(
        Guid memberId, Guid libraryId, IReadOnlyList<RecommendationItem> items,
        DateTimeOffset now, TimeSpan lifetime);

    public static RecommendationSet FromFallback(
        Guid memberId, IReadOnlyList<RecommendationItem> items,
        DateTimeOffset now, TimeSpan lifetime);
}
```

### `RecommendationItem` — entity

`BookId`, `Reason`, `MatchPercent`. `Create` **refuses an empty reason** (`BR-REC-010`), so a set
cannot be built with a suggestion that would render blank.

### `EncryptedSecret` — value object

Holds ciphertext, nonce and the key version that encrypted it. It has **no** `Value` property and no
`ToString` override that could leak it; the only way back to plaintext is `ISecretProtector`, which
lives in Infrastructure. A DTO cannot accidentally serialise what it cannot reach.

### Enumerations

`AiProvider` — `Claude`, `OpenAI`. The prototype offers exactly these two.
`RecommendationSource` — `Model`, `Fallback`.

### Domain events

`LibraryAiConfigured`, `LibraryAiDisabled` — the second evicts cached sets for that city, which is
how `BR-REC-012` takes effect immediately without every caller remembering to.

---

## 2. Application Layer

```text
Name:             ConfigureLibraryAiCommand
Type:             Command
Input:            libraryId, provider, credential (plaintext, inbound only)
Output:           Result<LibraryAiStatusDto>
Business rule:    BR-REC-001, BR-REC-008, BR-REC-013
Handler location: Application/Features/Recommendations/Commands/ConfigureLibraryAi/

Name:             DisableLibraryAiCommand
Type:             Command
Input:            libraryId
Output:           Result
Business rule:    BR-REC-012, BR-REC-013
Handler location: Application/Features/Recommendations/Commands/DisableLibraryAi/

Name:             RegenerateRecommendationsCommand
Type:             Command
Input:            — (the member comes from the token)
Output:           Result<RecommendationSetDto>
Business rule:    BR-REC-006, BR-REC-011
Handler location: Application/Features/Recommendations/Commands/RegenerateRecommendations/

Name:             GetMyRecommendationsQuery
Type:             Query
Input:            —
Output:           Result<RecommendationSetDto>
Business rule:    BR-REC-002, BR-REC-003, BR-REC-006, BR-REC-007
Handler location: Application/Features/Recommendations/Queries/GetMyRecommendations/

Name:             GetLibraryAiStatusQuery
Type:             Query
Input:            —  (scoped to the caller's libraries)
Output:           Result<IReadOnlyList<LibraryAiStatusDto>>
Business rule:    BR-REC-004, BR-REC-013
Handler location: Application/Features/Recommendations/Queries/GetLibraryAiStatus/
```

`LibraryAiStatusDto` carries `LibraryId`, `LibraryName`, `Provider`, `IsConnected`, `LastVerifiedAt`
and **nothing resembling a credential** — not a masked one, not a last-four, not a length.

### Seams

| Seam | Purpose |
|---|---|
| `IAiRecommendationProvider` | Calls the vendor. One implementation per provider, chosen by `AiProvider` |
| `ISecretProtector` | Encrypts and decrypts a credential. The only path back to plaintext |
| `IFallbackRecommender` | The most-borrowed ranking. A catalogue query, no provider involved |
| `IReadingProfileBuilder` | Builds the **anonymised** payload. `BR-REC-005` lives here and nowhere else |

---

## 3. Infrastructure

- `LibraryAiConfigurationConfiguration` — one row per library, unique on `library_id`. The credential
  columns are `bytea`; there is no unique index on them and no query filters by them.
- `RecommendationSetConfiguration` — owned collection of items; unique on `member_id` filtered on the
  newest, in the same shape `subscriptions` uses.
- `ClaudeRecommendationProvider`, `OpenAiRecommendationProvider` — RestSharp, per `GUIDELINES.md`.
  Both honour a short timeout, because `BR-REC-007` prefers a stale answer to a slow one.
- `DataProtectionSecretProtector` — ASP.NET Core Data Protection, key ring persisted with the
  application. Key version stored beside the ciphertext so a rotation can decrypt old rows.
- Tests mock the vendors with **WireMock.Net** per SDD+ §9.1. No real HTTP call in any unit test.

---

## 4. Architecture Decision Log

| Decision | Choice | Rationale | Alternatives rejected |
|---|---|---|---|
| Where the credential lives | Encrypted on the configuration aggregate, decrypted only in Infrastructure at the point of call | `BR-REC-004` is the rule this domain most needs to be unable to break. A value object with no way back to plaintext makes the leak unrepresentable rather than merely forbidden | A plaintext column with "do not return it" documented — rejected: that is the same protection every leaked key has ever had. A separate vault service — rejected as infrastructure this phase explicitly excludes |
| Verified and enabled as two flags | Separate | They answer different questions and change at different times: a credential can be verified and then switched off, and `BR-REC-012` requires switching off to preserve it. One flag would force a re-verification on every re-enable, which spends the library's money to learn what was already known | A single `IsConnected` — rejected: it loses which of the two is false, and staff need to know whether to fix a key or flip a switch |
| Fallback as a seam | `IFallbackRecommender`, a catalogue query | `BR-REC-003` and `BR-REC-007` both route to it, and it must work when every provider is down. Keeping it a plain query means the failure path has no dependency that can itself fail | Generating a fallback from the model — rejected: the failure path must not need the thing that failed |
| Caching | Stored aggregate with an expiry, not a memory cache | `BR-REC-007` needs the *last* set after a provider failure, which an evicted memory entry cannot give. Persisting it also means a restart does not cost every member a regeneration | `IMemoryCache` — rejected on both counts. Regenerating per render — rejected by `BR-REC-006` |
| Anonymisation in one builder | `IReadingProfileBuilder` | `BR-REC-005` is a rule about a payload, and a rule about a payload is only enforceable if exactly one place builds it. Two builders is two chances to include an email | Each provider client assembling its own — rejected: the same rule implemented twice |
| Which library answers | Any connected library in the member's city | Transcribed from the prototype, which filters live libraries by city rather than by home library | Home library only — rejected: not what the prototype does, and it would deny a member their own city's connected branch |

---

## 5. Testing Notes

The mandatory test is `AC-REC-004`: **no API response can expose a stored credential**. It is written
as a sweep over every recommendations endpoint, asserting the serialised body never contains the
stored ciphertext, the plaintext, or any substring of either — not as a per-DTO assertion, because a
per-DTO test only covers the DTOs somebody remembered to write one for.
