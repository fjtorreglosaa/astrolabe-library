import type { OrderFulfilment } from './api/storeApi';

/**
 * Purchase wording, transcribed from the prototype.
 *
 * Points get careful treatment: a member can earn them and cannot yet spend them, and saying so
 * plainly is better than a balance sitting on a screen with no explanation.
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

/**
 * What the member is told about redeeming. Deliberately explicit that the feature is not open
 * rather than leaving a balance unexplained — the points are safe and that is worth saying.
 */
export const REDEMPTION_PENDING_NOTE =
  'Redeeming points is not open yet. Everything you earn is kept and will be spendable when it is.';

/** A purchase never consumes a library copy — worth saying where a member might assume otherwise. */
export const PURCHASE_IS_A_NEW_COPY =
  'Buying a book does not take it off the library shelves. This is your own copy.';
