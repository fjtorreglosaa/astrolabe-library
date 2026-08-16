import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { act, render, screen, waitFor } from '@testing-library/react';
import MockAdapter from 'axios-mock-adapter';
import { httpClient, setAccessToken } from '../../../shared/api/httpClient';
import { AuthProvider, useAuth } from './AuthProvider';

/**
 * Covers switching accounts, which is where cached server state is most dangerous.
 *
 * Signing out and back in as somebody else must never leave a single field of the previous user on
 * screen. The interface decides what a member may see from the role it holds, so a stale role does
 * not merely look wrong — it shows an administrator the member's application, or the reverse.
 */

const MEMBER = {
  id: 'e68049f0-38b0-4fbd-be75-5863470a94ca',
  email: 'fjtorreglosaa@gmail.com',
  fullName: 'Francisco Torreglosa',
  role: 'Plus',
  countryId: 'c1',
  cityId: 'c2',
  isStaff: false,
};

const ADMIN = {
  id: '37518163-5a1b-43eb-9d7b-25823602ea2f',
  email: 'admin@astrolabe.co',
  fullName: 'Dana Whitfield',
  role: 'Admin',
  countryId: null,
  cityId: null,
  isStaff: true,
};

/** Exposes what the shell renders from: the role and the display name. */
const Probe = () => {
  const { role, user, signIn, signOut } = useAuth();

  return (
    <div>
      <span data-testid="role">{role ?? 'none'}</span>
      <span data-testid="name">{user?.fullName ?? 'none'}</span>
      <button type="button" onClick={() => void signIn('a@b.co', 'x')}>
        in
      </button>
      <button type="button" onClick={() => void signOut()}>
        out
      </button>
    </div>
  );
};

describe('AuthProvider account switching', () => {
  let mock: MockAdapter;
  let client: QueryClient;
  let currentUser: typeof MEMBER | typeof ADMIN | null;

  beforeEach(() => {
    currentUser = null;
    setAccessToken(null);

    mock = new MockAdapter(httpClient);
    mock.onPost('/api/v1/auth/sign-in').reply(() => [200, { accessToken: 'token', expiresAt: '' }]);
    mock.onPost('/api/v1/auth/sign-out').reply(() => {
      currentUser = null;
      return [204];
    });
    // The API answers from the token, so the double simply reports whoever signed in last.
    mock.onGet('/api/v1/auth/me').reply(() =>
      currentUser ? [200, currentUser] : [401, { status: 401 }],
    );

    client = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });
  });

  afterEach(() => {
    mock.restore();
    client.clear();
    setAccessToken(null);
  });

  const renderProbe = () =>
    render(
      <QueryClientProvider client={client}>
        <AuthProvider>
          <Probe />
        </AuthProvider>
      </QueryClientProvider>,
    );

  const signInAs = async (who: typeof MEMBER | typeof ADMIN) => {
    currentUser = who;
    await act(async () => {
      screen.getByText('in').click();
    });
  };

  const signOutNow = async () => {
    await act(async () => {
      screen.getByText('out').click();
    });
  };

  it('reports the member after signing in', async () => {
    renderProbe();
    await signInAs(MEMBER);

    await waitFor(() => expect(screen.getByTestId('role')).toHaveTextContent('Plus'));
    expect(screen.getByTestId('name')).toHaveTextContent('Francisco Torreglosa');
  });

  it('keeps nothing of the member after signing out', async () => {
    renderProbe();
    await signInAs(MEMBER);
    await waitFor(() => expect(screen.getByTestId('role')).toHaveTextContent('Plus'));

    await signOutNow();

    await waitFor(() => expect(screen.getByTestId('role')).toHaveTextContent('none'));
    expect(screen.getByTestId('name')).toHaveTextContent('none');
  });

  it('shows the administrator, not the member, after switching accounts', async () => {
    // The reported defect: sign in as the member, sign out, sign in as the administrator, and the
    // member's interface stays on screen until the page is reloaded by hand.
    renderProbe();

    await signInAs(MEMBER);
    await waitFor(() => expect(screen.getByTestId('role')).toHaveTextContent('Plus'));

    await signOutNow();
    await signInAs(ADMIN);

    await waitFor(() => expect(screen.getByTestId('role')).toHaveTextContent('Admin'));
    expect(screen.getByTestId('name')).toHaveTextContent('Dana Whitfield');
  });

  it('shows the administrator even when the member never signed out first', async () => {
    // A second sign-in without a sign-out is reachable: the token expires, the form reappears, and
    // somebody else uses the same browser.
    renderProbe();

    await signInAs(MEMBER);
    await waitFor(() => expect(screen.getByTestId('role')).toHaveTextContent('Plus'));

    await signInAs(ADMIN);

    await waitFor(() => expect(screen.getByTestId('role')).toHaveTextContent('Admin'));
  });
});
