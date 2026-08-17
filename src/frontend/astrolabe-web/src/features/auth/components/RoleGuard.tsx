import { Navigate, Outlet } from 'react-router-dom';
import { LoadingState } from '../../../shared/components/StateViews';
import type { UserRole } from '../api/authApi';
import { useAuth } from './AuthProvider';
import { homeRouteFor } from '../homeRoute';

/**
 * Hides routes a role has no business seeing. Like ProtectedRoute, a convenience rather than a
 * boundary: the API refuses the request regardless.
 */
export const RoleGuard = ({ allow }: { allow: readonly UserRole[] }) => {
  const { role, isLoading } = useAuth();

  if (isLoading) {
    return <LoadingState />;
  }

  if (!role || !allow.includes(role)) {
    // Their own home, not one fixed route. `/home` is the member dashboard, so bouncing staff there
    // sent them from a screen they may not see to one whose every request answers 403.
    return <Navigate to={homeRouteFor(role)} replace />;
  }

  return <Outlet />;
};

export const StaffRoles = ['Admin', 'SuperAdmin'] as const;
export const SuperAdminRoles = ['SuperAdmin'] as const;

/**
 * Screens that belong to a member and nobody else — the dashboard, the catalogue, loans, fines,
 * purchases, the profile and the plan.
 *
 * <p>
 * These needed a guard as much as the admin screens did. Every endpoint behind them refuses staff,
 * so an administrator who reached one by a stale link or a remembered path got a page of failed
 * requests rather than a redirect. Guarding them is also what makes it safe for sign-in to honour a
 * path remembered from somebody else's session.
 * </p>
 */
export const MemberRoles = ['Member'] as const;
