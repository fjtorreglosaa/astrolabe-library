import { Navigate, Outlet } from 'react-router-dom';
import { LoadingState } from '../../../shared/components/StateViews';
import type { UserRole } from '../api/authApi';
import { useAuth } from './AuthProvider';

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
    return <Navigate to="/home" replace />;
  }

  return <Outlet />;
};

export const StaffRoles = ['Admin', 'SuperAdmin'] as const;
export const SuperAdminRoles = ['SuperAdmin'] as const;
