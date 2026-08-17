/**
 * The title the header shows for a route.
 *
 * <p>
 * The prototype's header carries the <b>page title</b>, not the product name — the brand lives once,
 * in the sidebar. Repeating "Astrolabe Books" across the top of every screen spends the most
 * prominent line on the page telling somebody something they already knew, and leaves them without
 * the one thing a header is for: saying where they are.
 * </p>
 * <p>
 * Transcribed from the prototype's own `titles` map, with the routes this build added.
 * </p>
 */
const TITLES: Record<string, string> = {
  '/home': 'Home',
  '/catalog': 'Catalog',
  '/ai': 'AI recommendations',
  '/reservations': 'Book Reservations',
  '/profile': 'My profile',
  '/fines': 'Fines & payments',
  '/purchases': 'My purchases',
  '/settings': 'Settings',
  '/settings/devices': 'Devices and sessions',
  '/settings/notifications': 'Notification centre',
  '/settings/membership': 'Membership',
  '/support': 'Help & support',
  '/admin/books': 'Book management',
  '/admin/users': 'Users',
  '/admin/libraries': 'Libraries & admins',
  '/admin/payments': 'Manual payments',
  '/admin/ai': 'AI settings',
  '/admin/support': 'Support tickets',
};

/**
 * Falls back to the product name rather than to an empty header. An unknown route is a bug, and a
 * header that silently collapses hides it; one that reads "Astrolabe Books" still looks deliberate.
 */
export const pageTitleFor = (pathname: string): string =>
  TITLES[pathname] ?? 'Astrolabe Books';
