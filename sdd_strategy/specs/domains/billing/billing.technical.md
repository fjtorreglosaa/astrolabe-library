# Billing — Technical Specification

**Last reviewed:** 2026-08-16
**Reviewed by:** AI Agent — Claude — 2026-08-16
**Version:** 1
**Implements:** `BR-BIL-001` to `BR-BIL-022`

---

## 1. Domain Model

### FinePolicy — the rate and the cap, in one place

```csharp
public static class FinePolicy
{
    public static readonly Money PerDay = Money.FromCents(35);
    public static readonly Money Cap = Money.FromUnits(9);

    public static Money For(int daysLate);   // max(min(days × 35, 900), 0)
    public static int DaysToReachCap { get; } // 26
}
```

A **pure static function**, like `CatalogAccessPolicy`. The rate and the cap are the two numbers in
this domain that a mistake would mis-price for every member at once, so they live in one testable
place with no repository and no clock anywhere near them.

### Fine — aggregate root

```csharp
public sealed class Fine : AggregateRoot
{
    public Guid MemberId { get; private set; }
    public Guid ReservationId { get; private set; }
    public Guid LibraryId { get; private set; }
    public string BookTitle { get; private set; }
    public int DaysLate { get; private set; }
    public Money Amount { get; private set; }
    public FineStatus Status { get; private set; }   // Outstanding, AwaitingValidation, Paid
    public DateTimeOffset AssessedAt { get; private set; }

    public static Result<Fine> Assess(..., int daysLate, ...);
    public Result Reserve(Guid deskPaymentId);   // held for a desk code
    public Result Release();                     // code rejected or expired
    public Result Settle(DateTimeOffset now);
}
```

`Amount` is **frozen at assessment**, not recomputed on read. `reservations` has already recorded the
days late at check-in; a fine that recalculated itself would keep growing after the book was back,
which is exactly what `BR-BIL-003` forbids — and the defect would surface days later, on a real bill.

The title is **copied, not referenced**. A ledger has to be readable years later, and a book removed
from the catalogue must not turn a member's statement into a blank.

### LedgerEntry — append-only

```csharp
public sealed class LedgerEntry : Entity
{
    public Guid MemberId { get; private set; }
    public LedgerEntryKind Kind { get; private set; }  // Charge, Payment, Credit
    public Money Amount { get; private set; }          // signed: charges negative
    public string Description { get; private set; }
    public Guid? FineId { get; private set; }
    public Guid? ReservationId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
}
```

There is no `Update`, no `Delete` and no setter. A mistake is corrected by a **compensating entry**,
which leaves both movements visible — a ledger that can be edited is not a ledger.

The balance is `SUM(amount)`, computed in the database. It is deliberately not a column:
`BR-BIL-011`.

### PaymentMethod — display details only

```csharp
public sealed class PaymentMethod : Entity
{
    public Guid MemberId { get; private set; }
    public CardBrand Brand { get; private set; }
    public string Last4 { get; private set; }        // exactly 4 digits
    public string ExpiryMonthYear { get; private set; }  // "09/28"
    public string CardholderName { get; private set; }
    public bool IsPrimary { get; private set; }
}
```

**There is no field that could hold a card number, and no endpoint that accepts one.** `Last4` is
validated as exactly four digits and rejects anything longer, so a caller sending a full number is
refused rather than silently truncated into storage. These are the details a payment provider returns
after tokenising, which is the only shape this system is willing to know.

### DeskPayment — the code

```csharp
public sealed class DeskPayment : AggregateRoot
{
    public PaymentCode Code { get; private set; }          // "MP-48210"
    public Guid MemberId { get; private set; }
    public Guid LibraryId { get; private set; }
    public Money Amount { get; private set; }
    public DeskPaymentStatus Status { get; private set; }  // Pending, Validated, Rejected, Expired
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }  // IssuedAt + 72 h
    public IReadOnlyList<Guid> FineIds { get; }

    public bool IsExpiredAt(DateTimeOffset now);
    public Result Validate(DateTimeOffset now);
    public Result Reject(string reason, DateTimeOffset now);
}
```

