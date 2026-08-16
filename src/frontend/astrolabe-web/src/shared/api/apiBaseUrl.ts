/**
 * Where the API lives, baked in at build time.
 *
 * Isolated in its own module because `import.meta.env` is a Vite construct: it is replaced during
 * the build and is a syntax error anywhere else, which puts every file that touches it out of reach
 * of the test runner. Keeping it here means the HTTP client — which does have behaviour worth
 * testing — stays plain TypeScript.
 */
export const API_BASE_URL: string = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080';
