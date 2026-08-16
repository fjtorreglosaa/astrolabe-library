import { useQuery, useQueryClient } from '@tanstack/react-query';
import { createContext, use, useCallback, useMemo, type ReactNode } from 'react';
import { getDeviceId } from '../../../shared/api/deviceId';
import {
  getCurrentUser,
  roleFromCode,
  signIn as signInRequest,
  signOut as signOutRequest,
  type CurrentUser,
  type UserRole,
} from '../api/authApi';

interface AuthContextValue {
  user: CurrentUser | null;
  role: UserRole | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  signIn: (email: string, password: string) => Promise<void>;
  signOut: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

/**
 * Holds the session for the whole application.
 *
 * The signed-in user comes from `/auth/me` rather than from the token: the role in a claim is a
 * snapshot from sign-in, and a plan change or a revoked role since then must be reflected. A 401
 * simply means "not signed in", so it is not retried.
 */
export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['auth', 'me'],
    queryFn: getCurrentUser,
    retry: false,
    staleTime: 60_000,
  });

  const signIn = useCallback(
    async (email: string, password: string) => {
      await signInRequest(email, password, getDeviceId());
      await queryClient.invalidateQueries({ queryKey: ['auth', 'me'] });
    },
    [queryClient],
  );

  const signOut = useCallback(async () => {
    await signOutRequest();
    // Everything cached was fetched as this user, so none of it may survive them.
    queryClient.clear();
  }, [queryClient]);

  const value = useMemo<AuthContextValue>(
    () => ({
      user: data ?? null,
      role: data ? roleFromCode(data.role) : null,
      isAuthenticated: Boolean(data),
      isLoading,
      signIn,
      signOut,
    }),
    [data, isLoading, signIn, signOut],
  );

  return <AuthContext value={value}>{children}</AuthContext>;
};

export const useAuth = (): AuthContextValue => {
  const context = use(AuthContext);

  if (!context) {
    throw new Error('useAuth must be used inside an AuthProvider.');
  }

  return context;
};
