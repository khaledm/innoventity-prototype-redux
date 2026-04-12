# Registration UI Planning Prompt

## Context

This prompt guides the `/speckit.plan` workflow to create comprehensive planning artifacts for the **Registration User Interface (Phase 1+)** feature specified in Section 8 of `specs/001-platform-core/spec.md`.

**Phase Constraint**: Registration UI is explicitly OUT OF SCOPE for Phase 0. This planning supports Phase 1+ implementation.

**Specification Location**: `specs/001-platform-core/spec.md` Section 8 "Registration User Interface (Phase 1+)"

**Specification Status**: Complete with 5 clarifications applied (Session 2026-04-06)

---

## Planning Objectives

Create a technical design document (`plan.md`) that answers HOW to implement the WHAT defined in Section 8. The plan must cover:

1. **Component Architecture** - Angular component structure, hierarchy, and responsibilities
2. **Routing Design** - Route configuration, guards, lazy loading strategy
3. **Form Strategy** - Reactive Forms implementation, validation approach, state management
4. **UI/UX Implementation** - Material Design component mapping, responsive design strategy
5. **Service Layer Design** - API integration, error handling, state management
6. **Testing Strategy** - Unit test structure, integration test approach, E2E test coverage
7. **Dependencies** - NPM packages, Angular modules, shared utilities
8. **Implementation Phases** - Task sequencing, parallel work opportunities, risk mitigation

---

## Existing System Context

### Backend APIs (Phase 0 — Already Implemented)

**POST /auth/register**

- Input: `{ email, password, actorType, fullName, organizationName? }`
- Success: 201 Created, returns `{ message: "Registration successful. Please check your email to activate your account." }`
- Errors:
  - 400: Validation errors (invalid email, weak password, missing required fields)
  - 409: Email already registered for this actor type

**POST /auth/activate**

- Input: `{ token }` (activation token from email)
- Success: 200 OK, returns `{ message: "Account activated successfully" }`
- Errors:
  - 400: Invalid token format
  - 404: Token not found or already used
  - 410: Token expired (24-hour validity)

**Existing Email Infrastructure**: Email sending implemented in Phase 0 (uses templates, includes activation links)

### Frontend Stack (Angular 19)

**Technology Stack**:

- Angular 19 (standalone components preferred)
- Angular Material Design 19
- Angular Reactive Forms
- RxJS for asynchronous state management
- Playwright for E2E testing
- Jest for unit testing

**Existing Patterns**:

- Signal-based state management (prefer signals over observables where appropriate)
- Standalone components (avoid NgModules unless necessary)
- Lazy-loaded routes
- Shared validation utilities
- HTTP interceptors for error handling
- API service layer pattern

**Existing Components to Potentially Reuse**:

- LoginComponent (reference for form patterns, Material Design usage)
- Shared form field components (if any)
- Error display components (if any)

**Routing Context**:

- Base URL: `https://localhost:4200` (dev) or Azure Static Web App URL (prod)
- Existing routes: `/`, `/login`, `/dashboard`, `/innovations/*`
- Auth guards: Authenticated route protection already implemented

---

## Key Architectural Decisions to Address

### 1. Component Structure

**Decision Required**: How many components and how are they organized?

**Options**:

- **A. Three standalone components**: RegisterComponent, PendingActivationComponent, ActivateComponent (each handles its route)
- **B. Feature module**: Registration module with child components (more structure, potential for shared state)
- **C. Smart/dumb pattern**: Container components + presentational components (more granular, better testability)

**Recommendation**: Option A (three standalone components) — Simplest approach, aligns with Angular 19 standalone best practices, each component is self-contained.

**Questions to Address**:

- Should components be standalone or module-based?
- Where should components live in directory structure? (`src/app/features/registration/`?)
- What shared components/utilities are needed?

---

### 2. Form Implementation Strategy

**Decision Required**: How to implement the 6-field registration form?

**Options**:

- **A. Reactive Forms with FormBuilder**: Industry standard, strong typing, easier testing
- **B. Template-driven forms**: Simpler syntax, less code, but harder to test
- **C. Signal-based forms (Angular 19+)**: Cutting-edge, excellent reactivity, smaller bundle

**Recommendation**: Option A (Reactive Forms with FormBuilder) — Well-documented, strong typing, synchronous validation, easy to test.

