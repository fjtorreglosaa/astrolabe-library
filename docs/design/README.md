# Design reference

The approved UI prototype for Astrolabe Books. Per `GUIDELINES.md` §1, **the prototype is the product
authority**: where this repository's documents and the prototype disagree, the prototype prevails.

## Files

| File | What it is |
|---|---|
| `prototype.source.js` | The prototype's application source, decoded from the bundle. Contains all screens, state, seed data, and business rules |
| `prototype.styles.css` | The prototype's stylesheet |
| `prototype.text-outline.txt` | Every visible string in document order. Useful for locating a screen or copy string quickly |

These were extracted from `Astrolabe Books.html` at the repository root, which ships them base64-encoded
and gzipped inside a bundle loader. The files here are the decoded originals, unmodified.

## How to use them

The prototype uses **inline styles, not Material UI**. `GUIDELINES.md` §38 requires Material UI, so the
prototype is a **visual and behavioural reference, not reusable code**. Rebuild each screen on the MUI
theme defined in §38.1.

Read `prototype.source.js` when you need:

- Exact business rules — `copyState` and `bookAccess` hold the plan access logic, and the pricing and
  discount rules sit in the buy modal.
- Exact copy — every label, empty state, error message, and confirmation body.
- Seed data — books, libraries, users, tickets, fines, payments, and the demo accounts.
- Interaction detail — wizard steps, confirmation flows, and discard-guard behaviour.

Do not edit these files. To change the product, update the prototype and re-export.
