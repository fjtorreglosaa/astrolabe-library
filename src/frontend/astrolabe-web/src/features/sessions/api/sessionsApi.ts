import { httpClient } from '../../../shared/api/httpClient';

/**
 * How a session describes the device it runs on.
 *
 * Names, not numbers. The API serialises every enumeration by name through a global converter, and
 * this was typed `number` and compared against `1 | 2 | 3 | 4` until `GLOBAL-011` — so the
 * comparison never matched, every device fell through to the generic icon, and TypeScript could not
 * see it because the declared type was simply untrue.
 */
export type DeviceType = 'Unknown' | 'Web' | 'Mobile' | 'Tablet' | 'Desktop';

export interface Session {
  id: string;
  deviceName: string;
  deviceType: DeviceType;
  ipAddress: string;
  approximateLocation: string | null;
  /** When this device first signed in. An unfamiliar date is how a member spots an intruder. */
  createdAt: string;
  lastSeenAt: string;
  expiresAt: string;
  /** True for the session this browser is using, so the list can mark "this device". */
  isCurrent: boolean;
}

/**
 * Mirrors the API's `RevocationScope`.
 *
 * Sent by name for the same reason `nameof` is used server-side: a numeric literal keeps compiling
 * against a reordered enum and starts revoking the wrong thing, silently and only at run time.
 */
export const RevocationScope = {
  Specified: 'Specified',
  AllOthers: 'AllOthers',
  All: 'All',
} as const;

export type RevocationScopeName = (typeof RevocationScope)[keyof typeof RevocationScope];

export const getMySessions = async (): Promise<Session[]> => {
  const { data } = await httpClient.get<Session[]>('/api/v1/sessions');
  return data;
};

export const revokeSession = async (sessionId: string): Promise<number> => {
  const { data } = await httpClient.delete<{ revoked: number }>(`/api/v1/sessions/${sessionId}`);
  return data.revoked;
};

export const revokeSessions = async (
  scope: RevocationScopeName,
  sessionIds?: string[],
): Promise<number> => {
  const { data } = await httpClient.post<{ revoked: number }>('/api/v1/sessions/revoke', {
    scope,
    sessionIds: sessionIds ?? null,
  });

  return data.revoked;
};
