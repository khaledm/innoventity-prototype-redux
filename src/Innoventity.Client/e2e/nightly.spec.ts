import { test, expect, type APIRequestContext } from '@playwright/test';

/**
 * Nightly Production Smoke Tests — Pattern D (Post-Deploy E2E)
 *
 * Runs nightly against the live production deployment via `e2e-nightly.yml`.
 * See `playwright.config.nightly.ts` for URL and reporter configuration.
 *
 * Design constraints (vs journey-1.spec.ts):
 *
 *  1. NO registration or activation — the production API does not return
 *     `activationToken` in responses (it is email-only in production). Tests
 *     use a pre-seeded test account whose credentials are supplied via GitHub
 *     Secrets (`E2E_NIGHTLY_EMAIL`, `E2E_NIGHTLY_PASSWORD`).
 *
 *  2. Self-contained innovation data — tests create a fresh Draft innovation
 *     via API each run so no pre-existing data is required. Draft innovations
 *     are inert (not visible to other actors) and are an acceptable footprint
 *     for a production smoke suite.
 *
 *  3. Relative URLs — `page.goto()` calls use paths (e.g. `/innovations/…`)
 *     which Playwright resolves against `baseURL` from the nightly config.
 *     This makes every test environment-portable via `workflow_dispatch`.
 *
 *  4. localStorage seeding — access token is injected via `addInitScript`
 *     before the first navigation so the Angular auth interceptor picks it up
 *     on startup (avoids the 401 that occurred before the fix in 753f85b).
 *
 * Required GitHub Secrets (configured once per environment):
 *   E2E_NIGHTLY_EMAIL    — email of the pre-seeded production test account
 *   E2E_NIGHTLY_PASSWORD — password of the pre-seeded production test account
 *
 * Required environment variables (set in e2e-nightly.yml):
 *   E2E_API_URL  — backend API base URL (https://innoventity-dev-api.azurewebsites.net)
 *   BASE_URL     — Angular SPA URL      (https://innoventity-dev-web.azurestaticapps.net)
 */

// ---------------------------------------------------------------------------
// Configuration — resolved at runtime from env vars
// ---------------------------------------------------------------------------

const API_BASE_URL =
  process.env['E2E_API_URL'] ?? 'https://innoventity-dev-api.azurewebsites.net';

const NIGHTLY_EMAIL = process.env['E2E_NIGHTLY_EMAIL'];
const NIGHTLY_PASSWORD = process.env['E2E_NIGHTLY_PASSWORD'];

// ---------------------------------------------------------------------------
// Shared API helpers (scoped to nightly — do not share with journey-1.spec.ts)
// ---------------------------------------------------------------------------

interface LoginRequest {
  email: string;
  actorType: string;
  password: string;
}

interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  tokenType: string;
  actor: {
    actorId: string;
    email: string;
    firstName: string;
    lastName: string;
    displayName: string;
    actorType: string;
  };
}

interface CreateInnovationResponse {
  innovationId: string;
  ideaToken: string;
  status: string;
  createdAt: string;
  ownerId: string;
}

async function apiLogin(
  request: APIRequestContext,
  email: string,
  password: string
): Promise<LoginResponse> {
  const response = await request.post(`${API_BASE_URL}/auth/login`, {
    data: { email, actorType: 'IdeaGenerator', password } as LoginRequest,
  });

  if (response.status() !== 200) {
    const body = await response.text();
    throw new Error(`Login failed (${response.status()}): ${body}`);
  }

  return response.json() as Promise<LoginResponse>;
}

async function apiCreateInnovation(
  request: APIRequestContext,
  accessToken: string,
  title: string
): Promise<CreateInnovationResponse> {
  const response = await request.post(`${API_BASE_URL}/innovations`, {
    headers: { Authorization: `Bearer ${accessToken}` },
    data: {
      title,
      productType: 'Prototype',
      researchCategory: 'Engineering',
      researchBackground: 'Nightly smoke test — auto-generated Draft, safe to ignore.',
      hasIPR: false,
      hasRightToUse: true,
    },
  });

  if (response.status() !== 201) {
    const body = await response.text();
    throw new Error(`Create innovation failed (${response.status()}): ${body}`);
  }

  return response.json() as Promise<CreateInnovationResponse>;
}