**Questions to Address**:

- How to structure FormGroup? (flat vs. nested)
- Where to define custom validators? (shared validators service?)
- How to handle conditional validation (Organization Name required/optional)?
- Password strength calculation: custom validator or separate service?
- How to synchronize client-side validation with backend error responses?

**Example Structure to Plan**:

```typescript
registrationForm = this.fb.group({
  email: ['', [Validators.required, Validators.email, CustomValidators.rfc5322Email]],
  password: ['', [
    Validators.required,
    Validators.minLength(8),
    CustomValidators.passwordStrength
  ]],
  confirmPassword: ['', [Validators.required]],
  actorType: ['', [Validators.required]],
  fullName: ['', [
    Validators.required,
    Validators.minLength(2),
    Validators.maxLength(100),
    CustomValidators.nameFormat
  ]],
  organizationName: ['', [
    ConditionalValidators.requiredIf('actorType', ['RDOrganization', 'Manufacturing', ...]),
    Validators.minLength(2),
    Validators.maxLength(200)
  ]]
}, {
  validators: CustomValidators.passwordMatch('password', 'confirmPassword')
});
```

---

### 3. Material Design Component Mapping

**Decision Required**: Which Material components map to each form field?

**Planning Required**:

- Email field → `<mat-form-field>` + `<input matInput type="email">`
- Password field → `<mat-form-field>` + `<input matInput type="password">` + toggle button (mat-icon-button)
- Actor Type → `<mat-form-field>` + `<mat-select>` + `<mat-option>` per actor type
- Full Name → `<mat-form-field>` + `<input matInput type="text">`
- Organization Name → `<mat-form-field>` + conditional display logic
- Submit button → `<button mat-raised-button color="primary">` with loading state via `[disabled]` and mat-spinner

**Accessibility Considerations**:

- All form fields need `aria-label` or `<mat-label>`
- Error messages need `aria-live="polite"`
- Password toggle needs `aria-label="Show/hide password"`
- Actor type select needs help text in `<mat-hint>` or tooltip

**Responsive Design**:

- Form width constraints (max-width: 480px on desktop?)
- Stack fields vertically on mobile (Material handles this)
- Touch-friendly targets (Material handles this)

---

### 4. Routing Configuration

**Decision Required**: How to structure routes and navigation?

**Proposed Routes**:

```typescript
{
  path: 'register',
  component: RegisterComponent,
  canActivate: [UnauthenticatedGuard] // redirect to dashboard if already logged in
},
{
  path: 'register/pending-activation',
  component: PendingActivationComponent,
  canActivate: [RegistrationGuard] // ensure came from registration flow
},
{
  path: 'activate',
  component: ActivateComponent
  // No guard — allow direct access via email link
}
```

**Questions to Address**:

- Lazy loading strategy? (registration feature module?)
- Route guards needed? (prevent direct access to pending-activation page?)
- How to pass registration email to pending-activation page? (router state? query param? service?)
- Should `/activate` support both query param AND path param for future flexibility?

---

### 5. Service Layer Design

**Decision Required**: How to organize API calls and state management?

**Proposed Services**:

**RegistrationService** (handles API calls):

```typescript
register(data: RegistrationRequest): Observable<RegistrationResponse>
activate(token: string): Observable<ActivationResponse>
// Future: resendActivationEmail(email: string): Observable<void>
```

**RegistrationStateService** (optional - manages pending registration state):

```typescript
setPendingEmail(email: string): void
getPendingEmail(): string | null
clearPendingEmail(): void
```

**Questions to Address**:

- Should state be managed via service, router state, or localStorage?
- Error handling strategy: service-level vs. component-level?
- Should HTTP interceptors handle common errors (401, 500) globally?
- Token extraction from URL: in component or service?

---

### 6. Validation Strategy

**Decision Required**: How to implement password strength indicator and conditional validation?

**Password Strength Implementation**:

```typescript
// Option A: Custom validator that sets form control errors
// Option B: Separate service that calculates strength (weak/medium/strong)
// Option C: Directive that updates visual indicator in real-time

// Recommended: Option B (separation of concerns)
export class PasswordStrengthService {
  calculateStrength(password: string): 'weak' | 'medium' | 'strong' {
    if (password.length >= 16) return 'strong';
    if (password.length >= 12) return 'medium';
    return 'weak'; // 8-11 characters
  }

  getStrengthColor(strength: string): 'warn' | 'accent' | 'primary' {
    // Map to Material Design colors
  }
}
```

