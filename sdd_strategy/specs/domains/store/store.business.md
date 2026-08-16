# Store — Business Specification

**Last reviewed:** 2026-08-16
**Reviewed by:** AI Agent — Claude — 2026-08-16
**Version:** 1
**Ring:** MVP

> **Unblocked 2026-08-16.** `BR-STR-007` is defined and redemption is built (`GLOBAL-009`,
> `STR-017`). The prototype shows a balance and never implements spending, so the rule could not be
> arbitrated from it and was decided instead — the reasoning is in §8.

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
| `BR-STR-006` | Accrual is one point-cent per **$1.50 of the order total settled in money**, truncated downward. That is the total after the plan discount and after any points applied, and it excludes the delivery fee. A member earns on what they actually spent |
| `BR-STR-008` | Points already earned survive a downgrade, but may only be redeemed while the active plan is Max. The balance is **never forfeited** — a downgrade suspends spending, it does not take anything away |
| `BR-STR-018` | A points balance is the sum of its movements, never a stored number |
| `BR-STR-007` | Points may be applied to a purchase up to **50% of the book total after the plan discount**, excluding delivery. The smallest redemption is **100 point-cents ($1.00)**; one point-cent is one cent. Points are a **tender, not a discount** — the order total is unchanged and the card is asked for the remainder |

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

- **Refunding an order or reversing a redemption.** Points spent are spent; the product has no returns flow for purchased books
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

`BR-STR-006` says *post-discount*, so the two cannot both be right.

**Resolved 2026-08-16 (`GLOBAL-021`): post-discount stands.** It is the normative statement, and
earning on the list price pays the discount twice — the member is credited for money they did not
spend, and the reward grows in proportion to how generous their plan already is, which is backwards.
`AC-STR-006` is 85 point-cents and `PLAN-001`'s acceptance example has been corrected to $0.85.

---

## 9. `BR-STR-007` — how the redemption rule was decided (`GLOBAL-009`, 2026-08-16)

The prototype displays `3,240 pts · redeemable` and lists "Points on every purchase" as a plan
benefit, but implements no spending anywhere. Arbitration therefore had nothing to arbitrate, and the
rule was decided rather than transcribed. Each part, and why:

**One point-cent is one cent.** A rate would be a second place for money arithmetic to drift, and the
prototype's own copy only reads sensibly at parity.

**Capped at 50% of the book total.** At the earning rate of one point-cent per $1.50, a member needs
roughly seventy-five orders' worth of spending to reach this ceiling on a single order — so it will
almost never bind, which is the point. It bounds the pathological case, a long-dormant balance
emptied into one purchase, without touching the ordinary one. 50% is also the conventional loyalty
ceiling, so it will not surprise anyone.

**Measured after the plan discount, before delivery.** After the discount, because the discount is an
entitlement the member already holds and letting points go first would quietly shrink what their plan
is worth. Before delivery, because delivery is a cost passed straight through — points reward buying
books, not choosing a courier. It is the same base `BR-STR-006` earns on, so earning and spending
cannot drift apart.

**A floor of 100 point-cents.** A three-cent redemption costs a movement row, a ledger line and a line
on the receipt to save three cents. $1.00 is the smallest amount that reads as money.

**Points are a tender, not a discount.** The order total stays what the books came to, and the ledger
records the charge in full against two payments — the card and the points. Netting them off the
charge instead would hide from the member's own statement that they had spent a reward at all.

**The part paid with points earns nothing.** Otherwise points regenerate themselves. This is not a new
principle but the one `BR-STR-006` already applies to the discount: a member earns on what they
actually spent.

### What was deliberately *not* changed

`BR-STR-008` already said redemption requires an active Max plan, and it stands. It was tempting to
open spending to every plan on the grounds that earned points are the member's property — but the
rule is not an oversight. A banked balance a lapsed member cannot reach is precisely what brings them
back to Max, and the rule is careful to keep the balance rather than forfeit it. `GLOBAL-009` asked
what the cap should be, not who may redeem, and rewriting the second under cover of the first would
have been a product decision smuggled in as a technical one.
