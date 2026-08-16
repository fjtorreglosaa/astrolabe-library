import type { DeskPayment, Fine, FineStatus, LedgerEntryKind } from './api/billingApi';

/**
 * Every member-facing string about money, transcribed from the prototype.
 *
 * The wording around a desk code carries the most weight: a member must never read it as "paid".
 * They owe the money until a librarian confirms they took it, and a fine keeps accruing nothing only
 * because the copy is already back.
 */

export const FINE_STATUS_LABEL: Record<FineStatus, string> = {
  Outstanding: 'Unpaid',
  AwaitingValidation: 'Waiting at the desk',
  Paid: 'Paid',
};

export const LEDGER_KIND_LABEL: Record<LedgerEntryKind, string> = {
  Charge: 'Charge',
  Payment: 'Payment',
  Credit: 'Credit',
};

/** The prototype's own explanation of the rule, shown where a member first meets a fine. */
export const FINE_RULE_NOTE =
  'A late return costs $0.35 a day per title, capped at $9.00. Pay by card here, or get a code and pay at the desk.';

/**
 * What a desk code means, in the member's terms. Deliberately blunt about the money not being paid:
 * the fine is still theirs until somebody at a counter says otherwise.
 */
export const deskCodeNote = (payment: DeskPayment): string =>
  `Show this code at ${payment.libraryName} and pay in cash or by card. `
  + 'Nothing has been charged yet — the fine clears once a librarian validates it. '
  + 'The code expires in 72 hours.';

export const DESK_CODE_CONFIRM =
  'Nothing is charged now. Take the code to the desk within 72 hours; a librarian validates the '
  + 'cash or card payment and your fines clear.';

/** A fine promised to a counter cannot also be paid by card — BR-BIL-021. */
export const isPayableByCard = (fine: Fine): boolean => fine.status === 'Outstanding';

export const fineStatusTone = (fine: Fine): 'success' | 'warning' | 'error' => {
  if (fine.status === 'Paid') {
    return 'success';
  }

  return fine.status === 'AwaitingValidation' ? 'warning' : 'error';
};

/** The prototype writes an empty account as reassurance rather than as a zero. */
export const outstandingNote = (outstandingCents: number, count: number): string =>
  outstandingCents === 0
    ? 'Nothing outstanding'
    : `${count} unpaid ${count === 1 ? 'title' : 'titles'}`;

export const DESK_STATUS_FILTERS = [
  { label: 'Awaiting validation', value: 'Pending' },
  { label: 'Validated', value: 'Validated' },
  { label: 'Rejected', value: 'Rejected' },
] as const;
