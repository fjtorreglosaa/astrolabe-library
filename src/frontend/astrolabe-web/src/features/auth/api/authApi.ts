import { httpClient, setAccessToken } from '../../../shared/api/httpClient';
import type { PlanTier } from '../../membership/api/membershipApi';

/**
 * Roles as the API reports them.
 *
 * The three member tiers used to live in this union, so a member's role *was* their plan. They no
 * longer do: a role says what someone may do, and `CurrentUser.plan` says what they bought.
 */
export type UserRole = 'Member' | 'Admin' | 'SuperAdmin';

export interface CurrentUser {
  id: string;
  email: string;
  fullName: string;
  role: UserRole;
  /** The member's current plan, or null for staff, who hold none. */
  plan: PlanTier | null;
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

/** The plan a member registers on. Registration never chooses a role. */
export type MemberPlan = PlanTier;

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

/**
 * Sets a new password from an emailed token.
 *
 * The password is sent exactly as typed — never trimmed. A password may legitimately contain spaces,
 * and quietly removing them would lock somebody out of the account they had just recovered.
 */
export const resetPassword = async (token: string, newPassword: string): Promise<void> => {
  await httpClient.post('/api/v1/auth/reset-password', { token, newPassword });
};
