import { QueryClient } from '@tanstack/react-query';

/**
 * Server state lives here, never in Zustand, per GUIDELINES.md sections 33 and 34.
 *
 * Retries are disabled for 4xx: a 401, 403 or 404 is an answer, not a transient fault, and retrying
 * one only delays the error the user needs to see.
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      refetchOnWindowFocus: false,
      retry: (failureCount, error) => {
        const status = (error as { response?: { status?: number } })?.response?.status;
        if (status && status >= 400 && status < 500) {
          return false;
        }
        return failureCount < 2;
      },
    },
    mutations: { retry: false },
  },
});
