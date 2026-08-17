import '@testing-library/jest-dom';
import { TextDecoder, TextEncoder } from 'node:util';

// jsdom ships neither, and react-router reaches for TextEncoder as soon as it is imported. Without
// these, any test that renders a component containing a link fails before it runs — which is why
// the layout components had been tested around their router rather than through it.
Object.assign(globalThis, {
  TextEncoder: globalThis.TextEncoder ?? TextEncoder,
  TextDecoder: globalThis.TextDecoder ?? TextDecoder,
});

// jsdom does not implement matchMedia, which MUI queries for responsive behaviour.
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  }),
});
