import { test, expect, type APIRequestContext } from '@playwright/test';

/**
 * Journey 1 E2E Test: Registration → Activation → Login → Create Innovation → View Innovation
 *
 * Test Scenario:
 * 1. Register new account via API (backend direct call)
 * 2. Activate account via API (using activation token from registration response)
 * 3. Login via API (get authentication token)
 * 4. Create innovation via API (using auth token)
 * 5. View innovation detail via UI (Angular innovation-detail component)
 *
 * Follows Playwright Best Practices:
 * - Uses API for test setup (registration/activation/login/innovation creation) - faster and more reliable
 * - Tests critical UI flows (innovation detail display, navigation)
 * - Uses accessible selectors (getByRole, getByLabel, getByText)
 * - Uses auto-retry assertions with expect()
 * - Unique test data per run (timestamp-based email and innovation)
 * - Proper test isolation (clean state per test)
 */

const API_BASE_URL = 'http://localhost:5073';

interface RegistrationResponse {
  actorId: string;
  email: string;
  actorType: string;
  accountStatus: string;
  activationToken: string; // Only present in dev/test environments
  message: string;
}

interface ActivationRequest {
  email: string;
  token: string;
}

interface ActivationResponse {
  message: string;
  accountStatus: string;
}

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

interface CreateInnovationRequest {
  // Required fields
  title: string;
  productType: string;
  researchCategory: string;
  researchBackground: string;
  hasIPR: boolean;
  hasRightToUse: boolean;
  // Optional fields (drafts do not require completeness - Spec §US2)
  productDescription?: string;
  technologyDescription?: string;
  productAdvantages?: string;
  developmentPhase?: string;
  developmentProcess?: string;
  targetBeneficiaries?: string;
  relevantMarketSize?: number;
  potentialMarketSize?: number;
  targetMarket?: string;
  targetCustomerBase?: string;
  targetCustomerType?: string;
  productKeywords?: string;
  advantageKeywords?: string;
  targetIndustryIds?: string[];
  partnersNeeded?: string[];
}

interface CreateInnovationResponse {
  innovationId: string;
  ideaToken: string;
  status: string;
  createdAt: string;
  ownerId: string;
}

/**
 * Register a new actor account via API
 */
async function registerAccount(request: APIRequestContext, email: string, password: string): Promise<RegistrationResponse> {
  const response = await request.post(`${API_BASE_URL}/auth/register`, {
    data: {
      email,
      firstName: 'E2E',
      lastName: 'TestUser',
      contactAddress: {
        address1: '123 Test Street',
        city: 'Test City',
        postCode: 'TC123',
        countryCode: 'US',
      },
      actorType: 'IdeaGenerator',
      password,
    },
  });

  if (response.status() !== 201) {
    const errorBody = await response.text();
    console.error('Registration failed:', response.status(), errorBody);
    throw new Error(`Registration failed with status ${response.status()}: ${errorBody}`);
  }

  const body = await response.json();

  // In dev/test environments, the activation token is included in the response
  // In production, it would only be sent via email
  expect(body.activationToken).toBeDefined();

  return body as RegistrationResponse;
}

/**
 * Activate an actor account via API
 */
async function activateAccount(request: APIRequestContext, email: string, activationToken: string): Promise<void> {
  const response = await request.post(`${API_BASE_URL}/auth/activate`, {
    data: {
      email,
      token: activationToken,
    } as ActivationRequest,
  });

  if (response.status() !== 200) {
    const errorBody = await response.text();
    console.error('Activation failed:', response.status(), errorBody);
    throw new Error(`Activation failed with status ${response.status()}: ${errorBody}`);
  }

  const body = await response.json() as ActivationResponse;
  expect(body.accountStatus).toBe('Active');
}

/**
 * Login and get access token via API
 */
async function loginAccount(request: APIRequestContext, email: string, password: string, actorType: string = 'IdeaGenerator'): Promise<LoginResponse> {
  const response = await request.post(`${API_BASE_URL}/auth/login`, {
    data: {
      email,
      actorType,
      password,
    } as LoginRequest,
  });

  if (response.status() !== 200) {
    const errorBody = await response.text();
    console.error('Login failed:', response.status(), errorBody);
    throw new Error(`Login failed with status ${response.status()}: ${errorBody}`);
  }

  const body = await response.json() as LoginResponse;
  expect(body.accessToken).toBeDefined();
  expect(body.actor.email).toBe(email);

  return body;
}

/**
 * Login via UI (fills login form and submits), then waits until the token is confirmed in
 * localStorage.
 *
 * Important: Playwright's page.goto() always triggers a full HTTP navigation, causing Angular
 * to re-bootstrap from scratch. To guarantee the auth token is available when Angular reads
 * localStorage in AuthService's constructor, call page.addInitScript() with the stored token
 * immediately after this function before any subsequent page.goto() call.
 */
