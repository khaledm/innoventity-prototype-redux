# Quickstart: Registration UI Development

**Feature**: Registration User Interface (Phase 1+)
**Date**: April 6, 2026
**Audience**: Frontend developers implementing registration workflow

---

## Prerequisites

- **Node.js**: v20.x or higher
- **npm**: v10.x or higher
- **Angular CLI**: v19.x (`npm install -g @angular/cli@19`)
- **Git**: Clone repository (`git clone <repo-url>`)
- **Backend API**: Phase 0 backend must be running (`POST /api/auth/register`, `POST /api/auth/activate` endpoints available)

---

## Setup Steps

### 1. Install Dependencies

```bash
cd src/Innoventity.Client
npm install
```

**Expected Output**: Dependencies installed (Angular 19, Angular Material 19, RxJS, etc.)

---

### 2. Start Development Server

```bash
npm start
```

**Expected Output**:
```
Angular Live Development Server is listening on localhost:4200
```

**Access**: Open browser to `http://localhost:4200/register`

---

### 3. Verify Backend Connection

Ensure Phase 0 backend API is running on `http://localhost:5000` (or configured API URL):

```bash
# In separate terminal (backend project)
cd src/Innoventity.API
dotnet run
```

**Test Backend**:
```bash
curl -X GET http://localhost:5000/api/health
# Expected: {"status":"Healthy"}
```

---

## Project Structure

```
src/Innoventity.Client/src/app/
├── features/
│   └── registration/
│       ├── register/
│       │   ├── register.component.ts
│       │   ├── register.component.html
│       │   ├── register.component.scss
│       │   └── register.component.spec.ts
│       ├── pending-activation/
│       │   ├── pending-activation.component.ts
│       │   ├── pending-activation.component.html
│       │   ├── pending-activation.component.scss
│       │   └── pending-activation.component.spec.ts
│       ├── activate/
│       │   ├── activate.component.ts
│       │   ├── activate.component.html
│       │   ├── activate.component.scss
│       │   └── activate.component.spec.ts
│       └── services/
│           ├── registration.service.ts
│           ├── registration.service.spec.ts
│           ├── password-strength.service.ts
│           └── password-strength.service.spec.ts
├── shared/
│   ├── validators/
│   │   ├── password-validators.ts
│   │   ├── password-validators.spec.ts
│   │   ├── conditional-validators.ts
│   │   └── conditional-validators.spec.ts
│   └── models/
│       └── registration.model.ts
└── guards/
    └── registration.guard.ts
```

---

## Development Workflow

### 1. Generate Component (if not exists)

```bash
ng generate component features/registration/register --standalone --skip-tests=false
```

**Flags**:
- `--standalone`: Generate standalone component (Angular 19 pattern)
- `--skip-tests=false`: Generate .spec.ts file

---

### 2. Generate Service

```bash
ng generate service features/registration/services/registration --skip-tests=false
```

---

### 3. Generate Guard

```bash
ng generate guard guards/registration --functional --skip-tests=false
```

**Flags**:
- `--functional`: Generate functional guard (CanActivateFn)

---

### 4. Run Unit Tests

```bash
npm run test
```

**Expected Output**:
```
✔ RegisterComponent should create
✔ RegisterComponent should have form invalid when empty
✔ RegisterComponent should validate email format
... (15+ tests)

Test Suites: 5 passed, 5 total
Tests:       17 passed, 17 total
```

**Test Specific Component**:
```bash
npm run test -- register.component.spec.ts
```

---

### 5. Run E2E Tests

```bash
npm run e2e
```

**Expected Output**:
```
✔ registration-happy-path.spec.ts (browser: chromium)
  ✔ complete registration workflow
✔ registration-validation.spec.ts (browser: chromium)
  ✔ email validation
  ✔ password strength validation
... (10 scenarios)

10 passed (15s)
```

**Test Specific Scenario**:
```bash
npx playwright test e2e/registration/registration-happy-path.spec.ts
```

---

### 6. Run Accessibility Tests

```bash
npx playwright test e2e/registration/registration-accessibility.spec.ts
```

**Expected Output**:
```
✔ registration form meets WCAG 2.1 AA
✔ keyboard navigation works
```

---

## Testing Strategy

### Unit Tests (Jest)

**Test File Pattern**: `*.spec.ts` (same directory as source file)

**Run All Tests**:
```bash
npm run test
```

**Run Tests in Watch Mode**:
```bash
npm run test:watch
```

**Generate Coverage Report**:
```bash
npm run test:coverage
```

**Coverage Output**: `coverage/index.html` (open in browser)

**Target Coverage**: 80%+ (lines, branches)

---

### E2E Tests (Playwright)

**Test File Pattern**: `e2e/registration/*.spec.ts`

**Run All E2E Tests**:
```bash
npm run e2e
```

**Run Tests in Headed Mode** (see browser):
```bash
npx playwright test --headed
```

**Run Tests in Debug Mode**:
```bash
npx playwright test --debug
```

**View Test Report**:
```bash
npx playwright show-report
```

---

## Manual Testing Checklist

### Register Component

