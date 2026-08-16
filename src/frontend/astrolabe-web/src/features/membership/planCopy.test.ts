import type { Membership, PlanChangeQuote } from './api/membershipApi';
import { lossSentence, money, pendingChangeLine, planStatusLine, quoteCopy } from './planCopy';

/**
 * The plan copy is transcribed from the prototype, and the prototype has the final word on wording.
 * These tests pin the sentences a member reads before money changes hands, so a refactor cannot
 * quietly reword a disclosure that BR-MBR-020 requires.
 */

const membership: Membership = {
  plan: 'Plus',
  reach: 'City',
  priceCents: 699,
  discountPercent: 10,
  earnsPoints: false,
  seesRecommendations: true,
  cycleStartedOn: '2026-08-16T00:00:00+00:00',
  renewsOn: '2026-09-16T00:00:00+00:00',
  daysRemaining: 28,
  cityId: 'c1',
  cityName: 'New York',
  homeLibraryId: 'l1',
  homeLibraryName: 'Midtown',
  scheduledChange: null,
  canChangeCityThisCycle: true,
};

const upgradeQuote: PlanChangeQuote = {
  from: 'Plus',
  to: 'Max',
  direction: 'upgrade',
  chargeCents: 1173,
  creditCents: 631,
  amountDueCents: 542,
  effectiveOn: '2026-08-16T00:00:00+00:00',
  whatYouLose: [],
};

const downgradeQuote: PlanChangeQuote = {
  from: 'Plus',
  to: 'Basic',
  direction: 'downgrade',
  chargeCents: 0,
  creditCents: 0,
  amountDueCents: 0,
  effectiveOn: '2026-09-16T00:00:00+00:00',
  whatYouLose: ['HomeLibraryAndBasicCatalog', 'Recommendations'],
};

describe('money', () => {
  it('formats cents as the prototype does', () => {
    expect(money(0)).toBe('$0.00');
    expect(money(699)).toBe('$6.99');
    expect(money(1299)).toBe('$12.99');
  });
});

describe('quoteCopy for an upgrade', () => {
  const copy = quoteCopy(upgradeQuote, membership);

  it('is labelled an upgrade', () => {
    expect(copy.kicker).toBe('Upgrade');
    expect(copy.title).toBe('Move up to Max');
  });

  it('states the amount due, never the gross charge, on the call to action', () => {
    // The member pays the difference. Showing the charge here would overstate it by the credit.
    expect(copy.due).toBe('$5.42');
    expect(copy.cta).toBe('Pay $5.42 and upgrade');
    expect(copy.dueLabel).toBe('Due today');
  });

  it('shows the charge and the credit as separate lines', () => {
    expect(copy.rows[0]).toEqual({
      label: 'Max for 28 remaining days',
      value: '$11.73',
    });
    expect(copy.rows[1]).toEqual({
      label: 'Credit for the Plus days you already paid',
      value: '−$6.31',
    });
  });

  it('warns that the charge happens now', () => {
    expect(copy.confirmTitle).toBe('Charge $5.42 now?');
  });
});

describe('quoteCopy for a downgrade', () => {
  const copy = quoteCopy(downgradeQuote, membership);

  it('is labelled a scheduled downgrade', () => {
    expect(copy.kicker).toBe('Scheduled downgrade');
    expect(copy.title).toBe('Move down to Basic');
  });

  it('charges nothing today', () => {
    expect(copy.due).toBe('$0.00');
    expect(copy.dueLabel).toBe('Charged today');
    expect(copy.cta).toBe('Schedule the change');
  });

  it('says the change waits for the renewal date', () => {
    expect(copy.sub).toContain('Downgrades wait for the end of the period you already paid');
    expect(copy.confirmTitle).toBe('Schedule Basic for Sep 16, 2026?');
    expect(copy.confirmBody).toContain('You can cancel the change any time before then.');
  });

  it('says Basic costs nothing after the change', () => {
    expect(copy.after).toContain('you pay nothing');
  });
});

describe('lossSentence', () => {
  it('names the member’s city when borrowing narrows', () => {
    expect(lossSentence('HomeLibraryAndBasicCatalog', membership)).toBe(
      'Borrowing limited to New York — your home library and Basic-catalog titles.',
    );
  });

  it('falls back to a city-free sentence when the city is unknown', () => {
    expect(lossSentence('HomeLibraryAndBasicCatalog', { ...membership, cityName: null })).toBe(
      'Borrowing limited to your home library and Basic-catalog titles.',
    );
  });

  it('states that points stop accruing and stop being redeemable', () => {
    // Both halves matter: a member who thinks accrued points survive would be misled.
    const sentence = lossSentence('RewardPoints', membership);
    expect(sentence).toContain('stop accruing');
    expect(sentence).toContain('cannot be redeemed');
  });
});

describe('planStatusLine', () => {
  it('states the renewal date and price for a paid plan', () => {
    expect(planStatusLine(membership)).toBe(
      'Active until Sep 16, 2026 · renews automatically at $6.99',
    );
  });

  it('says there is nothing to renew on Basic', () => {
    expect(planStatusLine({ ...membership, plan: 'Basic', priceCents: 0 })).toBe(
      'Basic is free, so there is nothing to renew.',
    );
  });
});

describe('pendingChangeLine', () => {
  it('is absent when nothing is scheduled', () => {
    expect(pendingChangeLine(membership)).toBeNull();
  });

  it('states what the member keeps and until when', () => {
    const line = pendingChangeLine({
      ...membership,
      scheduledChange: {
        target: 'Basic',
        effectiveOn: '2026-09-16T00:00:00+00:00',
        requestedAt: '2026-08-16T00:00:00+00:00',
      },
    });

    expect(line).toBe(
      'You keep Plus until Sep 16, 2026. Basic starts that day and nothing is charged before then.',
    );
  });
});
