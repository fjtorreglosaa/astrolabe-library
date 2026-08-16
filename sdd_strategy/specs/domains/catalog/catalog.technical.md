# Catalog — Technical Specification

**Last reviewed:** 2026-08-16
**Reviewed by:** AI Agent — Claude — 2026-08-16
**Version:** 1
**Implements:** `BR-CAT-001` to `BR-CAT-031`

> **Growth threshold breached** at 31 business rules. `GLOBAL-018` proposes a `catalog` / `reviews`
> split and recommends deferring it until Stage 6. **No split without written approval.**

---

## 1. Domain Model

### Book — aggregate root

```csharp
public sealed class Book : AggregateRoot
{
    public Isbn Isbn { get; private set; }
    public string Title { get; private set; }
    public string Author { get; private set; }
    public string? Publisher { get; private set; }
    public Genre Genre { get; private set; }
    public PlanTier Tier { get; private set; }
    public Money RetailPrice { get; private set; }
    public BookStatus Status { get; private set; }
    public string? CoverUrl { get; private set; }

    private readonly List<BookCopy> _copies = [];
    public IReadOnlyList<BookCopy> Copies => _copies;

    public static Result<Book> CreateDraft(...);
    public Result Publish();
    public Result SendToRepair(RepairReason reason, DateTimeOffset? expectedBack, string? notes);
    public Result ReturnFromRepair();
    public Result Remove(RemovalReason reason, string? notes);
    public Result Restore();

    public Result AddCopies(Guid libraryId, int quantity);
}
```

`Tier` lives on the book, never on the member. Naming it `PlanTier` rather than `Tier` is deliberate:
the two are compared constantly and a bare `Tier` reads like a member attribute at the call site.

### BookCopy

```csharp
public sealed class BookCopy : Entity
{
    public Guid BookId { get; private set; }
    public Guid LibraryId { get; private set; }
    public int TotalCount { get; private set; }
    public int AvailableCount { get; private set; }

    public bool HasStock => AvailableCount > 0;
    public Result Take();      // reservations decrements
    public void Return();
}
```

Stock is a count on a per-library row rather than one row per physical volume. The prototype tracks
`"4 / 6"` per branch and never identifies an individual volume, so a row per volume would invent
data the product does not have.

### CatalogAccessPolicy — the authority

```csharp
public static class CatalogAccessPolicy
{
    public static CopyAccessVerdict EvaluateCopy(
        MemberEntitlement member, PlanTier bookTier, CopyLocation copy);

    public static BookAccessVerdict EvaluateBook(
        MemberEntitlement member, PlanTier bookTier, IReadOnlyList<CopyLocation> copies);
}
```

A **pure static function** over value inputs: no repository, no clock, no database. That is what lets
the full access matrix — 3 plans × 3 tiers × in/out of city × in/out of home library × with/without
stock — be exercised as fast unit tests rather than as integration tests nobody runs often enough.

`MemberEntitlement` is supplied by `membership`; this domain never reads a subscription itself.

```csharp
public sealed record MemberEntitlement(PlanTier MaxTier, ReachKind Reach, Guid? CityId, Guid? HomeLibraryId);
public sealed record CopyLocation(Guid LibraryId, Guid CityId, int AvailableCount);
public sealed record CopyAccessVerdict(bool CanReserve, CopyRejection? Reason);
public sealed record BookAccessVerdict(bool CanReserve, BookRejection? Badge, IReadOnlyList<CopyAccessVerdict> Copies);
```

Rejections are **enumerations, not strings**. The wording is fixed by the prototype and must be
identical everywhere, so the text is resolved once at the presentation edge and the domain carries
only the reason.

### Review

```csharp
public sealed class Review : Entity
{
    public Guid BookId { get; private set; }
    public Guid MemberId { get; private set; }
    public StarRating Rating { get; private set; }
    public string? Comment { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? EditedAt { get; private set; }

    public Result Edit(StarRating rating, string? comment, DateTimeOffset now);
}
```

### Value objects and enumerations

| Type | Invariant |
|---|---|
| `Isbn` | Normalised by stripping hyphens and spaces; validated as 10 or 13 digits |
| `StarRating` | An integer from 1 to 5. Rejects anything else at construction |
| `PlanTier` | `Basic`, `Plus`, `Max`. Ordered, so "tier within plan" is a comparison |
| `ReachKind` | `HomeLibraryOnly`, `City`, `Network` |
| `BookStatus` | `Draft`, `Catalog`, `Repair`, `Deleted` |
| `Genre` | Fiction, Essay, Science fiction, History, Biography, Technical — the prototype's own list |
| `CopyRejection` | `OutOfStock`, `NotInBasicCatalog`, `HomeLibraryOnly`, `OutsideCity` |
| `BookRejection` | `AllCopiesOut`, `NotInBasicPlan`, `HomeLibraryOnly`, `NotInCity`, `Unavailable` |

