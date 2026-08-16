# Store — Technical Specification

**Last reviewed:** 2026-08-16
**Reviewed by:** AI Agent — Claude — 2026-08-16
**Version:** 1
**Implements:** `BR-STR-001` to `BR-STR-006` and `BR-STR-008` to `BR-STR-018`

> `BR-STR-007` is undefined (`BLOCK-002`). Redemption is not implemented and nothing here describes
> one.

---

## 1. Domain Model

### PurchaseDiscountPolicy — the plan table for buying

```csharp
public static class PurchaseDiscountPolicy
{
    public static int PercentFor(MemberEntitlement member, IReadOnlyList<CopyLocation> copies);
    public static Money DiscountOn(Money linePrice, int percent);
}
```

A **pure static function**, like `CatalogAccessPolicy` and `FinePolicy`. It takes the entitlement and
where the book is held, and answers a percentage — no repository, no clock.

It is separate from the entitlement's own `DiscountPercent` on purpose. `membership` states what a
plan is *worth*; this decides what it is worth **for this book**, because `BR-STR-002` makes a Plus
member's discount depend on where the copies are. Folding the reach test into `membership` would put
a catalogue concept inside the plan table.

### Order — aggregate root

```csharp
public sealed class Order : AggregateRoot
{
    public Guid MemberId { get; private set; }
    public OrderFulfilment Fulfilment { get; private set; }
    public Money ShippingFee { get; private set; }
    public Money Subtotal { get; private set; }     // sum of line prices
    public Money DiscountTotal { get; private set; }
    public Money Total { get; private set; }
    public int PointsEarned { get; private set; }   // point-cents
    public string? IdempotencyKey { get; private set; }
    public IReadOnlyList<OrderLine> Lines { get; }
}
```

Every total is **stored, not recomputed**. An order is a receipt: what it says was charged must stay
what was charged, whatever a plan or a price does afterwards. The same reasoning as a frozen fine.

### OrderLine

```csharp
public sealed class OrderLine : Entity
{
    public Guid BookId { get; private set; }
    public string BookTitle { get; private set; }   // copied, not referenced
    public Money UnitPrice { get; private set; }
    public int DiscountPercent { get; private set; }
    public Money DiscountAmount { get; private set; }
    public Money LineTotal { get; private set; }
}
```

The discount is computed and rounded **here**, per line — `BR-STR-004`. Rounding a percentage once on
a total and once per line disagree by a cent often enough that a receipt stops adding up, and a
receipt that does not add up is the kind of defect a member reports and nobody can reproduce.

The title is copied for the same reason a fine copies it: an order has to be readable after the
catalogue moves on.

### PointsMovement — append-only

```csharp
public sealed class PointsMovement : Entity
{
    public Guid MemberId { get; private set; }
    public int PointCents { get; private set; }   // signed
    public string Description { get; private set; }
    public Guid? OrderId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
}
```

A balance is `SUM(point_cents)`, exactly as a money balance is. `BR-STR-018`. Points are value, so
they get the same treatment as money: movements, not a mutable number.

Only `Earned` movements exist today. The signed field is what lets redemption be added later without
changing the shape of anything.

### Enumerations

| Type | Values |
|---|---|
| `OrderFulfilment` | `Collection` (free), `Shipping` ($3.99) |
| `OrderStatus` | `Paid` — the only state an order reaches in this stage |

### Domain events

| Event | Raised when | Consumed by |
|---|---|---|
| `OrderPlaced` | An order is paid. Carries the total and the points earned | Audit, and `notifications` later |

---

## 2. Where the money goes

Store prices; `billing` records. An order writes **one charge** to the ledger and settles it with a
**payment** entry in the same commit, because a purchase is paid at the moment it is placed — unlike
a fine, which is charged first and paid later.

```text
PlaceOrderCommand
  → price the lines            (PurchaseDiscountPolicy, pure)
  → charge the card            (a stored method; no provider is called)
  → LedgerEntry.Charge         (billing)
  → LedgerEntry.Payment        (billing)
  → PointsMovement.Earned      (store, Max only)
  → one SaveChangesAsync
```

