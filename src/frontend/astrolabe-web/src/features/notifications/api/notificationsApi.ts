import { httpClient } from '../../../shared/api/httpClient';

/** What specifically happened. Finer than the family a member mutes by. */
export type NotificationKind =
  | 'Due'
  | 'Pending'
  | 'Paid'
  | 'Transit'
  | 'Returned'
  | 'Hold'
  | 'Desk'
  | 'Support';

/** What a member mutes. BR-NTF-002 — a family, never a single kind. */
export type NotificationFamily = 'Due' | 'Payments' | 'Returns' | 'Holds' | 'Support';

export interface Notification {
  id: string;
  kind: NotificationKind;
  family: NotificationFamily;
  title: string;
  body: string;
  /** Where to go about it. Null when there is nowhere useful to send the reader. */
  route: string | null;
  occurredAt: string;
  isRead: boolean;
}

export interface NotificationFeed {
  /** Counted server-side over everything, not over this page — BR-NTF-010. */
  unreadCount: number;
  mutedFamilies: NotificationFamily[];
  items: Notification[];
}

export const getMyNotifications = async (limit = 30): Promise<NotificationFeed> => {
  const { data } = await httpClient.get<NotificationFeed>('/api/v1/notifications', {
    params: { limit },
  });
  return data;
};

export const markNotificationRead = async (notificationId: string): Promise<void> => {
  await httpClient.post(`/api/v1/notifications/${notificationId}/read`);
};

export const markAllNotificationsRead = async (): Promise<void> => {
  await httpClient.post('/api/v1/notifications/read');
};

/** Permanent. BR-NTF-008 offers no undo, so neither does this. */
export const clearNotifications = async (): Promise<void> => {
  await httpClient.delete('/api/v1/notifications');
};

export const setNotificationPreference = async (
  family: NotificationFamily,
  muted: boolean,
): Promise<void> => {
  await httpClient.put(`/api/v1/notifications/preferences/${family}`, null, {
    params: { muted },
  });
};