### Domain events

| Event | Raised when | Consumed by |
|---|---|---|
| `BookPublished` | A draft enters the catalogue | Audit |
| `BookSentToRepair` / `BookRemoved` / `BookRestored` | Lifecycle transitions | Audit |
| `ReviewPublished` / `ReviewRemoved` | A review changes | Recalculates the book's rating |

---

## 2. Application Layer

### Commands

| Name | Input | Output | Rule |
|---|---|---|---|
| `CreateBookDraftCommand` | metadata, tier, price, copies per library | `Result<Guid>` | `BR-CAT-003`, `-022` |
| `PublishBookCommand` | bookId | `Result` | `BR-CAT-021` |
| `UpdateBookCommand` | bookId, metadata | `Result` | `BR-CAT-004` |
| `SendBookToRepairCommand` | bookId, reason, expectedBack, notes | `Result` | `BR-CAT-023`, `-025` |
| `ReturnBookFromRepairCommand` | bookId | `Result` | `BR-CAT-021` |
| `RemoveBookCommand` | bookId, reason, notes | `Result` | `BR-CAT-024`, `-025` |
| `RestoreBookCommand` | bookId | `Result` | `BR-CAT-021` |
| `PublishReviewCommand` | bookId, rating, comment | `Result` | `BR-CAT-027`, `-028` |
| `RemoveReviewCommand` | bookId | `Result` | `BR-CAT-028`, `-031` |

### Queries

| Name | Input | Output | Rule |
|---|---|---|---|
| `SearchBooksQuery` | term, genre, sortBy, direction, page, pageSize | `Result<PagedResult<BookSummaryDto>>` | `BR-CAT-017` to `-020` |
| `GetBookDetailQuery` | bookId | `Result<BookDetailDto>` | `BR-CAT-010`, `-016` |
| `SearchCatalogForStaffQuery` | term, status, sortBy, direction, page, pageSize | `Result<PagedResult<StaffBookDto>>` | `BR-CAT-022` |
| `GetBookReviewsQuery` | bookId, page, pageSize | `Result<PagedResult<ReviewDto>>` | `BR-CAT-029` |

Thirteen operations, under the threshold with headroom.

Every member-facing query resolves the caller's `MemberEntitlement` once and passes it to the policy,
so a listing of twenty books evaluates access twenty times against one entitlement rather than
loading a subscription twenty times.

---

## 3. Infrastructure

| Concern | Implementation |
|---|---|
| Persistence | `BookRepository`, `ReviewRepository` extending `Repository<TEntity>` |
| EF configuration | One class per entity in `Persistence/Configurations/Catalog` |
| Rating projection | `Book.AverageRating` maintained by a `ReviewPublished` / `ReviewRemoved` handler |

### Persistence notes

- `Isbn` is stored normalised with a unique index, so `BR-CAT-003` holds under concurrency rather
  than relying on a check-then-insert.
- `BookCopy` carries a `RowVersion` for optimistic concurrency. `reservations` decrements stock, and
  two members taking the last copy must not both succeed.
- Search filters on indexed columns and projects to a DTO. It never materialises `Book` aggregates it
  will not mutate.
- The average rating is a stored column, not a computed join. A catalogue listing shows ratings for
  every row, and recomputing them per page would be an aggregate query per book.

---

## 4. Architecture Decision Log

