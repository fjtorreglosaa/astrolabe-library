import { httpClient } from '../../../shared/api/httpClient';
import type { UserRole } from '../../auth/api/authApi';
import type { UserStatus } from '../../users/api/usersApi';
import type { CreatedResource } from '../../../shared/api/created';

export interface Library {
  id: string;
  cityId: string;
  name: string;
  isActive: boolean;
  isCityHomeLibrary: boolean;
}

/** Only ids. Names come from the library list, which is why callers usually want both. */
export interface LibraryScope {
  isUnrestricted: boolean;
  libraryIds: string[];
}

export interface Admin {
  id: string;
  email: string;
  fullName: string;
  role: UserRole;
  status: UserStatus;
  libraries: string[];
  since: string;
}

/** What a deactivated branch was still holding. A report, never a refusal — see BR-NET-005. */
export interface LibraryObligations {
  copies: number;
  activeReservations: number;
  unresolvedFines: number;
  hasAny: boolean;
}

export const getLibraries = async (cityId?: string): Promise<Library[]> => {
  const { data } = await httpClient.get<Library[]>('/api/v1/network/libraries', {
    params: { cityId },
  });
  return data;
};

export const getMyScope = async (): Promise<LibraryScope> => {
  const { data } = await httpClient.get<LibraryScope>('/api/v1/network/my-scope');
  return data;
};

/**
 * The libraries a staff caller may act on, with their names.
 *
 * Two calls rather than one because the API keeps them apart on purpose: the scope is a set of
 * identifiers and the list is geography, and the day one of them is cached differently the other
 * still has to be right. Combining them here keeps that seam in one place instead of in every
 * screen that needs a branch picker.
 */
export const getAdministeredLibraries = async (): Promise<Library[]> => {
  const [libraries, scope] = await Promise.all([getLibraries(), getMyScope()]);

  return scope.isUnrestricted
    ? libraries
    : libraries.filter((library) => scope.libraryIds.includes(library.id));
};

export const getAdmins = async (): Promise<Admin[]> => {
  const { data } = await httpClient.get<Admin[]>('/api/v1/network/admins');
  return data;
};

export const inviteAdmin = async (input: {
  email: string;
  fullName: string;
  role: UserRole;
  libraryIds: string[];
  message: string | null;
}): Promise<string> => {
  const { data } = await httpClient.post<CreatedResource>('/api/v1/network/admins', input);
  return data.id;
};

/**
 * BR-NET-015. Issues a fresh invitation and kills every one still outstanding for that account.
 *
 * Takes the user rather than an invitation, because that is what the console has in front of it and
 * because the rule is about the account, not about one link somebody happened to name.
 */
export const resendInvitation = async (userId: string): Promise<string> => {
  const { data } = await httpClient.post<CreatedResource>(
    `/api/v1/network/admins/${userId}/resend-invitation`,
  );
  return data.id;
};

/**
 * The "grant extended powers" half of BR-NET-008: raises an administrator to super administrator.
 *
 * There is no route back on purpose. BR-NET-012 keeps the network from ever being left without a
 * super administrator, and a demotion command would let two of them undo each other into that state.
 */
export const grantSuperAdmin = async (userId: string): Promise<void> => {
  await httpClient.post(`/api/v1/network/admins/${userId}/grant-super-admin`);
};

export const assignLibraries = async (userId: string, libraryIds: string[]): Promise<void> => {
  await httpClient.put(`/api/v1/network/admins/${userId}/libraries`, { libraryIds });
};

export const revokeAdmin = async (userId: string): Promise<void> => {
  await httpClient.delete(`/api/v1/network/admins/${userId}`);
};

export const createLibrary = async (cityId: string, name: string): Promise<string> => {
  const { data } = await httpClient.post<CreatedResource>('/api/v1/network/libraries', {
    cityId,
    name,
  });
  return data.id;
};

/** Returns what the branch still held. BR-NET-005: reported to the operator, never a refusal. */
export const deactivateLibrary = async (libraryId: string): Promise<LibraryObligations> => {
  const { data } = await httpClient.delete<LibraryObligations>(
    `/api/v1/network/libraries/${libraryId}`,
  );
  return data;
};

/**
 * BR-NET-013. Anonymous: the invitee has no account to sign into yet, and the token is the only
 * thing proving they own the address.
 *
 * The password is sent exactly as typed. It is never trimmed — a password may legitimately contain
 * spaces, and removing them would lock somebody out of the account they had just created.
 */
export const acceptInvitation = async (token: string, password: string): Promise<void> => {
  await httpClient.post('/api/v1/network/admins/accept-invitation', { token, password });
};
