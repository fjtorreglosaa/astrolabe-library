import { MutationObserver } from '@tanstack/react-query';
import { queryClient } from './queryClient';
import { useSnackbarStore } from '../shared/feedback/snackbarStore';

/**
 * That a mutation cannot fail silently.
 *
 * <p>
 * This is the whole reason the reporting lives on the cache rather than in sixty `onError`
 * callbacks. The failure mode being prevented is an action that appears to do nothing — and the one
 * place somebody forgets to write a handler is, by definition, the place nobody was thinking about.
 * </p>
 */
const run = async (options: Parameters<typeof queryClient.getMutationCache.prototype>[0] | object) =>
  new MutationObserver(queryClient, options as never).mutate().catch(() => undefined);

describe('mutation reporting', () => {
  beforeEach(() => {
    useSnackbarStore.setState({ queue: [] });
  });

  it('reports a failure nobody wrote a handler for', async () => {
    // A network error or a thrown exception — anything that is not a problem document. The wording
    // comes from `toProblemDetails`, which supplies a title for exactly this case.
    await run({ mutationFn: async () => Promise.reject(new Error('boom')) });

    const [message] = useSnackbarStore.getState().queue;

    expect(message?.tone).toBe('error');
    expect(message?.title).toBe('Something went wrong.');
  });

  it('still says something when the server sends a problem document with no title', async () => {
    // The one case the local fallback covers. Without it the toast would render an empty heading,
    // which reads as the app having nothing to say about a failure it clearly noticed.
    await run({
      mutationFn: async () =>
        Promise.reject({ isAxiosError: true, response: { data: { status: 500 } } }),
    });

    expect(useSnackbarStore.getState().queue[0]?.title).toBe('That did not work.');
  });

  it('prefers the message the server sent', async () => {
    await run({
      mutationFn: async () =>
        Promise.reject({
          isAxiosError: true,
          response: { data: { title: 'That library does not hold a copy of this book.' } },
        }),
    });

    expect(useSnackbarStore.getState().queue[0]?.title).toBe(
      'That library does not hold a copy of this book.',
    );
  });

  it('says nothing for a mutation that reports itself inline', async () => {
    // A form showing its own alert beside the field: a toast as well is the same news twice, and
    // the copy in the dialog is the one with the context.
    await run({
      meta: { silent: true },
      mutationFn: async () => Promise.reject(new Error('boom')),
    });

    expect(useSnackbarStore.getState().queue).toHaveLength(0);
  });

  it('announces a success only when the action declared what to say', async () => {
    await run({ mutationFn: async () => 'done' });
    expect(useSnackbarStore.getState().queue).toHaveLength(0);

    await run({ meta: { success: 'Reserved.' }, mutationFn: async () => 'done' });

    const [message] = useSnackbarStore.getState().queue;
    expect(message?.tone).toBe('success');
    expect(message?.title).toBe('Reserved.');
  });

  it('reports the same broken action twice when it is attempted twice', async () => {
    // The store deduplicates by id, and two presses of a button that is failing deserve two
    // answers — otherwise the second press looks like it worked.
    const failing = { mutationFn: async () => Promise.reject(new Error('boom')) };

    await run(failing);
    await run(failing);

    expect(useSnackbarStore.getState().queue).toHaveLength(2);
  });
});
