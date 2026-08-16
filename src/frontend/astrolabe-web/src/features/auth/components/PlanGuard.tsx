import { Navigate, Outlet } from 'react-router-dom';
import { LoadingState } from '../../../shared/components/StateViews';
import type { PlanTier } from '../../membership/api/membershipApi';
import { useAuth } from './AuthProvider';

/**
 * Hides routes that a member's plan does not include.
 *
 * Separate from `RoleGuard` although both redirect the same way: since GLOBAL-019 a role no longer
 * carries a tier, so guarding the AI surface with a list of roles is no longer expressible. Keeping
 * them apart also keeps the distinction visible at the call site — `/ai` is closed because of what
 * the member bought, `/admin/*` because of what they are.
 *
 * A convenience, not a boundary: the API refuses the call regardless.
 */
export const PlanGuard = ({ allow }: { allow: readonly PlanTier[] }) => {
  const { plan, isLoading } = useAuth();

  if (isLoading) {
    return <LoadingState />;
  }

  if (!plan || !allow.includes(plan)) {
    return <Navigate to="/home" replace />;
  }

  return <Outlet />;
};

/** The tiers that include the AI surface. Mirrors `requiresPaidPlan` in the navigation. */
export const PaidPlans = ['Plus', 'Max'] as const;
