# Phase 0 Research: Platform Core Technical Decisions

**Feature**: Platform Core (v1.0)
**Date**: February 8, 2026
**Status**: Complete (No unknowns - decisions from constitution)

---

## Overview

This research phase consolidates technical decisions already established in the project constitution (v1.0.0, ratified February 8, 2026). No unknowns require investigation—all technology choices, architectural patterns, and quality standards are pre-defined to support the learning objectives of this solo portfolio project.

---

## Decision 1: Backend Technology Stack

**Decision**: ASP.NET Core 8 with Minimal APIs + EF Core 8

**Rationale**:
- **Learning Objective**: Mastering modern .NET development is a primary project goal (per Constitution Constraint 3)
- **Production-Ready**: ASP.NET Core 8 LTS provides stable, performant, well-documented framework
- **Minimal APIs**: Lightweight, testable, aligns with Principle 3 (Simplicity Over Cleverness)
- **EF Core 8**: Modern ORM with excellent Azure SQL integration, migration support
- **Ecosystem**: Rich testing tools (xUnit, WebApplicationFactory), Azure deployment tooling

**Alternatives Considered**:
1. **Node.js + Express**: Rejected - learning objective is .NET mastery
2. **ASP.NET MVC Controllers**: Rejected - Minimal APIs simpler, more modern
3. **Dapper (micro-ORM)**: Rejected - EF Core migrations critical for iterative development

**Best Practices Applied**:
- Feature-based organization (Vertical Slice Architecture)
- Repository abstraction for v2.0 extensibility
- Dependency injection built-in
- Built-in support for health checks, logging, OpenAPI

---

## Decision 2: Frontend Technology Stack

**Decision**: Angular v19 with Standalone Components + Signal-Based Forms

**Rationale**:
- **Learning Objective**: Angular v19 is explicitly mentioned in Constitution Constraint 3
- **Microsoft Template**: Use `dotnet new angular` template for structure consistency
- **Standalone Components**: Modern Angular pattern (no NgModules), simpler mental model
- **Signals**: Reactive state management with fine-grained updates, better performance
- **TypeScript**: Strong typing improves maintainability, refactoring safety

**Alternatives Considered**:
1. **React**: Rejected - learning objective is Angular mastery
2. **Vue.js**: Rejected - same reason as React
3. **Blazor**: Rejected - want full-stack .NET+JS experience

**Best Practices Applied**:
- Feature-based folder structure (auth, innovations, bids, etc.)
- Angular Material for consistent, accessible UI components
- RxJS for async operations (HTTP calls, real-time notifications)
- Environment-specific configuration

---

## Decision 3: Authentication & Authorization

**Decision**: JWT-based authentication with policy-based authorization

**Rationale**:
- **Stateless**: JWT tokens enable horizontal scaling (Azure App Service)
- **Token Expiry**: 1hr access + 7d refresh balances security and UX (per Constitution Section 5)
- **Policy-Based Auth**: Composable, testable, extensible for v2.0 (organization policies)
- **Password Security**: BCrypt hashing (per Constitution Section 5 - Security Standards)

**Implementation Details**:
- ASP.NET Core Authentication middleware (JWT Bearer)
- Authorization policies:
  - `IAuthorizationRequirement` implementations for resource ownership
  - `IAuthorizationHandler` for policy evaluation
  - Example: `InnovationOwnerRequirement` validates user owns innovation before edit/selection

**Alternatives Considered**:
1. **Cookie-based auth**: Rejected - complicates SPA architecture, CORS issues
2. **OAuth2/OIDC**: Rejected - **added complexity for v1.0** (defer to v2.0 if external auth needed)
3. **Azure AD B2C**: Rejected - same reason as OAuth2

---

## Decision 4: Database & Persistence

**Decision**: Azure SQL Database with EF Core 8 Code-First

