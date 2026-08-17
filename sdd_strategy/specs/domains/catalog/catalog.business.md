# Catalog — Business Specification

> **Agent review note — 2026-08-16, AI Agent — Claude**
>
> `BR-CAT-032` and the edge case below were **corrected, not added**. This file recorded that a
> member may review a book they never borrowed, on the stated grounds that "the prototype places no
> restriction". The prototype does restrict it: its rating dialog is gated on
> `canRate: done && !isLibrarian`, it is opened from a returned loan (`onRate` hangs off `l.id`,
> a loan), there is no path to it from the catalogue, and the dialog's first line reads
> "You returned this copy on {date}".
>
> The user confirmed the rule independently. Raised here because a business rule is not something an
> agent may decide alone — the correction is recorded so the disagreement is visible rather than
> quietly overwritten.


**Last reviewed:** 2026-08-15
**Reviewed by:** Francisco Torregrosa
**Version:** 1
**Ring:** MVP

> **Growth threshold breached.** This domain carries **31 business rules** against a limit of 20
> (SDD+ §6.2). `GLOBAL-018` is raised in `global_task_spec.md` with a proposed `catalog` / `reviews`
> split. **No split without written approval.**

---

## 1. Purpose

Catalog owns what exists to read: books, their physical copies, how they are found, and — above all
— **whether a given member may reserve a given copy**. It also owns the lifecycle of a book within
the collection and the reviews members leave on it.

It answers *"what exists, where is it, and may this member have it"*.

The access rule is the single most consequential rule in the product: `reservations` refuses a loan
on it, `store` prices a purchase on it, and the interface explains itself with it. It is specified
here once and consumed everywhere.

---

## 2. Glossary

| Term | Definition |
|---|---|
| **Book** | The bibliographic work. Never borrowed or sold directly |
| **Copy** | A specific physical instance of a book, held by exactly one library. This is what gets reserved |
| **Stock** | How many copies of a book a library holds that are available right now |
| **Tier** | A property of a **book**: `Basic`, `Plus` or `Max`. Not a property of a member |
| **Reach** | The set of libraries a member's plan permits borrowing from. Supplied by `membership` |
| **Access verdict** | The decision for one member and one copy: reservable, or not and why |
| **Rejection reason** | The member-facing explanation of a negative verdict. Wording is fixed by the prototype |
| **Badge** | The single reason shown on a book card, chosen from the verdicts of all its copies |
| **Lifecycle state** | One of `draft`, `catalog`, `repair`, `deleted` |
| **Review** | A member's star rating and optional written comment on a book |

> **Tier and plan are different things.** A book's tier is what the book requires; a member's plan is
> what the member has. Access is the comparison of the two, never a property of either alone.

---

## 3. Business Rules

### Books and copies

| ID | Rule |
|---|---|
| `BR-CAT-001` | A book carries its own plan tier, independent of any member's plan |
| `BR-CAT-002` | A copy belongs to exactly one library, and stock is counted per library, never per book |
| `BR-CAT-003` | A book's ISBN must be unique across the catalogue |
| `BR-CAT-004` | A book must record at least title, author, ISBN, genre, tier, retail price and stock per library |
| `BR-CAT-005` | A book without a cover image is displayed with a generated colour tint, chosen deterministically so the same book always looks the same |

### The access rule

These five rules together decide every loan in the system.

| ID | Rule |
|---|---|
| `BR-CAT-006` | A copy is reservable only when its library's stock is greater than zero |
| `BR-CAT-007` | For a **Basic** member, the book's tier must be `Basic` **and** the copy must be at the member's home library |
| `BR-CAT-008` | For a **Plus** member, the copy must be at a library in the member's city of residence. No tier restriction applies |
| `BR-CAT-009` | For a **Max** member, no location and no tier restriction applies |
| `BR-CAT-010` | A book is reservable when **at least one** of its copies is reservable for that member |

### Explaining a refusal

A member must always be told why, in the prototype's own words.

| ID | Rule |
|---|---|
| `BR-CAT-011` | When no copy anywhere has stock, the reason is **"All copies out"** |
| `BR-CAT-012` | When a Basic member looks at a book above `Basic` tier, the reason is **"Not in Basic plan"**, and it takes precedence over every other reason |
| `BR-CAT-013` | When stock exists but only outside a Basic member's home library, the reason is **"Home library only"** |
| `BR-CAT-014` | When stock exists but only outside a Plus member's city, the reason is **"Not in {city}"** |
| `BR-CAT-015` | Per copy, the reasons are **"All copies out"**, **"Not in Basic catalog"**, **"Basic borrows at {library} only"** and **"Outside {city}"** |
| `BR-CAT-016` | A book a member cannot reserve is still listed and still opens. Reach restricts borrowing, never discovery |

### Search and listing

| ID | Rule |
|---|---|
| `BR-CAT-017` | A book must be findable by title, author, ISBN, genre and publisher, with partial matching |
| `BR-CAT-018` | Search must be case-insensitive and must ignore surrounding whitespace |
| `BR-CAT-019` | Results must be filterable by genre and sortable, and every listing must be paginated |
| `BR-CAT-020` | The catalogue lists only books in the `catalog` state. A `draft`, `repair` or `deleted` book is never shown to a member |

### Lifecycle