**Conditional Validation (Organization Name)**:

```typescript
// Custom validator that checks actorType value
export class ConditionalValidators {
  static requiredIf(
    controlName: string,
    requiredValues: string[]
  ): ValidatorFn {
    return (formGroup: AbstractControl): ValidationErrors | null => {
      const control = formGroup.get(controlName);
      const targetControl = formGroup.get('organizationName');

      if (requiredValues.includes(control?.value) && !targetControl?.value) {
        return { requiredIf: true };
      }
      return null;
    };
  }
}
```

**Questions to Address**:

- Where do custom validators live? (`src/app/shared/validators/`?)
- Should validators be reusable across forms?
- How to display validation errors in Material form fields?
- Debounce strategy for real-time validation (to avoid flickering)?

---

### 7. Error Handling Strategy

**Decision Required**: How to display and handle different error types?

**Error Categories**:

1. **Client-side validation errors** → Inline below form fields (Material error messages)
2. **HTTP 400 errors (API validation)** → Map to specific form field errors
3. **HTTP 409 (email exists)** → Display at email field + general message
4. **HTTP 500 (server error)** → Global error banner (not inline)
5. **Network errors** → Retry mechanism + error message

**Proposed Pattern**:

```typescript
this.registrationService.register(formData).subscribe({
  next: (response) => {
    // Navigate to pending-activation page
    this.router.navigate(['/register/pending-activation'], {
      state: { email: formData.email }
    });
  },
  error: (httpError: HttpErrorResponse) => {
    if (httpError.status === 400) {
      // Map API validation errors to form controls
      this.mapApiErrorsToForm(httpError.error);
    } else if (httpError.status === 409) {
      // Email already exists
      this.registrationForm.get('email').setErrors({
        emailExists: true
      });
    } else {
      // Generic error
      this.errorMessage = 'Registration failed. Please try again.';
    }
  }
});
```

---

### 8. Testing Strategy

**Decision Required**: What to test at each level?

**Unit Tests (Jest)**:

*RegisterComponent*:

- Form initialization with correct validators
- Password strength indicator updates on input
- Organization Name conditional display based on Actor Type
- Form submission calls RegistrationService
- Error handling and display
- Navigation on success
- 6+ tests minimum

*PendingActivationComponent*:

- Displays email from router state
- Guards against direct access (no email)
- "Back to Login" navigation
- 3+ tests minimum

*ActivateComponent*:

- Token extraction from query param
- API call on init
- Success state display
- Error state display (invalid/expired/already activated)
- Retry button functionality
- Navigation to login
- 6+ tests minimum

**Integration Tests** (optional for frontend — or use E2E):

- Mock HttpClient responses
- Test full form submission flow
- Test error response handling

**E2E Tests (Playwright)** - Already specified in spec.md Section 8:

- 10 scenarios defined in FR8.1, FR8.2, FR8.3 acceptance criteria
- Complete registration workflow (register → email → activate → login)
- Validation error scenarios
- Duplicate email handling
- Token expiration handling
- Mobile viewport testing
- Accessibility testing

**Questions to Address**:

- Test file organization (`*.spec.ts` co-located with components?)
- Mock data strategy (fixtures? factories?)
- Code coverage targets (80%?)
- How to test Material Design components (harnesses?)

---

### 9. Implementation Phases

**Decision Required**: What order should tasks be completed?

**Proposed Phasing**:

**Phase 1: Foundation** (can be done in parallel):

- Create component files (RegisterComponent, PendingActivationComponent, ActivateComponent)
- Create RegistrationService with API methods
- Create routing configuration
- Create shared validators (password strength, conditional validation, RFC 5322 email)

**Phase 2: Register Form** (sequential within phase):

- Implement RegisterComponent HTML template with Material components
- Implement Reactive Forms structure with FormBuilder
- Wire up form submission to RegistrationService
- Implement client-side validation and error display
- Add password strength indicator
- Add Actor Type conditional logic for Organization Name
- Implement loading state (spinner on submit button)
- Write unit tests for RegisterComponent
- Manual smoke test in browser

