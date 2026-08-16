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
  totalCents: number;
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
  pointsWouldEarn: number;
  /** Why the percentage is what it is — a 0% on a Plus plan needs explaining, not hiding. */
  discountNote: string;
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
   * False for everyone today: the redemption cap is undefined and BLOCK-002 is open. It travels
   * from the server rather than being assumed here, so the interface follows the day it is decided.
   */
  canRedeem: boolean;
  note: string;
  recent: PointsMovement[];
}

export const quoteOrder = async (
  bookIds: string[],
  fulfilment: OrderFulfilment,
): Promise<OrderQuote> => {
  const { data } = await httpClient.get<OrderQuote>('/api/v1/store/quote', {
    params: { bookIds, fulfilment },
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
}): Promise<Order> => {
  const { data } = await httpClient.post<Order>('/api/v1/store/orders', {
    lines: [{ bookId: input.bookId, quantity: 1 }],
    fulfilment: input.fulfilment,
    paymentMethodId: input.paymentMethodId,
    idempotencyKey: input.idempotencyKey,
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
