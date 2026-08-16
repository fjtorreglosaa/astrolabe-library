import type { Reservation } from './api/reservationsApi';
import {
  DELIVERY_LABEL,
  IN_TRANSIT_NOTE,
  handoverCopy,
  nextAction,
  statusLabel,
  statusTone,
} from './reservationCopy';

/**
 * The status a member reads is not the status the API stores.
 *
 * `Reserved` past its due date must read "Overdue · N days", and `InTransit` must never read as
 * finished — the copy is on a van, and telling the member their loan is closed would be a lie that
 * matters once a fine starts accruing.
 */

const base: Reservation = {
  id: 'r1',
  bookId: 'b1',
  title: 'Klara and the Sun',
  author: 'Kazuo Ishiguro',
  coverUrl: null,
  libraryName: 'Midtown',
  cityName: 'New York',
  delivery: 'Collection',
  deliveryFeeCents: 0,
  borrowedOn: '2026-08-16T00:00:00+00:00',
  dueOn: '2026-08-30T00:00:00+00:00',
  status: 'Reserved',
  isOverdue: false,
  daysLate: 0,
  daysRemaining: 14,
  returnMethod: null,
  handedOverAt: null,
  checkedInAt: null,
};

describe('statusLabel', () => {
  it('reads Reserved while the loan is current', () => {
    expect(statusLabel(base)).toBe('Reserved');
  });

  it('composes the overdue label from the flag and the day count', () => {
    // Overdue is not a stored state, so the label is assembled rather than looked up.
    expect(statusLabel({ ...base, isOverdue: true, daysLate: 3 })).toBe('Overdue · 3 days');
  });

  it('says one day, not one days', () => {
    expect(statusLabel({ ...base, isOverdue: true, daysLate: 1 })).toBe('Overdue · 1 day');
  });

  it('never calls a copy in transit returned', () => {
    // The library has not received it. Saying otherwise would tell a member with an accruing fine
    // that they are finished.
    expect(statusLabel({ ...base, status: 'InTransit' })).toBe('Return in progress');
  });

  it('reads Returned only once the library has checked it in', () => {
    expect(statusLabel({ ...base, status: 'Returned' })).toBe('Returned');
  });

  it('ignores a stale overdue flag on a returned loan', () => {
    // The lateness that matters is the one frozen at check-in; the row must not keep shouting.
    expect(statusLabel({ ...base, status: 'Returned', isOverdue: true, daysLate: 9 }))
      .toBe('Returned');
  });
});

describe('statusTone', () => {
  it('warns on overdue and settles on returned', () => {
    expect(statusTone({ ...base, isOverdue: true, daysLate: 2 })).toBe('error');
    expect(statusTone({ ...base, status: 'InTransit' })).toBe('warning');
    expect(statusTone({ ...base, status: 'Returned' })).toBe('success');
    expect(statusTone(base)).toBe('info');
  });
});

describe('nextAction', () => {
  it('offers the return the member prefers', () => {
    expect(nextAction(base, 'CourierPickup')).toEqual({
      label: 'Return by courier',
      enabled: true,
    });
    expect(nextAction(base, 'LibraryDropOff')).toEqual({
      label: 'Return at library',
      enabled: true,
    });
  });

  it('offers nothing while the copy is with the courier', () => {
    // BR-RSV-015. A live button here would suggest the member could hurry the library along.
    expect(nextAction({ ...base, status: 'InTransit' }, 'CourierPickup')).toEqual({
      label: 'With courier',
      enabled: false,
    });
  });

  it('offers to borrow it again once it is back', () => {
    expect(nextAction({ ...base, status: 'Returned' }, 'CourierPickup').label)
      .toBe('Reserve again');
  });
});

describe('handoverCopy', () => {
  it('names whoever reads the code out', () => {
    expect(handoverCopy('CourierPickup').codeLabel).toBe('Pickup code');
    expect(handoverCopy('CourierPickup').rejected).toContain('courier');

    expect(handoverCopy('LibraryDropOff').codeLabel).toBe('Drop-off code');
    expect(handoverCopy('LibraryDropOff').rejected).toContain('librarian');
  });

  it('tells the member the reservation is not finished by the handover', () => {
    for (const method of ['CourierPickup', 'LibraryDropOff'] as const) {
      expect(handoverCopy(method).intro).toContain('Return in progress');
    }
  });
});

describe('delivery wording', () => {
  it('matches the prototype', () => {
    expect(DELIVERY_LABEL.Collection).toBe('Pick up at library');
    expect(DELIVERY_LABEL.HomeDelivery).toBe('Home delivery');
  });

  it('explains that the library, not the member, closes the loan', () => {
    expect(IN_TRANSIT_NOTE).toContain('The library marks it Returned');
  });
});
