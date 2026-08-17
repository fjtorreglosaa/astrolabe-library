import { useMemberDefaults } from './memberDefaults';

/**
 * The defaults a member starts with, and that each of them is the free option.
 *
 * <p>
 * Worth a test rather than a comment: the two paid options carry a delivery fee, and a default that
 * silently adds $3.99 to a reservation would charge somebody for never opening this screen. It is
 * also the kind of value that gets flipped in passing while somebody is testing the other branch.
 * </p>
 */
describe('member defaults', () => {
  it('starts on the free option in all three groups', () => {
    const state = useMemberDefaults.getState();

    expect(state.delivery).toBe('Collection');
    expect(state.returns).toBe('LibraryDropOff');
    expect(state.purchase).toBe('Collection');
  });

  it('sets each group independently', () => {
    useMemberDefaults.getState().setDelivery('HomeDelivery');

    // The other two are untouched: three separate decisions, not one setting with three faces.
    expect(useMemberDefaults.getState().delivery).toBe('HomeDelivery');
    expect(useMemberDefaults.getState().returns).toBe('LibraryDropOff');
    expect(useMemberDefaults.getState().purchase).toBe('Collection');

    useMemberDefaults.getState().setReturns('CourierPickup');
    useMemberDefaults.getState().setPurchase('Shipping');

    expect(useMemberDefaults.getState().returns).toBe('CourierPickup');
    expect(useMemberDefaults.getState().purchase).toBe('Shipping');
  });
});
