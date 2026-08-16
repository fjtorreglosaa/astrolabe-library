# Reservations — Technical Specification

**Last reviewed:** 2026-08-16
**Reviewed by:** AI Agent — Claude — 2026-08-16
**Version:** 1
**Implements:** `BR-RSV-001` to `BR-RSV-024`

---

## 1. Domain Model

### Reservation — aggregate root

```csharp
public sealed class Reservation : AggregateRoot
{
    public Guid MemberId { get; private set; }
    public Guid BookId { get; private set; }
    public Guid BookCopyId { get; private set; }
    public Guid LibraryId { get; private set; }

    public LoanPeriod Period { get; private set; }
    public DeliveryMethod Delivery { get; private set; }
    public Money DeliveryFee { get; private set; }
    public ReservationStatus Status { get; private set; }

    public ReturnMethod? ReturnMethod { get; private set; }
    public DateTimeOffset? HandedOverAt { get; private set; }
    public DateTimeOffset? CheckedInAt { get; private set; }
    public int DaysLateAtCheckIn { get; private set; }

    /// <summary>Deduplicates a retried confirmation. See BR-RSV-008.</summary>
    public string? IdempotencyKey { get; private set; }

    public bool IsActive => Status is Reserved or InTransit;
    public bool IsOverdueAt(DateTimeOffset now);
    public int DaysLateAt(DateTimeOffset now);

    public static Result<Reservation> Confirm(...);
    public Result BeginReturn(ReturnMethod method, string code, DateTimeOffset now);
    public Result CheckIn(DateTimeOffset now);
}
```

`Overdue` is deliberately **absent from the status enumeration**. It is a function of the due date and
the clock, exposed as `IsOverdueAt`. A stored overdue flag needs a job to maintain it, and the day
that job fails every late loan quietly reads as current.

### LoanPeriod — value object

```csharp
public sealed record LoanPeriod(DateTimeOffset StartedOn, DateTimeOffset DueOn)
{
    public const int LoanDays = 14;
    public static LoanPeriod StartingAt(DateTimeOffset start);
    public bool IsOverdueAt(DateTimeOffset now);
    public int DaysLateAt(DateTimeOffset now);
}
```

`DaysLateAt` counts **days started** — `BR-RSV-024` — and floors at zero. A member one hour past the due date is
one day late, which is what the desk tells them, and a member returning early is never negative days
late.

### HandoverCode — value object

```csharp
public sealed record HandoverCode
{
    public string Value { get; }                       // "PU-1234"
    public static HandoverCode For(Guid reservationId);
    public bool Matches(string? typed);                // trims, case-insensitive
}
```

Derived from the reservation identifier exactly as the prototype's `pickupCode` does. `Matches`
trims and upper-cases the input because a courier reads the code aloud and the member types it — but
it never trims the value it is compared against.

### Enumerations

| Type | Values |
|---|---|
| `ReservationStatus` | `Reserved`, `InTransit`, `Returned`, `Cancelled` |
| `DeliveryMethod` | `Collection` (free), `HomeDelivery` ($3.99) |
| `ReturnMethod` | `CourierPickup`, `LibraryDropOff` |

`Cancelled` exists so a confirmation that fails after taking stock has somewhere to go. Nothing in
the MVP reaches it from the interface.

### Domain events

| Event | Raised when | Consumed by |
|---|---|---|
| `ReservationConfirmed` | A copy is taken | Audit, and `notifications` later |
| `ReturnStarted` | The handover code matches | Audit, `notifications` |
| `ReservationReturned` | Staff check the copy in. Carries `DaysLate` | Audit, and **`billing`** in Stage 4 |

`ReservationReturned` carries the days late rather than a fine. Pricing belongs to `billing`, and
carrying the number keeps this domain from needing to know the rate.

---

## 2. Concurrency — the heart of the stage

Two members reaching for the last copy is the one race the product actually has, and `BR-RSV-006`
admits no exception.

**Optimistic concurrency on `BookCopy`.** The row already carries an `xmin` token from Stage 2.
Confirmation reads the copy, calls `Take()`, and commits. If another transaction moved the row in
between, the token no longer matches and the commit fails.

```text
1. Load the book with its copies         (tracked, inside the unit of work)
2. Ask CatalogAccessPolicy               (pure, no I/O)
3. copy.Take()                           (in-memory guard, keeps the count sane)
4. Insert the reservation
5. SaveChangesAsync                      (xmin decides the winner)
6. On DbUpdateConcurrencyException       → translate to a domain conflict
```

The in-memory `Take()` is **not** the protection — the token is. `Take()` only keeps the aggregate
honest for the caller that wins.

`AstrolabeDbContext` already translates `DbUpdateConcurrencyException` into
`ConcurrencyConflictException`, added in Stage 1 for refresh tokens. The handler catches it and
returns a domain error rather than a 500, so the loser is told the copy has just gone rather than
being shown a stack trace.

**The race test is mandatory.** `AC-RSV-002` is exercised by firing simultaneous confirmations at a
copy holding exactly one volume and asserting: exactly one success, exactly one refusal, and a final
count of zero.

