import { httpClient } from '../../../shared/api/httpClient';
import type { Paged } from '../../catalog/api/catalogApi';

export type OrderFulfilment = 'Collection' | 'Shipping';

export interface OrderLine {
  bookId: string;
  bookTitle: string;
  quantity: number;
  unitPriceCents: number;
  discountPercent: number;
  discountAmountCents: number;
  lineTotalCents: number;
}

export interface Order {
  id: string;
  fulfilment: OrderFulfilment;
  subtotalCents: number;
  discountTotalCents: number;
  shippingFeeCents: number;
  /** What the order was worth, before any reward points were applied. */
  totalCents: number;
  /** Point-cents put toward it. Points are a tender, not a discount, so the total is unchanged. */
  pointsRedeemed: number;
  /** What the card was actually asked for: the total less the points applied. */
  amountChargedCents: number;
  pointsEarned: number;
  placedAt: string;
  description: string;
  lines: OrderLine[];
}

export interface OrderQuote {
  subtotalCents: number;
  discountTotalCents: number;
  shippingFeeCents: number;
  totalCents: number;
  /** Everything the member holds, whether or not this order can absorb it. */
  pointsBalance: number;
  /**
   * The most this order will accept. The control is bounded by this, so the screen can never offer
   * a redemption the server would then refuse.
   */
  maxRedeemablePointCents: number;
  pointsRedeemed: number;
  amountChargedCents: number;
  pointsWouldEarn: number;
  /** Why the percentage is what it is — a 0% on a Plus plan needs explaining, not hiding. */
  discountNote: string;
  /** Why the redemption control looks the way it does. A dead control needs a reason. */
  redemptionNote: string;
  lines: OrderLine[];
}

export interface PointsMovement {
  id: string;
  pointCents: number;
  description: string;
  occurredAt: string;
}

export interface PointsSummary {
  balancePointCents: number;
  earnsPoints: boolean;
  /**
   * Whether the balance is large enough to spend. Travels from the server rather than being
   * recomputed here, so the floor lives in one place.
   */
  canRedeem: boolean;
  note: string;
  recent: PointsMovement[];
}

export const quoteOrder = async (
  bookIds: string[],
  fulfilment: OrderFulfilment,
  pointsToRedeem = 0,
): Promise<OrderQuote> => {
  const { data } = await httpClient.get<OrderQuote>('/api/v1/store/quote', {
    params: { bookIds, fulfilment, pointsToRedeem },
    // Repeated key, as ASP.NET binds an array: bookIds=a&bookIds=b.
    paramsSerializer: { indexes: null },
  });
  return data;
};

export const placeOrder = async (input: {
  bookId: string;
  fulfilment: OrderFulfilment;
  paymentMethodId: string;
  idempotencyKey: string;
  pointsToRedeem: number;
}): Promise<Order> => {
  const { data } = await httpClient.post<Order>('/api/v1/store/orders', {
    lines: [{ bookId: input.bookId, quantity: 1 }],
    fulfilment: input.fulfilment,
    paymentMethodId: input.paymentMethodId,
    idempotencyKey: input.idempotencyKey,
    pointsToRedeem: input.pointsToRedeem,
  });
  return data;
};

export const getMyOrders = async (page = 1, pageSize = 20): Promise<Paged<Order>> => {
  const { data } = await httpClient.get<Paged<Order>>('/api/v1/store/orders', {
    params: { page, pageSize },
  });
  return data;
};

export const getMyPoints = async (): Promise<PointsSummary> => {
  const { data } = await httpClient.get<PointsSummary>('/api/v1/store/points');
  return data;
};
