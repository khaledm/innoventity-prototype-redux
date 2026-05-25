import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright Nightly Configuration — Pattern D (Post-Deploy Production Smoke)
 *
 * Targets the live Azure Static Web Apps production URL. Runs nightly via
 * `.github/workflows/e2e-nightly.yml` on a cron schedule (03:00 UTC) and on
 * manual `workflow_dispatch`.
 *
 * Key differences from `playwright.config.ts` (local dev config):
 *  - No `webServer` block — production is already running
 *  - `testMatch` scoped to `nightly.spec.ts` only — skips journey-1 (requires
 *    `activationToken` in API response, which is suppressed in production)
 *  - URLs driven by env vars so `workflow_dispatch` can target any environment
 *  - JUnit reporter included for `dorny/test-reporter` integration in CI
 *  - `retries: 2` for transient network resilience against CDN propagation
 *
 * Environment variables (all optional — defaults target production):
 *   BASE_URL        — Angular SPA URL  (default: https://innoventity-dev-web.azurestaticapps.net)
 *   E2E_API_URL     — Backend API URL  (default: https://innoventity-dev-api.azurewebsites.net)
 *   E2E_NIGHTLY_EMAIL    — Pre-seeded test account email (required at runtime)
 *   E2E_NIGHTLY_PASSWORD — Pre-seeded test account password (required at runtime)
 */
export default defineConfig({
  testDir: './e2e',

  /* Run only nightly-safe tests — journey-1.spec.ts requires activationToken
     which the production API does not expose in responses */
  testMatch: ['**/nightly.spec.ts'],

  /* Run tests sequentially — production has rate-limiting and one worker is
     sufficient for a small smoke suite */
  fullyParallel: false,

  /* Fail the build on CI if test.only was accidentally left in source */
  forbidOnly: !!process.env.CI,

  /* Retry twice to absorb CDN propagation delays and transient network blips */
  retries: process.env.CI ? 2 : 0,

  workers: 1,

  /* Dual reporters in CI: GitHub annotations + JUnit XML for dorny/test-reporter */
  reporter: process.env.CI
    ? [
        ['github'],
        ['junit', { outputFile: 'test-results/nightly-results.xml' }],
      ]
    : 'html',

  use: {
    /* Resolved at runtime from env var so workflow_dispatch can override */
    baseURL: process.env.BASE_URL ?? 'https://innoventity-dev-web.azurestaticapps.net',

    /* Capture diagnostic artefacts on failure */
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',

    /* Production HTTPS — do not suppress certificate errors */
    ignoreHTTPSErrors: false,

    /* Allow generous timeouts for CDN cold paths */
    actionTimeout: 30_000,
    navigationTimeout: 60_000,
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  /* No webServer — the production deployment is already live */
});