`Expired` is derived at read through `IsExpiredAt` and only written when something acts on it — the
same reasoning as `reservations`' overdue. A job that failed would otherwise leave stale codes
looking valid.

### Enumerations

| Type | Values |
|---|---|
| `FineStatus` | `Outstanding`, `AwaitingValidation`, `Paid` |
| `LedgerEntryKind` | `Charge`, `Payment`, `Credit` |
| `DeskPaymentStatus` | `Pending`, `Validated`, `Rejected`, `Expired` |
| `CardBrand` | `Visa`, `Mastercard`, `Amex`, `Other` |

### Domain events

| Event | Raised when | Consumed by |
|---|---|---|
| `FineAssessed` | A late return is priced | Audit, `notifications` later |
| `FinePaid` | A fine settles, by card or at the desk | Audit |
| `DeskPaymentIssued` / `Validated` / `Rejected` | The desk flow moves | Audit, `notifications` |

---

## 2. Fine accrual — event first, job second

`reservations` raises `ReservationReturned` carrying `DaysLate`. An event handler assesses the fine.

That handler runs **after** the commit and may be lost, which by our own rule bars it from carrying a
business outcome on its own. So the accrual job is not a nicety: it is the guarantee.

```text
ReservationReturned  →  AssessFineOnReturnHandler  →  AssessFineCommand
AssessOutstandingFinesJob (daily)  →  finds returned reservations with no fine  →  same command
```

Both paths call the same command, which is **idempotent by `BR-BIL-010`**: a unique index on
`reservation_id` means the second attempt finds the first fine and stops. The job is the safety net;
the handler is what makes the fine appear immediately.

Per SDD+ §9.1, the job runs with concurrent execution disabled, takes its schedule and batch size
through `IOptions<T>`, and dispatches through `ISender` rather than reaching for repositories itself.

---

## 3. Application Layer

### Commands

| Name | Input | Output | Rule |
|---|---|---|---|
| `AssessFineCommand` | reservationId | `Result<Guid?>` | `BR-BIL-001` to `-003`, `-009`, `-010` |
| `PayFinesCommand` | fineIds, paymentMethodId | `Result<PaymentReceiptDto>` | `BR-BIL-008`, `-014` to `-016` |
| `IssueDeskPaymentCommand` | fineIds | `Result<DeskPaymentDto>` | `BR-BIL-004`, `-017`, `-021` |
| `ValidateDeskPaymentCommand` | code | `Result` | `BR-BIL-005`, `-018`, `-020` |
| `RejectDeskPaymentCommand` | code, reason | `Result` | `BR-BIL-019` |
| `AddPaymentMethodCommand` | brand, last4, expiry, holder | `Result<Guid>` | `BR-BIL-006` |
| `RemovePaymentMethodCommand` | paymentMethodId | `Result` | |

### Queries

| Name | Input | Output | Rule |
|---|---|---|---|
| `GetMyFinesQuery` | — | `Result<FinesSummaryDto>` | `BR-BIL-016` |
| `GetMyLedgerQuery` | paging | `Result<PagedResult<LedgerEntryDto>>` | `BR-BIL-011` |
| `GetMyPaymentMethodsQuery` | — | `Result<IReadOnlyList<PaymentMethodDto>>` | `BR-BIL-006` |
| `GetDeskPaymentsQuery` | status, paging | `Result<PagedResult<DeskPaymentDto>>` | `BR-BIL-005` |

No member-facing query takes a member identifier. `BR-BIL-016` is enforced by the contract rather
than by a check inside it.

---

## 4. Infrastructure

| Concern | Implementation |
|---|---|
| Persistence | `FineRepository`, `LedgerRepository`, `PaymentMethodRepository`, `DeskPaymentRepository` |
| Unit of work | `IBillingUnitOfWork` over all four — a payment writes a fine and a ledger entry in one commit |
| Money | `ComplexProperty`, never a value converter. Balances are summed in the database |
| Idempotency | The fine's own state for payments; unique index on `reservation_id` for fines |
| Accrual | `AssessOutstandingFinesJob`, a `BackgroundService`, `FineAccrualOptions` |

