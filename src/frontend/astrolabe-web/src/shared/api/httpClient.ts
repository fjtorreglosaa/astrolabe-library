import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios';

/**
 * The single Axios instance for the application. HTTP concerns live here, never scattered through
 * components, per GUIDELINES.md sections 35 and 36.
 *
 * The access token is held in memory only — never in localStorage — per GUIDELINES.md section 6.3.
 * The refresh token travels in an HttpOnly cookie the browser attaches automatically, which is why
 * `withCredentials` is on.
 */

const baseURL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080';

let accessToken: string | null = null;

export const setAccessToken = (token: string | null): void => {
  accessToken = token;
};

export const getAccessToken = (): string | null => accessToken;

export const httpClient = axios.create({
  baseURL,
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

httpClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const request = error.config as InternalAxiosRequestConfig & { _retried?: boolean };

    const isUnauthorized = error.response?.status === 401;
    const isRefreshCall = request?.url?.includes('/auth/refresh') ?? false;

    if (!isUnauthorized || isRefreshCall || request?._retried) {
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
