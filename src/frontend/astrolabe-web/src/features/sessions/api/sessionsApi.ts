import { httpClient } from '../../../shared/api/httpClient';

export interface Session {
  id: string;
  deviceName: string;
  deviceType: number;
  ipAddress: string;
  approximateLocation: string | null;
  createdAt: string;
  lastSeenAt: string;
  expiresAt: string;
  /** True for the session this browser is using, so the list can mark "this device". */
  isCurrent: boolean;
}

/** Mirrors the API's RevocationScope. */
export const RevocationScope = {
  Specified: 0,
  AllOthers: 1,
  All: 2,
} as const;

export const getMySessions = async (): Promise<Session[]> => {
  const { data } = await httpClient.get<Session[]>('/api/v1/sessions');
  return data;
};

export const revokeSession = async (sessionId: string): Promise<number> => {
  const { data } = await httpClient.delete<{ revoked: number }>(`/api/v1/sessions/${sessionId}`);
  return data.revoked;
};

export const revokeSessions = async (
  scope: number,
  sessionIds?: string[],
): Promise<number> => {
  const { data } = await httpClient.post<{ revoked: number }>('/api/v1/sessions/revoke', {
    scope,
    sessionIds: sessionIds ?? null,
  });

  return data.revoked;
};
