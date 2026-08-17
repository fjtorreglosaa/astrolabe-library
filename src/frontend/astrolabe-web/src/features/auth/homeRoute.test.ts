import { homeRouteFor } from './homeRoute';
import { navigationSections } from '../../routes/navigation';

/**
 * Where each role lands, and — the part that actually broke — that the landing route is one the role
 * may use.
 *
 * <p>
 * Every user used to land on `/home`. It is the member dashboard, and its four requests all answer
 * 403 to staff, so every administrator's first screen was a page of errors. The last assertion is
 * what stops that returning: it checks the destination against the navigation model rather than
 * against a hard-coded string, so a route that changes audience fails here.
 * </p>
 */
describe('homeRouteFor', () => {
  it('sends a member to their dashboard', () => {
    expect(homeRouteFor('Member')).toBe('/home');
  });

  it('sends both kinds of staff to the users directory, not the member dashboard', () => {
    expect(homeRouteFor('Admin')).toBe('/admin/users');
    expect(homeRouteFor('SuperAdmin')).toBe('/admin/users');
  });

  it('falls back to the member dashboard when there is no role yet', () => {
    // Nobody signed in gets bounced to sign-in by ProtectedRoute long before this matters, so the
    // safe answer is the one that is not an administration screen.
    expect(homeRouteFor(null)).toBe('/home');
  });

  it.each(['Member', 'Admin', 'SuperAdmin'] as const)(
    'lands %s on a route their own sidebar offers them',
    (role) => {
      const audience = role === 'Member' ? 'member' : role === 'Admin' ? 'staff' : 'superAdmin';

      const offered = navigationSections
        .flatMap((section) => section.items)
        .filter((item) => item.visibleTo.includes(audience))
        .map((item) => item.route);

      expect(offered).toContain(homeRouteFor(role));
    },
  );
});
