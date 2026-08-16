import MockAdapter from 'axios-mock-adapter';
import { getAccessToken, httpClient, setAccessToken } from './httpClient';

/**
 * Covers the response interceptor's decision about *when* to refresh.
 *
 * The rule it encodes is not obvious from the status code alone: a 401 from an endpoint that
 * consumes a session means the access token expired, while a 401 from one that establishes a session
 * means the credentials were wrong. Treating the second as the first is how a mistyped password
 * turned into a refresh call — and, with an earlier session's cookie still valid, could have left
 * the application signed in as the previous user.
 */
describe('httpClient refresh interceptor', () => {
  let mock: MockAdapter;

  beforeEach(() => {
    mock = new MockAdapter(httpClient);
    setAccessToken(null);
  });

  afterEach(() => {
    mock.restore();
    setAccessToken(null);
  });

  it('does not refresh when sign-in is rejected', async () => {
    mock.onPost('/api/v1/auth/sign-in').reply(401, {
      title: 'The email address or password is incorrect.',
      status: 401,
    });
    mock.onPost('/api/v1/auth/refresh').reply(200, { accessToken: 'someone-elses-token' });

    await expect(
      httpClient.post('/api/v1/auth/sign-in', { email: 'a@b.co', password: 'wrong' }),
    ).rejects.toMatchObject({ response: { status: 401 } });

    const refreshes = mock.history.filter((call) => call.url?.includes('/auth/refresh'));
    expect(refreshes).toHaveLength(0);
  });

  it('never adopts a token from a refresh triggered by a failed sign-in', async () => {
    // The dangerous case: a stale but valid session cookie. Refreshing here would hand the
    // application a working token for whoever was signed in before.
    mock.onPost('/api/v1/auth/sign-in').reply(401, { status: 401 });
    mock.onPost('/api/v1/auth/refresh').reply(200, { accessToken: 'previous-user-token' });

    await expect(httpClient.post('/api/v1/auth/sign-in', {})).rejects.toBeDefined();

    expect(getAccessToken()).toBeNull();
  });

  it('does not refresh when accepting an invitation is rejected', async () => {
    // The entry for this route read '/auth/accept-invitation' until Stage 6 built the screen, and
    // no such route exists — so the exclusion matched nothing. It costs nothing today, because the
    // endpoint answers 409 and 400 rather than 401, and it would cost everything on the day that
    // changed: an invitee whose browser still holds a previous session's cookie would be refreshed
    // into somebody else's account while the form showed an error.
    mock.onPost('/api/v1/network/admins/accept-invitation').reply(401, { status: 401 });
    mock.onPost('/api/v1/auth/refresh').reply(200, { accessToken: 'someone-elses-token' });

    await expect(
      httpClient.post('/api/v1/network/admins/accept-invitation', {
        token: 'stale',
        password: 'a-long-enough-password',
      }),
    ).rejects.toMatchObject({ response: { status: 401 } });

    expect(mock.history.filter((call) => call.url?.includes('/auth/refresh'))).toHaveLength(0);
  });

  it('refreshes and retries once when an authenticated call expires', async () => {
    setAccessToken('expired');

    let attempts = 0;
    mock.onGet('/api/v1/catalog/books').reply(() => {
      attempts += 1;
      return attempts === 1 ? [401, {}] : [200, { items: [] }];
    });
    mock.onPost('/api/v1/auth/refresh').reply(200, { accessToken: 'fresh' });

    const response = await httpClient.get('/api/v1/catalog/books');

    expect(response.status).toBe(200);
    expect(attempts).toBe(2);
    expect(getAccessToken()).toBe('fresh');
  });

  it('gives up rather than looping when the retry is rejected too', async () => {
    setAccessToken('expired');

    mock.onGet('/api/v1/catalog/books').reply(401, {});
    mock.onPost('/api/v1/auth/refresh').reply(200, { accessToken: 'fresh' });

    await expect(httpClient.get('/api/v1/catalog/books')).rejects.toBeDefined();

    const attempts = mock.history.filter((call) => call.url?.includes('/catalog/books'));
    expect(attempts).toHaveLength(2);
  });

  it('clears the token when the refresh itself is rejected', async () => {
    setAccessToken('expired');

    mock.onGet('/api/v1/catalog/books').reply(401, {});
    mock.onPost('/api/v1/auth/refresh').reply(401, {});

    await expect(httpClient.get('/api/v1/catalog/books')).rejects.toBeDefined();

    expect(getAccessToken()).toBeNull();
  });

  it('does not refresh on a 403, which is an authorization answer rather than an expiry', async () => {
    setAccessToken('valid');

    mock.onGet('/api/v1/admin/catalog/books').reply(403, {});
    mock.onPost('/api/v1/auth/refresh').reply(200, { accessToken: 'fresh' });

    await expect(httpClient.get('/api/v1/admin/catalog/books')).rejects.toBeDefined();

    const refreshes = mock.history.filter((call) => call.url?.includes('/auth/refresh'));
    expect(refreshes).toHaveLength(0);
    expect(getAccessToken()).toBe('valid');
  });
});
