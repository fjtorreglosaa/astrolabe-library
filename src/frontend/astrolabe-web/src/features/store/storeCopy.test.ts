import {
  FULFILMENT_LABEL,
  FULFILMENT_NOTE,
  PURCHASE_IS_A_NEW_COPY,
  REDEMPTION_FLOOR_NOTE,
  REDEMPTION_NEEDS_MAX_NOTE,
  REDEMPTION_RULE_NOTE,
  pointsAsMoney,
} from './storeCopy';

/**
 * The points wording carries the weight here.
 *
 * Redemption opened in `STR-017`, so the copy no longer apologises for a missing feature — it has to
 * state the two rules that bound it instead. A control a member cannot use still needs a reason, or
 * it reads as a fault.
 */

describe('points wording', () => {
  it('converts point-cents to the money they are worth', () => {
    expect(pointsAsMoney(0)).toBe('$0.00');
    expect(pointsAsMoney(85)).toBe('$0.85');
    expect(pointsAsMoney(3240)).toBe('$32.40');
  });

  it('states the cap and the exchange rate, which are the two things a member needs', () => {
    // BR-STR-007. Without the rate, "100 points" is a number with no meaning attached.
    expect(REDEMPTION_RULE_NOTE).toMatch(/half/i);
    expect(REDEMPTION_RULE_NOTE).toContain('100 points');
    expect(REDEMPTION_RULE_NOTE).toContain('$1.00');
  });

  it('explains a balance too small to spend, rather than leaving a dead control', () => {
    expect(REDEMPTION_FLOOR_NOTE).toContain('100 points');
    expect(REDEMPTION_FLOOR_NOTE.toLowerCase()).toContain('keep');
  });

  it('tells a member below Max that the balance is safe before naming the condition', () => {
    // BR-STR-008. Leading with the condition reads as a penalty for downgrading, and the rule takes
    // nothing away — it suspends spending.
    expect(REDEMPTION_NEEDS_MAX_NOTE).toMatch(/safe/i);
    expect(REDEMPTION_NEEDS_MAX_NOTE.indexOf('safe'))
      .toBeLessThan(REDEMPTION_NEEDS_MAX_NOTE.indexOf('Max'));
  });

  it('never threatens a member with losing points', () => {
    // BR-STR-008: they survive a downgrade, and nothing in the product takes them away. "Never
    // expire" is the reassurance, so the test asserts the promise rather than banning the word.
    const all = `${REDEMPTION_RULE_NOTE} ${REDEMPTION_FLOOR_NOTE} ${REDEMPTION_NEEDS_MAX_NOTE}`;

    expect(all).not.toMatch(/forfeit|lose them|taken away|will expire|points expire/i);
    expect(REDEMPTION_NEEDS_MAX_NOTE).toMatch(/never expire/i);
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
