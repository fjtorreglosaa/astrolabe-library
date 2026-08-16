import { httpClient } from '../../../shared/api/httpClient';
import type { Paged } from '../../catalog/api/catalogApi';
import type { PlanTier } from '../../membership/api/membershipApi';
import type { CreatedResource } from '../../../shared/api/created';

/** The lifecycle of a book, as the API names it. Members only ever see `Catalog`. */
export type BookStatus = 'Draft' | 'Catalog' | 'Repair' | 'Deleted';

/** Transcribed from the prototype's genre picker. */
export type Genre = 'Fiction' | 'Essay' | 'ScienceFiction' | 'History' | 'Biography' | 'Technical';

export type BookSortKey = 'Title' | 'Author' | 'CreatedAt' | 'RetailPrice';

export type SortDirection = 'Ascending' | 'Descending';

/** Typed, never free text. A reason nobody can group by is a reason nobody can act on. */
export type RepairReason =
  | 'DamagedSpine'
  | 'WaterDamage'
  | 'MissingPages'
  | 'Rebinding'
  | 'CoverReplacement'
  | 'Other';

export type RemovalReason =
  | 'Donated'
  | 'DamagedBeyondRepair'
  | 'LostByMember'
  | 'WithdrawnFromCollection'
  | 'Other';

export interface StaffBook {
  id: string;
  isbn: string;
  title: string;
  author: string;
  genre: Genre;
  tier: PlanTier;
  status: BookStatus;
  retailPriceCents: number;
  availableCount: number;
  totalCount: number;
  createdAt: string;
}

export interface CreateBookInput {
  isbn: string;
  title: string;
  author: string;
  publisher: string | null;
  genre: Genre;
  tier: PlanTier;
  retailPriceCents: number;
  coverUrl: string | null;
  copies: { libraryId: string; quantity: number }[];
}

export type UpdateBookInput = Omit<CreateBookInput, 'isbn' | 'copies'>;

export const searchStaffBooks = async (search: {
  term?: string;
  status?: BookStatus;
  sortBy?: BookSortKey;
  direction?: SortDirection;
  page?: number;
  pageSize?: number;
}): Promise<Paged<StaffBook>> => {
  const { data } = await httpClient.get<Paged<StaffBook>>('/api/v1/admin/catalog/books', {
    params: {
      term: search.term || undefined,
      status: search.status,
      sortBy: search.sortBy,
      direction: search.direction,
      page: search.page ?? 1,
      pageSize: search.pageSize ?? 20,
    },
  });

  return data;
};

/** Creates the book as a **draft**. Publishing is a separate, deliberate act. */
export const createBookDraft = async (input: CreateBookInput): Promise<string> => {
  const { data } = await httpClient.post<CreatedResource>('/api/v1/admin/catalog/books', input);
  return data.id;
};

export const updateBook = async (bookId: string, input: UpdateBookInput): Promise<void> => {
  await httpClient.put(`/api/v1/admin/catalog/books/${bookId}`, input);
};

export const publishBook = async (bookId: string): Promise<void> => {
  await httpClient.post(`/api/v1/admin/catalog/books/${bookId}/publish`);
};

export const sendBookToRepair = async (
  bookId: string,
  reason: RepairReason,
  notes: string | null,
): Promise<void> => {
  await httpClient.post(`/api/v1/admin/catalog/books/${bookId}/repair`, {
    reason,
    expectedBack: null,
    notes,
  });
};

export const returnBookFromRepair = async (bookId: string): Promise<void> => {
  await httpClient.post(`/api/v1/admin/catalog/books/${bookId}/return-from-repair`);
};

export const removeBook = async (
  bookId: string,
  reason: RemovalReason,
  notes: string | null,
): Promise<void> => {
  await httpClient.post(`/api/v1/admin/catalog/books/${bookId}/remove`, { reason, notes });
};

export const restoreBook = async (bookId: string): Promise<void> => {
  await httpClient.post(`/api/v1/admin/catalog/books/${bookId}/restore`);
};
