# Implementation Plan: Registration User Interface (Phase 1+)

**Branch**: `001-platform-core` | **Date**: 2026-04-06 | **Spec**: [spec.md Section 8](./spec.md#8-registration-user-interface-phase-1)
**Input**: Feature specification from `/specs/001-platform-core/spec.md` Section 8

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

**Primary Requirement**: Browser-based user registration workflow enabling prospective users (all 5 actor types) to register, activate via email, and login without requiring API tools (Postman/curl). Removes technical barrier to user acquisition and enables marketing campaigns with shareable registration URLs.

**Technical Approach**:
- **Frontend**: Three standalone Angular 19 components (RegisterComponent, PendingActivationComponent, ActivateComponent) using Angular Material Design and Reactive Forms
- **Form Strategy**: Reactive Forms with FormBuilder, custom validators for password strength and conditional Organization Name field
- **Backend Integration**: Leverages existing Phase 0 APIs (POST /auth/register, POST /auth/activate) without modification
- **UI/UX**: Material Design components, responsive layout (320px-2560px), WCAG 2.1 AA accessible
- **Testing**: Jest unit tests + Playwright E2E tests covering 24 acceptance criteria scenarios

## Technical Context

**Language/Version**: TypeScript 5.x / Angular 19
**Primary Dependencies**:
- Angular 19 (standalone components, signals)
- Angular Material 19 (mat-form-field, mat-select, mat-button, mat-spinner, mat-icon)
- Angular Reactive Forms (@angular/forms)
- RxJS 7.x (asynchronous state management)
- Existing AuthService (from Phase 0)

**Storage**: N/A (frontend only; backend APIs handle persistence)
**Testing**:
- Jest 29.x (unit tests, component tests)
- Playwright 1.x (E2E tests covering 10 scenarios)
- Angular Material component harnesses (testing Material components)

**Target Platform**: Modern web browsers
- Chrome/Edge 120+ (last 2 versions)
- Firefox 121+ (last 2 versions)
- Safari 17+ (last 2 versions)
- Mobile browsers: iOS Safari 17+, Android Chrome 120+

**Project Type**: Web application (frontend SPA - feature addition to existing Angular app)

**Performance Goals**:
- Form rendering: <100ms on standard device
- Form submission: <2 seconds (API dependent)
- Activation page token validation: <1 second
- Real-time validation feedback: <100ms

**Constraints**:
- WCAG 2.1 Level AA accessibility compliance
- Responsive design: 320px - 2560px viewport width
- Touch targets minimum 44x44px (mobile)
- Passwords never stored in browser storage
- HTTPS-only in production

**Scale/Scope**:
- 3 standalone components (Register, PendingActivation, Activate)
- 1 registration service (API integration)
- 6-field registration form with complex validation
- Custom validators: password strength (3 tiers), conditional required field, RFC 5322 email, password match
- 15+ unit tests (5 per component minimum)
- 10 E2E test scenarios (Playwright)
- 24 acceptance criteria from spec.md Section 8
**Project Type**: [e.g., library/cli/web-service/mobile-app/compiler/desktop-app or NEEDS CLARIFICATION]
**Performance Goals**: [domain-specific, e.g., 1000 req/s, 10k lines/sec, 60 fps or NEEDS CLARIFICATION]
**Constraints**: [domain-specific, e.g., <200ms p95, <100MB memory, offline-capable or NEEDS CLARIFICATION]
**Scale/Scope**: [domain-specific, e.g., 10k users, 1M LOC, 50 screens or NEEDS CLARIFICATION]

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Principle 1: User Experience First ✅
**Assessment**: **PASS** - This feature is entirely focused on improving user experience by removing technical barriers to registration (no more Postman/curl requirement).

**Evidence**:
- Spec-driven user stories with clear WHOs, WHATs, WHYs
- Real-time validation feedback (error messages as user types)
- Accessibility requirements (keyboard navigation, screen reader support)
- Responsive design (mobile-friendly, touch targets)
- Clear error messages (e.g., "Email already registered for this actor type" vs. generic 409 error)

**Risks**: None - feature directly serves user needs

---

### Principle 2: Quality is Non-Negotiable ✅
**Assessment**: **PASS** - Comprehensive testing and validation strategy in place before implementation.

**Evidence**:
- 15+ unit tests specified (minimum 5 per component)
- 10 E2E test scenarios covering complete workflows
- 24 acceptance criteria from spec.md
- Security requirements (password never logged, HTTPS-only, no browser storage)
- Accessibility requirements (WCAG 2.1 AA)
- Performance requirements (quantified latency targets)

**Risks**: None - quality gates are explicit and testable

---

### Principle 3: Simplicity Over Cleverness ✅
**Assessment**: **PASS** - Uses industry-standard Angular patterns without clever abstractions.

**Evidence**:
- Three standalone components (simplest Angular 19 pattern)
- Reactive Forms with FormBuilder (industry standard, well-documented)
- Material Design components (framework used as intended)
- No custom state management libraries (router state + service sufficient)
- Explicit validation (no magic validators)

**Risks**: None - straightforward implementation

---

### Principle 4: Specification Drives Implementation ✅
**Assessment**: **PASS** - Complete spec exists with clarifications; this plan follows spec-kit workflow.

**Evidence**:
- spec.md Section 8 is complete (FR8.1-FR8.4 + User Story 8)
- 5 clarifications applied via interactive Q&A (Session 2026-04-06)
- This plan.md created via `/speckit.plan` workflow
- tasks.md will be generated via `/speckit.tasks` workflow (next step)
- Implementation follows SPECIFY → PLAN → TASKS → IMPLEMENT

**Risks**: None - specification-driven workflow followed

---

### Principle 5: Tests Must Prove They Work ✅
**Assessment**: **PASS** - E2E tests include observable failure scenarios; unit tests follow TDD.

**Evidence**:
- E2E Test 2: Validation errors (deliberately invalid inputs)
- E2E Test 3: Duplicate email error (tests business rule R1.3)
- E2E Test 5-6: Invalid/expired tokens (tests error handling)
- Unit tests for custom validators (password strength, conditional required)
- Tests validate business rules, not just "returns 200 OK"

**TDD Approach**:
1. Write unit test for password strength validator (expect specific thresholds:8-11=weak, 12-15=medium, 16+=strong)
2. See test fail (validator doesn't exist yet)
3. Implement validator
4. See test pass

**Risks**: None - tests have epistemic value (can fail if implementation wrong)

---

### Principle 6: AI Augments, Humans Decide ✅
**Assessment**: **PASS** - Human-driven architecture; AI can generate boilerplate after design locked.

**Evidence**:
- **Human decisions** (documented in this plan):
  - Component architecture: 3 standalone components (vs. feature module or smart/dumb pattern)
  - Form strategy: Reactive Forms (vs. template-driven or signal-based)
  - Routing: Query param for activation token (vs. path param)
  - Validation: Password strength service (vs. directive or validator)
  - State management: Router state for pending email (vs. localStorage or service)
- **AI can generate**:
  - Component boilerplate (ng generate component)
  - Form HTML templates (Material component syntax)
  - Test stubs (describe/it blocks)
  - Validator function signatures

**Critical areas requiring human implementation**:
- Password strength calculation logic (business rule from clarifications)
- Conditional OrganizationName validator (depends on ActorType)
- HTTP error mapping (400/409/500 to form field errors)
- E2E test scenarios (requires business context understanding)

**Risks**: None - clear separation of human design vs. AI generation

---

### Principle 7: Architecture Must Support Evolution ✅
**Assessment**: **PASS** - Registration feature is extensible for v2.0 enhancements without breaking v1.0.

**Evidence**:
- **Standalone components**: Can evolve independently (e.g., v2.0 adds OAuth providers → new SocialLoginComponent doesn't affect RegisterComponent)
- **Reusable validators**: Password strength and conditional validators can be used in future forms (Profile EditForm, ChangePasswordForm)
- **Service abstraction**: RegistrationService can gain new methods (v2.0: resendActivationEmail, verifyEmailAvailability)
- **Testable boundaries**: Unit tests validate component behavior independently of backend

**v2.0 Evolution Path** (future, not in this plan):
- Add OrganizationRegistrationComponent (extends RegisterComponent pattern)
- Add multi-factor authentication (add MFASetupComponent after activation)
- Add social login (SocialLoginComponent reuses auth patterns)

**Avoiding v2.0 Premature Implementation**:
- NOT building organization fields in v1.0
- NOT building social login in v1.0
- NOT building MFA in v1.0
- Keeping v1.0 focused: individual user registration only

**Risks**: None - clean boundaries enable evolution

---

### **CONSTITUTION CHECK RESULT: ✅ PASS ALL GATES**

**Summary**: Registration UI feature aligns with all 7 constitutional principles. No violations. No trade-offs required. Proceed to Phase 0 research.

**Gates Cleared**:
- ✅ User Experience First
- ✅ Quality is Non-Negotiable
- ✅ Simplicity Over Cleverness
- ✅ Specification Drives Implementation
- ✅ Tests Must Prove They Work
- ✅ AI Augments, Humans Decide
- ✅ Architecture Must Support Evolution

**Next Phase**: Phase 0 - Research (identify unknowns, evaluate technology choices, document best practices)

## Project Structure

### Documentation (this feature)

```text
specs/001-platform-core/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (technology choices, best practices)
├── data-model.md        # Phase 1 output (form data models, validation rules)
├── quickstart.md        # Phase 1 output (developer setup guide)
├── contracts/           # Phase 1 output (component interfaces)
│   ├── registration-form.interface.ts
│   ├── registration-service.interface.ts
│   └── validators.interface.ts
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/Innoventity.Client/
├── src/
│   ├── app/
│   │   ├── features/
│   │   │   └── registration/          # NEW: Registration feature
│   │   │       ├── register/
│   │   │       │   ├── register.component.ts
│   │   │       │   ├── register.component.html
│   │   │       │   ├── register.component.scss
│   │   │       │   └── register.component.spec.ts
│   │   │       ├── pending-activation/
│   │   │       │   ├── pending-activation.component.ts
│   │   │       │   ├── pending-activation.component.html
│   │   │       │   ├── pending-activation.component.scss
│   │   │       │   └── pending-activation.component.spec.ts
│   │   │       ├── activate/
│   │   │       │   ├── activate.component.ts
│   │   │       │   ├── activate.component.html
│   │   │       │   ├── activate.component.scss
│   │   │       │   └── activate.component.spec.ts
│   │   │       └── services/
│   │   │           ├── registration.service.ts
│   │   │           ├── registration.service.spec.ts
│   │   │           └── password-strength.service.ts
│   │   │
│   │   ├── shared/
│   │   │   ├── validators/            # NEW: Shared validators
│   │   │   │   ├── password-validators.ts
│   │   │   │   ├── password-validators.spec.ts
│   │   │   │   ├── conditional-validators.ts
│   │   │   │   ├── conditional-validators.spec.ts
│   │   │   │   ├── email-validators.ts
│   │   │   │   └── email-validators.spec.ts
│   │   │   └── models/
│   │   │       └── registration.model.ts  # NEW: TypeScript interfaces
│   │   │
│   │   └── app.routes.ts              # MODIFIED: Add registration routes
│   │
│   └── environments/
│       ├── environment.ts
│       └── environment.prod.ts
│
└── e2e/
    └── registration/                   # NEW: E2E test suite
        ├── registration-happy-path.spec.ts
        ├── registration-validation.spec.ts
        ├── registration-errors.spec.ts
        ├── activation.spec.ts
        └── registration-accessibility.spec.ts
```

**Structure Decision**:

**Feature-based organization** chosen for registration components (under `src/app/features/registration/`). This aligns with Angular best practices for bounded contexts and supports future feature growth (v2.0 could add `features/organizations/`, `features/partnerships/`, etc.).

**Standalone components** used throughout (Angular 19 recommendation). No feature module created - components declare their own dependencies directly.

**Shared validators** placed in `src/app/shared/validators/` for reusability across future forms (e.g., ChangePasswordForm in v2.0 can reuse password strength validator).

**E2E tests** organized by scenario type (happy path, validation, errors, accessibility) for clarity and maintainability.

## Complexity Tracking

**Status**: ✅ **NO VIOLATIONS** - Complexity tracking not required

**Ratification**: Constitution Check passed all 7 principles without violations. No trade-offs, exceptions, or justifications needed.

**Historical Record**:
- Constitution Check Date: 2026-04-06
- Violations: 0
- Trade-offs Justified: 0
- Exceptions Granted: 0

**Re-evaluation Trigger**: Phase 1 design completion (after data-model.md, contracts/, quickstart.md generated). If architectural decisions introduce complexity concerns, this section will be updated.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |

---

## Phase 0: Research & Technology Choices

**Status**: ✅ **COMPLETE** (Generated: research.md - Registration UI section)

**Research Completed**: All technical unknowns resolved. Registration UI research documented in [research.md](./research.md#registration-ui-feature-research-phase-1).

### Technology Choices Validated

**Component Architecture** (Research Decision 1):
- **Choice**: Standalone Components (no NgModules)
- **Rationale**: Angular 19 best practice, simpler, future-proof
- **Alternatives Rejected**: Feature modules (outdated), smart/dumb pattern (over-engineering)

**Form Strategy** (Research Decision 2):
- **Choice**: Reactive Forms with FormBuilder
- **Rationale**: Industry standard, strong typing, testable, custom validators straightforward
- **Alternatives Rejected**: Template-driven forms (weak typing), signal-based forms (experimental)

**Material Component Selection** (Research Decision 3):
- **Choice**: Angular Material 19 with "outline" appearance
- **Rationale**: Accessible, responsive, consistent with existing app, handles WCAG 2.1 AA automatically
- **Components**: mat-form-field, mat-input, mat-select, mat-progress-bar, mat-raised-button

**Custom Validators** (Research Decision 4):
- **Password Strength**: Custom ValidatorFn with detailed error object
- **Password Match**: Cross-field validator on FormGroup
- **Conditional Required**: Dynamic validator update on actorType change
- **Email**: Built-in Validators.email (sufficient)

**Password Strength Indicator** (Research Decision 5):
- **Choice**: Service-based calculation
- **Rationale**: Separation of concerns, testable, reusable
- **Implementation**: PasswordStrengthService with calculateStrength(), getStrengthColor(), getStrengthPercentage()

**Routing & Navigation** (Research Decision 6):
- **Routes**: `/register`, `/register/pending-activation` (guarded), `/activate?token={token}` (no guard)
- **Guard**: registrationGuard prevents direct access to pending-activation
- **State**: Router state for transient data (email passed to pending-activation)

**Error Handling** (Research Decision 7):
- **Strategy**: Inline errors (Material mat-error) + global banner for server errors
- **Mapping**: HTTP 400 → form field errors, HTTP 409 → email field error, HTTP 500 → global banner

**State Management** (Research Decision 8):
- **Choice**: Router state for transient data
- **Rationale**: Simplest solution, no persistence needed, security benefit (email doesn't persist)
- **Alternatives Rejected**: Service (over-engineering), localStorage (security risk)

**Testing Strategy** (Research Decision 9):
- **Unit Tests**: Jest with Material component harnesses, 80%+ coverage
- **E2E Tests**: Playwright with axe-core for accessibility, 10 scenarios from spec.md
- **TDD Approach**: Write test → implement → verify green

**Risks Identified & Mitigated**:
1. Password strength validator complexity → TDD approach, test all combinations
2. Conditional validator edge cases → Test all 5 actor types
3. E2E test flakiness → Playwright waitFor methods
4. Material 19 breaking changes → Follow upgrade guide, use component harnesses

**Best Practices Documented**:
- Angular 19: Standalone components, async pipe, takeUntilDestroyed
- Reactive Forms: FormBuilder, type-safe FormGroup, validators in separate files
- Material Design: appearance="outline", mat-label, mat-error, mat-hint
- Security: Never log passwords, never store in localStorage, HTTPS enforced
- Accessibility: aria-label, keyboard navigation, color contrast, touch targets

**Research Output**: See [research.md](./research.md) for complete research findings.

---

## Phase 1: Design & Contracts

**Status**: ✅ **COMPLETE** (Generated: data-model-registration-ui.md, quickstart-registration-ui.md, contracts/registration-ui-contracts.md)

### Data Model Design

**TypeScript Interfaces** (See [data-model-registration-ui.md](./data-model-registration-ui.md)):
- RegistrationFormModel (6 fields: email, password, confirmPassword, actorType, fullName, organizationName)
- ActorType (enum: 5 values)
- RegistrationResponse (API response)
- ActivationRequest/ActivationResponse
- PendingActivationState (router state)
- PasswordStrength (type: 'weak' | 'medium' | 'strong')
- PasswordStrengthIndicator (display properties)

**Validation Rules**:
- Email: Validators.required, Validators.email
- Password: Validators.required, passwordStrengthValidator() (8+ chars, uppercase, lowercase, digit, special)
- Confirm Password: Validators.required, passwordMatchValidator() (cross-field)
- Actor Type: Validators.required
- Full Name: Validators.required, minLength(2), maxLength(100)
- Organization Name: **Dynamic validators** (required for all except Idea Generator)

**Password Strength Calculation**:
- Weak: 8-11 characters (meets requirements)
- Medium: 12-15 characters
- Strong: 16+ characters

**API Error Models**:
- ValidationErrorResponse (HTTP 400)
- DuplicateEmailErrorResponse (HTTP 409)
- ActivationErrorResponse (HTTP 400/404)

**Component State Models**:
- RegisterComponent: registrationForm, isSubmitting, globalError, passwordStrength, hidePassword flags
- PendingActivationComponent: email (from router state)
- ActivateComponent: token, isActivating, activationSuccess, activationError, networkError

### Component Contracts

**RegisterComponent** (See [contracts/registration-ui-contracts.md](./contracts/registration-ui-contracts.md)):
- Public Interface: registrationForm, isSubmitting, globalError, passwordStrength, onSubmit(), togglePasswordVisibility()
- Dependencies: FormBuilder, Router, RegistrationService, PasswordStrengthService
- Template: 6 form fields, submit button, password strength indicator, error banner

**PendingActivationComponent**:
- Public Interface: email, navigateToLogin()
- Dependencies: Router
- Template: Success message, email display, "Continue to Login" button

**ActivateComponent**:
- Public Interface: token, isActivating, activationSuccess, activationError, retry(), navigateToLogin()
- Dependencies: ActivatedRoute, Router, ActivationService
- Template: Loading spinner, success message, error message, retry button

### Service Contracts

**RegistrationService**:
- Method: register(data: RegistrationFormModel): Observable<RegistrationResponse>
- HTTP: POST /api/auth/register
- Errors: 400 (validation), 409 (duplicate email), 500 (server error)

**ActivationService**:
- Method: activate(request: ActivationRequest): Observable<ActivationResponse>
- HTTP: POST /api/auth/activate
- Errors: 400 (invalid token), 404 (token not found)

**PasswordStrengthService**:
- Methods: calculateStrength(), getStrengthIndicator(), getStrengthColor(), getStrengthPercentage(), getStrengthLabel()
- Pure functions (testable, reusable)

### Validator Contracts

**passwordStrengthValidator()**:
- Type: ValidatorFn
- Error Object: `{ passwordStrength: { hasMinLength, hasUppercase, hasLowercase, hasDigit, hasSpecial } }`

**passwordMatchValidator()**:
- Type: ValidatorFn (form-level)
- Parameters: passwordKey, confirmPasswordKey
- Error Object: `{ passwordMismatch: true }`

### Routing Contracts

**Routes**:
- `/register` → RegisterComponent (no guard)
- `/register/pending-activation` → PendingActivationComponent (registrationGuard)
- `/activate?token={token}` → ActivateComponent (no guard, allows email link access)

**registrationGuard**:
- Purpose: Prevent direct access to pending-activation without registration flow
- Logic: Check router.getCurrentNavigation() for email in state
- Redirect: Navigate to /register if no email

### Developer Quickstart

**Setup** (See [quickstart-registration-ui.md](./quickstart-registration-ui.md)):
1. Install dependencies: `npm install`
2. Start dev server: `npm start` (localhost:4200)
3. Verify backend: `curl http://localhost:5000/api/health`

**Testing**:
- Unit tests: `npm run test` (Jest, 80%+ coverage target)
- E2E tests: `npm run e2e` (Playwright, 10 scenarios)
- Accessibility: `npx playwright test e2e/registration/registration-accessibility.spec.ts` (axe-core)

**Manual Testing Checklist**:
- Register component: 6 fields visible, actor type change toggles organization name "(Optional)" label
- Password strength indicator: Updates as user types (weak/medium/strong)
- Validation errors: Inline below fields, list missing password requirements
- Successful registration: Navigate to pending-activation with email displayed
- Activation: Token from email link activates account, success message + "Continue to Login" button

**Debugging**:
- Backend not reachable: Check CORS, proxy.conf.json
- Material components failing in tests: Import NoopAnimationsModule
- E2E tests flaky: Add waitForSelector(), increase timeout

---

## Phase 2: Architectural Decision Summary

### Decision 1: Component Structure

**Question**: How many components are needed, and what are their responsibilities?

**Decision**: 3 standalone components (separation by user flow stage)

**Components**:
1. **RegisterComponent**: Registration form (6 fields, validation, submission)
2. **PendingActivationComponent**: Success message (displays email, "Continue to Login" button)
3. **ActivateComponent**: Token validation and activation (query parameter, API call, success/error states)

**Rationale**:
- Each component has single responsibility (Principle 3: Simplicity)
- User flow stages are distinct (register → pending → activate)
- No unnecessary abstraction (no "FormWrapperComponent" or similar)
- Testable in isolation (each component has clear inputs/outputs)

**Alternatives Rejected**:
- Single component with wizard steps: Rejected - complex state management, harder to test
- Shared FormComponent: Rejected - over-abstraction, forms are different enough
- Smart/dumb component split: Rejected - over-engineering for simple forms

---

### Decision 2: Form Implementation Strategy

**Question**: How should the registration form be implemented?

**Decision**: Reactive Forms with FormBuilder, strong typing

**Implementation**:
```typescript
registrationForm = this.fb.group<RegistrationFormModel>({
  email: ['', [Validators.required, Validators.email]],
  password: ['', [Validators.required, passwordStrengthValidator()]],
  confirmPassword: ['', [Validators.required]],
  actorType: ['', [Validators.required]],
  fullName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
  organizationName: ['', []] // Dynamic validators
}, { validators: passwordMatchValidator('password', 'confirmPassword') });
```

**Rationale**:
- Reactive Forms are Angular industry standard
- FormBuilder reduces boilerplate
- Strong typing with `FormGroup<RegistrationFormModel>` provides IDE support and type safety
- Custom validators straightforward (ValidatorFn interface)
- Cross-field validation (password match) built-in
- Observable valueChanges perfect for password strength indicator

**Alternatives Rejected**:
- Template-driven forms: Weak typing, async validation harder to test
- Signal-based forms: Experimental, limited documentation, not production-ready

---

### Decision 3: Material Component Mapping

**Question**: Which Material components should be used for each form field?

**Decision**: Material Design components with "outline" appearance

**Mapping**:
| Form Field | Material Component | Why |
|------------|-------------------|-----|
| **Email** | `<mat-form-field appearance="outline">` + `<input matInput type="email">` | Standard text input, autocomplete support |
| **Password** | `<mat-form-field>` + `<input matInput [type]="hidePassword ? 'password' : 'text'">` + visibility toggle | Password masking, show/hide button |
| **Confirm Password** | Same as Password | Consistency |
| **Actor Type** | `<mat-select>` with `<mat-option>` (5 values) | Dropdown, keyboard accessible, Material styled |
| **Full Name** | `<mat-form-field>` + `<input matInput type="text">` | Standard text input |
| **Organization Name** | Same as Full Name | Consistency |
| **Password Strength** | `<mat-progress-bar mode="determinate">` | Visual indicator, color-coded (warn/accent/primary) |
| **Submit Button** | `<button mat-raised-button color="primary">` | Primary action, elevated style |

**appearance="outline"**: Modern style, clear field boundaries, good for data-heavy forms

**Accessibility Built-in**:
- Material components automatically add ARIA attributes
- Keyboard navigation (Tab order, Enter to submit)
- Touch targets 44x44px (WCAG compliant)
- Color contrast meets WCAG 2.1 AA

**Rationale**:
- Material Design handles responsive design automatically
- Consistent with existing app (LoginComponent uses Material)
- Reduces custom CSS (Material theming provides consistent colors)

---

### Decision 4: Routing & Navigation Flow

**Question**: How should users navigate between registration stages?

**Decision**: Simple routes with one guard

**Routes**:
```typescript
{
  path: 'register',
  component: RegisterComponent
},
{
  path: 'register/pending-activation',
  component: PendingActivationComponent,
  canActivate: [registrationGuard] // Prevent direct access
},
{
  path: 'activate',
  component: ActivateComponent
  // No guard - must allow email link access
}
```

**Navigation Flows**:
1. **Register → Pending Activation**: `router.navigate(['/register/pending-activation'], { state: { email } })`
2. **Pending Activation → Login**: `router.navigate(['/login'])`
3. **Activate → Login**: `router.navigate(['/login'])` (after success)
4. **Login ↔ Register**: `routerLink="/register"` (existing login page has link)

**registrationGuard Logic**:
```typescript
export const registrationGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const navigation = router.getCurrentNavigation();

  if (navigation?.extras.state?.['email']) {
    return true; // Allow navigation (came from registration flow)
  }

  router.navigate(['/register']); // Redirect if no email in state
  return false;
};
```

**Rationale**:
- Guard prevents users from bookmarking pending-activation page
- Activation route has NO guard (email links must work)
- Router state used for transient data (email) - simpler than service or localStorage
- Query parameter format `/activate?token={token}` (Clarification Q4)

**Alternatives Rejected**:
- Service-based state management: Over-engineering for transient email string
- localStorage for email: Security risk (email persisted in browser)
- Path parameter for token `/activate/:token`: Rejected - query parameter preferred per clarification

---

### Decision 5: Service Layer Design

**Question**: What services are needed, and what are their responsibilities?

**Decision**: 3 services (separation of concerns)

**Services**:
1. **RegistrationService**: HTTP calls to backend API
   - `register(data: RegistrationFormModel): Observable<RegistrationResponse>`
   - POST /api/auth/register
2. **ActivationService**: Activation API call (could be merged with RegistrationService, but kept separate for clarity)
   - `activate(request: ActivationRequest): Observable<ActivationResponse>`
   - POST /api/auth/activate
3. **PasswordStrengthService**: Password strength calculation (UI logic, not API call)
   - `calculateStrength(password: string): PasswordStrength`
   - `getStrengthIndicator(strength: PasswordStrength): PasswordStrengthIndicator`

**Why Separate PasswordStrengthService?**:
- Testable (pure functions, no HTTP dependencies)
- Reusable (future ChangePasswordForm can use same service)
- Separation of concerns (UI logic vs API calls)

**Rationale**:
- Services injected with `{ providedIn: 'root' }` (singleton, available app-wide)
- HTTP calls return Observables(async, chainable, cancellable)
- Error handling done in component (map HTTP errors to form field errors)

**Alternatives Rejected**:
- Single RegistrationService for all: Rejected - password strength is UI logic, not API related
- Password strength in component: Rejected - harder to test, not reusable

---

### Decision 6: Validation Strategy

**Question**: How should custom validation be implemented?

**Decision**: Custom validators in shared/validators/, dynamic validator updating

**Validators**:
1. **passwordStrengthValidator()**: ValidatorFn (synchronous)
   - Checks: 8+ chars, uppercase, lowercase, digit, special char
   - Error Object: `{ passwordStrength: { hasMinLength, hasUppercase, ... } }`
2. **passwordMatchValidator()**: ValidatorFn (form-level, cross-field)
   - Compares password and confirmPassword
   - Error Object: `{ passwordMismatch: true }`
3. **Dynamic Organization Name Validators**:
   - actorType === 'IdeaGenerator' → optional (no Validators.required)
   - actorType !== 'IdeaGenerator' → required (add Validators.required)
   - Implementation: Subscribe to actorType valueChanges, call setValidators()

**Error Display Strategy**:
```html
<mat-error *ngIf="password.hasError('passwordStrength')">
  Password must include:
  <ul>
    <li *ngIf="!password.getError('passwordStrength').hasMinLength">At least 8 characters</li>
    <li *ngIf="!password.getError('passwordStrength').hasUppercase">One uppercase letter</li>
    <!-- ... -->
  </ul>
</mat-error>
```

**Rationale**:
- Validators are pure functions (testable independently)
- Detailed error objects provide specific feedback
- Dynamic validators handle conditional logic (organization name required/optional based on actor type)
- Built-in Validators.email sufficient (no custom email validator needed)

**Clarification Applied**: Organization Name field ALWAYS VISIBLE, marked "(Optional)" for Idea Generator (not hidden dynamically)

---

### Decision 7: Error Handling Strategy

**Question**: How should API errors be displayed to users?

**Decision**: Inline errors (Material mat-error) + global banner for server errors

**Error Categories**:
1. **Client-side validation**: Inline below form fields (mat-error)
2. **HTTP 400 (Validation)**: Map API errors to form field errors (setErrors())
3. **HTTP 409 (Duplicate Email)**: Set error on email field with custom message
4. **HTTP 500 (Server Error)**: Global error banner (not inline)
5. **Network Error (status 0)**: Retry mechanism + error message

**HTTP Error Mapping**:
```typescript
error: (httpError: HttpErrorResponse) => {
  if (httpError.status === 400) {
    this.mapApiErrorsToForm(httpError.error); // Field-specific errors
  } else if (httpError.status === 409) {
    this.email.setErrors({ emailExists: 'Email already registered. Try logging in.' });
  } else if (httpError.status === 0) {
    this.globalError = 'Network error. Check your connection and try again.';
  } else {
    this.globalError = 'Registration failed. Please try again later.';
  }
}
```

**Rationale**:
- Clear separation: Form validation errors inline, server errors in banner
- User-friendly messages (no HTTP status codes exposed)
- Retry mechanism for network issues (UX improvement)
- Form state retained on error (user doesn't lose entered data)

**Alternatives Rejected**:
- All errors in global banner: Rejected - users can't see which field has error
- Modal dialogs for errors: Rejected - disrupts workflow, less accessible

---

### Decision 8: Testing Implementation Strategy

**Question**: How should unit tests and E2E tests be structured?

**Decision**: TDD approach, Material component harnesses, Playwright with axe-core

**Unit Testing (Jest)**:
- Test file pattern: `*.spec.ts` (same directory as source)
- Component tests: Form validation logic, dynamic validator updates, error handling
- Service tests: HTTP calls with HttpTestingController, password strength calculations
- Validator tests: Pure function testing (valid/invalid inputs)
- Material component harnesses: Future-proof testing (use harnesses, not DOM queries)

**Test Example**:
```typescript
it('should update organization name validators when actor type changes', () => {
  component.registrationForm.get('actorType')!.setValue('IdeaGenerator');
  const orgNameControl = component.registrationForm.get('organizationName')!;
  orgNameControl.setValue('');
  expect(orgNameControl.hasError('required')).toBeFalsy(); // Optional for Idea Generator

  component.registrationForm.get('actorType')!.setValue('RDOrganization');
  orgNameControl.updateValueAndValidity();
  expect(orgNameControl.hasError('required')).toBeTruthy(); // Required for R&D Org
});
```

**E2E Testing (Playwright)**:
- Test scenarios: 10 from spec.md Section 8 (happy path, validation, errors, activation, accessibility)
- Page objects: Not needed (simple forms, direct selectors sufficient)
- Accessibility: axe-core integration for WCAG 2.1 AA validation
- Test data: Factories for valid/invalid registration data

**E2E Example**:
```typescript
test('complete registration workflow', async ({ page }) => {
  await page.goto('/register');
  await page.fill('[formControlName="email"]', 'test@example.com');
  // ... fill all fields
  await page.click('button[type="submit"]');
  await expect(page).toHaveURL('/register/pending-activation');
  await expect(page.locator('.email-display')).toContainText('test@example.com');
});
```

**Coverage Target**: 80%+ (lines, branches)

**Rationale**:
- TDD ensures tests written before implementation (Principle 5: Tests Prove They Work)
- Material harnesses decouple tests from implementation details
- Playwright more reliable than Protractor (modern, cross-browser)
- axe-core automates accessibility testing (WCAG 2.1 AA compliance)

---

### Decision 9: Implementation Phases

**Question**: In what order should components be implemented?

**Decision**: 6 phases (sequential + parallel opportunities)

**Phases**:

**Phase 1: Foundation** (Parallel work possible):
- Task 1.1: Create models and interfaces (registration.model.ts, ActorType enum)
- Task 1.2: Create services (RegistrationService, ActivationService, PasswordStrengthService)
- Task 1.3: Create validators (passwordStrengthValidator, passwordMatchValidator)
- Task 1.4: Configure routes (app.routes.ts, registrationGuard)
- **Duration**: 2-3 hours
- **Dependencies**: None (all can start immediately)

**Phase 2: RegisterComponent** (Sequential):
- Task 2.1: Generate component, create FormGroup structure
- Task 2.2: Implement password strength indicator (subscribe to valueChanges)
- Task 2.3: Implement dynamic organization name validators (actorType valueChanges)
- Task 2.4: Implement form submission and error handling
- Task 2.5: Create template (6 fields, Material components, error messages)
- Task 2.6: Write unit tests (15+ tests)
- **Duration**: 4-6 hours
- **Dependencies**: Phase 1 complete

**Phase 3: PendingActivationComponent** (Can start after Phase 1):
- Task 3.1: Generate component, extract email from router state
- Task 3.2: Create template (success message, email display, button)
- Task 3.3: Implement guard logic (registrationGuard)
- Task 3.4: Write unit tests (3+ tests)
- **Duration**: 1-2 hours
- **Dependencies**: Phase 1 complete (router state, guard)

**Phase 4: ActivateComponent** (Can start after Phase 1):
- Task 4.1: Generate component, extract token from query parameter
- Task 4.2: Implement activation API call and state management
- Task 4.3: Implement retry mechanism
- Task 4.4: Create template (loading, success, error, retry button)
- Task 4.5: Write unit tests (5+ tests)
- **Duration**: 2-3 hours
- **Dependencies**: Phase 1 complete (ActivationService)

**Phase 5: E2E Testing** (After Phases 2-4):
- Task 5.1: Write happy path test (register → pending → activate → login)
- Task 5.2: Write validation tests (invalid email, weak password, password mismatch)
- Task 5.3: Write error tests (duplicate email, invalid token, network error)
- Task 5.4: Write activation tests (valid/expired token)
- Task 5.5: Write accessibility tests (axe-core, keyboard navigation)
- **Duration**: 3-4 hours
- **Dependencies**: Phases 2, 3, 4 complete

**Phase 6: Documentation** (Final phase):
- Task 6.1: Update README.md (setup instructions, testing commands)
- Task 6.2: Add inline code comments (complex logic only)
- Task 6.3: Verify all acceptance criteria met (24 from spec.md)
- **Duration**: 1 hour
- **Dependencies**: Phase 5 complete

**Total Estimated Duration**: 14-20 hours (solo developer)

**Parallelization Opportunities**:
- Phase 3 and Phase 4 can be done in parallel (independent components)
- Phase 1 tasks can be done in parallel (no interdependencies)

**Critical Path**: Phase 1 → Phase 2 → Phase 5 → Phase 6

**Rationale**:
- Phase 1 establishes foundation (all components depend on models/services)
- RegisterComponent is most complex (should be done before moving to simpler components)
- PendingActivationComponent and ActivateComponent are simple (can be parallelized)
- E2E tests verify end-to-end workflow (must wait for all components)
- Documentation is final step (ensures all work complete before documenting)

---

## Dependencies & Integration Points

### NPM Dependencies (Already Installed)

**Frontend (Angular)**:
- @angular/core: ^19.0.0 (already in package.json)
- @angular/material: ^19.0.0 (already in package.json)
- @angular/forms: ^19.0.0 (ReactiveFormsModule)
- @angular/router: ^19.0.0 (Router, ActivatedRoute)
- @angular/common: ^19.0.0 (CommonModule for *ngIf, *ngFor)
- rxjs: ^7.x (Observable, debounceTime, finalize)

**Testing**:
- jest: ^29.x (unit testing)
- @playwright/test: ^1.x (E2E testing)
- @axe-core/playwright: Latest (accessibility testing)

**NO NEW DEPENDENCIES REQUIRED** - All packages already installed in existing Angular app.

### Backend API Dependencies

**Existing Phase 0 APIs** (No modifications required):
- POST /api/auth/register
  - Request: `{ email, password, actorType, fullName, organizationName }`
  - Response (201): `{ message }`
  - Errors: 400 (validation), 409 (duplicate email), 500 (server error)
- POST /api/auth/activate
  - Request: `{ token }`
  - Response (200): `{ message }`
  - Errors: 400 (invalid token), 404 (not found)

**Backend Running Requirement**: Backend API must be running on http://localhost:5000 (or configured API URL) for development and E2E testing.

### Integration with Existing Components

**LoginComponent Integration**:
- Add "Register here" link in login template:
  ```html
  <p>Don't have an account? <a routerLink="/register">Register here</a></p>
  ```
- After successful activation, user is directed to login page with registered credentials

**DashboardComponent Integration**:
- After successful login (post-activation), user navigates to dashboard
- No changes needed to DashboardComponent (existing auth guard handles authenticated users)

**AuthService Integration**:
- RegistrationService does NOT use AuthService (registration happens before authentication)
- After activation + login, AuthService.login() is called (existing flow)

### Environment Configuration

**Development (`environment.ts`)**:
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api' // Local backend
};
```

**Production (`environment.prod.ts`)**:
```typescript
export const environment = {
  production: true,
  apiUrl: 'https://innoventity-api.azurewebsites.net/api' // Azure backend
};
```

**Proxy Configuration** (`proxy.conf.json`):
```json
{
  "/api": {
    "target": "http://localhost:5000",
    "secure": false
  }
}
```

---

## Open Questions & Assumptions

### Assumptions

1. **Backend APIs Functional**: Assumes POST /auth/register and POST /auth/activate APIs already implemented and tested in Phase 0.
2. **Email Sending Working**: Assumes backend sends activation email with token link (e.g., `https://app.innoventity.com/activate?token=abc123`).
3. **Token Format**: Assumes activation token is URL-safe string (base64, GUID, or JWT).
4. **No CAPTCHA Required**: Assumes no bot protection needed for v1.0 (defer to v2.0 if spam becomes issue).
5. **Existing LoginComponent**: Assumes LoginComponent already exists and can accept newly activated users.

### Resolved Questions

All questions from REGISTER_UI_PLANNING_PROMPT.md resolved:
1. ✅ Component structure: 3 standalone components
2. ✅ Form strategy: Reactive Forms with FormBuilder
3. ✅ Material component mapping: Documented in Decision 3
4. ✅ Routing: Simple routes with registrationGuard
5. ✅ Service layer: 3 services (Registration, Activation, PasswordStrength)
6. ✅ Validation: Custom validators in shared/validators/
7. ✅ Error handling: Inline + global banner
8. ✅ Testing: Jest + Playwright + axe-core
9. ✅ Implementation phases: 6 phases documented

### No Open Questions

**Status**: All architectural decisions documented. Ready for task generation (`/speckit.tasks`).

---

## Risk Assessment

### Technical Risks

**Risk 1: Password Strength Validator Complexity**
- **Impact**: Medium (weak passwords allowed if validator has bugs)
- **Likelihood**: Low (TDD approach reduces risk)
- **Mitigation**: Write comprehensive unit tests (test all requirement combinations), use existing regex patterns from backend for consistency

**Risk 2: Conditional Organization Name Validation**
- **Impact**: Medium (required field not enforced or vice versa)
- **Likelihood**: Low (straightforward logic)
- **Mitigation**: Test all 5 actor types, test actorType changes after user input, E2E test covers scenario

**Risk 3: E2E Test Flakiness**
- **Impact**: Low (test reliability, not production code)
- **Likelihood**: Medium (Playwright tests can be flaky)
- **Mitigation**: Use Playwright waitForSelector(), increase timeout, mock email in test environment

**Risk 4: Material Design Version Compatibility**
- **Impact**: Low (syntax changes)
- **Likelihood**: Low (Material 19 stable)
- **Mitigation**: Follow official upgrade guide, use Material component harnesses for future-proof tests

**Risk 5: Browser Compatibility (Safari)**
- **Impact**: Low (layout issues on Safari)
- **Likelihood**: Low (Material Design handles compatibility)
- **Mitigation**: E2E tests run on WebKit (Safari engine), use autoprefixer

### UX Risks

**Risk 6: Password Strength Indicator Confusing**
- **Impact**: Medium (users don't understand weak/medium/strong)
- **Likelihood**: Low (industry standard pattern)
- **Mitigation**: Provide hint text ("Use 16+ characters for strong password"), color-coded progress bar (red/yellow/green)

**Risk 7: Organization Name "(Optional)" Label Not Clear**
- **Impact**: Low (users confused about when field is required)
- **Likelihood**: Low (label is explicit)
- **Mitigation**: E2E test verifies field behavior, user testing during beta (Phase 7 - Launch Criteria)

### Security Risks

**Risk 8: Password Visible in Network Tab**
- **Impact**: Medium (password exposed in browser DevTools)
- **Likelihood**: N/A (inherent to web apps)
- **Mitigation**: HTTPS enforced (Azure Static Web App), educate users to not use public computers

**Risk 9: Email Stored in Router State**
- **Impact**: Low (email persisted in browser history)
- **Likelihood**: Low (router state is transient)
- **Mitigation**: Router state cleared on page refresh (security benefit), email is not sensitive PII

### Overall Risk Level

**Assessment**: ✅ **LOW RISK**

**Justification**:
- All high/medium impact risks have mitigations
- TDD approach reduces implementation bugs
- Existing backend APIs reduce integration risks
- Material Design reduces UI/UX risks
- No new dependencies or complex state management

---

## Success Criteria & Acceptance

### Functional Requirements (from spec.md Section 8)

**FR8.1 Registration Form** (24 acceptance criteria):
- ✅ All 24 criteria testable via E2E tests
- ✅ Password strength validation enforced (8+ chars, uppercase, lowercase, digit, special)
- ✅ Organization Name field always visible, marked "(Optional)" for Idea Generator

**FR8.2 Form Submission**:
- ✅ POST /api/auth/register called with form data (without confirmPassword)
- ✅ Success → Navigate to pending-activation with email displayed
- ✅ Duplicate email → Email field error "Email already registered"

**FR8.3 Activation Flow**:
- ✅ Route format: `/activate?token={token}` (query parameter, per Clarification Q4)
- ✅ Valid token → Success message + "Continue to Login" button (no auto-redirect, per Clarification Q5)
- ✅ Invalid token → Error message + "Retry" button

**FR8.4 Post-Activation**:
- ✅ User clicks "Continue to Login" → Navigate to /login
- ✅ User can log in with registered credentials

### Non-Functional Requirements (from spec.md Section 8)

**NFR8.1 Performance**:
- ✅ Form rendering: <100ms (Material Design optimized)
- ✅ Form submission: <2 seconds (backend API dependent)
- ✅ Activation validation: <1 second (backend API dependent)

**NFR8.2 Accessibility**:
- ✅ WCAG 2.1 Level AA compliant (Material Design handles, axe-core validates)
- ✅ Keyboard navigation (Tab through fields, Enter to submit)
- ✅ Screen reader support (ARIA labels, error announcements)

**NFR8.3 Responsive Design**:
- ✅ Mobile (320px - 767px): Full-width fields, vertical stacking
- ✅ Tablet (768px - 1024px): Centered form, max-width 480px
- ✅ Desktop (1024px+): Centered form, max-width 480px
- ✅ Touch targets: 44x44px (Material Design default)

**NFR8.4 Security**:
- ✅ Passwords never logged (no console.log in production)
- ✅ Passwords never stored in localStorage/sessionStorage
- ✅ HTTPS enforced (Azure Static Web App)
- ✅ Client + server validation (defense in depth)

**NFR8.5 Browser Compatibility**:
- ✅ Chrome 120+ (E2E tested on Chromium)
- ✅ Firefox 121+ (E2E tested on Firefox)
- ✅ Safari 17+ (E2E tested on WebKit)
- ✅ Edge 120+ (Chromium-based, same as Chrome)
- ✅ Mobile browsers: iOS Safari 17+, Android Chrome 120+

### Test Coverage

**Unit Tests**:
- ✅ 15+ unit tests (RegisterComponent: 7, PendingActivationComponent: 2, ActivateComponent: 4, validators: 2)
- ✅ 80%+ code coverage (lines, branches)

**E2E Tests**:
- ✅ 10 scenarios from spec.md Section 8
  1. Happy path (register → pending → activate → login)
  2. Email validation (invalid format)
  3. Password strength validation (missing requirements)
  4. Password mismatch
  5. Organization name required validation (R&D Org without org name)
  6. Organization name optional (Idea Generator without org name)
  7. Duplicate email (HTTP 409)
  8. Activation with valid token
  9. Activation with invalid token
  10. Keyboard navigation + accessibility (axe-core)

### Definition of Done

**Component Implementation**:
- ✅ All 3 components generated and implemented
- ✅ All services implemented (Registration, Activation, PasswordStrength)
- ✅ All validators implemented (passwordStrength, passwordMatch)
- ✅ All routes configured (register, pending-activation, activate)
- ✅ All templates created with Material Design components

**Testing**:
- ✅ All unit tests passing (0 failures)
- ✅ All E2E tests passing (0 failures)
- ✅ Code coverage ≥80%
- ✅ Accessibility tests passing (0 violations)

**Documentation**:
- ✅ README.md updated (setup instructions, testing commands)
- ✅ Inline comments for complex logic
- ✅ All 24 acceptance criteria verified

**Code Quality**:
- ✅ No linting errors (ng lint)
- ✅ No build warnings (ng build)
- ✅ No console.error or console.warn in production code

**Integration**:
- ✅ Backend APIs tested (POST /auth/register, POST /auth/activate)
- ✅ LoginComponent link added ("Register here")
- ✅ Environment configuration verified (dev + prod)

---

## Next Steps

**Phase 1 Complete** - All design artifacts generated:
- ✅ research.md (technology choices, best practices)
- ✅ data-model-registration-ui.md (TypeScript interfaces, validation rules, state models)
- ✅ quickstart-registration-ui.md (developer setup, testing, debugging)
- ✅ contracts/registration-ui-contracts.md (component contracts, service interfaces)
- ✅ Agent context updated (GitHub Copilot context file)

**Ready for Phase 2: Task Generation**

**Command**: Execute `/speckit.tasks` to generate tasks.md with 20-25 implementation tasks.

**Expected Output**: tasks.md with:
- Task ID, Title, Description
- Acceptance Criteria (specific, testable)
- Dependencies (which tasks must complete first)
- Estimated Effort (hours)
- Implementation order (Phase 1 → Phase 6)

**After Task Generation**: Proceed to `/speckit.implement` for automated task execution, or implement manually following task sequence.

---

**END OF PLANNING PHASE**
