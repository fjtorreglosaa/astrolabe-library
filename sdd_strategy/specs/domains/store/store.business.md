# Store — Business Specification

**Last reviewed:** 2026-08-16
**Reviewed by:** AI Agent — Claude — 2026-08-16
**Version:** 1
**Ring:** MVP

> **Partially blocked.** `BR-STR-007` — the reward point redemption cap — is undefined, and the
> prototype shows a balance without ever implementing redemption, so arbitration cannot settle it.
> `BLOCK-002` and `GLOBAL-009` are open. **Earning points is specified and built; spending them is
> not.** Nothing in this file invents a redemption flow.

---

## 1. Purpose

Store owns buying a book, as opposed to borrowing one.

It answers *"what does this book cost this member, and did they pay for it"*.

A purchase is a **sale of a new copy**, not a transfer of the library's. Buying never touches the
shelves: `reservations` is the only domain that moves stock, and it stays that way.

The money it produces belongs to `billing`. This domain prices an order and hands the movement to
the ledger; it holds no balance of its own.

---

## 2. Glossary

| Term | Definition |
|---|---|
| **Order** | One purchase by one member, made of lines |
| **Order line** | One book at one price, with its own discount |
| **Reach discount** | The percentage a plan earns, which depends on where the book is held |
| **Fulfilment** | How the book reaches the buyer: collection at a library, or shipping |
| **Reward points** | Value a Max member accrues on what they actually pay. Held in point-cents |
| **Point-cent** | One cent of redeemable value. Points are money, so they are integers like all money |

> **A discount is earned per line, never on the total.** Rounding a percentage once on a total and
> rounding it per line give different answers, and the member's receipt has to add up.

> **Buying does not lend.** A purchased book is a new copy. The library's stock is untouched.

---

## 3. Business Rules

### Pricing

| ID | Rule |
|---|---|
| `BR-STR-001` | A **Basic** member receives no purchase discount |
| `BR-STR-002` | A **Plus** member receives 10% on a book held by a library in their city of residence, and nothing on a book held only elsewhere |
| `BR-STR-003` | A **Max** member receives 15% on a book held by any library on the platform |
| `BR-STR-004` | The discount is applied **per order line**, never to the order total |
| `BR-STR-009` | A discount is rounded to the nearest cent per line, and can never exceed the line's price |
| `BR-STR-010` | Shipping adds $3.99 to the order once, however many lines it has. Collection is free |
| `BR-STR-011` | Every amount is an integer number of cents |

### Buying

| ID | Rule |
|---|---|
| `BR-STR-012` | A member may buy any book in the catalogue. Reach decides the **discount**, never the right to buy |
| `BR-STR-013` | A purchase does not change any library's stock |
| `BR-STR-014` | A purchase is paid by a card the member has on file, and writes one charge to the ledger per order |
| `BR-STR-015` | A repeated purchase carrying the same idempotency key returns the original order and never charges twice |
| `BR-STR-016` | A member sees only their own orders, and no endpoint accepts another member's identifier |
| `BR-STR-017` | A book that is not in the catalogue cannot be bought |

### Reward points

| ID | Rule |
|---|---|
| `BR-STR-005` | Only **Max** members accrue reward points |
| `BR-STR-006` | Accrual is one point-cent per **$1.50 of the post-discount order total**, truncated downward |
| `BR-STR-008` | Points already earned survive a downgrade, but may only be redeemed while the active plan is Max |
| `BR-STR-018` | A points balance is the sum of its movements, never a stored number |
| `BR-STR-007` | **UNDEFINED — `BLOCK-002`.** The redemption cap. Not implemented, and no redemption flow exists |

---

## 4. Acceptance Criteria