The two units of work share a `DbContext`, so a single commit covers the order, both ledger entries
and the points. An order that existed without its ledger entry would be a purchase the member's
statement denies.

---

## 3. Application Layer

### Commands

| Name | Input | Output | Rule |
|---|---|---|---|
| `PlaceOrderCommand` | lines, fulfilment, paymentMethodId, idempotencyKey | `Result<OrderDto>` | `BR-STR-001` to `-017` |

### Queries

| Name | Input | Output | Rule |
|---|---|---|---|
| `QuoteOrderQuery` | bookIds, fulfilment | `Result<OrderQuoteDto>` | `BR-STR-004`, `-009`, `-010` |
| `GetMyOrdersQuery` | paging | `Result<PagedResult<OrderDto>>` | `BR-STR-016` |
| `GetMyPointsQuery` | — | `Result<PointsSummaryDto>` | `BR-STR-018` |

`QuoteOrderQuery` exists so the modal shows the discount and the total before anything is charged,
priced by the same policy the command uses. Computing it in the frontend would put money arithmetic
in two languages.

---

## 4. Infrastructure

| Concern | Implementation |
|---|---|
| Persistence | `OrderRepository`, `PointsRepository` |
| Unit of work | `IStoreUnitOfWork` exposing `Orders`, `Points`, and the `Books` it prices from |
| Money | `ComplexProperty` throughout. Balances and totals are summed in SQL |
| Idempotency | Unique filtered index on `(member_id, idempotency_key)` |

`PointsRepository` offers append and read only, and does not extend `IRepository<T>` — the same
reasoning as the ledger, and for the same reason: points are value.

---

## 5. Architecture Decision Log

| Decision | Choice | Rationale | Alternatives rejected |
|---|---|---|---|
| Discount location | A store policy, not `membership`'s `DiscountPercent` | `BR-STR-002` makes a Plus member's discount depend on **where the book is held**, which is a catalogue fact. Putting the reach test in `membership` would drag the catalogue into the plan table | Reading `entitlement.DiscountPercent` directly — rejected: it is right for Max and Basic and silently wrong for Plus |
| Rounding | Per line, then summed | `BR-STR-004`. Rounding once on a total disagrees with the sum of the lines often enough that receipts stop adding up, and that defect is unreproducible from a bug report | Rounding the order total — rejected on the receipt not adding up |
| Order totals | Stored, never recomputed | An order is a receipt. What it says was charged must stay what was charged, whatever prices or plans do next | Recomputing on read — rejected: the same class of defect as a fine that keeps growing |
| Points | Movements summed, never a column | Points are value, and value gets a ledger. It also makes redemption additive later rather than a schema change | A balance column — rejected on the same grounds as a money balance |
| Charge and payment | Both written at placement | A purchase is paid when it is placed. Writing only a charge would leave every member's statement permanently in debit | A charge alone — rejected: the balance would be wrong by the value of every purchase ever made |
| Stock | Untouched | `BR-STR-013`. A sale is a new copy; the library's shelves are `reservations`' alone | Decrementing a copy — rejected: it would make buying a book remove it from lending |
| Redemption | Not built | `BR-STR-007` is undefined and `BLOCK-002` is open. The prototype implements no redemption, so there is nothing to transcribe and nothing to arbitrate | Inventing a 50% cap — rejected: it is a business decision nobody has taken |

---

## 6. Dependencies

**This domain depends on:** `catalog` for the book, its price and where its copies are; `membership`
for the entitlement; `billing` for the ledger and the payment method; `network` for the geography.

**Domains that depend on this one:** `recommendations` may read purchase history later. Nothing does
today.

---

## 7. Known Constraints and Limitations

- **Redemption does not exist.** Points accrue and cannot yet be spent.
- No basket. An order is created and paid in one request, as the prototype's modal does.
- No provider is called; the card is recorded, not charged.
- No shipping integration, tracking or returns.
- An order cannot be cancelled or refunded. `OrderStatus` has one member for that reason.

---

## 8. Superseded Decisions

| Decision | Superseded by | Reason | Date |
|---|---|---|---|
| — | — | None yet | — |