| Decision | Choice | Rationale | Alternatives rejected |
|---|---|---|---|
| Access rule shape | A pure static policy over value inputs | Lets the whole matrix be unit-tested without a database, and guarantees `catalog`, `reservations` and `store` reach identical verdicts | A method on `Book` — rejected: it would need the member's subscription, dragging `membership` into the aggregate. A service with repositories — rejected: makes the highest-value tests slow, so they get run less |
| Rejection reasons | Enumerations resolved to text at the edge | The wording is fixed by the prototype and must be identical in every surface. A string in the domain drifts the moment two call sites format it | Strings in the domain — rejected on drift and on untranslatable copy |
| Stock model | A count per library | The prototype tracks `"4 / 6"` per branch and never identifies a volume | A row per physical volume — rejected: invents data the product does not have and multiplies rows by an order of magnitude |
| Tier naming | `PlanTier` on the book | Tier and plan are compared constantly; a bare `Tier` reads like a member attribute at the call site and invites the exact confusion the rule turns on | `Tier` — rejected on readability at the point of comparison |
| Average rating | Stored column, updated by an event | A listing shows a rating per row; an aggregate query per book would be an N+1 by construction | Computed on read — rejected on N+1. A materialised view — rejected as infrastructure the MVP does not need |
| Entitlement source | Passed in from `membership` | Keeps the plan rules in one domain and the access decision in another, so neither reimplements the other | `catalog` reading subscriptions — rejected: two domains would own the same rule |
| `Isbn` and `StarRating` persistence | Owned types, not value converters | A converter turns the whole value object into an opaque scalar, so `book.Isbn.Value` and `review.Rating.Stars` become untranslatable and every query touching them — catalogue search and the rating average among them — fails at run time rather than at compile time. Owning them keeps the same single column and keeps the member queryable | A value converter — rejected after it shipped and broke search in the running system. `EF.Property<string>` — rejected: it fails in a projection and pushes parameters through the converter backwards in a predicate |
| Sorting | In the database, on a required `BookSortKey` and `SortDirection` | The results are paged, so sorting client-side would order twenty rows out of two hundred and quietly answer a different question. `Availability` sums the branch counts as a subquery, which is what makes it sortable at all across a page | Client-side sorting — rejected outright under paging. An unbounded sort expression — rejected: an enumeration means every header the interface offers has a key behind it and no key is orphaned |
| `Money` persistence | `ComplexProperty`, not a value converter | Same defect as the ISBN, found the same way: a converter hides `Money.Cents`, so ordering by price threw at run time. Money is filtered and sorted on in `store` and `fines` too, so fixing it once is cheaper than meeting it three more times | A value converter — rejected after it broke price sorting in the running system. An owned type — not applicable: `Money` is a struct, and `ComplexProperty` is the equivalent for value types |
| Lifecycle audit | Written inline in the command handler | `BR-CAT-025` makes the entry mandatory, so it must commit with the transition it records. A domain event handler runs after the commit and may be lost, and a trail that can silently skip a transition is not a trail | An event handler — rejected on losability. The events still carry the reason for any future consumer |
| Staff routes | A separate `AdminCatalogController` | These routes can return drafts and removed books, which members must never see. A controller-wide staff policy makes that structural instead of a per-method attribute somebody can forget | Per-endpoint attributes on one controller — rejected: the failure mode is silent and the blast radius is every unpublished book |
| Search status filter | A required parameter on the repository method | `BR-CAT-020` is then enforced by the signature rather than by remembering. No caller can produce a member-facing listing that includes drafts by forgetting to filter | An optional parameter — rejected: the safe value would be the one you get by omission, which is exactly the mistake to prevent |
| Draft visibility | Filtered in the query, not by a global filter | A global query filter would silently hide drafts from staff screens too, and the bug would look like missing data | An EF global query filter — rejected: correct for members, wrong for staff, and invisible when wrong |

---

## 5. Dependencies

**This domain depends on:** `network` for the library a copy sits in and its city; `membership` for
the caller's entitlement; `identity` for review authorship.

**Domains that depend on this one:** `reservations` for the access verdict and stock;
`store` for the retail price and the same verdict; `recommendations` for reading history.

---

## 6. Known Constraints and Limitations

- Search is relational and `LIKE`-based. No relevance ranking, stemming or typo tolerance.
- Cover images are referenced by URL; there is no upload pipeline in this stage.
- A book belongs to exactly one genre. The prototype offers no multi-genre book.
- Reviews are unmoderated: any member may publish one, and nothing screens the text.
- `Book.AverageRating` is eventually consistent with its reviews, updated after the commit that
  changed them.

---

## 7. Superseded Decisions

| Decision | Superseded by | Reason | Date |
|---|---|---|---|
| `Money` mapped with a value converter | `ComplexProperty` | Ordering the catalogue by price returned 500 in the running system, for the same reason as the ISBN. The mapping test now covers all three value objects | 2026-08-16 |
| `Isbn` and `StarRating` mapped with value converters | Owned types | The converters compiled and passed every unit test, then failed in the running system: catalogue search returned 500 and the seeder crashed the API at startup. Both were found by exercising the endpoints, not by the test suite | 2026-08-15 |
