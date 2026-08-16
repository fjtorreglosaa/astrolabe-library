/**
 * What the quick actions button offers, transcribed from the prototype's `quick` list.
 *
 * Its own module so it can be asserted without mounting the shell — importing the component pulls in
 * the router, and a test of four labels should not need a router to run.
 */
export interface QuickAction {
  icon: string;
  label: string;
  route: string;
}

/** A member's four. */
export const MEMBER_ACTIONS: QuickAction[] = [
  { icon: 'qr_code_scanner', label: 'Quick check-in', route: '/loans' },
  { icon: 'search', label: 'Search catalog', route: '/catalog' },
  { icon: 'local_shipping', label: 'Delivery status', route: '/loans' },
  { icon: 'payments', label: 'Pay fines', route: '/fines' },
];

/** Staff get three, and different ones — the prototype does not share a list between them. */
export const STAFF_ACTIONS: QuickAction[] = [
  { icon: 'group', label: 'Users', route: '/admin/users' },
  { icon: 'library_add', label: 'Book management', route: '/admin/books' },
  { icon: 'settings', label: 'AI settings', route: '/admin/ai' },
];