**Idempotency.** `ConfirmReservationCommand` accepts a key. A unique index on
`(member_id, idempotency_key)` makes replay a lookup rather than a second copy taken. A retried
network call is the common case; the key turns it from a bug into a no-op.

---

## 3. Application Layer

### Commands

| Name | Input | Output | Rule |
|---|---|---|---|
| `ConfirmReservationCommand` | bookId, libraryId, delivery, idempotencyKey | `Result<ReservationDto>` | `BR-RSV-001` to `-008` |
| `BeginReturnCommand` | reservationId, returnMethod, code | `Result` | `BR-RSV-013` to `-015` |
| `CheckInReservationCommand` | reservationId | `Result` | `BR-RSV-016` to `-020` |

`BeginReturnCommand` is the member's; `CheckInReservationCommand` is staff's. They are separate
commands rather than one with a role branch, because they are different acts by different people with
different authority, and merging them would put the member one boolean away from completing their own
return.

### Queries

| Name | Input | Output | Rule |
|---|---|---|---|
| `GetMyReservationsQuery` | status filter, paging | `Result<PagedResult<ReservationDto>>` | `BR-RSV-021` |
| `GetMyDashboardQuery` | — | `Result<MemberDashboardDto>` | Home screen |
| `GetLibraryReservationsQuery` | libraryId, status, paging | `Result<PagedResult<StaffReservationDto>>` | `BR-RSV-022` |
| `QuoteReservationQuery` | bookId, libraryId, delivery | `Result<ReservationQuoteDto>` | `BR-RSV-003` |

`GetMyReservationsQuery` takes **no member identifier**. `BR-RSV-021` is then enforced by the
signature rather than by a check somebody can forget.

---

## 4. Infrastructure

| Concern | Implementation |
|---|---|
| Persistence | `ReservationRepository` extending `Repository<Reservation>` |
| Unit of work | `IReservationUnitOfWork` exposing `Reservations`, and the book repository it must move stock through |
| EF configuration | `ReservationConfiguration`, `LoanPeriod` owned, money as a complex type |
| Idempotency | Unique filtered index on `(member_id, idempotency_key)` |

### Persistence notes

- `LoanPeriod` is an **owned type** and `Money` a **complex type**, never value converters. Both are
  filtered and ordered on — the reservations table is sorted by due date on every screen that shows
  it — and a converter would make that fail at run time. See GUIDELINES.md §14.1.
- The unit of work exposes `IBookRepository` as well as `IReservationRepository`. Taking a copy and
  recording who took it is one atomic fact, and they must share a change tracker or the count and the
  reservation can disagree.
- Indexed on `(member_id, status)` and on `(library_id, status)` — the two listings the product has.

---

## 5. Architecture Decision Log

| Decision | Choice | Rationale | Alternatives rejected |
|---|---|---|---|
| Overdue | Derived, never stored | A stored flag needs a job, and a failed job makes every late loan read as current. Deriving it means the answer is right when asked | A nightly job setting a flag — rejected: introduces a way to be silently wrong |
| Race resolution | Optimistic concurrency on the copy row | The contention is rare and the transaction short. A pessimistic lock would serialise every reservation in the network to protect against a case that happens a handful of times a day | Pessimistic locking — rejected on throughput. An in-memory guard alone — rejected: it is not a guard at all across two processes |
| Return in two steps | `BeginReturn` by the member, `CheckIn` by staff | A member cannot make a copy appear at the desk by pressing a button. The middle state is the physical truth: the copy is somewhere between them | One-step return — rejected: it lets stock come back before the book does |
| Handover code | Derived from the identifier, not random | It is proof of a handover between two people standing together, not a secret. The courier reads it aloud. A random code would need storing and rotating for no gain in safety | A stored random secret — rejected: cost without benefit, since the physical exchange is the real control |
| Days late on the event | Carried, not priced | `billing` owns the rate and the cap. Carrying the number keeps this domain from knowing either | Raising a fine amount — rejected: two domains owning one rule |
| Idempotency | A key on the command, unique per member | A retried confirmation is the common case on a flaky connection, and without a key it takes a second copy | No key — rejected: `BR-RSV-008` exists precisely because this happens |
| Member queries | No member identifier in the contract | `BR-RSV-021` becomes structural. A parameter would make leaking someone else's loans a one-line mistake | An identifier with a guard — rejected: the guard is the thing that gets forgotten |

---

## 6. Dependencies

**This domain depends on:** `catalog` for the access verdict and the copy; `membership` for the
entitlement `catalog` needs; `network` for the library and its staff scope; `identity` for the member.

**Domains that depend on this one:** `billing` for lateness, `recommendations` for reading history,
`notifications` for due-date reminders, and `store` for nothing at all.

---

## 7. Known Constraints and Limitations

- No renewals, no extensions, no holds queue. The prototype offers none.
- Delivery is simulated: the fee is recorded and no carrier is contacted.
- The handover code is derived, so it is stable for the life of the reservation and cannot be rotated.
- A reservation cannot be cancelled from the interface. `Cancelled` exists for a failed confirmation.
- Overdue reservations accrue nothing here; until Stage 4 exists, lateness is recorded and unpriced.

---

## 8. Superseded Decisions

| Decision | Superseded by | Reason | Date |
|---|---|---|---|
| — | — | None yet | — |