1. **Navigate** to `http://localhost:4200/register`
2. **Verify** all 6 form fields visible:
   - Email
   - Password (with visibility toggle)
   - Confirm Password (with visibility toggle)
   - Actor Type (dropdown with 5 options)
   - Full Name
   - Organization Name (always visible)
3. **Test Actor Type Change**:
   - Select "Idea Generator" → Organization Name shows "(Optional)"
   - Select "R&D Organization" → Organization Name required (no "(Optional)" label)
4. **Test Password Strength Indicator**:
   - Type `password` → Weak (red/orange)
   - Type `Password123!` → Weak (8-11 chars, meets requirements)
   - Type `SecurePass123!` → Medium (12-15 chars)
   - Type `VerySecurePassword123!` → Strong (16+ chars, green)
5. **Test Validation Errors**:
   - Submit empty form → All fields show "required" error
   - Enter invalid email (`test@`) → "Invalid email format"
   - Enter weak password (`weak`) → List of missing requirements
   - Enter mismatched passwords → "Passwords do not match" on Confirm Password
6. **Test Successful Registration**:
   - Fill all fields correctly
   - Click "Register" → Navigate to `/register/pending-activation`
   - Verify email displayed on pending-activation page

---

### Pending Activation Component

1. **After Registration**: Should redirect to `/register/pending-activation`
2. **Verify Email Display**: Email from registration form visible
3. **Test Direct Access**:
   - Navigate directly to `/register/pending-activation` (without registration flow)
   - Should redirect to `/register` (guard blocks access)
4. **Test "Continue to Login" Button**: Navigates to `/login`

---

### Activate Component

1. **Extract Activation Token**:
   - Option A: Extract from backend logs (email not actually sent in dev)
   - Option B: Mock email service returns token in console
   - Option C: Use test token from E2E helper
2. **Navigate**: `http://localhost:4200/activate?token=<TOKEN>`
3. **Test Valid Token**:
   - Loading spinner visible briefly
   - Success message: "Account activated successfully!"
   - "Continue to Login" button visible
   - Click button → Navigate to `/login`
4. **Test Invalid Token**:
   - Navigate: `/activate?token=invalid`
   - Error message: "Invalid or expired activation token."
   - "Retry" button visible (does nothing, token still invalid)
5. **Test Missing Token**:
   - Navigate: `/activate` (no query parameter)
   - Error message: "Missing activation token"

---

## Common Development Tasks

### Add New Validator

1. **Create Validator Function** in `src/app/shared/validators/`:
   ```typescript
   export function myValidator(): ValidatorFn {
     return (control: AbstractControl): ValidationErrors | null => {
       // Validation logic
       return valid ? null : { myError: true };
     };
   }
   ```

2. **Add Test** in `*.spec.ts`:
   ```typescript
   it('should accept valid input', () => {
     const control = new FormControl('valid');
     expect(myValidator()(control)).toBeNull();
   });
   ```

3. **Apply to Form**:
   ```typescript
   this.registrationForm = this.fb.group({
     myField: ['', [Validators.required, myValidator()]]
   });
   ```

---

### Add New Error Message

1. **Update Template** (`register.component.html`):
   ```html
   <mat-error *ngIf="myField.hasError('myError')">
     My custom error message
   </mat-error>
   ```

2. **Test Error Display** (unit test):
   ```typescript
   it('should display error message for myError', () => {
     component.registrationForm.get('myField')!.setErrors({ myError: true });
     fixture.detectChanges();

     const error = fixture.nativeElement.querySelector('mat-error');
     expect(error.textContent).toContain('My custom error message');
   });
   ```

---

### Mock API Calls (Unit Tests)

```typescript
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';

describe('RegistrationService', () => {
  let service: RegistrationService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [RegistrationService]
    });

    service = TestBed.inject(RegistrationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify(); // Verify no outstanding requests
  });

  it('should call POST /api/auth/register', () => {
    const mockData = { email: 'test@example.com', password: 'Test123!', /* ... */ };
    const mockResponse = { message: 'Success' };

    service.register(mockData).subscribe(response => {
      expect(response).toEqual(mockResponse);
    });

    const req = httpMock.expectOne('/api/auth/register');
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);
  });
});
```

---

## Environment Configuration

### Development (`src/environments/environment.ts`)

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api', // Local backend
  logLevel: 'debug'
};
```

### Production (`src/environments/environment.prod.ts`)

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://innoventity-api.azurewebsites.net/api', // Azure backend
  logLevel: 'error'
};
```

**Usage in Service**:
```typescript
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class RegistrationService {
  private apiUrl = environment.apiUrl;

  register(data: RegistrationFormModel) {
    return this.http.post(`${this.apiUrl}/auth/register`, data);
  }
}
```

---

## Debugging Tips

### Debug Component in Browser

1. Open DevTools (F12)
2. Navigate to **Sources** tab
3. Find component TypeScript file
4. Set breakpoints
5. Interact with form (trigger validation, submit)

---

### Debug Unit Tests

```bash
npm run test:debug
```

1. Open Chrome to `chrome://inspect`
2. Click "Open dedicated DevTools for Node"
3. Set breakpoints in test file
4. Tests run in debug mode

