import type { NotificationFamily, NotificationKind } from './api/notificationsApi';

/**
 * Notification wording and iconography, transcribed from the prototype's `NOTE_KINDS`.
 *
 * The icon is chosen by **kind** and the switch by **family**, which is the same asymmetry the rules
 * describe: a member wants a different picture for a fine and a receipt, and one decision about
 * whether to hear about money at all.
 */

export const KIND_ICON: Record<NotificationKind, string> = {
  Due: 'schedule',
  Pending: 'storefront',
  Paid: 'payments',
  Transit: 'local_shipping',
  Returned: 'assignment_turned_in',
  Hold: 'bookmark_added',
  Desk: 'confirmation_number',
  Support: 'support_agent',
};

/** MUI palette keys. Red for what costs money, teal for what settles it. */
export const KIND_COLOR: Record<NotificationKind, 'error' | 'warning' | 'success' | 'primary'> = {
  Due: 'error',
  Pending: 'warning',
  Paid: 'success',
  Transit: 'primary',
  Returned: 'success',
  Hold: 'primary',
  Desk: 'warning',
  Support: 'primary',
};

export const FAMILY_LABEL: Record<NotificationFamily, string> = {
  Due: 'Due dates and fines',
  Payments: 'Payments and receipts',
  Returns: 'Returns',
  Holds: 'Holds',
  Support: 'Support replies',
};

export const FAMILY_NOTE: Record<NotificationFamily, string> = {
  Due: 'When something is due or a fine is added.',
  Payments: 'Receipts, desk payment codes and settled fines.',
  Returns: 'When a return is on its way and when it arrives.',
  Holds: 'When a copy you asked for becomes available.',
  Support: 'When somebody answers your ticket.',
};

export const EMPTY_TITLE = 'Nothing new yet';
export const EMPTY_BODY = 'Due dates, payments and returns will show up here.';

/** Shown when every family is muted, which is different from having nothing. */
export const ALL_MUTED_TITLE = 'Notifications are off';
export const ALL_MUTED_BODY =
  'Nothing new will reach you until you turn a family back on in Settings.';

/** BR-NTF-008. The one place the interface should be blunt. */
export const CLEAR_CONFIRM_TITLE = 'Clear every notification?';
export const CLEAR_CONFIRM_BODY =
  'They are removed for good — there is no undo. What they were about is unaffected: fines still stand, returns still arrive.';
