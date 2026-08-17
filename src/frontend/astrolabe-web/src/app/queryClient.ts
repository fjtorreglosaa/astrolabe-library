import { MutationCache, QueryClient } from '@tanstack/react-query';
import { toProblemDetails } from '../shared/api/httpClient';
import { useSnackbarStore } from '../shared/feedback/snackbarStore';

/**
 * What a mutation may declare about how it reports itself.
 *
 * <p>
 * Declared through `meta` rather than through a callback per mutation, so the wording sits next to
 * the action it describes while the mechanism stays in one place.
 * </p>
 */
declare module '@tanstack/react-query' {
  interface Register {
    mutationMeta: {
      /** Shown on success. Omit for an action whose result is already visible on the screen. */
      success?: string;
      /** Replaces the server's message on failure. Omit to show what the server said. */
      failure?: string;
      /** Set for a mutation that reports its own outcome inline, in its own form. */
      silent?: boolean;
    };
  }
}

/**
 * Server state lives here, never in Zustand, per GUIDELINES.md sections 33 and 34.
 *
 * <p>
 * Retries are disabled for 4xx: a 401, 403 or 404 is an answer, not a transient fault, and retrying
 * one only delays the error the user needs to see.
 * </p>
 * <p>
 * <b>Every failed mutation reports itself.</b> That is handled here, on the cache, rather than in
 * sixty-odd `onError` callbacks — the failure mode of the per-callback version is the one that
 * matters: an action that quietly does nothing, because the one place somebody forgot to write the
 * handler is by definition the place nobody was thinking about. A mutation that shows its own error
 * inline opts out with `meta: { silent: true }`.
 * </p>
 */
export const queryClient = new QueryClient({
  mutationCache: new MutationCache({
    onSuccess: (_data, _variables, _context, mutation) => {
      const message = mutation.meta?.success;

      if (message) {
        useSnackbarStore.getState().push({ title: message, tone: 'success' });
      }
    },

    onError: (error, _variables, _context, mutation) => {
      if (mutation.meta?.silent) {
        return;
      }

      const problem = toProblemDetails(error);

      useSnackbarStore.getState().push({
        // A unique id per failure, so two attempts at the same broken action both show. The store
        // deduplicates by id, and a member who pressed a button twice should be told twice.
        id: `${mutation.mutationId}:${mutation.state.failureCount}`,
        title: mutation.meta?.failure ?? problem.title ?? 'That did not work.',
        body: problem.detail ?? undefined,
        tone: 'error',
      });
    },
  }),

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
