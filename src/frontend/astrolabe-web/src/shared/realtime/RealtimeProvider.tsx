import { HubConnectionState } from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { createContext, use, useEffect, useMemo, useState, type ReactNode } from 'react';
import { useAuth } from '../../features/auth/components/AuthProvider';
import { buildRealtimeConnection } from './realtimeConnection';
import { REALTIME_EVENTS, STALE_ON, type RealtimeEvent } from './realtimeEvents';

export type RealtimeStatus = 'connecting' | 'live' | 'reconnecting' | 'offline';

const RealtimeContext = createContext<RealtimeStatus>('offline');

/**
 * Keeps one hub connection open for as long as somebody is signed in, and turns what arrives on it
 * into cache invalidations.
 *
 * <p>
 * <b>One connection for the whole application.</b> A hook that opened a socket per screen would open
 * four on the dashboard and reconnect all of them on every navigation. This sits above the router,
 * so the connection outlives the page.
 * </p>
 * <p>
 * It owns no data and renders no UI beyond its children. Screens keep reading from TanStack Query
 * exactly as they did before this existed — the only difference is that a query now becomes stale
 * because the server said so, rather than because a timer expired. That is deliberate: a component
 * that had to subscribe to realtime to be correct would be a component that shows nothing when the
 * socket is down.
 * </p>
 */
export const RealtimeProvider = ({ children }: { children: ReactNode }) => {
  const { isAuthenticated, signOut } = useAuth();
  const queryClient = useQueryClient();
  const [status, setStatus] = useState<RealtimeStatus>('offline');

  useEffect(() => {
    // Nobody signed in, no connection. The hub refuses an anonymous handshake anyway, and retrying
    // it forever on the sign-in screen is a loop with no possible success.
    if (!isAuthenticated) {
      setStatus('offline');
      return;
    }

    const connection = buildRealtimeConnection();
    let disposed = false;

    connection.on('Changed', (event: RealtimeEvent) => {
      // Access ending is the one event that is not a refetch: every request after it answers 401,
      // so the honest response is to end the session here rather than let the member click into a
      // wall of errors. Covers a librarian revoking a session and an account being blocked.
      if (event.name === REALTIME_EVENTS.accessRevoked) {
        void signOut();
        return;
      }

      const stale = STALE_ON[event.name];

      if (!stale) {
        // A name this build does not know. Harmless — an older client simply does not react — and
        // logged rather than thrown, because a deploy in progress serves both versions at once.
        console.warn(`[realtime] Ignoring unknown event "${event.name}".`);
        return;
      }

      for (const queryKey of stale) {
        void queryClient.invalidateQueries({ queryKey });
      }
    });

    connection.onreconnecting(() => !disposed && setStatus('reconnecting'));

    connection.onreconnected(() => {
      if (disposed) {
        return;
      }

      setStatus('live');

      // Everything is suspect after a gap: whatever happened while the socket was down was pushed
      // to nobody. This is the one place a blanket invalidation is right, and it is why losing a
      // connection costs freshness rather than correctness.
      void queryClient.invalidateQueries();
    });

    connection.onclose(() => !disposed && setStatus('offline'));

    setStatus('connecting');

    connection
      .start()
      .then(() => !disposed && setStatus('live'))
      .catch(() => {
        // Not retried by hand. `withAutomaticReconnect` covers a connection that drops, and a first
        // attempt that fails usually means the API is not up yet — in which case the next sign-in or
        // reload tries again. Screens stay correct without it.
        if (!disposed) {
          setStatus('offline');
        }
      });

    return () => {
      disposed = true;
      // Only stop a connection that got somewhere. Calling stop() on one still negotiating throws.
      if (connection.state !== HubConnectionState.Disconnected) {
        void connection.stop();
      }
    };
  }, [isAuthenticated, queryClient, signOut]);

  const value = useMemo(() => status, [status]);

  return <RealtimeContext value={value}>{children}</RealtimeContext>;
};

/** The connection's state, for the one indicator that shows it. */
export const useRealtimeStatus = (): RealtimeStatus => use(RealtimeContext);
