import type { Membership } from '../membership/api/membershipApi';
import type { CopyAvailability } from './api/catalogApi';
import {
  availabilityLabel,
  bookBadgeLabel,
  copyReasonLabel,
  coverInitials,
  tintFor,
} from './catalogCopy';

/**
 * The access wording is transcribed from the prototype, and the prototype has the final word. These
 * tests pin the sentences a member reads when they are refused a book, so a refactor cannot quietly
 * reword an explanation the rule depends on.
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

const copyInChicago: CopyAvailability = {
  libraryId: 'l2',
  libraryName: 'Loop',
  cityName: 'Chicago',
  availableCount: 2,
  totalCount: 3,
  canReserve: false,
  reason: 'OutsideCity',
};

describe('bookBadgeLabel', () => {
  it('uses the prototype wording for each reason', () => {
    expect(bookBadgeLabel('AllCopiesOut', membership)).toBe('All copies out');
    expect(bookBadgeLabel('NotInBasicPlan', membership)).toBe('Not in Basic plan');
    expect(bookBadgeLabel('HomeLibraryOnly', membership)).toBe('Home library only');
    expect(bookBadgeLabel('Unavailable', membership)).toBe('Unavailable');
  });

  it('names the member’s city when the book is out of reach', () => {
    expect(bookBadgeLabel('NotInCity', membership)).toBe('Not in New York');
  });

  it('falls back to a city-free sentence when the city is unknown', () => {
    // A badge reading "Not in null" would be worse than a vaguer but correct one.
    expect(bookBadgeLabel('NotInCity', undefined)).toBe('Not in your city');
    expect(bookBadgeLabel('NotInCity', { ...membership, cityName: null })).toBe('Not in your city');
  });
});

describe('copyReasonLabel', () => {
  it('names the branch that holds the copy, not the member’s own', () => {
    // The member is in New York; the copy is in Chicago, and the message must say so.
    expect(copyReasonLabel('OutsideCity', copyInChicago, membership)).toBe('Outside Chicago');
  });

  it('names the member’s home library when Basic reach is the obstacle', () => {
    expect(copyReasonLabel('HomeLibraryOnly', copyInChicago, membership)).toBe(
      'Basic borrows at Midtown only',
    );
  });

  it('uses the prototype wording for stock and tier', () => {
    expect(copyReasonLabel('OutOfStock', copyInChicago, membership)).toBe('All copies out');
    expect(copyReasonLabel('NotInBasicCatalog', copyInChicago, membership)).toBe(
      'Not in Basic catalog',
    );
  });
});

describe('availabilityLabel', () => {
  it('distinguishes an empty shelf from a stocked one', () => {
    expect(availabilityLabel(0)).toBe('No copies left');
    expect(availabilityLabel(1)).toBe('1 available');
    expect(availabilityLabel(4)).toBe('4 available');
  });
});

describe('tintFor', () => {
  it('gives the same book the same colour every time', () => {
    // BR-CAT-005. A tint that changed between visits would make a book unrecognisable in a grid.
    expect(tintFor('abc-123')).toBe(tintFor('abc-123'));
  });

  it('spreads different books across the palette', () => {
    const tints = new Set(
      ['a1', 'b2', 'c3', 'd4', 'e5', 'f6', 'g7', 'h8'].map((id) => tintFor(id)),
    );

    expect(tints.size).toBeGreaterThan(1);
  });
});

describe('coverInitials', () => {
  it('takes at most two initials', () => {
    expect(coverInitials('One Hundred Years of Solitude')).toBe('OH');
    expect(coverInitials('Sapiens')).toBe('S');
  });

  it('ignores extra spacing', () => {
    expect(coverInitials('  Klara   and the Sun ')).toBe('KA');
  });
});
