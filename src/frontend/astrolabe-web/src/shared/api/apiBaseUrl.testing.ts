/**
 * Stands in for `apiBaseUrl.ts` under Jest, which cannot parse Vite's `import.meta`.
 *
 * The value is irrelevant to every test: requests are intercepted before they reach the network.
 */
export const API_BASE_URL = 'http://localhost:5080';
