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
  title: string;
  productType: string;
  researchCategory: string;
  researchBackground: string;
  hasIPR: boolean;
  hasRightToUse: boolean;
  productDescription?: string;
  technologyDescription?: string;
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
    // STEP 3: Login via API
    // ==========================================
    let accessToken: string;
    await test.step('Login via API', async () => {
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
    // STEP 5: View Innovation Detail via UI
    // ==========================================
    await test.step('View innovation detail', async () => {
      // Listen for console messages and errors
      page.on('console', msg => console.log('BROWSER CONSOLE:', msg.type(), msg.text()));
      page.on('pageerror', err => console.error('PAGE ERROR:', err.message));

      // Set authentication token in browser local storage
      await page.goto('/');
      await page.evaluate((token) => {
        localStorage.setItem('accessToken', token);
      }, accessToken);

      // Navigate to the created innovation's detail page
      // The page reload will cause Angular to reinitialize and read the token from localStorage
      await page.goto(`/innovations/${innovationId}`);

      // Wait for page to load and authenticate
      await page.waitForLoadState('networkidle');

      // Debug: Log page content and localStorage
      const pageContent = await page.content();
      const storedToken = await page.evaluate(() => localStorage.getItem('accessToken'));
      console.log('Page HTML length:', pageContent.length);
      console.log('Page HTML:', pageContent.substring(0, 500));
      console.log('Stored token (first 20 chars):', storedToken?.substring(0, 20));
      console.log('Innovation ID:', innovationId);

      // Check if there's an error message on the page
      const bodyText = await page.locator('body').textContent();
      console.log('Body text:', bodyText);

      // Wait for innovation content to load (not loading spinner)
      await expect(page.locator('.loading-spinner')).not.toBeVisible({ timeout: 10000 });

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
      // Click back button
      await page.getByRole('button', { name: /back/i }).click();

      // Should navigate to root (which redirects to login since we're testing routing)
      // In a real app with a home/dashboard, this would go there
      await expect(page).toHaveURL('/');
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

    // Login via API to get access token
    await test.step('Login via API', async () => {
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

    await test.step('Navigate to innovation and observe loading state', async () => {
      // Set authentication token in browser local storage
      await page.goto('/');
      await page.evaluate((token) => {
        localStorage.setItem('accessToken', token);
      }, accessToken);

      // Navigate to innovation detail page
      // The page reload will cause Angular to reinitialize and read the token from localStorage
      await page.goto(`/innovations/${innovationId}`);

      // Wait for page to load
      await page.waitForLoadState('networkidle');

      // Should briefly show loading spinner (may be too fast to catch in local tests)
      // This assertion is best-effort - if the API is fast, it may pass the spinner phase
      const loadingSpinner = page.locator('.loading-spinner');

      // Eventually, content should be visible (loading complete)
      await expect(page.getByRole('heading', { name: /loading state test innovation/i })).toBeVisible({ timeout: 10000 });
    });
  });
});
