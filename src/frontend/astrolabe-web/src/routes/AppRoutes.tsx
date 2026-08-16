import { Navigate, Route, Routes } from 'react-router-dom';
import { ProtectedRoute } from '../features/auth/components/ProtectedRoute';
import { RoleGuard, StaffRoles, SuperAdminRoles, PaidPlanRoles } from '../features/auth/components/RoleGuard';
import { LoginPage } from '../features/auth/pages/LoginPage';
import { SignUpPage } from '../features/auth/pages/SignUpPage';
import { VerifyEmailPage } from '../features/auth/pages/VerifyEmailPage';
import { DevicesAndSessionsPage } from '../features/sessions/pages/DevicesAndSessionsPage';
import { AppLayout } from '../layouts/AppLayout';
import { AuthLayout } from '../layouts/AuthLayout';
import { CatalogPage } from '../features/catalog/pages/CatalogPage';
import { AdminPaymentsPage } from '../features/billing/pages/AdminPaymentsPage';
import { FinesPage } from '../features/billing/pages/FinesPage';
import { HomePage } from '../features/reservations/pages/HomePage';
import { LoansPage } from '../features/reservations/pages/LoansPage';
import { MembershipPage } from '../features/membership/pages/MembershipPage';
import { PlaceholderScreen } from '../shared/components/PlaceholderScreen';

/**
 * The route table.
 *
 * Guards here improve the experience; they are never the security boundary. Every endpoint enforces
 * its own authorization, so a role that slips past a guard still gets a 403 from the API.
 */
export const AppRoutes = () => (
  <Routes>
    <Route element={<AuthLayout />}>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/signup" element={<SignUpPage />} />
      <Route path="/verify" element={<VerifyEmailPage />} />
      <Route path="/forgot-password" element={<PlaceholderScreen title="Reset your password" stage="Stage 1 — pending" />} />
      <Route path="/accept-invitation" element={<PlaceholderScreen title="Accept your invitation" stage="Stage 1 — pending" />} />
    </Route>

    <Route element={<ProtectedRoute />}>
      <Route element={<AppLayout />}>
        <Route path="/home" element={<HomePage />} />
        <Route path="/catalog" element={<CatalogPage />} />
        <Route path="/loans" element={<LoansPage />} />
        <Route path="/fines" element={<FinesPage />} />
        <Route path="/purchases" element={<PlaceholderScreen title="My purchases" stage="Stage 5" />} />
        <Route path="/support" element={<PlaceholderScreen title="Help & support" stage="Stage 9" />} />
        <Route path="/profile" element={<PlaceholderScreen title="My profile" stage="Stage 2" />} />
        <Route path="/settings" element={<PlaceholderScreen title="Settings" stage="Stage 2" />} />
        <Route path="/settings/devices" element={<DevicesAndSessionsPage />} />
        <Route path="/settings/membership" element={<MembershipPage />} />

        <Route element={<RoleGuard allow={PaidPlanRoles} />}>
          <Route path="/ai" element={<PlaceholderScreen title="AI recommendations" stage="Stage 7" />} />
        </Route>

        <Route element={<RoleGuard allow={StaffRoles} />}>
          <Route path="/admin/users" element={<PlaceholderScreen title="Users" stage="Stage 6" />} />
          <Route path="/admin/books" element={<PlaceholderScreen title="Book management" stage="Stage 6" />} />
          <Route path="/admin/payments" element={<AdminPaymentsPage />} />
          <Route path="/admin/support" element={<PlaceholderScreen title="Support tickets" stage="Stage 9" />} />
        </Route>

        <Route element={<RoleGuard allow={SuperAdminRoles} />}>
          <Route path="/admin/libraries" element={<PlaceholderScreen title="Libraries & admins" stage="Stage 6" />} />
        </Route>
      </Route>
    </Route>

    <Route path="/" element={<Navigate to="/home" replace />} />
    <Route path="*" element={<PlaceholderScreen title="Page not found" stage="—" />} />
  </Routes>
);