| ID | Rule |
|---|---|
| `BR-CAT-021` | A book moves `draft → catalog → repair → catalog`, and `→ deleted`, from which it may be restored |
| `BR-CAT-022` | A `draft` book is visible to staff only and is not reservable |
| `BR-CAT-023` | Sending a book to `repair` requires a typed reason: damaged spine, water damage, missing pages, rebinding, cover replacement, or other |
| `BR-CAT-024` | Removing a book requires a typed reason: donated, damaged beyond repair, lost by member, withdrawn from collection, or other |
| `BR-CAT-025` | Every lifecycle transition writes an audit entry recording who, what, when and the stated reason |
| `BR-CAT-026` | A book in `repair` or `deleted` does not invalidate reservations already in progress on its copies |

### Reviews

| ID | Rule |
|---|---|
| `BR-CAT-027` | A member may leave at most one review per book, with a star rating from 1 to 5 and an optional comment |
| `BR-CAT-028` | A member may edit or remove their own review, and may never touch anyone else's |
| `BR-CAT-029` | A review is attributed with the member's name and initials, as shown in the catalogue |
| `BR-CAT-030` | A book's displayed rating is the mean of its reviews, and a book with no reviews shows no rating rather than zero |
| `BR-CAT-031` | Removing a review recalculates the book's rating immediately |
| `BR-CAT-032` | A member may review a book only once they have borrowed a copy and returned it. The entry point is a returned reservation, never the catalogue |

---

## 4. Acceptance Criteria

| ID | Criterion | Covers |
|---|---|---|
| `AC-CAT-001` | A Basic member can reserve a `Basic` book with stock at their home library | `BR-CAT-007` |
| `AC-CAT-002` | A Basic member cannot reserve a `Plus` book, even with stock at their home library, and is told "Not in Basic plan" | `BR-CAT-012` |
| `AC-CAT-003` | A Basic member cannot reserve a `Basic` book held only at another branch of their own city, and is told "Home library only" | `BR-CAT-013` |
| `AC-CAT-004` | A Plus member can reserve any tier at any library in their city | `BR-CAT-008` |
| `AC-CAT-005` | A Plus member cannot reserve a book held only in another city, and is told "Not in {city}" | `BR-CAT-014` |
| `AC-CAT-006` | A Max member can reserve any tier at any library in the network | `BR-CAT-009` |
| `AC-CAT-007` | No member can reserve a book whose every copy is out, and all are told "All copies out" | `BR-CAT-006`, `BR-CAT-011` |
| `AC-CAT-008` | A book a member cannot reserve still appears in search and still opens | `BR-CAT-016` |
| `AC-CAT-009` | The full access matrix behaves as specified: 3 plans × 3 tiers × in/out of city × in/out of home library × with/without stock | `BR-CAT-006` to `BR-CAT-010` |
| `AC-CAT-010` | Searching `"  KLARA  "` finds *Klara and the Sun* | `BR-CAT-018` |
| `AC-CAT-011` | A member's search never returns a `draft`, `repair` or `deleted` book | `BR-CAT-020` |
| `AC-CAT-012` | Sending a book to repair without a reason is refused | `BR-CAT-023` |
| `AC-CAT-013` | A member reviewing the same book twice updates their review rather than creating a second | `BR-CAT-027` |
| `AC-CAT-014` | A book with no reviews reports no rating, not a rating of zero | `BR-CAT-030` |
| `AC-CAT-015` | A member who has never returned a copy of a book cannot review it, and nothing is written | `BR-CAT-032` |

---

## 5. Edge Cases

| Scenario | Expected behaviour |
|---|---|
| A book has stock in the member's city but zero at their home library, and the member is Basic | Refused with "Home library only". The stock exists but not where this plan can reach it |
| A `Plus`-tier book has stock only at a Basic member's home library | Refused with "Not in Basic plan". The tier check takes precedence, because telling them about the library would imply a different library would help |
| A member's city has no active library | They can reserve nothing. This must not be an error: `network` guarantees a city offered at registration has one, so it can only arise after a library is deactivated |
| A member changes city while holding reservations | The reservations stand. Their reach changes for new reservations only |
| A book is sent to repair while copies are on loan | The loans run to completion. The book simply stops appearing in member-facing search |
| A book is restored from `deleted` | It returns to `catalog` and becomes reservable again if stock allows. Its reviews and rating survive |
| Two members reserve the last copy at the same moment | Out of scope here. `catalog` answers whether it is reservable; `reservations` resolves the race |
| A member reviews a book they never borrowed | **Refused** (`BR-CAT-032`). This entry previously said the opposite — see the agent review note at the top of this file |
| A deleted member's reviews | Remain visible and keep counting toward the rating. Removing them would silently change a book's score |

---

## 6. Out of Scope

Explicitly **not** handled by this domain:

- Creating the reservation itself, and any concurrency around the last copy — that is `reservations`
- Selling a book, and any discount on the price — that is `store`. This domain owns the retail price
- What a plan entitles a member to — that is `membership`, which supplies the reach
- Which library holds what — `network` owns the geography; this domain owns the stock in it
- Full-text and semantic search, and any AI-driven discovery — that is `recommendations`
- Reserving a book that is out of stock, and any holds queue
- Transferring a copy between libraries
- Digital and audio formats

---

## 7. Prototype Reference

Screens: `catalog` with card and table views, the book detail panel, and `admin-books` with its
three-step creation wizard.

The access rule is implemented in `docs/design/prototype.source.js` as `copyState` and `bookAccess`.
**Those two functions are the authority** — the rules above are a transcription of them, and if they
ever disagree, the prototype wins.

Seed catalogue: 12 books across 5 libraries, with tiers `Basic`, `Plus` and `Max` distributed so
every branch of the access rule is reachable in a demo. *The Savage Detectives* has zero stock, which
is what makes "All copies out" observable without editing data.
