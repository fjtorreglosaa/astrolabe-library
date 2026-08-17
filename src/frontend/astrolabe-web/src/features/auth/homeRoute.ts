import type { UserRole } from './api/authApi';

/**
 * Where a signed-in user belongs when no particular screen was asked for.
 *
 * <p>
 * There is no single home. `/home` is the <b>member</b> dashboard, and every endpoint behind it —
 * the reservations summary, the membership, the fines, the points — answers 403 to staff, who hold
 * no plan and no loans. Sending an administrator there produces four failed requests and a screen of
 * errors, which is what happens when one route is treated as everybody's landing place.
 * </p>
 * <p>
 * Used in three places that must agree: the redirect after signing in, and both guards when they
 * turn somebody away. A guard that bounced staff to the member dashboard would be sending them from
 * one screen they cannot use to another.
 * </p>
 */
export const homeRouteFor = (role: UserRole | null): string =>
  role === 'Admin' || role === 'SuperAdmin' ? '/admin/users' : '/home';
