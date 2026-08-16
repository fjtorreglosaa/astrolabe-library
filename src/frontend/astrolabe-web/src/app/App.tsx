import { QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter } from 'react-router-dom';
import { AuthProvider } from '../features/auth/components/AuthProvider';
import { AppRoutes } from '../routes/AppRoutes';
import { ThemeModeProvider } from '../theme/ThemeModeProvider';
import { queryClient } from './queryClient';

export const App = () => (
  <QueryClientProvider client={queryClient}>
    <ThemeModeProvider>
      <BrowserRouter>
        <AuthProvider>
          <AppRoutes />
        </AuthProvider>
      </BrowserRouter>
    </ThemeModeProvider>
  </QueryClientProvider>
);