**Rationale**:
- **No Data Migration**: Fresh schema optimized for v1.0 (per Constitution Constraint 2)
- **Code-First**: Migrations enable iterative schema evolution during development
- **Azure SQL**: Managed service (scaling, monitoring)
- **EF Core Abstractions**: Repository pattern enables v2.0 multi-tenancy filtering

**Schema Approach**:
- Entities map directly to specification business rules
- Inheritance for Actor types (TPH - Table Per Hierarchy for simplicity)
- Value objects for immutable domain concepts (e.g., IPRStatus, ResearchCategory)
- Audit fields (CreatedAt, UpdatedAt) on all entities

**Alternatives Considered**:
1. **PostgreSQL**: Rejected - Azure SQL better integrated with Azure ecosystem
2. **Cosmos DB**: Rejected - relational data model fits SQL better, complex querying
3. **SQLite**: Rejected - not production-suitable for Azure deployment

---

## Decision 5: Testing Strategy

**Decision**: Multi-layered testing with TDD workflow + mutation testing

**Test Pyramid**:
1. **Unit Tests**: xUnit for business logic, domain entities, value objects
   - Target: >80% coverage (Constitution Section 5)
   - Test data builders for readable setup
   - Focus: Business rules, validation, state transitions

2. **Integration Tests**: WebApplicationFactory for HTTP endpoint testing
   - Target: 100% endpoint coverage (Constitution Section 5)
   - In-memory or test database (separate from dev database)
   - Focus: Request/response shapes, authorization, error handling

3. **End-to-End Tests**: Playwright for critical user journeys
   - Target: P0 journeys (J1-J3: innovation submission, bidding, partner selection)
   - Headless execution in CI/CD, headed for debugging
   - Focus: User workflows, UI interactions, multi-step processes

4. **Mutation Testing**: Stryker.NET validates test quality
   - Target: >70% mutation score (Constitution Section 5)
   - Run periodically (not every commit - performance cost)
   - Focus: Critical business logic paths

**TDD Workflow** (Constitution Principle 5):
1. **Red**: Write failing test that describes desired behavior
2. **Green**: Implement minimal code to make test pass
3. **Refactor**: Improve code while keeping tests green
4. **Validate**: Mutation testing proves tests catch bugs

**Alternatives Considered**:
1. **BDD with SpecFlow**: Rejected - Gherkin tests in specification are sufficient, adds tooling complexity
2. **Jest for .NET**: Rejected - xUnit is standard, well-integrated with Visual Studio/.NET CLI

---

## Decision 6: Architecture Pattern

**Decision**: Vertical Slice Architecture

**Rationale**:
- **Feature Cohesion**: Each feature (auth, innovations, bids) contains its own endpoints, logic, data access
- **Reduced Coupling**: Changes to one feature don't ripple through shared layers
- **Spec-Kit Alignment**: One specification often maps to one or more vertical slices
- **Simplicity**: Easier to navigate than traditional layered architecture (per Principle 3)
- **Solo Development**: Reduces cognitive load (entire feature in one place)

**Structure**:
```
src/Innoventity.API/Features/
├── Authentication/
│   ├── Register.cs              # Endpoint + handler + validation + persistence
│   ├── Login.cs
│   └── Activate.cs
├── Innovations/
│   ├── CreateDraft.cs
│   ├── SubmitForPublication.cs
│   ├── GetById.cs               # Phase 0 first slice
│   └── GetByIndustry.cs
```

**Shared Concerns** (extracted when truly shared):
- Domain entities (Innovation, Actor, Bid) in `Domain/Entities/`
- Repository abstractions (`IInnovationRepository`) in `Domain/Interfaces/`
- Infrastructure (DbContext, JWT generator) in `Infrastructure/`

**Alternatives Considered**:
1. **Clean Architecture (Onion)**: Rejected - over-engineered for this project, violates Principle 3
2. **Traditional Layered (MVC)**: Rejected - high coupling, feature changes touch many layers
3. **Modular Monolith**: Rejected - premature for v1.0, consider for v2.0

---

## Decision 7: API Design

