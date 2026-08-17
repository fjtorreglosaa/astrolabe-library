import type {
  DeliveryMethod,
  Reservation,
  ReservationStatus,
  ReturnMethod,
} from './api/reservationsApi';

/**
 * Every member-facing string about loans, transcribed from the prototype.
 *
 * The status a member reads is not the status the API stores: `Reserved` past its due date shows as
 * "Overdue · N days". That translation lives here, once, because it appears on the loans table, the
 * dashboard card and the return modal.
 */

export const DELIVERY_LABEL: Record<DeliveryMethod, string> = {
  Collection: 'Pick up at library',
  HomeDelivery: 'Home delivery',
};

export const DELIVERY_NOTE: Record<DeliveryMethod, string> = {
  Collection: 'Ready in 2 h · free',
  HomeDelivery: '24–48 h · +$3.99',
};

export const RETURN_LABEL: Record<ReturnMethod, string> = {
  CourierPickup: 'Courier pickup',
  LibraryDropOff: 'Drop off at library',
};

/** The colour a status chip takes, matching the prototype's own palette. */
export type StatusTone = 'success' | 'warning' | 'error' | 'info';

/**
 * What the member reads. `Overdue` is not a stored state, so it is composed here from the flag and
 * the day count the API computed.
 */
export const statusLabel = (reservation: Reservation): string => {
  if (reservation.status === 'Returned') {
    return 'Returned';
  }

  if (reservation.status === 'InTransit') {
    return 'Return in progress';
  }

  if (reservation.status === 'Cancelled') {
    return 'Cancelled';
  }

  return reservation.isOverdue
    ? `Overdue · ${reservation.daysLate} ${reservation.daysLate === 1 ? 'day' : 'days'}`
    : 'Reserved';
};

export const statusTone = (reservation: Reservation): StatusTone => {
  if (reservation.status === 'Returned') {
    return 'success';
  }

  if (reservation.status === 'InTransit') {
    return 'warning';
  }

  return reservation.isOverdue ? 'error' : 'info';
};

/**
 * What the member can do next.
 *
 * A copy already with the courier offers nothing: BR-RSV-015 makes the middle state one the member
 * cannot act on, and a live button there would suggest they could hurry it along.
 */
export const nextAction = (
  reservation: Reservation,
  preferredReturn: ReturnMethod,
): { label: string; enabled: boolean } => {
  switch (reservation.status) {
    case 'Returned':
      return { label: 'Reserve again', enabled: true };
    case 'InTransit':
      return { label: 'With courier', enabled: false };
    case 'Cancelled':
      return { label: 'Cancelled', enabled: false };
    default:
      return {
        label: preferredReturn === 'LibraryDropOff' ? 'Return at library' : 'Return by courier',
        enabled: true,
      };
  }
};

/** The prototype's wording for the handover modal, which differs by who reads the code out. */
export const handoverCopy = (method: ReturnMethod) =>
  method === 'LibraryDropOff'
    ? {
        kicker: 'Library drop-off',
        // Split so the two state names can be emphasised in place, as the prototype does.
        intro: 'Hand the copy to the desk, then type the code the librarian reads out. The reservation moves to ',
        codeLabel: 'Drop-off code',
        confirmLabel: 'Confirm drop-off',
        rejected: 'That drop-off code is not valid. Ask the librarian to read it again.',
      }
    : {
        kicker: 'Courier pickup',
        intro: 'Hand the book to the courier, then type the code they read out. The reservation moves to ',
        codeLabel: 'Pickup code',
        confirmLabel: 'Confirm pickup',
        rejected: 'That pickup code is not valid. Ask the courier to read it again.',
      };

/**
 * The tail of the handover sentence, shared by both methods.
 *
 * <p>
 * Kept apart from `intro` because the prototype sets both state names in bold inside the sentence,
 * and a single string could not carry that without markup in the copy file.
 * </p>
 */
export const HANDOVER_OUTCOME = {
  before: 'Return in progress',
  middle: ' and becomes ',
  after: 'Returned',
  tail: ' when the library checks the parcel in.',
} as const;

/** A reservation is not complete until the library has the copy — the member is told so plainly. */
export const IN_TRANSIT_NOTE =
  'It is on its way back. The library marks it Returned when the copy arrives.';

export const STATUS_FILTERS: readonly { label: string; value: ReservationStatus | 'All' }[] = [
  { label: 'All', value: 'All' },
  { label: 'Reserved', value: 'Reserved' },
  { label: 'Return in progress', value: 'InTransit' },
  { label: 'Returned', value: 'Returned' },
];
