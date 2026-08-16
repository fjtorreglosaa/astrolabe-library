import { useQuery, useQueryClient } from '@tanstack/react-query';
import { createContext, use, useCallback, useMemo, type ReactNode } from 'react';
import { getDeviceId } from '../../../shared/api/deviceId';
import {
  getCurrentUser,
  signIn as signInRequest,
  signOut as signOutRequest,
  type CurrentUser,
  type UserRole,
} from '../api/authApi';
import type { PlanTier } from '../../membership/api/membershipApi';

interface AuthContextValue {
  user: CurrentUser | null;
  role: UserRole | null;
  /** The signed-in member's plan, or null for staff and for nobody. */
  plan: PlanTier | null;
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
 * snapshot from sign-in, and a revoked role since then must be reflected. The plan is not in the
 * token at all — it changes far more often than a session lives. A 401 simply means "not signed
 * in", so it is not retried.
 */
export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['auth', 'me'],
    queryFn: getCurrentUser,
    retry: false,
    staleTime: 60_000,
  });

  /**
   * Discards every cached response and refetches whatever is on screen.
   *
   * Called on both sides of an identity change, because everything in the cache was fetched *as*
   * somebody — a catalogue verdict, a membership, a session list — and none of it survives them.
   *
   * `resetQueries` rather than `clear`: `clear` empties the cache but leaves mounted observers
   * holding their last result, so the interface keeps rendering the previous user until the page is
   * reloaded by hand. It also removes the entries that a later `invalidateQueries` would need to
   * match, so the refetch that was supposed to load the new user never fired at all.
   */
  const resetServerState = useCallback(() => queryClient.resetQueries(), [queryClient]);

  const signIn = useCallback(
    async (email: string, password: string) => {
      await signInRequest(email, password, getDeviceId());
      await resetServerState();
    },
    [resetServerState],
  );

  const signOut = useCallback(async () => {
    await signOutRequest();
    await resetServerState();
  }, [resetServerState]);

  const value = useMemo<AuthContextValue>(
    () => ({
      user: data ?? null,
      role: data ? data.role : null,
      plan: data ? data.plan : null,
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
