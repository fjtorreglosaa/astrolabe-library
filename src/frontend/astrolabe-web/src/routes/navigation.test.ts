import { isVisibleTo, navigationSections, sectionsFor } from './navigation';
import type { UserRole } from '../features/auth/api/authApi';

/**
 * Covers the role-driven sidebar. These mirror rules the API also enforces: hiding an entry is a
 * convenience, and a member who guesses the URL still gets a 403.
 */
describe('sidebar composition', () => {
  const entry = (route: string) =>
    navigationSections.flatMap((section) => section.items).find((item) => item.route === route)!;

  it.each<UserRole>(['Basic', 'Plus', 'Max'])('shows the member sections to %s', (role) => {
    const labels = sectionsFor(role).map((section) => section.label);

    expect(labels).toContain('Discover');
    expect(labels).toContain('My account');
  });

  it.each<UserRole>(['Basic', 'Plus', 'Max'])('hides administration from %s', (role) => {
    expect(sectionsFor(role).map((section) => section.label)).not.toContain('Administration');
  });

  it('never shows AI recommendations to Basic', () => {
    // The prototype is explicit: Basic never sees this surface.
    expect(isVisibleTo(entry('/ai'), 'Basic')).toBe(false);
  });

  it.each<UserRole>(['Plus', 'Max'])('shows AI recommendations to %s', (role) => {
    expect(isVisibleTo(entry('/ai'), role)).toBe(true);
  });

  it('shows administration to staff but not the member sections', () => {
    const labels = sectionsFor('Admin').map((section) => section.label);

    expect(labels).toContain('Administration');
    expect(labels).not.toContain('My account');
  });

  it('hides Libraries & admins from an Admin', () => {
    // BR-NET-008 reserves network management to a super administrator.
    expect(isVisibleTo(entry('/admin/libraries'), 'Admin')).toBe(false);
    expect(isVisibleTo(entry('/admin/libraries'), 'SuperAdmin')).toBe(true);
  });

  it('gives a super admin every administration entry', () => {
    const administration = sectionsFor('SuperAdmin').find((s) => s.label === 'Administration');

    expect(administration?.items.map((item) => item.route)).toEqual([
      '/admin/users',
      '/admin/books',
      '/admin/payments',
      '/admin/support',
      '/admin/libraries',
    ]);
  });

  it('drops a section entirely when a role may see none of its entries', () => {
    // An empty section header would be worse than no section at all.
    expect(sectionsFor('Admin').every((section) => section.items.length > 0)).toBe(true);
  });
});
