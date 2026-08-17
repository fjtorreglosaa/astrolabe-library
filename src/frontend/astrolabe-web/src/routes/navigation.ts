/**
 * Sidebar structure, transcribed from the prototype's `navRaw` definition.
 *
 * `visibleTo` names the audience an entry belongs to; `requiresPaidPlan` narrows a member entry to
 * the tiers that paid for it. Both are convenience only — the API refuses the call regardless.
 */

import type { UserRole } from '../features/auth/api/authApi';
import type { PlanTier } from '../features/membership/api/membershipApi';

export type Audience = 'member' | 'staff' | 'superAdmin';

const AUDIENCE_ROLES: Record<Audience, readonly UserRole[]> = {
  member: ['Member'],
  staff: ['Admin', 'SuperAdmin'],
  superAdmin: ['SuperAdmin'],
};

/** The tiers that unlock a `requiresPaidPlan` entry. */
const PAID_PLANS: readonly PlanTier[] = ['Plus', 'Max'];

/**
 * Whether a user may see an entry.
 *
 * Takes the role *and* the plan because the two answer different halves of the question: the role
 * decides which audience someone belongs to, and only the plan decides whether a paid surface is
 * open to them. Until GLOBAL-019 one argument did both, which worked exactly as long as nobody
 * could be a member on one tier while their role said another.
 *
 * `requiresPaidPlan` implements the prototype's rule that Basic never sees the AI surface — the
 * entry is hidden, and the API refuses the call regardless.
 */
export const isVisibleTo = (
  item: NavigationItem,
  role: UserRole,
  plan: PlanTier | null,
): boolean => {
  const allowed = item.visibleTo.some((audience) => AUDIENCE_ROLES[audience].includes(role));

  if (!allowed) {
    return false;
  }

  return !item.requiresPaidPlan || (plan !== null && PAID_PLANS.includes(plan));
};

/** Sections with at least one entry the user may see. */
export const sectionsFor = (role: UserRole, plan: PlanTier | null): NavigationSection[] =>
  navigationSections
    .map((section) => ({
      ...section,
      items: section.items.filter((item) => isVisibleTo(item, role, plan)),
    }))
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
      { route: '/reservations', label: 'Book Reservations', icon: 'bookmarks', visibleTo: ['member'] },
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
        route: '/admin/ai',
        label: 'AI settings',
        icon: 'auto_awesome',
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
