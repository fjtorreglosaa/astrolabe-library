# Catalog — Tasks

**Last reviewed:** 2026-08-16
**Overall progress:** 22/22 (100%)

> **Growth threshold breached** at 31 business rules. `GLOBAL-018` proposes a `catalog` / `reviews`
> split and recommends deferring it until Stage 6.

---

## Blocking Dependencies

| Block ID | Description | Status |
|---|---|---|
| `MBR-004` | `MemberEntitlement` must exist before the access policy can evaluate anything | Resolved 2026-08-15 |

---

## Task List

| ID | Task | Status | Blocker | Tracking | Notes |
|---|---|---|---|---|---|
| `CAT-001` | `Isbn`, `StarRating` value objects | ✅ | — | `Isbn.cs`, `StarRating.cs` | `BR-CAT-003` |
| `CAT-002` | `BookStatus`, `Genre`, `RepairReason`, `RemovalReason`, rejection enumerations | ✅ | — | 7 enumerations | Reasons are enums, never strings |
| `CAT-003` | `BookCopy` with stock, `Take` and `Return` | ✅ | — | `BookCopy.cs` | `BR-CAT-002`, `-006` |
| `CAT-004` | `Book` aggregate and its metadata invariants | ✅ | `CAT-001` | `Book.cs` | `BR-CAT-001`, `-004` |
| `CAT-005` | Book lifecycle with typed reasons | ✅ | `CAT-004` | `Publish` to `Restore` | `BR-CAT-021` to `-026` |
| `CAT-006` | **`CatalogAccessPolicy` — the pure access rule** | ✅ | `MBR-004` | `CatalogAccessPolicy.cs`, 23 tests | `BR-CAT-006` to `-016`. Highest-value unit in the stage |
| `CAT-007` | `Review` entity with edit and remove | ✅ | `CAT-001` | `Review.cs` | `BR-CAT-027` to `-031` |
| `CAT-008` | Domain events for lifecycle and reviews | ✅ | `CAT-005` | 6 domain events | |
| `CAT-009` | EF configurations for book, copy and review | ✅ | `CAT-004` | 3 configurations, owned VOs | Fluent API only |
| `CAT-010` | Unique index on normalised ISBN | ✅ | `CAT-009` | Unique index on `isbn` | `BR-CAT-003` in the database |
| `CAT-011` | `RowVersion` on `BookCopy` | ✅ | `CAT-009` | `xmin` on `book_copies` | Two members, one last copy |
| `CAT-012` | Migration for the catalog schema | ✅ | `CAT-009` | `AddCatalogDomain` | Verified down-migration |
| `CAT-013` | `IBookRepository`, `IReviewRepository`, `ICatalogUnitOfWork` | ✅ | `CAT-009` | 2 repositories, `CatalogUnitOfWork` | RULE 16, RULE 18 |
| `CAT-014` | `CatalogSeeder`: 12 books across 5 libraries, every branch of the rule reachable | ✅ | `CAT-012` | `CatalogSeeder`, 12 books, 22 holdings | Data from the prototype |
| `CAT-015` | Rating projection kept by the review events | ✅ | `CAT-008` | `RecalculateBookRatingHandler` | Avoids an N+1 on every listing |
| `CAT-016` | `SearchBooksQuery` with filters and paging | ✅ | `CAT-013` | `SearchBooksQuery` | `BR-CAT-017` to `-020` |
| `CAT-017` | `GetBookDetailQuery` with the per-copy verdict | ✅ | `CAT-006` | `GetBookDetailQuery` | `BR-CAT-015` |
| `CAT-018` | Lifecycle commands | ✅ | `CAT-005` | 7 commands | Staff only |
| `CAT-019` | `PublishReviewCommand` and `RemoveReviewCommand` | ✅ | `CAT-007` | `PublishReviewCommand`, `RemoveReviewCommand` | |
| `CAT-020` | `CatalogController` | ✅ | `CAT-017` | `CatalogController`, `AdminCatalogController` | Thin |
| `CAT-021` | `catalog` screen: card and table views, filters, paging, plan-lock badges | ✅ | `CAT-020` | `CatalogPage`, `BookCard`, `BookTable` | Copy from the prototype |
| `CAT-022` | Book detail panel with availability per branch | ✅ | `CAT-021` | `BookDetailDialog` | |

### Status values

⬜ Not started · 🔄 In progress · ✅ Done · ❌ Removed · 🔴 Blocked

---

## Test Obligations

| Test | Covers |
|---|---|
| **The full access matrix**: 3 plans × 3 tiers × in/out of city × in/out of home library × with/without stock | `AC-CAT-009` |
| Basic is refused a `Plus` book with "Not in Basic plan", even with stock at home | `AC-CAT-002` |
| Basic is refused a `Basic` book held only at another branch, with "Home library only" | `AC-CAT-003` |
| Plus is refused a book held only in another city, with "Not in {city}" | `AC-CAT-005` |
| Every plan is refused a book with no stock anywhere, with "All copies out" | `AC-CAT-007` |
| The tier rejection takes precedence over the location rejection | Edge case, §5 |
| A book a member cannot reserve still appears in search and still opens | `AC-CAT-008` |
| Search is case-insensitive and trims the term | `AC-CAT-010` |
| A member's search never returns a draft, repaired or deleted book | `AC-CAT-011` |
| Reviewing the same book twice updates rather than duplicates | `AC-CAT-013` |
| A book with no reviews reports no rating, not zero | `AC-CAT-014` |

---

## Completion Log

| Date | Task ID | Completed by | Notes |
|---|---|---|---|
| 2026-08-15 | `CAT-001` to `CAT-008` | AI Agent — Claude | Domain model and lifecycle. `CatalogAccessPolicy` covered by the full access matrix, 23 tests |
| 2026-08-15 | `CAT-009` to `CAT-014` | AI Agent — Claude | Persistence and the 12-book seed. Down-migration reverted and reapplied against the running database on 2026-08-16 |
| 2026-08-15 | `CAT-015` to `CAT-022` | AI Agent — Claude | Queries, commands, controllers and both member screens |
| 2026-08-16 | `CAT-009` | AI Agent — Claude | **Defect.** `Isbn` and `StarRating` were mapped with value converters. Every unit test passed and catalogue search returned 500 in the running system; the seeder crashed the API at startup. Remapped as owned types, `ValueObjectMappingTests` added as the guard |
| 2026-08-16 | `CAT-016` | AI Agent — Claude | **Reopened.** `BR-CAT-019` requires sorting and none existed. `BookSortKey` and `SortDirection` added through the repository, both queries, both controllers and the table headers. Ordering by price then exposed the same converter defect in `Money`, now a complex type |
| 2026-08-16 | `CAT-005`, `CAT-018` | AI Agent — Claude | **Reopened.** `BR-CAT-025` requires an audit entry per lifecycle transition and none was written. Entries are now staged inside the command handler, in the same transaction, via the new `IAuditUnitOfWork` (`GLOBAL-020`) |
| 2026-08-16 | `CAT-016` to `CAT-019` | AI Agent — Claude | 13 application-layer handler tests added; the layer had none for this domain |

---

## Progress Summary

| Layer | Done | Total |
|---|---|---|
| Domain | 0 | 8 |
| Infrastructure | 0 | 7 |
| Application | 0 | 5 |
| Presentation and frontend | 0 | 2 |
| **Total** | **0** | **22** |
