import { MEMBER_ACTIONS, STAFF_ACTIONS } from './quickActionItems';

/**
 * The quick actions are transcribed from the prototype's `quick` list, and the labels are the part
 * worth pinning: they are what a reader scans, and the first implementation of this screen used a
 * speed dial whose labels only appeared on hover — which on a touch device means never.
 */
describe('quick actions', () => {
  it('offers a member the four the prototype offers', () => {
    expect(MEMBER_ACTIONS.map((action) => action.label)).toEqual([
      'Quick check-in',
      'Search catalog',
      'Delivery status',
      'Pay fines',
    ]);
  });

  it('offers staff a different three', () => {
    // Not a subset and not a superset — the prototype gives the two audiences separate lists, and
    // merging them would put "Pay fines" in front of somebody who has none.
    expect(STAFF_ACTIONS.map((action) => action.label)).toEqual([
      'Users',
      'Book management',
      'AI settings',
    ]);
  });

  it('sends every action somewhere that exists', () => {
    // A shortcut to a route the router does not know is worse than no shortcut: it looks like a
    // feature and behaves like a dead end.
    const routes = [
      '/reservations',
      '/catalog',
      '/fines',
      '/admin/users',
      '/admin/books',
      '/admin/ai',
    ];

    for (const action of [...MEMBER_ACTIONS, ...STAFF_ACTIONS]) {
      expect(routes).toContain(action.route);
    }
  });

  it('gives every action its own icon', () => {
    // Two identical icons in a list of four make the list harder to scan than no icons at all.
    const icons = MEMBER_ACTIONS.map((action) => action.icon);

    expect(new Set(icons).size).toBe(icons.length);
  });
});