**Phase 3: Pending Activation Page** (can start after Phase 1):

- Implement PendingActivationComponent template
- Implement route guard (prevent direct access)
- Display email from router state
- Add "Back to Login" link
- Write unit tests

**Phase 4: Activation Page** (can start after Phase 1):

- Implement ActivateComponent template
- Extract token from query param
- Call activation API on init
- Implement success state (display message + "Continue to Login" button)
- Implement error states (invalid/expired/already activated)
- Implement retry button for network errors
- Write unit tests

**Phase 5: E2E Testing** (after Phases 2-4 complete):

- Implement 10 Playwright test scenarios from spec.md
- Test complete registration workflow
- Test error scenarios
- Test mobile viewport
- Test accessibility (WCAG 2.1 AA)

**Phase 6: Documentation**:

- Component README files
- Update main README with registration instructions
- Developer guide for extending registration (future fields)

---

## Non-Functional Requirements to Address

**Performance** (NFR8.1 from spec.md):

- Form rendering: <100ms on standard device
- Form submission: <2 seconds (API dependent)
- Activation page load: <1 second
- Bundle size impact: Track before/after (lazy loading helps)

**Accessibility** (NFR8.2 from spec.md):

- WCAG 2.1 Level AA compliance
- Keyboard navigation (tab order, Enter to submit)
- Screen reader support (ARIA labels, live regions for errors)
- Color contrast (Material Design handles this)
- Focus indicators (Material Design handles this)

**Security** (NFR8.3 from spec.md):

- Passwords never stored in localStorage or sessionStorage
- Form data cleared after submission
- Activation tokens extracted securely (no XSS risk)
- HTTPS-only in production (enforced by infrastructure)

**Browser Compatibility** (NFR8.4 from spec.md):

- Chrome/Edge (last 2 versions)
- Firefox (last 2 versions)
- Safari (last 2 versions)
- Mobile browsers (iOS Safari, Chrome Android)

**Responsive Design** (NFR8.5 from spec.md):

- Desktop: centered form, max-width 480px
- Tablet: responsive Material form fields
- Mobile: full-width form, touch-friendly targets
- Viewport tested: 320px - 1920px width

---

## Planning Deliverables

The plan.md document should include:

### 1. Architecture Overview

- Component hierarchy diagram (text or ASCII)
- Directory structure
- Service layer organization

### 2. Component Design

- RegisterComponent specification (inputs/outputs, form structure, methods)
- PendingActivationComponent specification
- ActivateComponent specification

### 3. Routing Design

- Route paths and configuration
- Route guards strategy
- Navigation flows (success/error paths)

### 4. Form Design

- FormGroup structure
- Validation rules per field
- Custom validator implementations
- Error message mapping

### 5. UI/UX Design

- Material component mapping per field
- Responsive layout strategy
- Loading states (spinner placement)
- Error display patterns

### 6. Service Layer Design

- RegistrationService API methods
- State management approach
- Error handling patterns
- HTTP interceptor usage

### 7. Testing Plan

- Unit test coverage per component
- E2E test mapping to acceptance criteria
- Test data fixtures/mocks
- Accessibility test checklist

### 8. Implementation Phases

- Task grouping and sequencing
- Parallel work opportunities
- Dependencies between tasks
- Risk mitigation (e.g., password strength complexity)

### 9. Dependencies

- NPM packages needed (if any beyond existing Angular Material)
- Angular modules to import
- Shared utilities to create

### 10. Open Questions / Risks

- Technical decisions requiring stakeholder input
- Known risks and mitigation strategies
- Assumptions that need validation

---

## Integration Points to Consider

**Existing LoginComponent**:

- Reuse same form styling patterns
- Reuse error display patterns
- Link from login page ("Don't have an account? Register")
- Link to login from registration ("Already have an account? Login")

**Existing Auth System**:

- JWT token storage (not relevant for registration, but activation might set token)
- Auth guards (UnauthenticatedGuard for /register)
- HTTP error handling (leverage existing patterns)

**Email Templates** (Backend):

- Activation email includes link to `/activate?token=...`
- Email copy already defined in Phase 0

**Existing Navigation**:

- Update header/nav to include "Sign Up" link (if not already present)

---

## Constraints and Assumptions

**Constraints**:

