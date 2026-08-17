import { REALTIME_EVENTS, STALE_ON } from './realtimeEvents';

/**
 * The contract with the API, and the completeness of the invalidation table.
 *
 * <p>
 * The names are the contract, and the failure mode when they drift is the quietest one there is: the
 * server pushes, the client receives, no name matches, nothing is invalidated, and the screen simply
 * goes back to being as stale as it was before any of this existed. Nothing errors. These tests are
 * the only thing standing between a renamed constant and a feature that silently stops working.
 * </p>
 */
describe('realtime events', () => {
  it('has an entry in the invalidation table for every event the server can send', () => {
    // Missing here means "received and ignored". The provider logs it, but a log nobody reads is
    // not a safety net.
    // `Object.hasOwn`, not `toHaveProperty` — the latter reads a dot as a path into a nested
    // object, so every one of these names looks like a miss when it is present.
    for (const name of Object.values(REALTIME_EVENTS)) {
      expect(Object.hasOwn(STALE_ON, name)).toBe(true);
    }
  });

  it('has no entries in the table that no event can produce', () => {
    const known = new Set<string>(Object.values(REALTIME_EVENTS));

    for (const name of Object.keys(STALE_ON)) {
      expect(known.has(name)).toBe(true);
    }
  });

  it('invalidates something for every event except the one that ends the session', () => {
    for (const [name, keys] of Object.entries(STALE_ON)) {
      if (name === REALTIME_EVENTS.accessRevoked) {
        // Nothing to refetch — every request after this answers 401.
        expect(keys).toHaveLength(0);
        continue;
      }

      expect(keys.length).toBeGreaterThan(0);
    }
  });

  it('keeps a reservation and its money in step', () => {
    // The specific bug this prevents: a member reserves with home delivery, the loan appears, and
    // the catalogue keeps offering the copy they just took.
    expect(STALE_ON[REALTIME_EVENTS.reservationConfirmed]).toEqual(
      expect.arrayContaining([['reservations'], ['catalog']]),
    );
  });

  it('names events after the business, not after screens', () => {
    // A name like "refresh-fines-table" outlives neither a redesign nor a second audience. Every
    // name is `<context>.<what happened>`, and the dot is what makes that visible.
    for (const name of Object.values(REALTIME_EVENTS)) {
      expect(name).toMatch(/^[a-z]+\.[a-z-]+$/);
    }
  });
});
