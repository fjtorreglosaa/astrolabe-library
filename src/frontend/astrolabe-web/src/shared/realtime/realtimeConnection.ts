import {
  HubConnection,
  HubConnectionBuilder,
  HttpTransportType,
  LogLevel,
} from '@microsoft/signalr';
import { API_BASE_URL } from '../api/apiBaseUrl';
import { getAccessToken } from '../api/httpClient';

/** Where the hub is mounted. Mirrors `HubRoutes.Realtime` in the API. */
export const REALTIME_HUB_PATH = '/hubs/realtime';

/**
 * Builds the hub connection.
 *
 * <p>
 * `accessTokenFactory` is a <b>factory</b>, and that matters: SignalR calls it again on every
 * reconnect, so a connection that comes back after the access token was refreshed presents the new
 * one. Passing the token by value instead works perfectly until the first refresh, after which every
 * reconnect fails with a 401 that looks like the server dropping connections.
 * </p>
 * <p>
 * WebSockets only. The fallbacks — server-sent events and long polling — exist for proxies that
 * cannot carry a socket, and silently degrading to one of them turns a real deployment problem into
 * a mysteriously sluggish app. Failing outright is the honest behaviour, and the interface already
 * copes with no connection at all.
 * </p>
 */
export const buildRealtimeConnection = (): HubConnection =>
  new HubConnectionBuilder()
    .withUrl(`${API_BASE_URL}${REALTIME_HUB_PATH}`, {
      transport: HttpTransportType.WebSockets,
      skipNegotiation: true,
      accessTokenFactory: () => getAccessToken() ?? '',
    })
    // Retries indefinitely rather than the default four attempts. A laptop that was asleep for an
    // hour must come back on its own; a connection that gave up twenty seconds in leaves the user
    // with a screen that looks live and is not — the one outcome worse than no realtime at all.
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: (context) =>
        Math.min(1_000 * 2 ** context.previousRetryCount, 30_000),
    })
    .configureLogging(import.meta.env.DEV ? LogLevel.Information : LogLevel.Warning)
    .build();
