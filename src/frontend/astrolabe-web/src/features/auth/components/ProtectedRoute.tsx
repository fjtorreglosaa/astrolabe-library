import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { LoadingState } from '../../../shared/components/StateViews';
import { useAuth } from './AuthProvider';

/**
 * Keeps anonymous visitors out of the application shell.
 *
 * This improves the experience; it is **not** a security boundary. Every endpoint enforces its own
 * authorization, because anything decided in a browser can be bypassed in a browser.
 */
export const ProtectedRoute = () => {
  const { isAuthenticated, isLoading } = useAuth();
  const location = useLocation();

  if (isLoading) {
    return <LoadingState label="Checking your session…" />;
  }

  if (!isAuthenticated) {
    // The attempted path is remembered so sign-in can return the visitor to it.
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  return <Outlet />;
};
