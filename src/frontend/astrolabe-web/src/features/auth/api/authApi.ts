import { httpClient, setAccessToken } from '../../../shared/api/httpClient';

/** Roles as the API reports them. A member's role is their plan. */
export type UserRole = 'Basic' | 'Plus' | 'Max' | 'Admin' | 'SuperAdmin';

export interface CurrentUser {
  id: string;
  email: string;
  fullName: string;
  role: UserRole;
  countryId: string | null;
  cityId: string | null;
  isStaff: boolean;
}

interface AccessTokenResponse {
  accessToken: string;
  expiresAt: string;
}

/**
 * Signs in and keeps the access token in memory.
 *
 * The refresh token is never seen here: the API sets it as an HttpOnly cookie, which is the point —
 * script that could read it could steal it.
 */
export const signIn = async (
  email: string,
  password: string,
  deviceId: string,
): Promise<void> => {
  const { data } = await httpClient.post<AccessTokenResponse>('/api/v1/auth/sign-in', {
    email,
    password,
    deviceId,
  });

  setAccessToken(data.accessToken);
};

export const signOut = async (): Promise<void> => {
  try {
    await httpClient.post('/api/v1/auth/sign-out');
  } finally {
    // Cleared even if the call fails: a token the server has already revoked must not linger in
    // memory and make the interface look signed in.
    setAccessToken(null);
  }
};

export const getCurrentUser = async (): Promise<CurrentUser> => {
  const { data } = await httpClient.get<CurrentUser>('/api/v1/auth/me');
  return data;
};

/** The plan a member registers on. Only the three member tiers are selectable. */
export type MemberPlan = Extract<UserRole, 'Basic' | 'Plus' | 'Max'>;

export interface RegisterInput {
  email: string;
  password: string;
  fullName: string;
  countryId: string;
  cityId: string;
  plan: MemberPlan;
}

export const register = async (input: RegisterInput): Promise<void> => {
  await httpClient.post('/api/v1/auth/register', input);
};

export const verifyEmail = async (token: string): Promise<void> => {
  await httpClient.post('/api/v1/auth/verify-email', { token });
};

export const resendVerification = async (email: string): Promise<void> => {
  await httpClient.post('/api/v1/auth/resend-verification', { email });
};

export const forgotPassword = async (email: string): Promise<void> => {
  await httpClient.post('/api/v1/auth/forgot-password', { email });
};
