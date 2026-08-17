/**
 * The events the server pushes, and what each one makes stale.
 *
 * <p>
 * These names mirror `RealtimeEventNames` in the API and must stay in step with it — they are the
 * contract. They are named after what happened in the business rather than after a screen, so the
 * decision about <em>which</em> queries a change invalidates lives here, on the side that knows how
 * the screens are built.
 * </p>
 * <p>
 * Everything below invalidates rather than writes. A push says a thing changed; it never says what
 * the thing now is. Patching the cache from a message would put a second, unauthorized copy of every
 * projection in the browser, and the first time the two disagreed the member would believe the wrong
 * one. Invalidating costs one request and cannot be wrong.
 * </p>
 */

/** A key prefix, matching how the feature queries are keyed. */
export type QueryKeyPrefix = readonly (string | number)[];

export const REALTIME_EVENTS = {
  reservationConfirmed: 'reservation.confirmed',
  returnStarted: 'reservation.return-started',
  reservationReturned: 'reservation.returned',
  fineAssessed: 'billing.fine-assessed',
  finePaid: 'billing.fine-paid',
  deskPaymentIssued: 'billing.desk-payment-issued',
  deskPaymentValidated: 'billing.desk-payment-validated',
  deskPaymentRejected: 'billing.desk-payment-rejected',
  orderPlaced: 'store.order-placed',
  ticketAnswered: 'support.ticket-answered',
  notificationRaised: 'notifications.raised',
  accessRevoked: 'identity.access-revoked',
} as const;

export type RealtimeEventName = (typeof REALTIME_EVENTS)[keyof typeof REALTIME_EVENTS];

/** The payload as it arrives from the hub. */
export interface RealtimeEvent {
  name: string;
  occurredAt: string;
  subjectId: string | null;
}

/**
 * What each event makes stale.
 *
 * <p>
 * Generous on purpose. A reservation moves the loan list, the member's dashboard counters and the
 * copy counts in the catalogue, and listing all three is cheaper than the alternative: a member who
 * reserves a book, sees it in their loans, and then sees the catalogue still offering the copy they
 * just took. Refetching a screen nobody is looking at costs nothing — TanStack Query only refetches
 * queries that are actually mounted.
 * </p>
 */
export const STALE_ON: Record<string, QueryKeyPrefix[]> = {
  [REALTIME_EVENTS.reservationConfirmed]: [
    ['reservations'],
    ['catalog'],
    ['billing', 'ledger'],
  ],
  [REALTIME_EVENTS.returnStarted]: [['reservations']],
  [REALTIME_EVENTS.reservationReturned]: [
    ['reservations'],
    ['catalog'],
    ['billing', 'fines'],
  ],

  [REALTIME_EVENTS.fineAssessed]: [['billing'], ['reservations', 'dashboard']],
  [REALTIME_EVENTS.finePaid]: [['billing']],
  [REALTIME_EVENTS.deskPaymentIssued]: [['billing']],
  [REALTIME_EVENTS.deskPaymentValidated]: [['billing']],
  [REALTIME_EVENTS.deskPaymentRejected]: [['billing']],

  // A purchase settles money and earns points in the same transaction, so all three move together.
  [REALTIME_EVENTS.orderPlaced]: [['store'], ['billing', 'ledger']],

  [REALTIME_EVENTS.ticketAnswered]: [['support']],
  [REALTIME_EVENTS.notificationRaised]: [['notifications']],

  // Nothing to refetch: the session is gone, and every request that follows would answer 401. The
  // provider handles this one itself rather than through this table.
  [REALTIME_EVENTS.accessRevoked]: [],
};