---

### Debug E2E Tests

```bash
npx playwright test --debug
```

**Playwright Inspector** opens:
- Step through test line-by-line
- Inspect page state
- View network requests

---

### Debug Validation Errors

**Check Form Status in Console**:
```typescript
// In component ngOnInit() or method
console.log('Form valid:', this.registrationForm.valid);
console.log('Form errors:', this.registrationForm.errors);
console.log('Email errors:', this.registrationForm.get('email')!.errors);
```

**Check Validator Logic**:
```typescript
const testControl = new FormControl('TestPassword123!');
const result = passwordStrengthValidator()(testControl);
console.log('Validator result:', result); // null = valid, object = error
```

---

## Troubleshooting

### Issue: Backend API not reachable

**Symptoms**: HTTP errors (CORS, 502, connection refused)

**Solutions**:
1. Verify backend running: `curl http://localhost:5000/api/health`
2. Check CORS configuration in backend (`Program.cs`):
   ```csharp
   app.UseCors(policy => policy
       .WithOrigins("http://localhost:4200")
       .AllowAnyMethod()
       .AllowAnyHeader());
   ```
3. Check proxy configuration (`src/Innoventity.Client/proxy.conf.json`):
   ```json
   {
     "/api": {
       "target": "http://localhost:5000",
       "secure": false
     }
   }
   ```

---

### Issue: Unit tests failing (Angular Material components)

**Symptoms**: `NullInjectorError: No provider for MatFormField`

**Solution**: Import `NoopAnimationsModule` in test:
```typescript
TestBed.configureTestingModule({
  imports: [RegisterComponent, NoopAnimationsModule], // Add NoopAnimationsModule
  providers: [/* ... */]
});
```

---

### Issue: E2E tests flaky (Playwright timeouts)

**Symptoms**: Tests fail intermittently with "Timeout exceeded"

**Solutions**:
1. Add explicit waits:
   ```typescript
   await page.waitForSelector('button[type="submit"]:not([disabled])');
   await page.click('button[type="submit"]');
   ```
2. Increase timeout:
   ```typescript
   test('my test', async ({ page }) => {
     test.setTimeout(60000); // 60 seconds
     // Test logic
   });
   ```

---

### Issue: Password strength indicator not updating

**Symptoms**: Progress bar doesn't change when typing password

**Solution**: Verify subscription in `ngOnInit()`:
```typescript
ngOnInit() {
  this.registrationForm.get('password')!.valueChanges.pipe(
    debounceTime(200),
    takeUntilDestroyed(this.destroyRef) // Auto-cleanup
  ).subscribe(password => {
    this.updatePasswordStrength(password);
  });
}
```

---

## Performance Optimization

### Lazy Load Registration Feature

**App Routes** (`app.routes.ts`):
```typescript
{
  path: 'register',
  loadComponent: () => import('./features/registration/register/register.component').then(m => m.RegisterComponent)
}
```

**Benefits**: Smaller initial bundle size (registration code loaded only when needed)

---

### Debounce Validation

**Without Debounce** (flicker):
```typescript
this.form.get('email')!.valueChanges.subscribe(value => this.validate(value));
```

**With Debounce** (smooth):
```typescript
this.form.get('email')!.valueChanges.pipe(
  debounceTime(300),
  distinctUntilChanged()
).subscribe(value => this.validate(value));
```

---

## Accessibility Testing

### Manual Keyboard Navigation Test

1. Open `/register`
2. Press **Tab** repeatedly:
   - Email field focused
   - Password field focused
   - Password visibility toggle focused
   - Confirm Password field focused
   - Confirm Password visibility toggle focused
   - Actor Type dropdown focused
   - Full Name field focused
   - Organization Name field focused
   - Submit button focused
3. Press **Enter** on submit button (should submit form)
4. Verify error messages announced (use screen reader)

---

### Screen Reader Testing (Windows)

Install **NVDA** (free screen reader):
1. Download: https://www.nvaccess.org/download/
2. Run NVDA
3. Navigate to `/register`
4. Tab through form fields
5. Verify field labels announced ("Email", "Password", etc.)
6. Trigger validation error
7. Verify error message announced ("Invalid email format")

---

## Next Steps

1. **Implement Components**: Follow [plan.md](./plan.md) Phase 1 design decisions
2. **Write Tests First** (TDD): Write unit test → implement feature → verify green
3. **Run E2E Tests**: Verify end-to-end workflow (register → activate → login)
4. **Code Review**: Submit PR, ensure all tests pass in CI/CD

---

## Additional Resources

- **Angular Documentation**: https://angular.dev/
- **Angular Material Components**: https://material.angular.io/components
- **Reactive Forms Guide**: https://angular.dev/guide/forms/reactive-forms
- **Playwright Documentation**: https://playwright.dev/
- **WCAG 2.1 Guidelines**: https://www.w3.org/WAI/WCAG21/quickref/
- **Project Spec**: [spec.md Section 8](./spec.md#8-registration-user-interface-phase-1)

---

**Quickstart Complete** - You are ready to implement Registration UI components!
