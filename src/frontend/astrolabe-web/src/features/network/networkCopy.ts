import type { LibraryObligations } from './api/networkApi';

/**
 * Network administration wording.
 *
 * Withdrawing a branch is the act that needs the most care in words. BR-NET-005 allows it whatever
 * the branch still holds, so the copy has to make clear that "allowed" is not "harmless" — and, just
 * as importantly, that nothing is destroyed.
 */

export const WITHDRAW_TITLE = 'Withdraw this library?';

export const WITHDRAW_BODY = (name: string): string =>
  `“${name}” disappears from the catalogue and stops taking reservations. `
  + 'Loans already out stay returnable and fines stay payable — nothing is lost, and staff can '
  + 'still work the branch down. You will be told what it was still holding.';

/** Turns the report into a sentence. Zero everywhere deserves saying too — it means it is done. */
export const obligationsSummary = (obligations: LibraryObligations): string => {
  if (!obligations.hasAny) {
    return 'It was holding nothing outstanding.';
  }

  const parts: string[] = [];

  if (obligations.copies > 0) {
    parts.push(`${obligations.copies} copies`);
  }

  if (obligations.activeReservations > 0) {
    parts.push(`${obligations.activeReservations} live reservations`);
  }

  if (obligations.unresolvedFines > 0) {
    parts.push(`${obligations.unresolvedFines} unresolved fines`);
  }

  return `It was still holding ${parts.join(', ')}.`;
};
