# Store — Tasks

**Last reviewed:** 2026-08-16
**Overall progress:** 16/16 (100%), plus one blocked

PLAN-001 Stage 5, less redemption. Depends on Stage 4: an order writes to `billing`'s ledger.

---

## Blocking Dependencies

| Block ID | Description | Status |
|---|---|---|
| `BIL-010` | The ledger must exist before an order can write a charge to it | Resolved 2026-08-16 |
| `BIL-007` | A payment method must exist before an order can be paid | Resolved 2026-08-16 |
| `BLOCK-002` | `BR-STR-007`, the redemption cap, is undefined. **Blocks redemption only** — earning is unaffected | **Open** |

---

## Task List

| ID | Task | Status | Blocker | Tracking | Notes |
|---|---|---|---|---|---|
| `STR-001` | `OrderFulfilment`, `OrderStatus` enumerations | ✅ | — | 2 enumerations | |
| `STR-002` | `PurchaseDiscountPolicy` — the plan table for buying | ✅ | — | `PurchaseDiscountPolicy.cs` | `BR-STR-001` to `-003`, `-009`. Highest-value unit |
| `STR-003` | `StoreErrors` | ✅ | — | `StoreErrors.cs` | Typed, never strings |
| `STR-004` | `OrderLine` with per-line rounding | ✅ | `STR-002` | `OrderLine.cs` | `BR-STR-004` |
| `STR-005` | `Order` aggregate, totals stored | ✅ | `STR-004` | `Order.cs` | `BR-STR-010`, `-011` |
| `STR-006` | Points accrual on the order | ✅ | `STR-005` | `RewardPointsPolicy.cs` | `BR-STR-005`, `-006` |
| `STR-007` | `PointsMovement`, append-only | ✅ | `STR-001` | `PointsMovement.cs` | `BR-STR-018` |
| `STR-008` | `OrderPlaced` event | ✅ | `STR-005` | `OrderPlaced.cs` | |
| `STR-009` | Repository contracts and `IStoreUnitOfWork` | ✅ | `STR-005` | 2 contracts, `StoreUnitOfWork` | No update path on points |
| `STR-010` | EF configuration and migration | ✅ | `STR-009` | `AddStoreDomain` | Complex `Money`. Down-migration verified |
| `STR-011` | `PlaceOrderCommand` writing charge, payment and points in one commit | ✅ | `STR-009` | `PlaceOrderCommandHandler` | `BR-STR-014`, `-015` |
| `STR-012` | `QuoteOrderQuery` | ✅ | `STR-002` | `QuoteOrderQueryHandler` | Same policy as the command |
| `STR-013` | `GetMyOrdersQuery`, `GetMyPointsQuery` | ✅ | `STR-011` | 2 queries | Neither takes a member identifier |
| `STR-014` | `StoreController` | ✅ | `STR-013` | `StoreController` | Thin |
| `STR-015` | Purchase modal on the catalogue | ✅ | `STR-014` | `BuyBookDialog.tsx` | Copy from the prototype |
| `STR-016` | `purchases` screen with the points balance | ✅ | `STR-015` | `PurchasesPage.tsx` | Balance shown, not spendable |
| `STR-017` | ~~Redemption~~ | 🔴 | `BLOCK-002` | — | **Not started. `BR-STR-007` undefined; the prototype implements no redemption** |

### Status values

⬜ Not started · 🔄 In progress · ✅ Done · ❌ Removed · 🔴 Blocked

---

## Test Obligations

| Test | Covers |
|---|---|
| Basic pays the list price | `AC-STR-001` |
| Plus gets 10% in their city and 0% outside it | `AC-STR-002` |
| Max gets 15% wherever the book is held | `AC-STR-003` |
| **Three lines are discounted per line, and the total equals the sum of the lines** | `AC-STR-004` |
| Shipping is added once, not per line | `AC-STR-005` |
| **A Max order of $150 at 15% accrues 85 point-cents** | `AC-STR-006` |
| A Plus member accrues nothing | `AC-STR-007` |
| **Buying leaves every library's stock exactly as it was** | `AC-STR-008` |
| A replayed idempotency key returns the first order and charges once | `AC-STR-009` |
| A member's query returns only their own orders | `AC-STR-010` |
| A draft or removed book cannot be bought | `AC-STR-011` |
| A points balance is the sum of its movements | `AC-STR-012` |
| A discount can never exceed a line's price | `BR-STR-009` |

---

## Completion Log

| Date | Task ID | Completed by | Notes |
|---|---|---|---|
| 2026-08-16 | `STR-001` to `STR-008` | AI Agent — Claude | Domain model. 34 tests, including one proving that rounding per line and rounding on the total differ by a cent |
| 2026-08-16 | `STR-009`, `STR-010` | AI Agent — Claude | `PointsRepository` deliberately does not extend `IRepository<T>` — points are value, and value gets a ledger. Down-migration reverted and reapplied against the running database |
| 2026-08-16 | `STR-011` to `STR-014` | AI Agent — Claude | 13 application tests |
| 2026-08-16 | `STR-002` | AI Agent — Claude | **Defect found by running it.** Books were loaded without their copies, so every book looked unheld and *every discount silently became zero* — a Max member was quoted 0%. The policy was right and its own tests passed; the gap was between the repository and the policy. `GetByIdsWithCopiesAsync` added, and a handler test that fails when the old call is restored |
| 2026-08-16 | `STR-015`, `STR-016` | AI Agent — Claude | Purchase modal and the purchases screen with the points balance. 5 frontend tests |
| 2026-08-16 | `STR-017` | AI Agent — Claude | **Not started, blocked.** `BR-STR-007` is undefined and the prototype implements no redemption |

---

## Progress Summary

| Layer | Tasks | Done |
|---|---|---|
| Domain | `STR-001` to `STR-008` | 8/8 |
| Infrastructure | `STR-009`, `STR-010` | 2/2 |
| Application | `STR-011` to `STR-013` | 3/3 |
| Presentation | `STR-014` | 1/1 |
| Frontend | `STR-015`, `STR-016` | 2/2 |
| Blocked | `STR-017` | 🔴 `BLOCK-002` |
