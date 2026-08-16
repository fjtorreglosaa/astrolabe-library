import { httpClient } from '../../../shared/api/httpClient';
import type { PlanTier } from '../../membership/api/membershipApi';

/** The prototype's own genre list, as the API names them. */
export type Genre =
  | 'Fiction'
  | 'Essay'
  | 'ScienceFiction'
  | 'History'
  | 'Biography'
  | 'Technical';

/** The single reason a book card shows when it cannot be reserved. */
export type BookRejection =
  | 'AllCopiesOut'
  | 'NotInBasicPlan'
  | 'HomeLibraryOnly'
  | 'NotInCity'
  | 'Unavailable';

/** Why one branch's copy cannot be reserved. */
export type CopyRejection =
  | 'OutOfStock'
  | 'NotInBasicCatalog'
  | 'HomeLibraryOnly'
  | 'OutsideCity';

export interface BookSummary {
  id: string;
  isbn: string;
  title: string;
  author: string;
  genre: Genre;
  tier: PlanTier;
  retailPriceCents: number;
  coverUrl: string | null;
  averageRating: number | null;
  reviewCount: number;
  availableCount: number;
  totalCount: number;
  canReserve: boolean;
  badge: BookRejection | null;
}

export interface CopyAvailability {
  libraryId: string;
  libraryName: string;
  cityName: string;
  availableCount: number;
  totalCount: number;
  canReserve: boolean;
  reason: CopyRejection | null;
}

export interface BookDetail extends Omit<BookSummary, 'availableCount' | 'totalCount'> {
  publisher: string | null;
  copies: CopyAvailability[];
}

export interface Review {
  id: string;
  memberId: string;
  memberName: string;
  initials: string;
  rating: number;
  comment: string | null;
  createdAt: string;
  editedAt: string | null;
  isMine: boolean;
}

export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  isEmpty: boolean;
}

/** The prototype's sortable columns. Every header it offers has a key here and no key is orphaned. */
export type BookSortKey =
  | 'Title'
  | 'Author'
  | 'Genre'
  | 'Tier'
  | 'Availability'
  | 'Rating'
  | 'Price';

export type SortDirection = 'Ascending' | 'Descending';

export interface SearchBooksParams {
  term?: string;
  genre?: Genre;
  sortBy?: BookSortKey;
  direction?: SortDirection;
  page?: number;
  pageSize?: number;
}

export const searchBooks = async (params: SearchBooksParams): Promise<Paged<BookSummary>> => {
  const { data } = await httpClient.get<Paged<BookSummary>>('/api/v1/catalog/books', {
    // Empty values are dropped rather than sent blank: the API treats an empty term as no term,
    // and sending one anyway makes the request URL misreport what was searched.
    params: {
      term: params.term?.trim() || undefined,
      genre: params.genre,
      // Sorting is server-side because the results are paged: ordering the page the API already
      // chose would sort twenty rows out of two hundred and answer a different question.
      sortBy: params.sortBy ?? 'Title',
      direction: params.direction ?? 'Ascending',
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 12,
    },
  });

  return data;
};

export const getBook = async (bookId: string): Promise<BookDetail> => {
  const { data } = await httpClient.get<BookDetail>(`/api/v1/catalog/books/${bookId}`);
  return data;
};

export const getReviews = async (bookId: string): Promise<Paged<Review>> => {
  const { data } = await httpClient.get<Paged<Review>>(`/api/v1/catalog/books/${bookId}/reviews`);
  return data;
};

export const publishReview = async (
  bookId: string,
  rating: number,
  comment: string | null,
): Promise<void> => {
  await httpClient.put(`/api/v1/catalog/books/${bookId}/review`, { rating, comment });
};

export const removeReview = async (bookId: string): Promise<void> => {
  await httpClient.delete(`/api/v1/catalog/books/${bookId}/review`);
};
