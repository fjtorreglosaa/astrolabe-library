import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { DeliveryMethod, ReturnMethod } from '../reservations/api/reservationsApi';
import type { OrderFulfilment } from '../store/api/storeApi';

/**
 * What the app proposes by default when a member reserves, returns or buys a book.
 *
 * <p>
 * These are <b>proposals, not decisions</b>. The prototype says so in the section's own words — "You
 * can still switch them when you reserve or buy a book" — and it holds them in browser state for the
 * same reason this does. The binding choice is made per reservation and per order, where the
 * reservations and store domains validate it; a default that disagreed with what the member picked at
 * the last step would still lose, so there is nothing here for a domain to enforce.
 * </p>
 * <p>
 * Kept out of `app/uiStore` deliberately. That store holds app chrome — theme, sidebar, the quick
 * actions button — and these are a member's own preferences about three different domains. They also
 * carry each domain's real type rather than a private copy of it, so renaming `HomeDelivery` breaks
 * this file instead of silently sending a value no server accepts.
 * </p>
 * <p>
 * The consequence of holding them in the browser is that they do not follow a member to another
 * device. Making them do so needs a home for member preferences that no bounded context obviously
 * owns — raised as `GLOBAL-027` rather than decided here.
 * </p>
 */
export interface MemberDefaults {
  delivery: DeliveryMethod;
  returns: ReturnMethod;
  purchase: OrderFulfilment;

  setDelivery: (value: DeliveryMethod) => void;
  setReturns: (value: ReturnMethod) => void;
  setPurchase: (value: OrderFulfilment) => void;
}

export const useMemberDefaults = create<MemberDefaults>()(
  persist(
    (set) => ({
      // The free option in each pair. A default that quietly adds a delivery fee is a default that
      // charges somebody for not reading a settings screen.
      delivery: 'Collection',
      returns: 'LibraryDropOff',
      purchase: 'Collection',

      setDelivery: (delivery) => set({ delivery }),
      setReturns: (returns) => set({ returns }),
      setPurchase: (purchase) => set({ purchase }),
    }),
    { name: 'astrolabe-member-defaults' },
  ),
);
