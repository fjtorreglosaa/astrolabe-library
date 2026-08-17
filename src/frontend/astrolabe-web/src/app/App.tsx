import { QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter } from 'react-router-dom';
import { AuthProvider } from '../features/auth/components/AuthProvider';
import { AppRoutes } from '../routes/AppRoutes';
import { ThemeModeProvider } from '../theme/ThemeModeProvider';
import { queryClient } from './queryClient';
import { RealtimeProvider } from '../shared/realtime/RealtimeProvider';
import { SnackbarHost } from '../shared/feedback/SnackbarHost';

export const App = () => (
  <QueryClientProvider client={queryClient}>
    <ThemeModeProvider>
      <BrowserRouter>
        <AuthProvider>
          {/* Inside AuthProvider, because the connection only exists while somebody is signed in
              and carries their token. Above the routes, so it survives navigation instead of
              reconnecting on every page. */}
          <RealtimeProvider>
            <AppRoutes />
            <SnackbarHost />
          </RealtimeProvider>
        </AuthProvider>
      </BrowserRouter>
    </ThemeModeProvider>
  </QueryClientProvider>
);