### Persistence notes

- `LedgerEntry` has no update path in the repository contract at all. Append-only is enforced by the
  shape of the interface, not by a convention somebody has to follow.
- `Money` is a complex type everywhere. Balances are `SUM` in SQL, and a value converter would make
  that untranslatable — the defect Stage 2 met three times.
- `Fine` is indexed on `(member_id, status)` and uniquely on `reservation_id`.
- `DeskPayment` is indexed on its code, uniquely, and on `(library_id, status)` for the desk queue.

---

## 5. Architecture Decision Log

| Decision | Choice | Rationale | Alternatives rejected |
|---|---|---|---|
| Fine amount | Frozen at assessment | `reservations` already froze the days at check-in. A fine that recomputed itself would keep growing after the book was back, and the defect would only appear on a real bill days later | Computing on read — rejected: it silently contradicts `BR-BIL-003` |
| Balance | Summed from entries, never stored | A stored balance is a second source of truth, and the day it disagrees with the entries nothing can say which is right | A balance column — rejected on divergence. A cached projection — deferred until it is actually slow, and it must stay rebuildable |
| Ledger mutability | No update, no delete, anywhere in the contract | A ledger that can be edited is not a ledger. Corrections are compensating entries, which leave both movements visible | Soft delete — rejected: it hides a movement that happened |
| Card data | Only brand, last four, expiry, holder. `Last4` rejects anything but four digits | The system must be incapable of holding a card number, not merely disinclined. A caller sending a full number is refused rather than truncated into storage | Storing an encrypted PAN — rejected outright: this product has no reason to hold one, and encryption is not a licence to collect |
| Accrual | Event handler **and** a daily job | A post-commit reaction may be lost, and our own rule bars it from carrying a business outcome alone. The job is the guarantee; the handler is why the fine appears immediately | The handler alone — rejected: a lost event is an unbilled fine. The job alone — rejected: a member would see nothing owed for up to a day |
| Desk code settlement | Only validation settles | Nobody has paid when a code is printed. Settling at issue would let any member clear their debt by generating a code and never going in | Settling at issue — rejected: it is free money |
| Expiry | Derived, written when acted on | The same reasoning as overdue in `reservations`: a job that failed would leave stale codes looking valid | A status-sweeping job as the only source — rejected on the same grounds |
| Payment idempotency | The fine's state, not a key | A reservation takes a copy off a shelf — a new fact each time, which needs a key to deduplicate. A payment settles *named* fines, and settling a settled fine is naturally a no-op. A key would be a second mechanism guarding what is already guarded, and the receipt is made stable by describing every requested fine that is now paid rather than only the ones this call moved | An idempotency key and its table — rejected as machinery for a guarantee the aggregate already gives |
| Fines held by a code | `AwaitingValidation`, a real state | `BR-BIL-021` needs to refuse a card payment for a fine already promised to the desk, or the member pays twice | A boolean flag — rejected: three states exist and two of them are not "paid" |

---

## 6. Dependencies

**This domain depends on:** `reservations` for the days late and the owning library; `identity` for
the member; `network` for the staff scope that guards validation.

**Domains that depend on this one:** `store` in Stage 5, which will write purchases to this ledger,
and `notifications`, which announces what is owed.

---

## 7. Known Constraints and Limitations

- No payment provider. A card payment is recorded, never charged, and the receipt is generated here.
- No refunds, partial payments or instalments.
- USD only, with no currency stored — `BR-GLOBAL-001` fixes the platform to one currency.
- Subscription charges are not routed through this ledger yet; `membership` computes proration and
  records it on the subscription.
- The balance is summed on every read. Correct at any size the MVP will see, and the note above says
  what to do when that stops being true.

---

## 8. Superseded Decisions

| Decision | Superseded by | Reason | Date |
|---|---|---|---|
| — | — | None yet | — |
