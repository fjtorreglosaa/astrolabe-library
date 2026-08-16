import type { Config } from 'jest';

/**
 * Jest is mandated by GUIDELINES.md sections 7 and 58. Transformation uses SWC rather than ts-jest
 * because it type-checks separately (`npm run typecheck`) and keeps the suite fast.
 */
const config: Config = {
  testEnvironment: 'jsdom',
  setupFilesAfterEnv: ['<rootDir>/jest.setup.ts'],
  moduleNameMapper: {
    '\\.(css|less|scss)$': 'identity-obj-proxy',
    // Vite replaces import.meta.env at build time; Jest cannot parse it at all. The stub keeps the
    // HTTP client itself testable rather than excluding it from the suite.
    'apiBaseUrl$': '<rootDir>/src/shared/api/apiBaseUrl.testing.ts',
  },
  transform: {
    '^.+\\.(t|j)sx?$': [
      '@swc/jest',
      {
        jsc: {
          parser: { syntax: 'typescript', tsx: true },
          transform: { react: { runtime: 'automatic' } },
        },
      },
    ],
  },
  testMatch: ['<rootDir>/src/**/*.test.{ts,tsx}'],
  collectCoverageFrom: [
    'src/**/*.{ts,tsx}',
    '!src/main.tsx',
    '!src/shared/api/apiBaseUrl*.ts',
    '!src/**/*.d.ts',
  ],
  coverageThreshold: {
    // GUIDELINES.md section 56. Enforced from the first feature stage; Stage 0 ships only the shell.
    global: { statements: 80, branches: 80, functions: 80, lines: 80 },
  },
};

export default config;
