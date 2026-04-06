# Playwright E2E Tests

End-to-end tests for the Innoventity Angular client using Playwright.

## Prerequisites

1. **Backend API running**: The E2E tests require the Innoventity.API backend to be running on `http://localhost:5001`
   ```bash
   # In the repository root
   cd src/Innoventity.API
   dotnet run
   ```

2. **Database seeded**: The backend database should have the test seed data loaded (happens automatically on first run in Development environment)

3. **Playwright browsers installed**: Chromium browser should be installed
   ```bash
   npx playwright install chromium
   ```

## Running Tests

### Run all E2E tests
```bash
npm run e2e
```

### Run E2E tests in UI mode (interactive)
```bash
npx playwright test --ui
```

### Run specific test file
```bash
npx playwright test e2e/journey-1.spec.ts
```

### Run tests in headed mode (see browser)
```bash
npx playwright test --headed
```

### Debug tests
```bash
npx playwright test --debug
```

## Test Structure

### Journey 1: Registration → Activation → Login → View Innovation
- **File**: `e2e/journey-1.spec.ts`
- **Scenario**: Full user journey from account creation to viewing innovation details
- **Steps**:
  1. Register new account via API (backend)
  2. Activate account via API (using activation token)
  3. Login via UI (Angular login component)
  4. View innovation detail via UI (Angular innovation-detail component)

## Best Practices Followed

1. **API for Setup**: Uses Playwright's request context to call backend API for registration/activation (faster, more reliable than UI automation for setup)
2. **UI for Critical Flows**: Tests actual user interactions for login and navigation (what users will experience)
3. **Accessible Selectors**: Uses `getByRole`, `getByLabel`, `getByText` for robust selectors
4. **Auto-Retry**: Uses Playwright's built-in auto-retry for assertions (`expect().toBeVisible()`)
5. **Test Isolation**: Each test generates unique credentials (timestamp-based email)
6. **Deterministic Data**: Uses seeded innovation data for predictable test assertions

## Troubleshooting

### "Connection refused" or "ECONNREFUSED"
- **Cause**: Backend API is not running
- **Solution**: Start the backend API on port 5001

### "Navigation timeout" or page doesn't load
- **Cause**: Angular dev server is not running
- **Solution**: The Playwright config auto-starts the dev server with `npm start`, but ensure no other process is using port 4200

### "Innovation not found" or missing seed data
- **Cause**: Database is not seeded with test data
- **Solution**: 
  ```bash
  # Delete database and restart backend to trigger seeding
  cd src/Innoventity.API
  rm Innoventity.db  # or delete via file explorer
  dotnet run
  ```

### Tests are flaky (sometimes pass, sometimes fail)
- **Cause**: Race conditions or timing issues
- **Solution**: Playwright auto-retries assertions, but if tests are still flaky:
  - Check for explicit timeouts that might be too short
  - Ensure selectors are robust (prefer `getByRole` over CSS selectors)
  - Add `await page.waitForLoadState('networkidle')` if needed

## CI/CD Integration

In CI pipelines, the tests:
- Run with `retries: 2` (auto-retry failed tests)
- Use GitHub reporter for better CI output
- Capture screenshots and videos on failure
- Run in headless mode by default

See `playwright.config.ts` for full configuration.
