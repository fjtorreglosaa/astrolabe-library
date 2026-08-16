import { httpClient } from '../../../shared/api/httpClient';
import type { Paged } from '../../catalog/api/catalogApi';
import type { UserRole } from '../../auth/api/authApi';
import type { PlanTier } from '../../membership/api/membershipApi';

/** Account lifecycle, as the API names it. Mirrors the prototype's four status chips. */
export type UserStatus = 'PendingVerification' | 'Active' | 'Blocked' | 'Deleted' | 'Invited';

/** Sortable columns, matching the prototype's table headers. */
export type UserSortKey = 'CreatedAt' | 'FullName' | 'Email' | 'Role' | 'Status';

export type SortDirection = 'Ascending' | 'Descending';

/** What the directory does to an account. One route, one action, four verbs. */
export type UserAdministrationAction = 'Block' | 'Unblock' | 'Delete' | 'Restore';

export interface UserSummary {
  id: string;
  fullName: string;
  email: string;
  role: UserRole;
  /** Null for staff, who hold no plan. A field of its own since GLOBAL-019. */
  plan: PlanTier | null;
  status: UserStatus;
  cityName: string | null;
  homeLibraryName: string | null;
  createdAt: string;
  /**
   * Whether the caller may act on this row. Decided by the server so the screen cannot offer a
   * button the API would refuse; the handler checks again before acting.
   */
  canAdminister: boolean;
}

export interface UserDetail extends UserSummary {
  lastActiveAt: string | null;
  activeReservations: number;
  outstandingFineCents: number;
  purchases: number;
  /** Null when nothing has been returned yet — an em dash, not 0%. */
  onTimeReturnPercent: number | null;
  /** Why the actions are unavailable, or null when they are not. */
  administrationBlockedReason: string | null;
}

export interface UserSearch {
  term?: string;
  status?: UserStatus;
  role?: UserRole;
  includeDeleted?: boolean;
  sortBy?: UserSortKey;
  direction?: SortDirection;
  page?: number;
  pageSize?: number;
}

export const searchUsers = async (search: UserSearch): Promise<Paged<UserSummary>> => {
  const { data } = await httpClient.get<Paged<UserSummary>>('/api/v1/users', {
    // Undefined keys are dropped by axios, so an absent filter genuinely means "no filter" rather
    // than an empty string the API would have to interpret.
    params: {
      term: search.term || undefined,
      status: search.status,
      role: search.role,
      includeDeleted: search.includeDeleted,
      sortBy: search.sortBy,
      direction: search.direction,
      page: search.page ?? 1,
      pageSize: search.pageSize ?? 20,
    },
  });

  return data;
};

export const getUserDetail = async (userId: string): Promise<UserDetail> => {
  const { data } = await httpClient.get<UserDetail>(`/api/v1/users/${userId}`);
  return data;
};

export const administerUser = async (
  userId: string,
  action: UserAdministrationAction,
): Promise<void> => {
  await httpClient.post(`/api/v1/users/${userId}/administer`, { action });
};

export const resendVerificationForUser = async (userId: string): Promise<void> => {
  await httpClient.post(`/api/v1/users/${userId}/resend-verification`);
};
