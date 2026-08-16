import { FULFILMENT_LABEL, FULFILMENT_NOTE, PURCHASE_IS_A_NEW_COPY, REDEMPTION_PENDING_NOTE, pointsAsMoney } from './storeCopy';

/**
 * The points wording carries the weight here.
 *
 * A member can earn points and cannot yet spend them. A balance on a screen with no way to use it
 * and no explanation reads as a fault; saying plainly that the feature is not open, and that nothing
 * is lost, reads as a promise.
 */

describe('points wording', () => {
  it('converts point-cents to the money they are worth', () => {
    expect(pointsAsMoney(0)).toBe('$0.00');
    expect(pointsAsMoney(85)).toBe('$0.85');
    expect(pointsAsMoney(3240)).toBe('$32.40');
  });

  it('says redemption is not open and that nothing is lost', () => {
    expect(REDEMPTION_PENDING_NOTE).toContain('not open yet');
    expect(REDEMPTION_PENDING_NOTE.toLowerCase()).toContain('kept');
  });

  it('never promises a member can spend them today', () => {
    expect(REDEMPTION_PENDING_NOTE).not.toMatch(/spend them now|redeem now|available now/i);
  });
});

describe('fulfilment wording', () => {
  it('matches the prototype', () => {
    expect(FULFILMENT_LABEL.Collection).toBe('Collect at library');
    expect(FULFILMENT_LABEL.Shipping).toBe('Ship to my address');
    expect(FULFILMENT_NOTE.Collection).toContain('free');
    expect(FULFILMENT_NOTE.Shipping).toContain('$3.99');
  });
});

describe('the purchase note', () => {
  it('tells the member a purchase does not take a library copy', () => {
    // BR-STR-013 in the member's own terms. Somebody buying a book they could have borrowed may
    // reasonably wonder whether they just removed it from the shelves.
    expect(PURCHASE_IS_A_NEW_COPY).toContain('does not take it off the library shelves');
  });
});
