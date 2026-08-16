import type { UserAdministrationAction, UserStatus } from './api/usersApi';

/**
 * Directory wording, transcribed from the prototype's user console.
 *
 * The confirmations carry the weight. Every one of these acts on somebody's access, and the
 * prototype is careful to say what survives the act — reservations stay on record, fines remain
 * payable, history is kept. A member who is blocked has not been erased, and the person doing the
 * blocking is entitled to know that before they click.
 */

export const USER_STATUS_LABEL: Record<UserStatus, string> = {
  Active: 'Active',
  PendingVerification: 'Pending verification',
  Blocked: 'Blocked',
  Deleted: 'Deleted',
  Invited: 'Invited',
};

export const USER_STATUS_ICON: Record<UserStatus, string> = {
  Active: 'check_circle',
  PendingVerification: 'mark_email_unread',
  Blocked: 'block',
  Deleted: 'person_off',
  Invited: 'outgoing_mail',
};

/** MUI palette keys. Matches the prototype's green / amber / red / grey. */
export const USER_STATUS_COLOR: Record<UserStatus, 'success' | 'warning' | 'error' | 'default'> = {
  Active: 'success',
  PendingVerification: 'warning',
  Blocked: 'error',
  Deleted: 'default',
  Invited: 'warning',
};

export interface ActionCopy {
  title: string;
  body: string;
  confirmLabel: string;
  destructive: boolean;
}

export const ACTION_COPY: Record<UserAdministrationAction, (name: string) => ActionCopy> = {
  Block: (name) => ({
    title: 'Block this user?',
    body: `“${name}” loses access immediately. Active reservations stay on record and fines remain payable.`,
    confirmLabel: 'Yes, block',
    destructive: true,
  }),
  Unblock: (name) => ({
    title: 'Restore access?',
    body: `“${name}” can sign in and reserve again straight away.`,
    confirmLabel: 'Yes, restore',
    destructive: false,
  }),
  Delete: (name) => ({
    title: 'Delete this account?',
    body: `“${name}” is removed from the directory. Reservation history is kept for the audit log.`,
    confirmLabel: 'Yes, delete',
    destructive: true,
  }),
  Restore: (name) => ({
    title: 'Restore this account?',
    body: `“${name}” returns to the directory as an active user.`,
    confirmLabel: 'Yes, restore',
    destructive: false,
  }),
};

/** The status chips above the table. "All" is the absence of a filter, not a value. */
export const STATUS_FILTERS: readonly (UserStatus | 'All')[] = [
  'All',
  'Active',
  'PendingVerification',
  'Blocked',
  'Deleted',
];

/**
 * An administrator seeing only their own cities may think the directory is broken. Saying so is
 * cheaper than a support ticket — BR-NET-006 and BR-NET-010 in the reader's own terms.
 */
export const SCOPE_NOTE =
  'You see members of the cities your libraries are in. A super administrator sees the whole network.';