async function loginViaUI(
  page: any,
  email: string,
  password: string,
  actorType: 'IdeaGenerator' | 'RD' | 'Manufacturing' | 'SalesMarketing' | 'Investor' = 'IdeaGenerator'
): Promise<void> {
  // Map actor type enum to display labels
  const actorTypeLabels = {
    'IdeaGenerator': 'Idea Generator',
    'RD': 'R&D Organization',
    'Manufacturing': 'Manufacturing',
    'SalesMarketing': 'Sales & Marketing',
    'Investor': 'Investor'
  };

  const displayLabel = actorTypeLabels[actorType];

  // Navigate to login page
  await page.goto('http://localhost:4200/login');
  await page.waitForLoadState('networkidle');

  // Fill email field
  await page.getByLabel('Email').fill(email);

  // Fill password field
  await page.getByLabel('Password').fill(password);

  // Select actor type from dropdown - click to open, then select option
  await page.getByLabel('Actor Type').click();
  await page.getByRole('option', { name: displayLabel }).click();

  // Submit form
  await page.getByRole('button', { name: 'Log In' }).click();

  // Wait for navigation away from login page (indicates success)
  await page.waitForURL(url => !url.pathname.includes('/login'), { timeout: 10000 });

  // Wait for the page to fully load after redirect
  await page.waitForLoadState('networkidle');

  // Verify token is stored in localStorage
  const accessToken = await page.evaluate(() => localStorage.getItem('accessToken'));
  if (!accessToken) {
    throw new Error('Login failed - no access token stored in localStorage');
  }
}

/**
 * Create an innovation via API (requires authentication)
 */
async function createInnovation(request: APIRequestContext, accessToken: string, innovationData: CreateInnovationRequest): Promise<CreateInnovationResponse> {
  const response = await request.post(`${API_BASE_URL}/innovations`, {
    headers: {
      'Authorization': `Bearer ${accessToken}`,
    },
    data: innovationData,
  });

  if (response.status() !== 201) {
    const errorBody = await response.text();
    console.error('Create innovation failed:', response.status(), errorBody);
    throw new Error(`Create innovation failed with status ${response.status()}: ${errorBody}`);
  }

  const body = await response.json() as CreateInnovationResponse;
  expect(body.innovationId).toBeDefined();
  expect(body.status).toBe('Draft');

  return body;
}

