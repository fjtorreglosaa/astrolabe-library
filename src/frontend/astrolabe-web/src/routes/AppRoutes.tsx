import { Navigate, Route, Routes } from 'react-router-dom';
import { ProtectedRoute } from '../features/auth/components/ProtectedRoute';
import { PaidPlans, PlanGuard } from '../features/auth/components/PlanGuard';
import { RoleGuard, StaffRoles, SuperAdminRoles } from '../features/auth/components/RoleGuard';
import { LoginPage } from '../features/auth/pages/LoginPage';
import { SignUpPage } from '../features/auth/pages/SignUpPage';
import { VerifyEmailPage } from '../features/auth/pages/VerifyEmailPage';
import { DevicesAndSessionsPage } from '../features/sessions/pages/DevicesAndSessionsPage';
import { AppLayout } from '../layouts/AppLayout';
import { AuthLayout } from '../layouts/AuthLayout';
import { CatalogPage } from '../features/catalog/pages/CatalogPage';
import { AdminPaymentsPage } from '../features/billing/pages/AdminPaymentsPage';
import { FinesPage } from '../features/billing/pages/FinesPage';
import { PurchasesPage } from '../features/store/pages/PurchasesPage';
import { HomePage } from '../features/reservations/pages/HomePage';
import { LoansPage } from '../features/reservations/pages/LoansPage';
import { MembershipPage } from '../features/membership/pages/MembershipPage';
import { PlaceholderScreen } from '../shared/components/PlaceholderScreen';
import { AdminUsersPage } from '../features/users/pages/AdminUsersPage';
import { AdminBooksPage } from '../features/admin-catalog/pages/AdminBooksPage';
import { AdminLibrariesPage } from '../features/network/pages/AdminLibrariesPage';
import { AcceptInvitationPage } from '../features/network/pages/AcceptInvitationPage';
import { AiRecommendationsPage } from '../features/recommendations/pages/AiRecommendationsPage';
import { AdminAiSettingsPage } from '../features/recommendations/pages/AdminAiSettingsPage';
import { NotificationSettingsPage } from '../features/notifications/pages/NotificationSettingsPage';
import { SupportPage } from '../features/support/pages/SupportPage';
import { ForgotPasswordPage } from '../features/auth/pages/ForgotPasswordPage';
import { ResetPasswordPage } from '../features/auth/pages/ResetPasswordPage';
import { SettingsPage } from '../features/settings/pages/SettingsPage';

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
      <Route path="/forgot-password" element={<ForgotPasswordPage />} />
      <Route path="/reset-password" element={<ResetPasswordPage />} />
      <Route path="/accept-invitation" element={<AcceptInvitationPage />} />
    </Route>

    <Route element={<ProtectedRoute />}>
      <Route element={<AppLayout />}>
        <Route path="/home" element={<HomePage />} />
        <Route path="/catalog" element={<CatalogPage />} />
        <Route path="/loans" element={<LoansPage />} />
        <Route path="/fines" element={<FinesPage />} />
        <Route path="/purchases" element={<PurchasesPage />} />
        <Route path="/support" element={<SupportPage />} />
        <Route path="/profile" element={<PlaceholderScreen title="My profile" stage="Stage 2" />} />
        <Route path="/settings" element={<SettingsPage />} />
        <Route path="/settings/devices" element={<DevicesAndSessionsPage />} />
        <Route path="/settings/notifications" element={<NotificationSettingsPage />} />
        <Route path="/settings/membership" element={<MembershipPage />} />

        <Route element={<PlanGuard allow={PaidPlans} />}>
          <Route path="/ai" element={<AiRecommendationsPage />} />
        </Route>

        <Route element={<RoleGuard allow={StaffRoles} />}>
          <Route path="/admin/users" element={<AdminUsersPage />} />
          <Route path="/admin/books" element={<AdminBooksPage />} />
          <Route path="/admin/payments" element={<AdminPaymentsPage />} />
          <Route path="/admin/ai" element={<AdminAiSettingsPage />} />
          <Route path="/admin/support" element={<SupportPage />} />
        </Route>

        <Route element={<RoleGuard allow={SuperAdminRoles} />}>
          <Route path="/admin/libraries" element={<AdminLibrariesPage />} />
        </Route>
      </Route>
    </Route>

    <Route path="/" element={<Navigate to="/home" replace />} />
    <Route path="*" element={<PlaceholderScreen title="Page not found" stage="—" />} />
  </Routes>
);