// ---------------------------------------------------------------------------
// Test suite
// ---------------------------------------------------------------------------

test.describe('Nightly Smoke: Production Journey Validation', () => {
  // Fail fast with a clear message if credentials are missing — prevents
  // confusing auth errors deeper in the tests.
  test.beforeAll(() => {
    if (!NIGHTLY_EMAIL || !NIGHTLY_PASSWORD) {
      throw new Error(
        'E2E_NIGHTLY_EMAIL and E2E_NIGHTLY_PASSWORD must be set. ' +
          'Configure them as GitHub repository secrets and map them in e2e-nightly.yml.'
      );
    }
  });

  // -------------------------------------------------------------------------
  // Test 1 — Critical journey: Login → Create Innovation → View Detail → Navigate back
  // -------------------------------------------------------------------------
  test('should login and view an innovation detail page', async ({ page, request }) => {
    let accessToken: string;
    let innovationId: string;
    const innovationTitle = `Nightly Smoke ${new Date().toISOString().slice(0, 16)}`;

    // Step 1: Login via API
    await test.step('Obtain access token via API login', async () => {
      const loginResponse = await apiLogin(request, NIGHTLY_EMAIL!, NIGHTLY_PASSWORD!);
      accessToken = loginResponse.accessToken;
      expect(accessToken).toBeTruthy();
      expect(loginResponse.actor.email).toBe(NIGHTLY_EMAIL);
    });

    // Step 2: Create a fresh Draft innovation via API (self-contained test data)
    await test.step('Create Draft innovation via API', async () => {
      const created = await apiCreateInnovation(request, accessToken, innovationTitle);
      innovationId = created.innovationId;
      expect(innovationId).toBeTruthy();
      expect(created.status).toBe('Draft');
    });

    // Step 3: Seed localStorage before the Angular app boots so the auth
    // interceptor reads the token from the signal initialisation on startup.
    await page.addInitScript((token: string) => {
      localStorage.setItem('accessToken', token);
    }, accessToken);

    // Step 4: Navigate directly to the innovation detail page
    await test.step('Navigate to innovation detail page', async () => {
      await page.goto(`/innovations/${innovationId}`);
      await page.waitForLoadState('networkidle');
    });

    // Step 5: Assert the Angular innovation-detail component rendered correctly
    await test.step('Assert innovation detail renders', async () => {
      await expect(page.locator('mat-card-title')).toContainText(
        new RegExp(innovationTitle, 'i')
      );
      await expect(page.getByText(/nightly smoke test/i)).toBeVisible();
      await expect(
        page.getByRole('button', { name: /back to innovations/i })
      ).toBeVisible();
    });

    // Step 6: Navigate back to the list — validates Angular router
    await test.step('Navigate back to innovations list', async () => {
      await page.getByRole('button', { name: /back to innovations/i }).click();
      await expect(page).toHaveURL(/\/innovations$/);
      await page.waitForLoadState('networkidle');
    });
  });

  // -------------------------------------------------------------------------
  // Test 2 — Auth gate: invalid credentials are rejected (API-only, no UI)
  // -------------------------------------------------------------------------
  test('should reject invalid credentials with 401', async ({ request }) => {
    const response = await request.post(`${API_BASE_URL}/auth/login`, {
      data: {
        email: NIGHTLY_EMAIL,
        actorType: 'IdeaGenerator',
        password: 'deliberatelyWrongPassword!Nightly',
      },
    });

    expect(response.status()).toBe(401);

    const body = await response.json();
    expect(body.detail).toMatch(/invalid email/i);
  });

  // -------------------------------------------------------------------------
  // Test 3 — API health: backend is reachable and returns expected content type
  // -------------------------------------------------------------------------
  test('should reach the backend API health endpoint', async ({ request }) => {
    // The .NET health endpoint exposed at /health returns 200 when the app is
    // healthy (see Program.cs — MapHealthChecks). This is a lightweight liveness
    // check independent of authentication.
    const response = await request.get(`${API_BASE_URL}/health`);

    expect(response.status()).toBe(200);
  });
});
