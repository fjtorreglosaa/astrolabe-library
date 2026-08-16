import type { TicketCategory, TicketStatus } from './api/supportApi';

/** Support wording, transcribed from the prototype's ticket screens. */

export const CATEGORY_LABEL: Record<TicketCategory, string> = {
  PaymentsAndFines: 'Payments and fines',
  ReservationsAndReturns: 'Reservations and returns',
  CatalogueAndAvailability: 'Catalogue and availability',
  AccountAndPlan: 'Account and plan',
  SomethingIsBroken: 'Something is broken',
};

export const STATUS_LABEL: Record<TicketStatus, string> = {
  Created: 'Created',
  InReview: 'In review',
  Resolved: 'Resolved',
};

export const STATUS_COLOR: Record<TicketStatus, 'warning' | 'primary' | 'success'> = {
  Created: 'warning',
  InReview: 'primary',
  Resolved: 'success',
};

export const STATUS_ICON: Record<TicketStatus, string> = {
  Created: 'fiber_new',
  InReview: 'hourglass_top',
  Resolved: 'task_alt',
};

export const STATUS_FILTERS: readonly (TicketStatus | 'All')[] = [
  'All',
  'Created',
  'InReview',
  'Resolved',
];

/** Said beside the rating. BR-SUP-007 makes this literally true, so it is worth saying. */
export const RATING_NOTE =
  'If you reopen this ticket the rating is cleared — it is about how it was resolved, and that is open again.';

export const RESOLVED_NOTE =
  'This ticket is resolved and takes no new messages. Reopen it if something is still wrong.';
