import type { OrderFulfilment } from './api/storeApi';

/**
 * Purchase wording, transcribed from the prototype.
 *
 * Points get careful treatment. Earning is a Max benefit; spending what you already earned is open
 * to every plan, because a balance that survives a downgrade and can never be spent has not
 * survived in any sense the member would recognise.
 */

export const FULFILMENT_LABEL: Record<OrderFulfilment, string> = {
  Collection: 'Collect at library',
  Shipping: 'Ship to my address',
};

export const FULFILMENT_NOTE: Record<OrderFulfilment, string> = {
  Collection: 'Ready in 2 h · free',
  Shipping: '3–5 days · +$3.99',
};

/** Point-cents are money. One hundred of them is a dollar off a future book. */
export const pointsAsMoney = (pointCents: number): string => `$${(pointCents / 100).toFixed(2)}`;

/** The rule in one line, for the purchase modal. BR-STR-007. */
export const REDEMPTION_RULE_NOTE =
  'Points cover up to half a purchase. 100 points is $1.00.';

/** Shown against a balance too small to spend yet. */
export const REDEMPTION_FLOOR_NOTE =
  'You need 100 points before you can spend them. Yours keep until then.';

/**
 * Shown to a member holding points on a plan below Max. BR-STR-008.
 *
 * Two facts, and the order matters: the balance is safe first, the condition second. Leading with
 * the condition reads as a penalty for downgrading, which is not what the rule does — nothing is
 * ever taken away.
 */
export const REDEMPTION_NEEDS_MAX_NOTE =
  'Your points are safe and never expire. Spending them needs the Max plan.';

/** A purchase never consumes a library copy — worth saying where a member might assume otherwise. */
export const PURCHASE_IS_A_NEW_COPY =
  'Buying a book does not take it off the library shelves. This is your own copy.';
