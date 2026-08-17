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
  /** Resolves with the user who just signed in, so a caller can route on their role at once. */
  signIn: (email: string, password: string) => Promise<CurrentUser>;
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

  /**
   * Signs in and returns who signed in.
   *
   * <p>
   * The user is fetched here rather than left to the query that renders the shell, because the
   * caller has to route on the role <em>before</em> the next render — and at that moment the context
   * still holds the previous user, or none. Reading `role` from the context right after awaiting
   * this would route the new person to the old person's screen.
   * </p>
   */
  const signIn = useCallback(
    async (email: string, password: string): Promise<CurrentUser> => {
      await signInRequest(email, password, getDeviceId());
      await resetServerState();

      // Primes the same cache entry the shell reads, so this costs one request rather than two.
      return queryClient.fetchQuery({ queryKey: ['auth', 'me'], queryFn: getCurrentUser });
    },
    [queryClient, resetServerState],
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
