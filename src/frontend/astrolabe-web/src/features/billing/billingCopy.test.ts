import type { DeskPayment, Fine } from './api/billingApi';
import {
  DESK_CODE_CONFIRM,
  FINE_RULE_NOTE,
  FINE_STATUS_LABEL,
  deskCodeNote,
  fineStatusTone,
  isPayableByCard,
  outstandingNote,
} from './billingCopy';

/**
 * The wording around a desk code is the part that matters most.
 *
 * A member who reads "paid" when they have only printed a code will walk away owing money they think
 * they have settled — and the fine keeps standing. These tests pin the sentences that stop that.
 */

const fine: Fine = {
  id: 'f1',
  bookTitle: 'The Savage Detectives',
  reason: '20 days late',
  daysLate: 20,
  amountCents: 700,
  status: 'Outstanding',
  assessedAt: '2026-08-16T00:00:00+00:00',
  libraryName: 'Midtown',
};

const deskPayment: DeskPayment = {
  id: 'd1',
  code: 'MP-48210',
  memberName: 'Francisco Torreglosa',
  amountCents: 700,
  status: 'Pending',
  isExpired: false,
  libraryName: 'New York — Midtown',
  concept: 'Late fine — The Savage Detectives',
  issuedAt: '2026-08-16T00:00:00+00:00',
  expiresAt: '2026-08-19T00:00:00+00:00',
  rejectionReason: null,
};

describe('the desk code wording', () => {
  it('says plainly that nothing has been charged', () => {
    // The single most important sentence in this domain.
    expect(deskCodeNote(deskPayment)).toContain('Nothing has been charged yet');
    expect(DESK_CODE_CONFIRM).toContain('Nothing is charged now');
  });

  it('names the library the member must go to', () => {
    expect(deskCodeNote(deskPayment)).toContain('New York — Midtown');
  });

  it('states the 72-hour window on both screens', () => {
    expect(deskCodeNote(deskPayment)).toContain('72 hours');
    expect(DESK_CODE_CONFIRM).toContain('72 hours');
  });

  it('never calls an unvalidated code paid', () => {
    for (const text of [deskCodeNote(deskPayment), DESK_CODE_CONFIRM]) {
      expect(text.toLowerCase()).not.toMatch(/\bpaid\b/);
    }
  });
});

describe('fine status', () => {
  it('distinguishes owed, promised and settled', () => {
    expect(FINE_STATUS_LABEL.Outstanding).toBe('Unpaid');
    expect(FINE_STATUS_LABEL.AwaitingValidation).toBe('Waiting at the desk');
    expect(FINE_STATUS_LABEL.Paid).toBe('Paid');
  });

  it('never reads a promised fine as settled', () => {
    expect(FINE_STATUS_LABEL.AwaitingValidation.toLowerCase()).not.toContain('paid');
  });

  it('tones warn while money is still owed', () => {
    expect(fineStatusTone(fine)).toBe('error');
    expect(fineStatusTone({ ...fine, status: 'AwaitingValidation' })).toBe('warning');
    expect(fineStatusTone({ ...fine, status: 'Paid' })).toBe('success');
  });
});

describe('isPayableByCard', () => {
  it('allows only an outstanding fine', () => {
    // BR-BIL-021. Offering the button for a held fine would produce a refusal the member did not
    // earn, because the API correctly rejects it.
    expect(isPayableByCard(fine)).toBe(true);
    expect(isPayableByCard({ ...fine, status: 'AwaitingValidation' })).toBe(false);
    expect(isPayableByCard({ ...fine, status: 'Paid' })).toBe(false);
  });
});

describe('outstandingNote', () => {
  it('reassures rather than showing a bare zero', () => {
    expect(outstandingNote(0, 0)).toBe('Nothing outstanding');
  });

  it('counts titles, singular and plural', () => {
    expect(outstandingNote(700, 1)).toBe('1 unpaid title');
    expect(outstandingNote(1085, 2)).toBe('2 unpaid titles');
  });
});

describe('the rule shown to members', () => {
  it('states the rate and the cap the product actually charges', () => {
    expect(FINE_RULE_NOTE).toContain('$0.35 a day');
    expect(FINE_RULE_NOTE).toContain('$9.00');
  });
});
