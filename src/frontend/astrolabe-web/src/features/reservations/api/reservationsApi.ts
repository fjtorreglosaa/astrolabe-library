import { httpClient } from '../../../shared/api/httpClient';
import type { Paged } from '../../catalog/api/catalogApi';

/** Where a loan sits. There is no `Overdue` — lateness is derived and travels as a flag. */
export type ReservationStatus = 'Reserved' | 'InTransit' | 'Returned' | 'Cancelled';

export type DeliveryMethod = 'Collection' | 'HomeDelivery';

export type ReturnMethod = 'CourierPickup' | 'LibraryDropOff';

export interface Reservation {
  id: string;
  bookId: string;
  title: string;
  author: string;
  coverUrl: string | null;
  libraryName: string;
  cityName: string;
  delivery: DeliveryMethod;
  deliveryFeeCents: number;
  borrowedOn: string;
  dueOn: string;
  status: ReservationStatus;
  /** Computed by the API. A browser clock in another zone would disagree with the desk. */
  isOverdue: boolean;
  daysLate: number;
  daysRemaining: number;
  returnMethod: ReturnMethod | null;
  handedOverAt: string | null;
  checkedInAt: string | null;
}

export interface ReservableCopy {
  libraryId: string;
  libraryName: string;
  cityName: string;
  availableCount: number;
  canReserve: boolean;
  reason: string | null;
}

export interface ReservationQuote {
  bookId: string;
  title: string;
  author: string;
  coverUrl: string | null;
  tier: string;
  genre: string;
  planNote: string;
  deliveryFeeCents: number;
  totalCents: number;
  dueOn: string;
  copies: ReservableCopy[];
}

export interface TopicInterest {
  genre: string;
  count: number;
  percent: number;
}

export interface MemberDashboard {
  activeReservations: number;
  dueThisWeek: number;
  overdue: number;
  returnedAllTime: number;
  readThisYear: number;
  activeSoonest: Reservation[];
  topics: TopicInterest[];
}

export const getMyReservations = async (
  status?: ReservationStatus,
  page = 1,
  pageSize = 20,
): Promise<Paged<Reservation>> => {
  const { data } = await httpClient.get<Paged<Reservation>>('/api/v1/reservations', {
    params: { status, page, pageSize },
  });
  return data;
};

export const getDashboard = async (): Promise<MemberDashboard> => {
  const { data } = await httpClient.get<MemberDashboard>('/api/v1/reservations/dashboard');
  return data;
};

export const quoteReservation = async (
  bookId: string,
  delivery: DeliveryMethod,
): Promise<ReservationQuote> => {
  const { data } = await httpClient.get<ReservationQuote>('/api/v1/reservations/quote', {
    params: { bookId, delivery },
  });
  return data;
};

export const confirmReservation = async (
  bookId: string,
  libraryId: string,
  delivery: DeliveryMethod,
  idempotencyKey: string,
): Promise<Reservation> => {
  const { data } = await httpClient.post<Reservation>('/api/v1/reservations', {
    bookId,
    libraryId,
    delivery,
    // Generated once per attempt the member makes, not once per request, so a retry is a no-op but
    // a second deliberate reservation is still possible.
    idempotencyKey,
  });
  return data;
};

export const beginReturn = async (
  reservationId: string,
  method: ReturnMethod,
  code: string,
): Promise<void> => {
  await httpClient.post(`/api/v1/reservations/${reservationId}/return`, { method, code });
};