**Decision**: RESTful HTTP APIs documented with OpenAPI/Scalar

**Rationale**:
- **OpenAPI**: Auto-generated from endpoints (ASP.NET Core built-in support)
- **Scalar**: Modern OpenAPI UI (per Constitution Section 6 - Definition of Done)
- **REST Conventions**: Standard patterns (GET/POST/PUT/DELETE, status codes)
- **Minimal API Endpoints**: Lightweight, functional style, easy to test

**API Conventions**:
- **/api/auth/**: Authentication endpoints (register, login, activate, refresh-token)
- **/api/innovations/<id>**: Innovation CRUD and lifecycle operations
- **/api/innovations/<id>/bids**: Bid submission and management
- **/api/innovations/<id>/select-partners**: Partner selection (POST, irreversible)
- **/api/incubator/<innovation-id>/**: Virtual incubator workspace operations

**Response Patterns**:
- Success: 200 OK (query), 201 Created (create), 204 No Content (delete)
- Validation Error: 400 Bad Request with problem details (RFC 7807)
- Auth Error: 401 Unauthorized (not authenticated), 403 Forbidden (not authorized)
- Not Found: 404 Not Found
- Server Error: 500 Internal Server Error (logged, monitored)

**Alternatives Considered**:
1. **GraphQL**: Rejected - added complexity, REST sufficient for v1.0
2. **gRPC**: Rejected - overkill for web application, tooling complexity
3. **OData**: Rejected - over-engineered querying, violates Principle 3

---

## Decision 8: Deployment & Hosting

**Decision**: Azure App Service (Linux) with Azure SQL Database

**Rationale**:
- **Managed Platform**: No infrastructure management (focus on code)
- **Azure Ecosystem**: Integrated with Application Insights, Service Bus
- **Staging Slots**: Blue/green deployment for zero-downtime releases
- **Auto-Scaling**: Horizontal scaling based on load (future optimization)
- **Linux Containers**: Cost-effective, .NET 8 runs natively on Linux

**Supporting Services**:
- **Azure SQL Database**: Managed relational database
- **Azure Application Insights**: Monitoring, telemetry, logging
- **Azure Service Bus**: Async messaging (email notifications, future event-driven features)
- **Azure Blob Storage**: Document storage (future v1.0 feature)

**CI/CD**:
- GitHub Actions for build, test, deploy pipeline
- Automated deployment to staging on PR merge
- Manual promotion to production (Constitution Section 7 - Launch Criteria)

**Alternatives Considered**:

1. **Azure Kubernetes Service (AKS)**: Rejected - over-engineered for v1.0 traffic, high complexity
2. **Azure Container Apps**: Rejected - App Service simpler, sufficient for v1.0
3. **VM-based hosting**: Rejected - requires infrastructure management, violates Constraint 1

---

## Decision 9: Observability & Monitoring

**Decision**: Azure Application Insights OTEL) + Structured Logging

**Rationale**:

- **Application Insights**: Automatic telemetry collection, dashboards, alerting
- **OpenTelemetry**: Industry-standard instrumentation (future vendor flexibility)
- **Structured Logging**: Serilog with JSON output (queryable, filterable)
- **Log Levels**: Debug (dev), Information (important events), Warning (recoverable), Error (failures)

**Instrumentation**:

- HTTP request/response logging (duration, status code, user ID)
- Database query performance (EF Core logging)
- Business event tracking (innovation submitted, bid submitted, partners selected)
- Exception tracking with stack traces

**Alerting** (per Constitution Section 7):

- API error rate >1% → alert
- API p95 latency >200ms → warning
- Database connection failures → immediate alert
- Authentication failures spike → security alert

**Alternatives Considered**:

1. **ELK Stack (Elasticsearch, Logstash, Kibana)**: Rejected - complexity, cost, Azure AI simpler
2. **Prometheus + Grafana**: Rejected - requires infrastructure setup, Azure AI managed

---

## Decision 10: Phase 0 Minimal Slice

**Decision**: Registration + Authentication + View Single Innovation

**Scope** (per Specification Section 6):

1. User can register as Idea Generator
2. User receives activation email and activates account
3. User logs in with JWT authentication
4. Authenticated user can retrieve innovation by ID

**Rationale**:

- **End-to-End Proof**: Validates entire stack (frontend → API → auth → database → response)
- **Foundation**: All features build on auth + database + API patterns
- **1-2 Week Target**: Realistic for solo developer, fits Principle 3 (Simplicity)
- **Testable**: Unit, integration, E2E tests prove TDD workflow

**What Phase 0 Proves**:

- ✅ ASP.NET Core API responding to HTTP requests
- ✅ EF Core database connectivity and queries
- ✅ JWT authentication working end-to-end
- ✅ Angular frontend calling backend APIs
- ✅ Deployment to Azure successful
- ✅ Monitoring/observability operational
- ✅ Test automation pipeline working

---

## Technology Matrix Summary

| Category | Technology | Version | Rationale |
| -------- | ---------- | ------- | --------- |
| **Backend Language** | C# | 12 | Modern .NET features, learning objective |
| **Backend Framework** | ASPNET Core | 8 (LTS) | Minimal APIs, performance, ecosystem |
| **ORM** | EF Core | 8 | Code-first migrations, Azure SQL integration |
| **Frontend Language** | TypeScript | 5.x | Strong typing, refactoring safety |
| **Frontend Framework** | Angular | 18 | Standalone components, signals, learning objective |
| **UI Library** | Angular Material | 18 | Consistent, accessible components |
| **Database** | Azure SQL | - | Managed, scalable, relational model fit |
| **Authentication** | JWT | - | Stateless, scalable, standard |
| **Password Hashing** | BCrypt | - | Strong hashing, constitution requirement |
| **Unit Testing** | xUnit | Latest | .NET standard, Visual Studio integrated |
| **E2E Testing** | Playwright | Latest | Cross-browser, modern, reliable |
| **Mutation Testing** | Stryker.NET | Latest | Test quality validation |
| **API Docs** | OpenAPI + Scalar** | - | Auto-generated, interactive, modern UI |
| **Hosting** | Azure App Service | Linux | Managed, integrated with Azure services |
| **Monitoring** | Azure Application Insights | - | Telemetry, logging, alerting |
| **Messaging** | Azure Service Bus | - | Async patterns, email notifications |

---

## Next Steps

**Phase 0 Complete** - All technical decisions documented. Proceed to **Phase 1: Design & Contracts**.

**Phase 1 Outputs**:
1. `data-model.md` - Entity relationship diagram, domain model
2. `contracts/` - OpenAPI specifications for Phase 0 endpoints
3. `quickstart.md` - Developer setup and onboarding guide

---

## Registration UI Feature Research (Phase 1+)

**Date**: April 6, 2026
**Feature**: Registration User Interface (Section 8)
**Status**: Phase 0 Research Complete

### Overview

Registration UI builds upon Phase 0 backend infrastructure (POST /auth/register, POST /auth/activate APIs already implemented). This section documents frontend-specific technology choices and best practices for implementing the browser-based registration workflow.

---

### Angular 19 Component Architecture

**Decision**: Standalone Components (no NgModules)

**Rationale**:

- Angular 19 recommendation (standalone is the future of Angular)
- Simpler dependency management (each component declares own imports)
- Better tree-shaking (smaller bundle sizes)
-Easier to test (no NgModule configuration boilerplate)
- Aligns with Principle 3 (Simplicity Over Cleverness)

**Components**:

1. **RegisterComponent**: Main registration form (6 fields, Material Design, Reactive Forms)
2. **PendingActivationComponent**: Success message after registration (router state for email display)
3. **ActivateComponent**: Token validation and activation (query parameter from email link)

**Implementation Pattern**:

```typescript
@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, CommonModule],
  templateUrl: './register.component.html'
})
export class RegisterComponent { }
```

---

### Registration Form Strategy

> **Scope**: This decision applies only to the Registration form (6 fields, cross-field validators). The overall architectural form strategy is Signal-Based Forms — see `frontend-architecture.md §A2`. Reactive Forms is chosen here because the Registration form exceeds the Signal-Based Forms threshold: it has complex cross-field validators (password match, dynamic required on actorType) that the frontend architecture rules assign to Reactive Forms.

**Decision**: Reactive Forms with FormBuilder (Registration form only)

**Rationale**:

- Industry standard (proven pattern, extensive documentation)
- Strong typing (`FormGroup<RegistrationForm>` for type safety)
- Custom validators straightforward (ValidatorFn interface)
- Cross-field validation (password match validator)
- Observable valueChanges (password strength indicator)
- Synchronous validation (easier to test than template-driven)
- Existing codebase uses Reactive Forms (LoginComponent consistency)

**Form Structure**:

```typescript
registrationForm = this.fb.group({
  email: ['', [Validators.required, Validators.email]],
  password: ['', [Validators.required, passwordStrengthValidator()]],
  confirmPassword: ['', [Validators.required]],
  actorType: ['', [Validators.required]],
  fullName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
  organizationName: ['', []] // Validators updated dynamically based on actorType
}, { validators: passwordMatchValidator('password', 'confirmPassword') });
```

**Alternative Considered**:

- Signal-based Forms: Used for simpler Phase 0 forms (Login, Activation — ≤3 fields, no cross-field validation). Rejected here for the Registration form specifically due to cross-field validation complexity (password match, dynamic validators), per the rule in `frontend-architecture.md §A2`.

---

### Material Design Component Selection

**Decision**: Angular Material 19 with "outline" appearance

**Form Fields**:
- Text inputs (Email, Full Name, Organization Name): `<mat-form-field appearance="outline">` + `<input matInput>`
- Password inputs: `<mat-form-field>` + `<input matInput [type]="hidePassword ? 'password' : 'text'>` + visibility toggle button
- Dropdown (Actor Type): `<mat-select>` with 5 `<mat-option>` values
- Password strength indicator: `<mat-progress-bar mode="determinate">` (color: warn/accent/primary)
- Submit button: `<button mat-raised-button color="primary">`

**Why "outline" appearance**: Modern style, clear field boundaries, good for data-heavy forms

**Accessibility Built-in**:
- ARIA attributes automatically added by Material components
- Keyboard navigation (Tab order, Enter to submit)
- Touch targets automatically 44x44px (WCAG compliant)
- Color contrast meets WCAG 2.1 AA (Material Design system)

**Responsive Strategy**:
- Desktop: Centered form, max-width 480px
- Mobile (<768px): Full-width, vertical stacking (Material default behavior)
- Material handles responsive breakpoints automatically

---

### Custom Validators Implementation

**Password Strength Validator**:
- **Requirements**: 8 chars minimum, uppercase, lowercase, digit, special char
- **Type**: Synchronous ValidatorFn
- **Error Object**: `{ passwordStrength: { hasMinLength, hasUppercase, hasLowercase, hasDigit, hasSpecial } }`
- **Display**: Show specific missing requirements in `<mat-error>`

**Password Match Validator** (Cross-field):
- **Type**: Form-level validator (applied to FormGroup)
- **Logic**: Compare password and confirmPassword values
- **Error**: `{ passwordMismatch: true }` when values don't match
- **Display**: Show error on confirmPassword field only

**Conditional Required Validator** (Organization Name):
- **Strategy**: Dynamic validator update on actorType change
- **Logic**:
  - actorType === 'IdeaGenerator' → organizationName optional (no Validators.required)
  - actorType !== 'IdeaGenerator' → organizationName required (add Validators.required)
- **Implementation**: Subscribe to actorType valueChanges, call `setValidators()` and `updateValueAndValidity()`
- **Clarification Applied**: Organization Name field ALWAYS VISIBLE, marked "(Optional)" for Idea Generator

**Email Validator**:
- **Decision**: Use built-in `Validators.email` (sufficient for RFC 5322 subset)
- **Rationale**: Framework feature, no need for custom validator

---

### Password Strength Indicator

**Decision**: Service-based calculation

**Implementation**:

```typescript
@Injectable({ providedIn: 'root' })
export class PasswordStrengthService {
  calculateStrength(password: string): 'weak' | 'medium' | 'strong' {
    if (password.length >= 16) return 'strong';
    if (password.length >= 12) return 'medium';
    return 'weak'; // 8-11 characters
  }

  getStrengthColor(strength: string): 'warn' | 'accent' | 'primary' { /* ... */ }
  getStrengthPercentage(strength: string): number { /* ... */ }
}
```

**Component Integration**:

- Subscribe to `password` formControl valueChanges
- Debounce 200ms (avoid flickering indicator)
- Update `<mat-progress-bar>` value and color based on service output

**Rationale**:

- Separation of concerns (service = calculation, component = display)
- Easily testable (pure functions, no DOM dependencies)
- Reusable (future ChangePasswordForm can use same service)

---

### Routing & Navigation

**Routes**:
```typescript
{
  path: 'register',
  component: RegisterComponent,
  title: 'Register - Innoventity'
},
{
  path: 'register/pending-activation',
  component: PendingActivationComponent,
  title: 'Activation Pending - Innoventity',
  canActivate: [registrationGuard] // Prevent direct access without registration flow
},
{
  path: 'activate',
  component: ActivateComponent,
  title: 'Activate Account - Innoventity'
  // No guard - must allow direct access via email link
}
```

**registrationGuard**:

- **Purpose**: Prevent users from bookmarking /register/pending-activation
- **Logic**: Check if router navigation state contains email address
- **Redirect**: Navigate to /register if no email in state

**State Management**: Router state for transient data (email passed to pending-activation page)

- **Alternative Rejected**: Service or localStorage - over-engineering for single transient string, security concern (email persisted)

**Navigation Flows**:

1. Register → Pending Activation (router.navigate with state: { email })
2. Pending Activation → Login (router.navigate)
3. Activate → Login (router.navigate after success)
4. Login ↔ Register (routerLink)

**Clarification Applied**: Activate route uses query parameter format `/activate?token={token}` (not path parameter)

---

### Error Handling Strategy

**Error Categories**:

1. **Client-side validation**: Inline below form fields (`<mat-error>`)
2. **HTTP 400 (Validation)**: Map API errors to form field errors (setErrors())
3. **HTTP 409 (Duplicate Email)**: Set error on email field with custom message
4. **HTTP 500 (Server Error)**: Global error banner (not inline)
5. **Network Error (status 0)**: Retry mechanism + error message

**HTTP Error Mapping**:

```typescript
error: (httpError: HttpErrorResponse) => {
  if (httpError.status === 400) {
    this.mapApiErrorsToForm(httpError.error); // Set errors on specific fields
  } else if (httpError.status === 409) {
    this.registrationForm.get('email')!.setErrors({
      emailExists: 'Email already registered. Try logging in.'
    });
  } else if (httpError.status === 0) {
    this.globalError = 'Network error. Check your connection and try again.';
  } else {
    this.globalError = 'Registration failed. Please try again later.';
  }
}
```

**User-Friendly Messages**: Never show raw HTTP status codes or stack traces

---

### Testing Strategy

**Unit Tests (Jest)**:

- **Component Tests**: Form validation, actorType change updates organizationName validators, password strength updates
- **Validator Tests**: Password strength accepts/rejects based on requirements, password match detects mismatch
- **Service Tests**: RegistrationService POST call, PasswordStrengthService calculations

**Test Example**:

```typescript
it('should update organization name validators when actor type changes', () => {
  component.registrationForm.get('actorType')!.setValue('IdeaGenerator');
  component.registrationForm.get('organizationName')!.setValue('');
  expect(component.registrationForm.get('organizationName')!.hasError('required')).toBeFalsy();

  component.registrationForm.get('actorType')!.setValue('RDOrganization');
  component.registrationForm.get('organizationName')!.updateValueAndValidity();
  expect(component.registrationForm.get('organizationName')!.hasError('required')).toBeTruthy();
});
```

**E2E Tests (Playwright)**:

- **Happy Path**: Register → pending-activation → activate → login → dashboard
- **Validation**: Test all 24 acceptance criteria from spec.md Section 8
- **Errors**: Invalid token, network error, duplicate email
- **Accessibility**: axe-core WCAG 2.1 AA validation, keyboard navigation test

**Coverage Target**: 80%+ (measured by Jest coverage report)

---

### Best Practices Summary

**Angular 19**:
- ✅ Standalone components (no NgModules)
- ✅ async pipe for subscriptions (automatic cleanup)
- ✅ takeUntilDestroyed() for manual subscriptions in ngOnInit

**Reactive Forms**:
- ✅ FormBuilder for concise syntax
- ✅ Type-safe FormGroup interfaces
- ✅ Validators in separate files (shared/validators/)
- ✅ Debounce valueChanges (avoid flickering)

**Material Design**:
- ✅ appearance="outline" for modern form fields
- ✅ `<mat-label>` always included (accessibility)
- ✅ `<mat-error>` for validation messages
- ✅ `<mat-hint>` for helper text (e.g., password requirements)
- ✅ color="primary" for primary action buttons

**Security**:
- ✅ Never log passwords
- ✅ Never store passwords in localStorage/sessionStorage
- ✅ HTTPS enforced (Azure Static Web App)
- ✅ Client + server validation (defense in depth)

**Accessibility**:
- ✅ aria-label for icon buttons (password visibility toggle)
- ✅ Keyboard navigation (Tab order, Enter to submit)
- ✅ Color contrast (Material Design handles automatically)
- ✅ Touch targets 44x44px (Material Design default)

---

### Risks & Mitigations

**Risk 1: Password Strength Validator Complexity**
- **Impact**: Medium (weak passwords allowed if validator has bugs)
- **Mitigation**: TDD approach, test all requirement combinations

**Risk 2: Conditional Validator Edge Cases**
- **Impact**: Medium (required field not enforced or vice versa)
- **Mitigation**: Test all 5 actor types, test actorType changes after user input

**Risk 3: E2E Test Flakiness (Activation Token)**
- **Impact**: Low (test reliability only, not production code)
- **Mitigation**: Playwright waitFor(), mock email in test environment

**Risk 4: Material 19 Breaking Changes**
- **Impact**: Low (syntax changes from Material 18)
- **Mitigation**: Follow official upgrade guide, use Material component harnesses for testing

---

### Technology Choices - Registration UI Summary

| Aspect | Technology | Rationale |
|--------|------------|-----------|
| **Component Architecture** | Standalone Components | Angular 19 recommendation, simpler, future-proof |
| **Form Strategy** | Reactive Forms + FormBuilder | Industry standard, strong typing, testable |
| **UI Components** | Angular Material 19 | Accessible, responsive, consistent with existing app |
| **Validation** | Custom ValidatorFn + built-in | Reusable, testable, specific error feedback |
| **Password Strength** | Service-based calculation | Separation of concerns, reusable, testable |
| **Routing** | Simple routes + 1 guard | Minimal complexity, allows email link access |
| **State Management** | Router state (transient data) | Simplest solution, no persistence needed |
| **Error Handling** | Inline (Material) + global banner | Clear UX, separates field errors from server errors |
| **Unit Testing** | Jest + Material harnesses | Existing project standard, future-proof |
| **E2E Testing** | Playwright + axe-core | Cross-browser, accessibility validation |

---

**Registration UI Research Complete** - Proceed to Phase 1 data-model.md generation.

---

**END OF RESEARCH PHASE**
