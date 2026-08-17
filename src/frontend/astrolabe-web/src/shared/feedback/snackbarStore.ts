import { create } from 'zustand';

export type SnackbarTone = 'info' | 'success' | 'warning' | 'error';

export interface SnackbarMessage {
  id: string;
  title: string;
  body?: string;
  tone: SnackbarTone;
  /** Where clicking it takes the reader, when there is somewhere useful to go. */
  route?: string;
}

interface SnackbarState {
  /** A queue, not a slot. Two things can happen at once and the second is not less important. */
  queue: SnackbarMessage[];
  push: (message: Omit<SnackbarMessage, 'id'> & { id?: string }) => void;
  dismiss: (id: string) => void;
}

/**
 * Transient messages — the prototype's `snack`.
 *
 * <p>
 * A queue rather than a single current message, because a member can settle three fines with one
 * tap and each produces its own outcome. Showing only the last one would report two of them as
 * having quietly not happened.
 * </p>
 * <p>
 * Deduplicated by id. The same notification can arrive twice — a push lands, then a reconnect
 * refetches the feed and finds it again — and a member should be told once.
 * </p>
 */
export const useSnackbarStore = create<SnackbarState>((set) => ({
  queue: [],

  push: (message) =>
    set((state) => {
      const id = message.id ?? `${message.title}:${state.queue.length}`;

      if (state.queue.some((queued) => queued.id === id)) {
        return state;
      }

      return { queue: [...state.queue, { ...message, id }] };
    }),

  dismiss: (id) => set((state) => ({ queue: state.queue.filter((message) => message.id !== id) })),
}));