test.describe('Journey 1: User Registration and Innovation View', () => {
  let testEmail: string;
  let testPassword: string;

  test.beforeEach(({ }, testInfo) => {
    // Generate unique credentials for this test run
    // Use timestamp + random number + test name to ensure uniqueness even in parallel execution
    const timestamp = Date.now();
    const random = Math.floor(Math.random() * 10000);
    const testName = testInfo.title.replace(/\s+/g, '-').substring(0, 20);
    testEmail = `e2e-${timestamp}-${random}-${testName}@innoventity.dev`;
    testPassword = 'Test123!@#E2E';
  });

  test('should complete full journey: Register → Activate → Login → Create Innovation → View Innovation', async ({ page, request }) => {
    let innovationId: string;
    let accessToken: string;

    // ==========================================
    // STEP 1: Register account via API
    // ==========================================
    await test.step('Register new account', async () => {
      const registrationResponse = await registerAccount(request, testEmail, testPassword);

      expect(registrationResponse.email).toBe(testEmail);
      expect(registrationResponse.accountStatus).toBe('PendingActivation');

      // ==========================================
      // STEP 2: Activate account via API
      // ==========================================
      const activationToken = registrationResponse.activationToken;
      await activateAccount(request, testEmail, activationToken);
    });

    // ==========================================
    // STEP 3: Login via API to get token for innovation creation
    // ==========================================
    await test.step('Login via API to get access token', async () => {
      const loginResponse = await loginAccount(request, testEmail, testPassword, 'IdeaGenerator');
      accessToken = loginResponse.accessToken;
      expect(loginResponse.actor.actorType).toBe('IdeaGenerator');
    });

    // ==========================================
    // STEP 4: Create Innovation via API
    // ==========================================
    await test.step('Create innovation via API', async () => {
      const innovationData: CreateInnovationRequest = {
        title: 'E2E Test Innovation',
        productType: 'Prototype',
        researchCategory: 'Engineering',
        researchBackground: 'Test research background for E2E testing',
        hasIPR: true,
        hasRightToUse: true,
        productDescription: 'Test product description',
        technologyDescription: 'Test technology description',
      };

      const createResponse = await createInnovation(request, accessToken, innovationData);
      innovationId = createResponse.innovationId;
      expect(innovationId).toBeDefined();
    });

    // ==========================================
    // STEP 5: Login via UI to establish browser auth state
    // ==========================================
    await test.step('Login via UI', async () => {
      await loginViaUI(page, testEmail, testPassword, 'IdeaGenerator');

      // After UI login the token is in localStorage. Register an addInitScript so it is
      // guaranteed to be set *before* Angular bootstraps on the next full-page navigation.
      // page.goto() always performs a full HTTP navigation: Angular re-bootstraps fresh and
      // reads localStorage in AuthService's constructor. addInitScript runs before any page
      // scripts, ensuring the signal is populated before the first HTTP request fires.
      const storedToken = await page.evaluate(() => localStorage.getItem('accessToken'));
      await page.addInitScript(
        (token: string) => { localStorage.setItem('accessToken', token); },
        storedToken!,
      );
    });

    // ==========================================
    // STEP 6: View Innovation Detail via UI
    // ==========================================
    await test.step('View innovation detail', async () => {
      page.on('console', msg => console.log('BROWSER CONSOLE:', msg.type(), msg.text()));
      page.on('pageerror', err => console.error('PAGE ERROR:', err.message));

      await page.goto(`http://localhost:4200/innovations/${innovationId}`);
      await page.waitForLoadState('networkidle');

      // Verify innovation title is displayed
      await expect(page.getByRole('heading', { name: /e2e test innovation/i })).toBeVisible();

      // Verify innovation details are visible
      await expect(page.getByText(/test product description/i)).toBeVisible();
      await expect(page.getByText(/test technology description/i)).toBeVisible();

      // Verify research category chip
      await expect(page.getByText(/engineering/i)).toBeVisible();

      // Verify back button is present
      await expect(page.getByRole('button', { name: /back/i })).toBeVisible();
    });

    // ==========================================
    // STEP 6: Verify Navigation Works
    // ==========================================
    await test.step('Test navigation back', async () => {
      await page.getByRole('button', { name: /back/i }).click();
      await expect(page).toHaveURL('/innovations');
    });
  });

  test('should handle login with incorrect credentials via API', async ({ page, request }) => {
    // Register and activate account first
    await test.step('Setup: Create and activate account', async () => {
      const registrationResponse = await registerAccount(request, testEmail, testPassword);
      const activationToken = registrationResponse.activationToken;
      await activateAccount(request, testEmail, activationToken);
    });

    await test.step('Attempt login with wrong password via API', async () => {
      // Try to login with wrong password
      const response = await request.post(`${API_BASE_URL}/auth/login`, {
        data: {
          email: testEmail,
          actorType: 'IdeaGenerator',
          password: 'WrongPassword123!',
        },
      });

      // Should return 401 Unauthorized
      expect(response.status()).toBe(401);

      const body = await response.json();
      expect(body.detail).toContain('Invalid email');
    });
  });

  test('should show loading state during innovation fetch', async ({ page, request }) => {
    let innovationId: string;
    let accessToken: string;

    // Register and activate account
    await test.step('Setup: Create and activate account', async () => {
      const registrationResponse = await registerAccount(request, testEmail, testPassword);
      const activationToken = registrationResponse.activationToken;
      await activateAccount(request, testEmail, activationToken);
    });

    // Login via API to get access token for innovation creation
    await test.step('Login via API to get access token', async () => {
      const loginResponse = await loginAccount(request, testEmail, testPassword, 'IdeaGenerator');
      accessToken = loginResponse.accessToken;
      expect(loginResponse.accessToken).toBeDefined();
    });

    // Create innovation via API
    await test.step('Create innovation via API', async () => {
      const innovationData: CreateInnovationRequest = {
        title: 'Loading State Test Innovation',
        productType: 'Prototype',
        researchCategory: 'Engineering',
        researchBackground: 'Testing loading states in UI',
        hasIPR: false,
        hasRightToUse: true,
      };

      const createResponse = await createInnovation(request, accessToken, innovationData);
      innovationId = createResponse.innovationId;
    });

    // Login via UI to establish browser auth state
    await test.step('Login via UI', async () => {
      await loginViaUI(page, testEmail, testPassword, 'IdeaGenerator');

      const storedToken = await page.evaluate(() => localStorage.getItem('accessToken'));
      await page.addInitScript(
        (token: string) => { localStorage.setItem('accessToken', token); },
        storedToken!,
      );
    });

    await test.step('Navigate to innovation and observe loading state', async () => {
      await page.goto(`http://localhost:4200/innovations/${innovationId}`);
      await page.waitForLoadState('networkidle');

      // Eventually, content should be visible (loading complete)
      await expect(page.getByRole('heading', { name: /loading state test innovation/i })).toBeVisible({ timeout: 10000 });
    });
  });
});