| ID | Criterion | Covers |
|---|---|---|
| `AC-STR-001` | A Basic member pays the list price | `BR-STR-001` |
| `AC-STR-002` | A Plus member gets 10% on a book held in their city, and 0% on one held only elsewhere | `BR-STR-002` |
| `AC-STR-003` | A Max member gets 15% wherever the book is held | `BR-STR-003` |
| `AC-STR-004` | Three lines of $9.99 at 15% are discounted line by line, and the total equals the sum of the lines | `BR-STR-004`, `BR-STR-009` |
| `AC-STR-005` | Shipping is added once to a three-line order | `BR-STR-010` |
| `AC-STR-006` | **A Max member spending $150 on books from another city receives 15% off and accrues 85 point-cents** | `BR-STR-003`, `BR-STR-006` |
| `AC-STR-007` | A Plus member accrues nothing | `BR-STR-005` |
| `AC-STR-008` | Buying leaves every library's stock exactly as it was | `BR-STR-013` |
| `AC-STR-009` | A replayed idempotency key returns the first order and charges once | `BR-STR-015` |
| `AC-STR-010` | A member requesting another member's orders receives their own | `BR-STR-016` |
| `AC-STR-011` | A draft or removed book cannot be bought | `BR-STR-017` |
| `AC-STR-012` | A points balance is the sum of its movements | `BR-STR-018` |

---

## 5. Edge Cases

| Scenario | Expected behaviour |
|---|---|
| A Plus member buys a book held in two cities, one of them theirs | 10%. Held *by a library in their city* is satisfied by one copy |
| A book with no copies anywhere | Buyable. Stock is about lending, and `BR-STR-013` means a sale never consumed one |
| A Max member downgrades with points banked | The points remain. Redeeming them is Max-only, and redemption does not exist yet |
| An order whose discount would exceed the price | Impossible by `BR-STR-009`, which clamps per line |
| A purchase while the member owes fines | Allowed. Nothing in the product blocks buying on an unpaid fine, and inventing that would be a policy nobody asked for |
| A book removed from the catalogue after an order | The order stands, and the title on it stays readable — the line copies the title as a fine does |
| The card is removed after a purchase | The order and its ledger entry stand. A payment method is not a receipt |

---

## 6. Out of Scope

Explicitly **not** handled by this domain:

- **Redeeming points.** `BR-STR-007` is undefined and `BLOCK-002` is open. No cap, no flow, no screen
- Taking real money — `billing` records payments and no provider is integrated
- Holding a balance. Money lives in `billing`'s ledger; this domain writes to it
- Stock of any kind. A sale is a new copy, and lending stock is `reservations`'
- Shipping, tracking, carriers and returns of purchased books
- A basket that survives between sessions. An order is created and paid in one act

---

## 7. Prototype Reference

Screens: the purchase modal opened from the catalogue — price, plan discount, fulfilment choice, fee,
total and card — and `purchases`, the member's order history.

The pricing is implemented in `prototype.source.js` as the `buy` block, and it is the authority:

```js
const discount = plan==='Max' ? 0.15 : plan==='Plus' ? 0.10 : 0;
const total = price - off + fee;
```

The prototype **never implements reward points**. It displays `3,240 pts · redeemable` as a profile
statistic and lists "Points on every purchase" as a plan benefit. That is why `BR-STR-007` cannot be
arbitrated from it.

---

## 8. Open Questions

**`BR-STR-007` — the redemption cap. `BLOCK-002`, `GLOBAL-009`.** A cap of 50% of the order total is
on the table and nothing has approved it. Redemption is therefore not built. Earning is, so no value
is lost while the question is open — the balance simply accumulates.

**The plan's acceptance example disagrees with `BR-STR-006`.** PLAN-001 states: *"A Max member
spending $150 on books from another city receives a 15% discount and accrues $1.00 of redeemable
balance."*

- $150 pre-discount ÷ $1.50 = **100 point-cents = $1.00** — matches the plan's figure
- $150 less 15% = $127.50 ÷ $1.50 = **85 point-cents = $0.85** — matches `BR-STR-006` as written

`BR-STR-006` says *post-discount*, so the two cannot both be right. This specification follows the
**rule**, because it is the normative statement and because earning on what the customer actually
paid is the ordinary practice — earning on the list price effectively pays the discount twice.
`AC-STR-006` is therefore written as 85 point-cents, and the plan's example is recorded here as an
arithmetic slip rather than silently ignored. **Awaiting confirmation:** reply "post-discount" to
keep this, or "pre-discount" to change the rule and the code.
