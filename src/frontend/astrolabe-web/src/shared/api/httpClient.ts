import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { API_BASE_URL } from './apiBaseUrl';

/**
 * The single Axios instance for the application. HTTP concerns live here, never scattered through
 * components, per GUIDELINES.md sections 35 and 36.
 *
 * The access token is held in memory only — never in localStorage — per GUIDELINES.md section 6.3.
 * The refresh token travels in an HttpOnly cookie the browser attaches automatically, which is why
 * `withCredentials` is on.
 */

let accessToken: string | null = null;

export const setAccessToken = (token: string | null): void => {
  accessToken = token;
};

export const getAccessToken = (): string | null => accessToken;

export const httpClient = axios.create({
  baseURL: API_BASE_URL,
  withCredentials: true,
  headers: { 'Content-Type': 'application/json' },
});

httpClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }
  return config;
});

/**
 * Serialises refreshes. Without this, a burst of parallel 401s would fire one refresh each, and
 * refresh token rotation would treat the extra calls as token reuse and revoke the whole session.
 */
let refreshInFlight: Promise<string> | null = null;

const refreshAccessToken = async (): Promise<string> => {
  refreshInFlight ??= httpClient
    .post<{ accessToken: string }>('/api/v1/auth/refresh')
    .then((response) => {
      setAccessToken(response.data.accessToken);
      return response.data.accessToken;
    })
    .finally(() => {
      refreshInFlight = null;
    });

  return refreshInFlight;
};

/**
 * Endpoints that *establish* a session rather than consume one.
 *
 * A 401 from any of these means the credentials were wrong — never that an access token expired — so
 * refreshing and retrying is not just wasteful, it is wrong. Left unlisted, a failed sign-in while
 * an earlier session's cookie is still valid would refresh successfully and leave the application
 * holding a working token for the **previous** user, with the sign-in form showing an error. That is
 * how a "wrong password" turns into signing in as somebody else.
 */
const CREDENTIAL_ENDPOINTS = [
  '/auth/sign-in',
  '/auth/refresh',
  '/auth/register',
  '/auth/forgot-password',
  '/auth/reset-password',
  '/auth/verify-email',
  '/auth/resend-verification',
  // The real path, which lives under network rather than auth. This read '/auth/accept-invitation'
  // until Stage 6 built the screen — a route that never existed, so the entry matched nothing and
  // the protection it claimed was not there. Harmless so far, because the endpoint answers 409, 400
  // and 404 and never 401; corrected rather than deleted, because accepting an invitation does
  // establish credentials and the day it returns a 401 the guard has to be real.
  '/network/admins/accept-invitation',
] as const;

const establishesCredentials = (url: string | undefined): boolean =>
  CREDENTIAL_ENDPOINTS.some((endpoint) => url?.includes(endpoint) ?? false);

httpClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const request = error.config as InternalAxiosRequestConfig & { _retried?: boolean };

    const isUnauthorized = error.response?.status === 401;

    if (!isUnauthorized || establishesCredentials(request?.url) || request?._retried) {
      return Promise.reject(error);
    }

    request._retried = true;

    try {
      const token = await refreshAccessToken();
      request.headers.Authorization = `Bearer ${token}`;
      return await httpClient.request(request);
    } catch {
      setAccessToken(null);
      return Promise.reject(error);
    }
  },
);

/** Shape of an RFC 7807 error body as produced by the API. */
export interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  code?: string;
  correlationId?: string;
}

export const toProblemDetails = (error: unknown): ProblemDetails => {
  if (axios.isAxiosError(error) && error.response?.data) {
    return error.response.data as ProblemDetails;
  }
  return { title: 'Something went wrong.', status: 0 };
};
