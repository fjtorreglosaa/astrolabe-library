import { httpClient } from '../../../shared/api/httpClient';
import type { Paged } from '../../catalog/api/catalogApi';

export type FineStatus = 'Outstanding' | 'AwaitingValidation' | 'Paid';

export type DeskPaymentStatus = 'Pending' | 'Validated' | 'Rejected' | 'Expired';

export type CardBrand = 'Visa' | 'Mastercard' | 'Amex' | 'Other';

export type LedgerEntryKind = 'Charge' | 'Payment' | 'Credit';

export interface Fine {
  id: string;
  bookTitle: string;
  reason: string;
  daysLate: number;
  amountCents: number;
  status: FineStatus;
  assessedAt: string;
  libraryName: string;
}

export interface DeskPayment {
  id: string;
  code: string;
  memberName: string;
  amountCents: number;
  status: DeskPaymentStatus;
  /** Computed server-side. A browser clock in another zone would send a member to a closed counter. */
  isExpired: boolean;
  libraryName: string;
  concept: string;
  issuedAt: string;
  expiresAt: string;
  rejectionReason: string | null;
}

export interface FinesSummary {
  outstandingCents: number;
  /** Owed, but promised to a counter — deliberately not folded into the payable total. */
  awaitingValidationCents: number;
  totalOwedCents: number;
  balanceCents: number;
  fines: Fine[];
  openDeskPayments: DeskPayment[];
}

export interface PaymentMethod {
  id: string;
  brand: CardBrand;
  last4: string;
  expiryMonthYear: string;
  cardholderName: string;
  isPrimary: boolean;
  displayName: string;
}

export interface PaymentReceipt {
  receipt: string;
  amountCents: number;
  paidWith: string;
  fineCount: number;
  paidAt: string;
}

export interface LedgerEntry {
  id: string;
  kind: LedgerEntryKind;
  /** Signed: a charge is negative, so the interface renders it without knowing which kinds are debits. */
  amountCents: number;
  description: string;
  occurredAt: string;
}

export const getMyFines = async (): Promise<FinesSummary> => {
  const { data } = await httpClient.get<FinesSummary>('/api/v1/billing/fines');
  return data;
};

export const getMyLedger = async (page = 1, pageSize = 20): Promise<Paged<LedgerEntry>> => {
  const { data } = await httpClient.get<Paged<LedgerEntry>>('/api/v1/billing/ledger', {
    params: { page, pageSize },
  });
  return data;
};

export const getMyPaymentMethods = async (): Promise<PaymentMethod[]> => {
  const { data } = await httpClient.get<PaymentMethod[]>('/api/v1/billing/payment-methods');
  return data;
};

/**
 * Puts a card on file.
 *
 * There is no parameter for a card number, a CVV or a full expiry year — and there is nowhere in the
 * API to send one. These are the display details a payment provider returns after tokenising, and a
 * full number sent as `last4` is refused by the server rather than truncated.
 */
export const addPaymentMethod = async (input: {
  brand: CardBrand;
  last4: string;
  expiryMonthYear: string;
  cardholderName: string;
  makePrimary: boolean;
}): Promise<void> => {
  await httpClient.post('/api/v1/billing/payment-methods', input);
};

export const removePaymentMethod = async (paymentMethodId: string): Promise<void> => {
  await httpClient.delete(`/api/v1/billing/payment-methods/${paymentMethodId}`);
};

export const payFines = async (
  fineIds: string[],
  paymentMethodId: string,
): Promise<PaymentReceipt> => {
  const { data } = await httpClient.post<PaymentReceipt>('/api/v1/billing/payments', {
    fineIds,
    paymentMethodId,
  });
  return data;
};

export const issueDeskPayment = async (fineIds: string[]): Promise<DeskPayment> => {
  const { data } = await httpClient.post<DeskPayment>('/api/v1/billing/desk-payments', { fineIds });
  return data;
};

// ---------- Staff ----------

export const getDeskQueue = async (
  status?: DeskPaymentStatus,
  page = 1,
  pageSize = 20,
): Promise<Paged<DeskPayment>> => {
  const { data } = await httpClient.get<Paged<DeskPayment>>('/api/v1/admin/payments', {
    params: { status, page, pageSize },
  });
  return data;
};

export const validateDeskPayment = async (code: string): Promise<void> => {
  await httpClient.post(`/api/v1/admin/payments/${code}/validate`);
};

export const rejectDeskPayment = async (code: string, reason: string): Promise<void> => {
  await httpClient.post(`/api/v1/admin/payments/${code}/reject`, { reason });
};
