/**
 * Sidebar structure, transcribed from the prototype's `navRaw` definition.
 *
 * `visibleTo` is declared now but not enforced until Stage 1 introduces roles. Keeping it here means
 * the shape does not have to change when authorization arrives.
 */

import type { UserRole } from '../features/auth/api/authApi';

export type Audience = 'member' | 'staff' | 'superAdmin';

const AUDIENCE_ROLES: Record<Audience, readonly UserRole[]> = {
  member: ['Basic', 'Plus', 'Max'],
  staff: ['Admin', 'SuperAdmin'],
  superAdmin: ['SuperAdmin'],
};

/**
 * Whether a role may see an entry.
 *
 * `requiresPaidPlan` implements the prototype's rule that Basic never sees the AI surface — the
 * entry is hidden, and the API refuses the call regardless.
 */
export const isVisibleTo = (item: NavigationItem, role: UserRole): boolean => {
  const allowed = item.visibleTo.some((audience) => AUDIENCE_ROLES[audience].includes(role));

  if (!allowed) {
    return false;
  }

  return !item.requiresPaidPlan || role === 'Plus' || role === 'Max';
};

/** Sections with at least one entry the role may see. */
export const sectionsFor = (role: UserRole): NavigationSection[] =>
  navigationSections
    .map((section) => ({ ...section, items: section.items.filter((item) => isVisibleTo(item, role)) }))
    .filter((section) => section.items.length > 0);

export interface NavigationItem {
  /** Route path. Matches the prototype's route key. */
  route: string;
  label: string;
  /** Material Symbols icon name used by the prototype. */
  icon: string;
  visibleTo: Audience[];
  /** Plus and Max only. Basic members never see this surface. */
  requiresPaidPlan?: boolean;
}

export interface NavigationSection {
  label: string;
  items: NavigationItem[];
}

export const navigationSections: NavigationSection[] = [
  {
    label: 'Discover',
    items: [
      { route: '/home', label: 'Home', icon: 'space_dashboard', visibleTo: ['member'] },
      { route: '/catalog', label: 'Catalog', icon: 'menu_book', visibleTo: ['member'] },
      {
        route: '/ai',
        label: 'AI recommendations',
        icon: 'auto_awesome',
        visibleTo: ['member'],
        requiresPaidPlan: true,
      },
    ],
  },
  {
    label: 'My account',
    items: [
      { route: '/loans', label: 'Book Reservations', icon: 'bookmarks', visibleTo: ['member'] },
      { route: '/fines', label: 'Fines & payments', icon: 'receipt_long', visibleTo: ['member'] },
      { route: '/purchases', label: 'My purchases', icon: 'shopping_bag', visibleTo: ['member'] },
      { route: '/support', label: 'Help & support', icon: 'support_agent', visibleTo: ['member'] },
    ],
  },
  {
    label: 'Administration',
    items: [
      { route: '/admin/users', label: 'Users', icon: 'group', visibleTo: ['staff', 'superAdmin'] },
      {
        route: '/admin/books',
        label: 'Book management',
        icon: 'library_add',
        visibleTo: ['staff', 'superAdmin'],
      },
      {
        route: '/admin/payments',
        label: 'Manual payments',
        icon: 'point_of_sale',
        visibleTo: ['staff', 'superAdmin'],
      },
      {
        route: '/admin/support',
        label: 'Support tickets',
        icon: 'contact_support',
        visibleTo: ['staff', 'superAdmin'],
      },
      {
        route: '/admin/libraries',
        label: 'Libraries & admins',
        icon: 'admin_panel_settings',
        visibleTo: ['superAdmin'],
      },
    ],
  },
];

/** Reached from the user menu rather than the sidebar, matching the prototype. */
export const userMenuRoutes = [
  { route: '/profile', label: 'My profile', icon: 'person' },
  { route: '/settings', label: 'Settings', icon: 'tune' },
] as const;