- Must use Angular 19 and Angular Material 19
- Must follow existing code patterns in src/Innoventity.Client
- Must not modify backend APIs (use as-is)
- Must be accessible (WCAG 2.1 AA)
- Must work on mobile browsers

**Assumptions**:

- Backend APIs (/auth/register, /auth/activate) are stable and tested
- Email sending infrastructure works reliably
- Activation token lifetime (24 hours) is acceptable
- User can receive emails (no email deliverability issues)
- No need for reCAPTCHA or bot prevention (Phase 1+ scope)

---

## Success Criteria

A successful plan.md should:

1. **Answer all HOW questions** for each WHAT in Section 8
2. **Provide clear component contracts** (inputs, outputs, responsibilities)
3. **Specify concrete implementation details** (FormGroup structure, Material components, validators)
4. **Map to acceptance criteria** (each scenario traceable to implementation approach)
5. **Enable task generation** (next step: /speckit.tasks can generate 20-25 actionable tasks)
6. **Identify risks early** (technical complexity, unknown patterns, integration challenges)
7. **Be implementation-ready** (a developer can start coding immediately after reading the plan)

---

## Additional Guidance

### Code Style Alignment

- Follow Angular style guide
- Use TypeScript strict mode
- Prefer standalone components over modules
- Use signals where appropriate (Angular 19 feature)
- Use async pipe for observables in templates

### Material Design Best Practices

- Use `<mat-form-field>` for all inputs
- Use `<mat-error>` for validation messages
- Use `appearance="outline"` for form fields (modern style)
- Use `color="primary"` for submit button
- Use `mat-spinner` for loading states

### Reusability Considerations

- Custom validators should be reusable across future forms
- Error display patterns should be consistent
- Service layer should be extendable (e.g., future resendActivationEmail method)

---

## Final Checklist

Before completing the plan, ensure:

- [ ] All 4 functional requirements (FR8.1-FR8.4) have implementation strategies
- [ ] All 5 non-functional requirements (NFR8.1-NFR8.5) are addressed
- [ ] All 24 acceptance criteria scenarios are covered
- [ ] All 5 clarifications from Session 2026-04-06 are incorporated
- [ ] Component architecture is clear and simple
- [ ] Form validation strategy is comprehensive
- [ ] Error handling covers all edge cases
- [ ] Testing strategy maps to acceptance criteria
- [ ] Implementation can be broken into 20-25 discrete tasks
- [ ] No architectural unknowns remain

---

## Prompt for `/speckit.plan`

**Run this command:**

```
/speckit.plan
```

**With this context:**

"Create comprehensive technical design plan for Registration User Interface (Phase 1+) feature specified in Section 8 of specs/001-platform-core/spec.md. The plan must detail HOW to implement the browser-based registration workflow using Angular 19, Material Design, and Reactive Forms.

Context:

- Backend APIs (POST /auth/register, POST /auth/activate) already implemented in Phase 0
- Frontend stack: Angular 19, Material Design 19, Reactive Forms, Playwright E2E testing
- Requirements: 3 components (RegisterComponent, PendingActivationComponent, ActivateComponent), 6-field registration form with complex validation, password strength indicator, conditional Organization Name field, and complete activation workflow
- 5 clarifications already applied (inline spinner, password strength thresholds, Organization Name visibility, query parameter route, manual navigation)
- Must follow existing Angular patterns in src/Innoventity.Client codebase

Planning must address:

1. Component architecture (standalone vs. module, directory structure)
2. Reactive Forms implementation (FormGroup structure, custom validators, conditional validation)
3. Material Design component mapping (which mat-* components for each field)
4. Routing configuration (guards, navigation flows)
5. Service layer design (RegistrationService, state management)
6. Validation strategy (password strength calculator, RFC 5322 email, conditional OrganizationName)
7. Error handling (client-side vs. API errors, inline vs. global display)
8. Testing approach (unit tests with Jest, E2E tests with Playwright mapped to 24 acceptance criteria)
9. Implementation phasing (task sequencing, parallel work opportunities)
10. Non-functional requirements (performance, accessibility WCAG 2.1 AA, responsive design, browser compatibility)

Deliverables: Complete plan.md with architecture diagrams, component specifications, form design, routing design, service layer design, testing strategy, implementation phases, and task breakdown guidance enabling /speckit.tasks to generate 20-25 actionable implementation tasks."
