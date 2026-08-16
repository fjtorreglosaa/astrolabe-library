# Network — Business Specification

**Last reviewed:** 2026-08-15
**Reviewed by:** Francisco Torregrosa
**Version:** 1
**Ring:** MVP

---

## 1. Purpose

Network owns the physical shape of the platform — countries, cities, and libraries — and the
assignment of administrators to libraries. It answers *"where does this happen, and who is allowed to
act there"*.

It is the authority on **scope**: the set of libraries a staff user may act on. Every other domain
asks `network` that question rather than deciding for itself, so the rule exists in exactly one place.

---

## 2. Glossary

| Term | Definition |
|---|---|
| **Country** | A top-level geographic grouping offered at registration |
| **City** | A grouping of libraries. Determines the reach of the Basic and Plus plans |
| **Library** | A physical branch belonging to exactly one city. Holds copies. Called a *branch* in the interface |
| **Assignment** | The link granting an administrator authority over one specific library |
| **Scope** | The set of libraries a staff user may act on. Unrestricted for a super administrator |
| **Invitation** | How an administrator is onboarded. The account is `Invited` until the recipient confirms |
| **Home library** | The single library a Basic member may borrow from. `network` supplies it; `membership` decides what it means |

---

## 3. Business Rules

### Structure

| ID | Rule |
|---|---|
| `BR-NET-001` | A city must belong to exactly one country, and a library to exactly one city |
| `BR-NET-002` | A library must have a name unique within its city |
| `BR-NET-003` | A city must expose exactly one library as the **home library** for members residing in it |
| `BR-NET-004` | A country offered at registration must have at least one city, and a city at least one library. A member must never be able to register into an empty network branch |
| `BR-NET-005` | A library must not be deleted while it holds copies, active reservations, or unresolved fines. It may be **deactivated**, which hides it from members while preserving history |

### Scope and authority

| ID | Rule |
|---|---|
| `BR-NET-006` | An administrator may only act on libraries explicitly assigned to them |
| `BR-NET-007` | A super administrator has unrestricted scope and never requires an assignment |
| `BR-NET-008` | Only a super administrator may create administrators, assign libraries, grant extended powers, and revoke administrators |
| `BR-NET-009` | An administrator may hold assignments to any number of libraries, across any number of cities |
| `BR-NET-010` | An administrator with no assignments must be able to sign in but must see no administrative data |
| `BR-NET-011` | Revoking an assignment must take effect on the **next request**. No cached authorization may survive it |
| `BR-NET-012` | A super administrator must not be able to revoke their own super administrator role, so the network can never be left without one |

### Invitations

| ID | Rule |
|---|---|
| `BR-NET-013` | An invited administrator must appear as `Invited` and must not gain access until they confirm the emailed invitation |
| `BR-NET-014` | An invitation must carry the libraries and role the super administrator selected, applied on confirmation |
| `BR-NET-015` | Resending an invitation must invalidate the previously issued one |
| `BR-NET-016` | Revoking an invited administrator must remove the account entirely; revoking an active one must preserve it and remove only their assignments and role |

### Auditing

| ID | Rule |
|---|---|
| `BR-NET-017` | Creating, assigning, granting, revoking, and deactivating must each write an audit entry recording actor, action, subject, and timestamp |

---

## 4. Acceptance Criteria

| ID | Criterion | Covers |
|---|---|---|
| `AC-NET-001` | The demo administrator operates Midtown and Harlem and receives 403 on every operation against Chicago or Austin | `BR-NET-006` |
| `AC-NET-002` | The demo super administrator succeeds on the same operations across every library | `BR-NET-007` |
| `AC-NET-003` | A member calling any `network` administrative endpoint receives 403 | `BR-NET-008` |
| `AC-NET-004` | Revoking an assignment makes the very next request from that administrator fail for the removed library | `BR-NET-011` |
| `AC-NET-005` | An administrator with no assignments sees empty administrative lists rather than an error | `BR-NET-010` |
| `AC-NET-006` | An invited administrator cannot act until the invitation is confirmed | `BR-NET-013` |
| `AC-NET-007` | The last super administrator cannot remove their own role | `BR-NET-012` |
| `AC-NET-008` | The seed network contains 6 countries, 18 cities and 35 libraries, every city designates exactly one home library, and all six countries are offered at registration | `BR-NET-003`, `BR-NET-004` |

---

## 5. Edge Cases

| Scenario | Expected behaviour |
|---|---|
| An assignment is revoked mid-session | The next request is evaluated against the new scope. The administrator is not signed out; they simply lose access to that library's data |
| An administrator is assigned a library in a city they do not live in | Permitted. Staff scope is unrelated to a member's city of residence |
| An administrator is also a member | Both roles coexist. Their personal reservations follow their plan; their staff powers follow their assignments. The two must never be conflated |
| A library is deactivated while members hold reservations from it | Existing reservations run to completion. The library stops appearing in member-facing search |
| A city's home library is deactivated | Blocked. A city must always expose a home library, so another must be designated first |
| A super administrator revokes the only other administrator of a library | Permitted. A library with no administrator is valid; only the super administrator can act on it until one is assigned |
| An invitation is confirmed after the super administrator who sent it is revoked | The invitation still stands. It carries its own libraries and role, and does not depend on its sender remaining in post |

---

## 6. Out of Scope

Explicitly **not** handled by this domain:

- Authentication of staff users, and the account lifecycle — that belongs to `identity`
- Stock held by a library — that belongs to `catalog`
- Validating desk payments — that belongs to `billing`, which consumes scope from here
- What a plan reaches — that belongs to `membership`. `network` supplies the geography, not the entitlement
- Opening hours, addresses, geolocation, and maps
- Inter-library transfer of copies
- Staffing beyond the administrator role. There is no separate librarian role in this product

---

## 7. Prototype Reference

Screens: `admin-libraries` (*Libraries & admins*), super administrator only, plus the country and
city selectors on `signup`.

### Seed network

`NET-OPEN-001` is **resolved**: the seed grows so that every country offered at registration carries
libraries, satisfying `BR-NET-004` without shortening the registration list.

The United States data is taken verbatim from the prototype and is authoritative. The remaining
fifteen cities are **new seed data**, named after real neighbourhoods to match the prototype's
convention. They are product data, not architecture, and may be renamed freely.

| Country | City | Libraries — home library first |
|---|---|---|
| United States | New York | **Midtown**, Harlem |
| United States | Chicago | **Loop**, Pilsen |
| United States | Austin | **Mueller** |
| Canada | Toronto | **Annex**, Leslieville |
| Canada | Vancouver | **Kitsilano**, Gastown |
| Canada | Montreal | **Plateau**, Verdun |
| United Kingdom | London | **Bloomsbury**, Shoreditch |
| United Kingdom | Manchester | **Ancoats**, Didsbury |
| United Kingdom | Edinburgh | **Newington**, Leith |
| Mexico | Mexico City | **Condesa**, Coyoacan |
| Mexico | Guadalajara | **Chapalita**, Americana |
| Mexico | Monterrey | **San Pedro**, Obispado |
| Colombia | Bogota | **Chapinero**, Usaquen |
| Colombia | Medellin | **Laureles**, El Poblado |
| Colombia | Cali | **Granada**, San Antonio |
| Spain | Madrid | **Chamberi**, Lavapies |
| Spain | Barcelona | **Gracia**, Eixample |
| Spain | Valencia | **Ruzafa**, El Carmen |

Totals: 6 countries, 18 cities, 35 libraries. Every city designates exactly one home library, as
`BR-NET-003` requires.

Read `docs/design/prototype.source.js` for the exact copy and the administrator seed team.
