import { useEffect, useRef } from 'react';
import { useSnackbarStore } from '../../../shared/feedback/snackbarStore';
import type { Notification } from '../api/notificationsApi';

/**
 * Raises a toast for notifications that were not in the feed a moment ago.
 *
 * <p>
 * Driven by the <b>feed</b> rather than by the realtime event, and deliberately so. The push says
 * only that something was written; the wording, the icon and the route all come from the server,
 * and re-deriving them in the browser would give a member two slightly different sentences about
 * one event depending on whether their socket was up. This way there is one text, written once.
 * </p>
 * <p>
 * The first load announces nothing. Somebody opening the app to eleven unread notifications does not
 * want eleven toasts about things they already know — the badge is what tells them. Only what
 * arrives while they are watching is worth interrupting for.
 * </p>
 */
export const useAnnounceNewNotifications = (items: Notification[] | undefined) => {
  const push = useSnackbarStore((state) => state.push);
  const seen = useRef<Set<string> | null>(null);

  useEffect(() => {
    if (!items) {
      return;
    }

    // First run: remember everything and say nothing.
    if (seen.current === null) {
      seen.current = new Set(items.map((item) => item.id));
      return;
    }

    const known = seen.current;

    // Oldest first, so a burst reads in the order it happened.
    const arrived = items.filter((item) => !known.has(item.id)).reverse();

    for (const item of arrived) {
      known.add(item.id);

      push({
        id: item.id,
        title: item.title,
        body: item.body,
        // The member is being told, not warned. An error-coloured toast for "your book is ready"
        // would be shouting, and one for a fine would be scolding somebody who already knows.
        tone: 'info',
        route: item.route ?? undefined,
      });
    }
  }, [items, push]);
};
