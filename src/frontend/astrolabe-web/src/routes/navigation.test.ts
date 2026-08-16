import { isVisibleTo, navigationSections, sectionsFor } from './navigation';
import type { PlanTier } from '../features/membership/api/membershipApi';

/**
 * Covers the sidebar, which is composed from a role *and* a plan since GLOBAL-019. These mirror
 * rules the API also enforces: hiding an entry is a convenience, and a member who guesses the URL
 * still gets a 403.
 */
describe('sidebar composition', () => {
  const entry = (route: string) =>
    navigationSections.flatMap((section) => section.items).find((item) => item.route === route)!;

  const PLANS: PlanTier[] = ['Basic', 'Plus', 'Max'];

  it.each(PLANS)('shows the member sections on %s', (plan) => {
    const labels = sectionsFor('Member', plan).map((section) => section.label);

    expect(labels).toContain('Discover');
    expect(labels).toContain('My account');
  });

  it.each(PLANS)('hides administration from a member on %s', (plan) => {
    expect(sectionsFor('Member', plan).map((section) => section.label)).not.toContain(
      'Administration',
    );
  });

  it('never shows AI recommendations to Basic', () => {
    // The prototype is explicit: Basic never sees this surface.
    expect(isVisibleTo(entry('/ai'), 'Member', 'Basic')).toBe(false);
  });

  it.each<PlanTier>(['Plus', 'Max'])('shows AI recommendations on %s', (plan) => {
    expect(isVisibleTo(entry('/ai'), 'Member', plan)).toBe(true);
  });

  it('hides a paid surface from a member whose plan has not loaded', () => {
    // A null plan is what the shell holds for a heartbeat after sign-in, and for staff always.
    // Defaulting an unknown plan to "allowed" would flash the AI entry at a Basic member.
    expect(isVisibleTo(entry('/ai'), 'Member', null)).toBe(false);
  });

  it('does not open a paid surface to staff, who hold no plan at all', () => {
    // Staff are not in the member audience, so the entry is refused before the plan is consulted.
    expect(isVisibleTo(entry('/ai'), 'SuperAdmin', null)).toBe(false);
  });

  it('shows administration to staff but not the member sections', () => {
    const labels = sectionsFor('Admin', null).map((section) => section.label);

    expect(labels).toContain('Administration');
    expect(labels).not.toContain('My account');
  });

  it('hides Libraries & admins from an Admin', () => {
    // BR-NET-008 reserves network management to a super administrator.
    expect(isVisibleTo(entry('/admin/libraries'), 'Admin', null)).toBe(false);
    expect(isVisibleTo(entry('/admin/libraries'), 'SuperAdmin', null)).toBe(true);
  });

  it('gives a super admin every administration entry', () => {
    const administration = sectionsFor('SuperAdmin', null).find((s) => s.label === 'Administration');

    expect(administration?.items.map((item) => item.route)).toEqual([
      '/admin/users',
      '/admin/books',
      '/admin/payments',
      // Added at Stage 7. The list is asserted exactly on purpose: an entry appearing in the
      // sidebar without anyone deciding it should is how a surface leaks to the wrong audience.
      '/admin/ai',
      '/admin/support',
      '/admin/libraries',
    ]);
  });

  it('drops a section entirely when a user may see none of its entries', () => {
    // An empty section header would be worse than no section at all.
    expect(sectionsFor('Admin', null).every((section) => section.items.length > 0)).toBe(true);
  });
});
