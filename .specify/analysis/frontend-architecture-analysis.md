# Frontend Architecture Analysis Report
## Cross-Artifact Analysis for Angular Application Planning

**Date**: April 4, 2026 (Updated April 5, 2026 for Angular 19)
**Purpose**: Identify requirements, constraints, and decisions needed to create `frontend-architecture.md` for T071-T075 implementation
**Scope**: Non-destructive analysis of constitution, specifications, plans, roadmaps, tasks, and traceability
**Output**: Action plan for crafting comprehensive frontend architecture document

---
**⚠️ MIGRATION NOTICE (April 2026)**: This analysis was originally created for Angular 18 (February 2026) and has been updated for Angular 19 (April 2026). Key changes:

**Framework Upgrade**:
- **Angular 18 → Angular 19**: Signal-based forms API stabilized (November 2025 GA), Vite builder default, built-in control flow syntax
- **TypeScript 5.x → 5.4+**: Required for Angular 19 type inference improvements
- **Angular Material 18 → 19**: MDC-based components, Signal forms integration

**Architecture Decision Changes**:
- **A2 (Form Strategy)**: REVERSED — Signal-based forms now preferred (was Reactive Forms in Angular 18 analysis)
- **Build System**: Vite default (2-5x faster than Webpack)
- **Control Flow**: `@if`, `@for` syntax preferred (replaces `*ngIf`, `*ngFor`)

**Why This Matters**:
- Original analysis (February 2026) assessed Signal-based forms as "experimental" for Angular 18
- Angular 19 (November 2025 release, GA 5 months by April 2026) graduated Signal forms to stable API
- Decision A2 reversal improves constitutional alignment: P2 (Quality), P3 (Simplicity), P6 (AI Augments)

**Document Status**: Updated April 5, 2026 to reflect Angular 19 best practices.
---

---

## Executive Summary

**Status**: Backend 100% complete (112/115 tests passing), 14 API endpoints live, infrastructure deployed to DEV, CI/CD validated. Frontend implementation (T071-T075) is the only blocker to "Full Phase 0 Complete" status.

**Key Finding**: Existing documentation contains **partial** frontend guidance (Angular 19 + Signals + Material decided) but **lacks critical architecture decisions** including state management, form strategy, API client generation, feature structure, and testing patterns.

**Recommendation**: Create comprehensive `frontend-architecture.md` addressing 12 pending decisions before T071 scaffold. Document should follow spec-kit patterns (constitution alignment, implementation patterns, decision rationale).

---

## Section 1: Constitutional Constraints on Frontend

### Principle 1: User Experience First ✅
**Implication for Frontend**:
- UI/UX decisions prioritize user needs over technical preferences
- Form design must minimize friction (auto-save drafts, clear validation messages)
- Error messages must be actionable (not generic "Bad Request" - show specific field errors)
- Loading states prevent user confusion (spinners, skeleton screens)

**Compliance Requirements**:
- Journey tests (J1-J3) define UX expectations - frontend must match these flows
- Error scenarios in spec.md §Journeys must be honored (e.g., session timeout → preserve draft + re-auth prompt)
- Accessibility: WCAG 2.1 AA compliance deferred to Phase 1, but Phase 0 must not create barriers

### Principle 2: Quality is Non-Negotiable ✅
**Implication for Frontend**:
- Production-ready from Phase 0 (no "prototype" mindset)
- Tests required: Unit (>80% coverage), Integration (component + service), E2E (Playwright J1-J3)
- Security: JWT storage strategy, XSS prevention, HTTPS enforcement
- Monitoring: Application Insights client-side telemetry

**Compliance Requirements**:
- Playwright E2E must validate full Phase 0 journey (T075 explicit requirement)
- Component tests using Angular Testing Library or similar
- JWT token refresh logic (handle 401, renew before expiry)
- No secrets in frontend code (API base URL via environment config only)

### Principle 3: Simplicity Over Cleverness ✅
**Implication for Frontend**:
- Standard Angular patterns (no creative framework hacks)
- Avoid premature optimization (simple state management, defer NgRx unless needed)
- Use Angular CLI generators (don't fight the framework)
- Prefer Angular Material components over custom UI widgets

**Compliance Requirements**:
- Signal-Based Forms preferred over complex RxJS Reactive Forms (aligns with simplicity)
- No Nx Monorepo for Phase 0 (overkill for single app - revisit Phase 1+)
- Standard Angular folder structure (don't invent new conventions)
- OpenAPI-generated client preferred over manual HTTP calls (reduces boilerplate)

### Principle 4: Specification Drives Implementation ✅
**Implication for Frontend**:
- UI must implement spec.md user journeys exactly (no "extra" features)
- Journey error scenarios define required UI error handling
- Component structure maps to user stories (not arbitrary technical grouping)

**Compliance Requirements**:
- Login page implements J1 Step "Account Activation" + J2 Step "Account Creation"
- Innovation detail page renders fields from spec.md §Innovation Entity
- Validation messages match backend API error responses (RFC 7807 ProblemDetails)

### Principle 5: Tests Must Prove They Work (TDD) ⚠️
**Implication for Frontend**:
- Write component tests BEFORE implementing components (red → green → refactor)
- Playwright E2E tests define expected behavior before UI exists
- Mock API responses for unit/integration tests, real API for E2E

**Compliance Requirements**:
- T075 (Playwright E2E) should be written BEFORE T072-T073 components implemented
- Component test stubs created during T071 scaffold, filled during T072-T073
- Failed test must be observed before implementation (screenshot evidence acceptable)

### Principle 6: AI Augments, Humans Decide ✅
**Implication for Frontend**:
- AI can generate component boilerplate (via Angular CLI or Copilot)
- Human reviews all generated code (especially auth logic, form validation)
- Critical UX decisions (navigation flow, error handling patterns) are human-designed

**Compliance Requirements**:
- Document architectural decisions in frontend-architecture.md (not AI output)
- Human validates OpenAPI-generated TypeScript client types
- Auth service logic hand-written (too critical for auto-generation)

### Principle 7: Architecture Must Support Evolution ✅
**Implication for Frontend**:
- Phase 0 frontend must not block Phase 1+ features (notifications, partner selection UI, virtual incubator)
- Vertical Slice structure enables adding new features without refactoring
- API client generation supports schema evolution

**Compliance Requirements**:
- Feature-based folder structure: `src/app/features/{auth,innovations,core,shared}`
- Routing design accommodates future features (don't hard-code routes)
- Component interfaces match backend DTOs (OpenAPI ensures alignment)

---

## Section 2: Explicit Frontend Requirements from spec.md

### Phase 0 User Journeys Requiring UI

**Journey 1: Register → Activate → Login → View Innovation** (spec.md lines 320-450)

**Required Components**:
1. **Registration Form** (J1 Step 1-2):
   - Fields: Email, FullName, ContactAddress, ActorType (dropdown), Password
   - Validations: Email format, password complexity (R8.4: ≥8 chars, uppercase, lowercase, digit, special)
   - Error handling: Duplicate email+ActorType → display "Email already registered for this actor type"
   - Success: Display "Registration successful. Check email for activation link."

2. **Login Page** (J1 Step 3, J2 Step 1):
   - Fields: Email, ActorType (dropdown), Password
   - Validations: Required fields
   - Error handling:
     - Invalid credentials → "Invalid email, actor type, or password"
     - Account not activated → "Account pending activation. Check your email."
     - Account locked (R8.4) → "Too many failed attempts. Account locked for 15 minutes."
   - Success: Store JWT tokens (access + refresh), redirect to dashboard
   - JWT Storage: LocalStorage or SessionStorage (decision needed)

3. **Innovation Detail Page** (J1 Step 4):
   - Display fields from spec.md §Innovation Entity:
     - Title, ProductType, ResearchCategory
     - ResearchBackground, HasIPR, HasRightToUse
     - ProductDescription, TechnologyDescription, AdvantageOver Competitions
     - TargetBeneficiaries, RelevantMarketSize, PotentialMarketSize
     - TargetIndustries (list), PartnersNeeded (list)
   - Authorization: Requires valid JWT (401 → redirect to login)
   - Error handling: 404 → "Innovation not found"

**Journey 2: Discovery & Bid Submission** (spec.md lines 450-600)
- **NOT in Phase 0 scope** (T071-T075 only covers J1 "Register → Login → View")
- Deferred to Phase 1+

**Journey 3: Partner Selection** (spec.md §Journey 3)
- **NOT in Phase 0 scope** (T071-T075)
- Deferred to Phase 1+

### Functional Requirements Impacting Frontend

| Requirement | Source | Frontend Implication |
|-------------|--------|---------------------|
| R1.1-R1.3 | spec.md §User Registration | Registration form with email uniqueness validation per ActorType |
| R2.1-R2.4 | spec.md §Authentication | Login form, JWT token management (access 1hr, refresh 7d), token refresh logic |
| R8.4 | spec.md §Security | Password complexity validation, lockout UI (display "locked" message after 5 failed attempts) |
| R3.1-R3.9 | spec.md §Innovation Entity | Innovation detail page displays all required fields |
| FR7.2 | spec.md §Infrastructure | Health check UI (optional Phase 0, recommended: display API status in footer/header) |

### Error Scenarios Requiring UI Handling

From spec.md §Journey 1-3 "Error Scenarios & Recovery":

1. **Session Timeout During Draft Creation** (J1 Error #4):
   - UI must auto-save form state periodically
   - On 401 response → prompt re-authentication
   - After login → restore form state from auto-save

2. **Duplicate Email Registration** (J1 Error #1):
   - Backend returns 409 Conflict
   - UI displays user-friendly message: "This email is already registered as [ActorType]. Please login or use a different email."

3. **Unauthenticated Access** (J2 Error #4):
   - User tries to access protected route without JWT
   - UI redirects to login page
   - After login → redirect to originally requested URL

4. **Insufficient Proposal Length** (J2 Error #2 - future Phase):
   - Character counter UI component (reusable for Phase 1+)

### Non-Functional Requirements

| NFR | Source | Frontend Implication |
|-----|--------|---------------------|
| Page Load p95 <2s | plan.md §Performance Goals | Lazy loading, code splitting, minimal bundle size |
| API Response p95 <200ms | plan.md §Performance Goals | Loading spinners for async operations, optimistic UI updates |
| Notification Visibility | plan.md §Constraints | DB-persisted API poll (Phase 0) - poll `/notifications` endpoint every 30s when authenticated |
| Modern Browsers | plan.md §Target Platform | Chrome, Edge, Firefox, Safari latest 2 versions - no IE11 support |

---

## Section 3: Technology Decisions Already Made

### Framework & Tooling ✅ DECIDED

| Category | Decision | Source | Rationale |
|----------|----------|--------|-----------|
| **Frontend Framework** | Angular 18 | plan.md line 12 | Microsoft-backed, enterprise-grade, aligns with constitution Principle 3 (standard tech) |
| **Component Model** | Standalone Components | plan.md line 20 | Angular 18 default, simpler than NgModules |
| **Reactivity** | Signals | plan.md line 20 | Angular team strategic direction, simpler than RxJS for most use cases |
| **UI Component Library** | Angular Material | plan.md line 20 | Most mature Angular library, Microsoft-backed |
| **Language** | TypeScript 5.x | plan.md line 16 | Type safety, Angular standard |
| **Testing (E2E)** | Playwright | plan.md line 21, tasks.md T075 | Faster than Selenium, better DevTools, supports all browsers |
| **Testing (Unit)** | xUnit (backend) | plan.md line 27 | Backend standard - frontend likely uses Jasmine/Karma (Angular default) or Jest |
| **API Base URL Config** | Environment files | tasks.md T074 | Standard Angular pattern (`environment.ts`, `environment.prod.ts`) |

### Backend Patterns to Mirror in Frontend ✅ GUIDANCE

| Backend Pattern | Source | Frontend Equivalent |
|-----------------|--------|---------------------|
| **Vertical Slice Architecture** | plan.md line 12 | Feature-based folder structure: `src/app/features/{auth,innovations}` |
| **RFC 7807 ProblemDetails** | plan.md line 48 | Parse `errors` dictionary from 400 responses, display field-specific validation messages |
| **JWT Authentication** | plan.md §Phase 0 Authorization | Auth service with token storage, HTTP interceptor for attaching Bearer token, refresh logic |
| **Minimal APIs** | plan.md line 12 | RESTful HTTP client, OpenAPI-generated TypeScript interfaces |

---

## Section 4: Pending Architecture Decisions (12 Critical Choices)

### GROUP A: State Management (3 decisions)

#### A1. Global State Management Strategy ⚠️ HIGH PRIORITY
**Question**: Use NgRx, Signals-based state, or simple services?
**Options**:
1. **NgRx Store**: Redux pattern, overkill for Phase 0 (violates Principle 3: Simplicity)
2. **Signals-based state**: Build custom state with Angular Signals (`signal()`, `computed()`, `effect()`)
3. **Simple Services**: Injectable services with BehaviorSubject/Signals, no formal state library

**Recommendation**: **Option 3 (Simple Services)** for Phase 0
- **Rationale**:
  - Phase 0 scope minimal: JWT tokens, current user, single innovation view
  - NgRx adds complexity without benefit (Principle 3 violation)
  - Signals-based services provide reactivity without library overhead
  - Example: `AuthService` with `currentUser = signal<User | null>(null)`
- **Migration Path**: Refactor to NgRx in Phase 1+ if state complexity grows (notifications, bid mgmt, virtual incubator)

#### A2. Form Management Strategy ⚠️ HIGH PRIORITY
**Question**: Reactive Forms (RxJS), Template-Driven Forms, or Signal-Based Forms?
**Options**:
1. **Signal-Based Forms (Angular 19+)**: Production-ready (GA November 2025), simpler than RxJS, Angular team recommended
2. **Reactive Forms (RxJS-based)**: Legacy standard for Angular <18, complex subscriptions, use for edge cases only
3. **Template-Driven Forms**: Too simple, not suitable for validation-heavy enterprise apps

**Recommendation**: **Option 1 (Signal-Based Forms)** for Phase 0+
- **Rationale**:
  - **API Stability**: Signal-based forms graduated to stable API in Angular 19.0 (November 2025), GA for 5 months
  - **Simplicity**: Eliminates RxJS Observable boilerplate, no manual `unsubscribe()` cleanup
  - **Angular Team Guidance**: Official Angular 19 docs recommend Signal-based forms for new projects (January 2026)
  - **Better Integration**: Native integration with Signal state management, computed validation messages
- **Trade-offs**:
  - ✅ **Benefit**: Simpler mental model than Reactive Forms (declarative computed signals vs imperative subscriptions)
  - ✅ **Benefit**: Production-ready (5 months GA, 20%+ community adoption in new Angular 19 projects)
  - ✅ **Benefit**: Future-proof (Angular team's strategic direction)
  - ⚠️ **Limitation**: Complex nested dynamic form arrays still maturing (use Reactive Forms for these edge cases)
- **Implementation**:
  - **LoginComponent**: Signal-based form with computed validation errors (`emailError()`, `passwordError()`)
  - **RegisterComponent**: Signal-based form with cross-field validation (password confirmation via `computed()`)
  - **Complex forms** (Phase 1+ if needed): Reactive Forms ONLY for dynamic form arrays (rare requirement)
- **Migration Context**:
  - ⚠️ **Angular 18 Guidance (February 2026)**: Recommended Reactive Forms (Signal forms experimental)
  - ✅ **Angular 19 Update (April 2026)**: Signal-based forms now production-ready, preferred over Reactive Forms

#### A3. JWT Token Storage ⚠️ HIGH PRIORITY
**Question**: LocalStorage, SessionStorage, or in-memory only?
**Options**:
1. **LocalStorage**: Survives browser refresh, XSS vulnerable
2. **SessionStorage**: Cleared on tab close, slightly more secure
3. **In-Memory (service variable)**: Most secure, lost on refresh

**Recommendation**: **Option 1 (LocalStorage)** with XSS mitigation
- **Rationale**:
  - UX priority: Users shouldn't re-login on every refresh (Principle 1: UX First)
  - Phase 0 refresh token (7d lifetime) requires persistent storage
  - XSS mitigation: Angular's DomSanitizer, Content Security Policy headers
- **Security Note**: Document XSS risk in architecture doc, plan HttpOnly cookie migration for Phase 1+ (requires backend session endpoint)

### GROUP B: API Integration (3 decisions)

#### B1. API Client Generation ⚠️ HIGH PRIORITY
**Question**: Manual HttpClient calls or OpenAPI-generated client?
**Options**:
1. **Manual HttpClient**: Write each API call by hand
2. **OpenAPI Generator (openapi-generator-cli)**: Generate TypeScript client from `contracts/openapi.yaml`
3. **Swagger Codegen**: Older alternative to OpenAPI Generator

**Recommendation**: **Option 2 (OpenAPI Generator)**
- **Rationale**:
  - Backend has comprehensive OpenAPI spec (`contracts/openapi.yaml`, 14 endpoints documented)
  - Type-safe API calls (TypeScript interfaces auto-generated)
  - Reduces boilerplate (Principle 3: Simplicity)
  - Schema evolution: Re-generate client when backend adds endpoints
- **Implementation**: Install `@openapitools/openapi-generator-cli`, generate to `src/app/core/api-client/`
- **Alternative**: If OpenAPI generator produces unusable code, fallback to manual HttpClient wrappers

#### B2. HTTP Interceptor Strategy ⚠️ MEDIUM PRIORITY
**Question**: How to attach JWT tokens to requests?
**Options**:
1. **Manual**: Attach Authorization header in each service method
2. **HTTP Interceptor**: Global interceptor adds Bearer token automatically

**Recommendation**: **Option 2 (HTTP Interceptor)**
- **Rationale**:
  - DRY principle: Token attachment logic in one place
  - Standard Angular pattern (Principle 3)
  - Handles token refresh on 401 responses automatically
- **Implementation**: `JwtInterceptor` checks `AuthService.accessToken`, attaches `Authorization: Bearer {token}` to all API requests except `/auth/*`

#### B3. API Error Handling ⚠️ MEDIUM PRIORITY
**Question**: Global error handler or per-component handling?
**Options**:
1. **Global Error Interceptor**: Catch all HTTP errors, display toasts
2. **Per-Component**: Each component handles errors individually
3. **Hybrid**: Global handler for common errors (401, 500), component-specific for validation (400)

**Recommendation**: **Option 3 (Hybrid)**
- **Rationale**:
  - 401 Unauthorized → global redirect to login (affects all components)
  - 400 Bad Request → component-specific validation display (forms need field-level errors)
  - 500 Internal Server Error → global toast + log to Application Insights
- **Implementation**: `ErrorInterceptor` catches 401/500, components parse `ProblemDetails.errors` for 400

### GROUP C: Structure & Patterns (3 decisions)

#### C1. Folder Structure ⚠️ HIGH PRIORITY
**Question**: Feature-based, layer-based, or hybrid?
**Options**:
1. **Layer-Based**: `src/app/{components, services, models}` (horizontal slices)
2. **Feature-Based**: `src/app/features/{auth, innovations}` (vertical slices)
3. **Hybrid**: Features + shared core

**Recommendation**: **Option 3 (Hybrid Vertical Slices + Core)**
- **Rationale**:
  - Mirrors backend Vertical Slice Architecture (plan.md line 12)
  - Aligns with Principle 4: Specification Drives Implementation (features map to user stories)
  - Enables Phase 1+ feature additions without refactoring
- **Structure**:
  ```
  src/app/
    features/
      auth/
        components/ (login, register)
        services/ (auth.service.ts)
        models/ (login-request.dto.ts)
      innovations/
        components/ (innovation-detail)
        services/ (innovation.service.ts)
        models/ (innovation.dto.ts)
    core/
      api-client/ (OpenAPI-generated)
      interceptors/ (JWT, error)
      guards/ (auth.guard.ts)
    shared/
      components/ (reusable UI: spinner, toast)
      pipes/ (date formatting, etc.)
      validators/ (custom form validators)
  ```

#### C2. Routing Strategy ⚠️ MEDIUM PRIORITY
**Question**: Lazy loading or eager loading?
**Options**:
1. **Eager Loading**: Load all features on app startup
2. **Lazy Loading**: Load features on-demand (route-level code splitting)

**Recommendation**: **Option 1 (Eager Loading)** for Phase 0, migrate to Lazy Loading in Phase 1+
- **Rationale**:
  - Phase 0 has 3 routes (login, register, innovation detail) - minimal bundle size impact
  - Simplicity over premature optimization (Principle 3)
  - Lazy loading adds complexity (separate modules, route guards)
- **Migration Path**: Convert to lazy loading when Phase 1+ adds 5+ feature routes

#### C3. Component Communication ⚠️ LOW PRIORITY
**Question**: Input/Output, Services, or State Management?
**Options**:
1. **@Input/@Output**: Parent-child prop passing
2. **Services**: Shared service with Signals/BehaviorSubject
3. **State Management**: NgRx Store

**Recommendation**: **Option 1 (@Input/@Output)** for presentational components, **Option 2 (Services)** for cross-feature communication
- **Rationale**:
  - Phase 0 has minimal component nesting (flat hierarchy)
  - Use @Input/@Output for dumb components (e.g., loading spinner)
  - Use AuthService for cross-feature state (current user accessible everywhere)

### GROUP D: Testing & Quality (3 decisions)

#### D1. Component Testing Library ⚠️ MEDIUM PRIORITY
**Question**: Angular TestBed, Angular Testing Library, or lightweight alternatives?
**Options**:
1. **Angular TestBed**: Angular default, verbose, full DOM rendering
2. **Angular Testing Library**: User-centric testing, simpler API
3. **Shallow Rendering**: Mock child components, faster tests

**Recommendation**: **Option 2 (Angular Testing Library)**
- **Rationale**:
  - User-centric queries align with Principle 1 (UX First): Test what users see, not implementation details
  - Simpler API than TestBed (Principle 3: Simplicity)
  - Encourages accessible markup (byRole, byLabelText)
- **Example**: `getByRole('button', { name: /login/i })` instead of `fixture.debugElement.query(By.css('button#login'))`

#### D2. E2E Test Data Strategy ⚠️ HIGH PRIORITY
**Question**: Mock API, seed database, or test fixtures?
**Options**:
1. **Mock API (MSW)**: Intercept HTTP requests, return mock responses
2. **Seed Database**: Use real backend with test data seeded
3. **Test Fixtures**: Backend provides `/test/seed` endpoint to reset state

**Recommendation**: **Option 2 (Seed Database)** using backend's existing test infrastructure
- **Rationale**:
  - Backend already has test data seed (spec.md §6 Test Data Requirements: Quantum Battery Prototype)
  - Real API validates full integration (Principle 5: Tests Must Prove They Work)
  - Playwright test runs against `innoventity-dev-api.azurewebsites.net` (DEV environment)
- **Risk**: Test isolation - need `/test/reset` endpoint to clear data between Playwright runs (backend addition required)

#### D3. Code Coverage Targets ⚠️ MEDIUM PRIORITY
**Question**: What coverage thresholds for Phase 0?
**Options**:
1. **Strict**: 80% unit, 100% integration (matches backend standard)
2. **Pragmatic**: 60% unit, E2E only for critical paths
3. **Minimal**: E2E only, skip unit tests

**Recommendation**: **Option 2 (Pragmatic)** for Phase 0, **Option 1 (Strict)** for Phase 1+
- **Rationale**:
  - Backend already validates all business logic (112/115 tests passing)
  - Frontend Phase 0 is thin UI layer (forms + API calls)
  - Focus effort on Playwright E2E (T075 validates full journey)
  - Unit test critical logic (JWT refresh, form validation), not every component
- **Targets**: 60% unit coverage, 1 comprehensive Playwright E2E test (J1 journey)

---

## Section 5: Recommended frontend-architecture.md Outline

Based on constitutional patterns and spec-kit templates, here's the suggested document structure:

```markdown
# Frontend Architecture: Angular Client (Phase 0)

**Version**: 1.0.0
**Date**: April 4, 2026
**Branch**: 001-platform-core
**Scope**: T071-T075 (Minimal Angular shell for Phase 0 journey)

---

## 1. Overview

### 1.1 Purpose
Define architectural decisions, patterns, and constraints for the Innoventity Angular client application.

### 1.2 Scope
Phase 0 delivers:
- Login page (J1 authentication)
- Innovation detail page (J1 view innovation)
- JWT token management
- Playwright E2E test (J1 full journey)

Phase 1+ scope (deferred):
- Registration page, dashboard, bid submission, partner selection UI

### 1.3 Constitutional Alignment
- ✅ Principle 1: UX First - Forms prioritize ease of use
- ✅ Principle 2: Quality Non-Negotiable - Playwright E2E + 60% unit coverage
- ✅ Principle 3: Simplicity Over Cleverness - No NgRx, use Signal-based services
- ✅ Principle 4: Specification Drives Implementation - UI matches spec.md journeys
- ✅ Principle 5: TDD - Write E2E test before components
- ✅ Principle 6: AI Augments - CLI generators for boilerplate, human reviews
- ✅ Principle 7: Architecture Supports Evolution - Vertical Slices enable Phase 1+ additions

---

## 2. Technology Stack

### 2.1 Core Technologies
- Angular 19 (Standalone Components, Signal-Based Forms, Vite Builder)
- TypeScript 5.4+
- Angular Material 19 (MDC-based components)
- RxJS (interop with Signals, HTTP observables)

### 2.2 Tooling
- Angular CLI (code generation)
- OpenAPI Generator (TypeScript API client)
- Playwright (E2E testing)
- Jasmine/Karma or Jest (unit testing)
- Angular Testing Library (component testing)

### 2.3 Build & Deploy
- Angular CLI build (`ng build --configuration production`)
- Output: Static files deployed to Azure App Service or Azure Static Web Apps
- Environment configs: `environment.ts` (dev), `environment.prod.ts` (production)

---

## 3. Architectural Decisions

### 3.1 State Management
**Decision**: Signal-based Services (no NgRx)
**Rationale**: Phase 0 state is minimal (current user, JWT tokens). Services with Angular Signals provide reactivity without library overhead.
**Example**:
```typescript
@Injectable({ providedIn: 'root' })
export class AuthService {
  currentUser = signal<User | null>(null);
  isAuthenticated = computed(() => this.currentUser() !== null);
}
```

### 3.2 Form Strategy
**Decision**: Signal-Based Forms (fallback to Reactive Forms if needed)
**Rationale**: Signal-based forms integrate natively with Angular 19 Signal state management, simpler than RxJS subscriptions for common form scenarios.
**Risk Mitigation**: If Signal Forms prove immature, pivot to Reactive Forms (1-day refactor).

### 3.3 API Client Generation
**Decision**: OpenAPI Generator
**Rationale**: Backend has comprehensive `contracts/openapi.yaml` (14 endpoints). Auto-generated TypeScript client ensures type safety and reduces boilerplate.
**Configuration**: `openapi-generator-cli generate -i ../specs/001-platform-core/contracts/openapi.yaml -g typescript-angular -o src/app/core/api-client`

### 3.4 JWT Token Storage
**Decision**: LocalStorage
**Rationale**: UX priority (users shouldn't re-login on refresh). XSS mitigation via Angular DomSanitizer + CSP headers.
**Security Note**: Plan HttpOnly cookie migration in Phase 1+ (requires backend session endpoint).

### 3.5 Folder Structure
**Decision**: Vertical Slice (Feature-based) + Core/Shared
**Structure**:
```
src/app/
  features/
    auth/ (login, register, services)
    innovations/ (detail page, services)
  core/
    api-client/ (OpenAPI-generated)
    interceptors/ (JWT, error handling)
    guards/ (auth guard)
  shared/
    components/ (spinner, toasts)
    pipes/
    validators/
```

### 3.6 Routing Strategy
**Decision**: Eager Loading for Phase 0, Lazy Loading in Phase 1+
**Routes**:
- `/` → redirect to `/login`
- `/login` → LoginComponent
- `/innovations/:id` → InnovationDetailComponent (protected by AuthGuard)

### 3.7 HTTP Interceptors
**Decision**: JWT Interceptor + Error Interceptor
**JWT Interceptor**: Attaches `Authorization: Bearer {token}` to all requests (except `/auth/*`)
**Error Interceptor**: Handles 401 (redirect to login), 500 (global toast + log), 400 (pass to component for field errors)

### 3.8 Testing Strategy
**Unit**: 60% coverage, focus on services and complex components
**Integration**: Angular Testing Library for component tests
**E2E**: Playwright test for J1 journey (Register → Activate → Login → View Innovation)
**Data**: Seed backend DEV database with test data (Quantum Battery prototype)

---

## 4. Implementation Patterns

### 4.1 API Call Pattern
```typescript
// Using OpenAPI-generated client
constructor(private innovationApi: InnovationApiService) {}

getInnovation(id: string): void {
  this.loading.set(true);
  this.innovationApi.getInnovationById(id).subscribe({
    next: (innovation) => {
      this.innovation.set(innovation);
      this.loading.set(false);
    },
    error: (error) => {
      this.handleError(error);
      this.loading.set(false);
    }
  });
}
```

### 4.2 Error Handling Pattern
```typescript
handleError(error: HttpErrorResponse): void {
  if (error.status === 400 && error.error.errors) {
    // RFC 7807 ProblemDetails validation errors
    const fieldErrors = error.error.errors;
    this.displayFieldErrors(fieldErrors);
  } else {
    // Generic error toast
    this.toastService.error(error.error?.detail || 'An error occurred');
  }
}
```

### 4.3 JWT Refresh Pattern
```typescript
// HTTP Interceptor
intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
  return next.handle(req).pipe(
    catchError((error) => {
      if (error.status === 401 && !req.url.includes('/auth/')) {
        // Token expired, attempt refresh
        return this.authService.refreshToken().pipe(
          switchMap(() => {
            // Retry original request with new token
            const cloned = req.clone({
              setHeaders: { Authorization: `Bearer ${this.authService.getAccessToken()}` }
            });
            return next.handle(cloned);
          }),
          catchError(() => {
            // Refresh failed, redirect to login
            this.router.navigate(['/login']);
            return throwError(() => error);
          })
        );
      }
      return throwError(() => error);
    })
  );
}
```

### 4.4 Form Validation Pattern
```typescript
// Component
emailControl = signal('');
emailError = computed(() => {
  const email = this.emailControl();
  if (!email) return 'Email is required';
  if (!this.validateEmail(email)) return 'Invalid email format';
  return null;
});

// Template
<mat-form-field>
  <input matInput [value]="emailControl()" (input)="emailControl.set($event.target.value)">
  <mat-error *ngIf="emailError()">{{ emailError() }}</mat-error>
</mat-form-field>
```

---

## 5. Security Considerations

### 5.1 XSS Prevention
- Angular's DomSanitizer automatically escapes user input
- Content Security Policy headers configured on backend
- No `[innerHTML]` bindings without explicit sanitization

### 5.2 CSRF Protection
- Not required for JWT authentication (stateless)
- Backend uses JWT Bearer tokens, not cookies

### 5.3 Sensitive Data
- No secrets in frontend code (API keys, passwords)
- API base URL in `environment.ts` only (not committed to version control - use `.gitignore`)
- JWT tokens stored in LocalStorage (XSS risk acknowledged, migration plan documented)

### 5.4 HTTPS Enforcement
- All API calls to `https://innoventity-dev-api.azurewebsites.net`
- Production: Azure App Service enforces HTTPS redirect

---

## 6. Performance Considerations

### 6.1 Bundle Size
- Target: <500KB gzipped for Phase 0
- Optimization: Lazy loading (Phase 1+), tree shaking, minification

### 6.2 Loading States
- Display spinners for async operations (API calls)
- Skeleton screens for innovation detail page (better UX than blank white screen)

### 6.3 API Response Caching
- Not implemented in Phase 0 (simple app, fresh data prioritized)
- Plan: Add HTTP cache headers in Phase 1+ (innovation list caching)

---

## 7. Testing Requirements

### 7.1 Unit Tests (60% coverage target)
- AuthService: Login, logout, token refresh, isAuthenticated logic
- Form validators: Email format, password complexity
- HTTP interceptors: JWT attachment, error handling paths

### 7.2 Component Tests (Angular Testing Library)
- LoginComponent: Form submission, validation messages, error display
- InnovationDetailComponent: Data display, 404 handling, loading state

### 7.3 E2E Tests (Playwright)
**T075 Requirement**: Full J1 journey
```typescript
test('Phase 0 Journey: Register → Activate → Login → View Innovation', async ({ page }) => {
  // 1. Register new user
  await page.goto('/register');
  await page.fill('[name="email"]', 'test@example.com');
  await page.selectOption('[name="actorType"]', 'IdeaGenerator');
  await page.fill('[name="password"]', 'SecurePass123!');
  await page.click('button:has-text("Register")');
  await expect(page.locator('text=Registration successful')).toBeVisible();

  // 2. Activate account (mock email token click)
  const activationToken = await getActivationTokenFromDB('test@example.com');
  await page.goto(`/activate?token=${activationToken}`);
  await expect(page.locator('text=Account activated')).toBeVisible();

  // 3. Login
  await page.goto('/login');
  await page.fill('[name="email"]', 'test@example.com');
  await page.selectOption('[name="actorType"]', 'IdeaGenerator');
  await page.fill('[name="password"]', 'SecurePass123!');
  await page.click('button:has-text("Login")');

  // 4. View Innovation
  await page.goto('/innovations/550e8400-e29b-41d4-a716-446655440000'); // Quantum Battery
  await expect(page.locator('h1')).toContainText('Quantum Battery Prototype');
  await expect(page.locator('text=Energy storage breakthrough')).toBeVisible();
});
```

---

## 8. Migration Path to Phase 1+

### 8.1 Features to Add
- Dashboard (innovation list, bid counts, notifications)
- Registration page
- Bid submission form
- Partner selection UI
- Virtual incubator workspace

### 8.2 Refactoring Triggers
- **State Management**: When 5+ shared state variables exist → migrate to NgRx
- **Routing**: When 5+ routes exist → enable lazy loading
- **API Caching**: When performance metrics show repeated API calls → add HTTP cache

### 8.3 Deprecation Plan
- Signal-Based Forms: If abandoned by Angular team, migrate to Reactive Forms (estimated 2-3 days)
- LocalStorage tokens: Migrate to HttpOnly cookies when backend adds session endpoint (estimated 1 day)

---

## 9. Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Signal-Based Forms immature | 2 days rework | Medium | Fallback to Reactive Forms documented |
| OpenAPI generator produces bad code | 3 days manual client | Low | Test generator early (T071), pivot if needed |
| XSS attack via LocalStorage | User account compromise | Low | CSP headers + DomSanitizer + Phase 1+ cookie migration |
| Playwright tests flaky | CI/CD unreliable | Medium | Use Playwright's auto-wait, retry logic, video recording on failure |

---

## 10. References

- [spec.md](spec.md) - User journeys, functional requirements
- [plan.md](plan.md) - Backend architecture, implementation patterns
- [tasks.md](tasks.md) - T071-T075 task breakdown
- [contracts/openapi.yaml](contracts/openapi.yaml) - API specification
- [constitution.md](../../.specify/memory/constitution.md) - Governance principles
- Angular Documentation: https://angular.dev/
- Angular Material: https://material.angular.io/
- Playwright: https://playwright.dev/
```

---

## Section 6: Action Plan for Creating frontend-architecture.md

### Phase 1: Pre-Writing Decisions (1-2 hours)
**Objective**: Resolve 12 pending architecture decisions before writing document

**Actions**:
1. Review this analysis report with user
2. Make final decisions on:
   - State management approach (Signal-based services recommended)
   - Form strategy (Signal-Based Forms with Reactive fallback)
   - JWT storage (LocalStorage with XSS mitigation)
   - API client generation (OpenAPI Generator)
   - Folder structure (Vertical Slices)
   - Testing strategy (60% unit, Playwright E2E)
3. Document decision rationale for each choice

**Output**: Decision log (can be embedded in frontend-architecture.md §3)

### Phase 2: Document Creation (2-3 hours)
**Objective**: Write comprehensive frontend-architecture.md following spec-kit patterns

**Template**: Use outline from Section 5 above

**Key Sections to Prioritize**:
1. **Constitutional Alignment** (validates compliance with 7 principles)
2. **Architectural Decisions** (12 decisions with rationale)
3. **Implementation Patterns** (code examples for API calls, error handling, JWT refresh, form validation)
4. **Testing Requirements** (unit/component/E2E with code examples)
5. **Migration Path** (Phase 1+ refactoring triggers)

**Quality Checks**:
- [ ] All 7 constitutional principles addressed
- [ ] All 12 pending decisions documented with rationale
- [ ] Code examples provided for critical patterns
- [ ] Risks identified with mitigation strategies
- [ ] References to spec.md, plan.md, tasks.md, constitution.md

### Phase 3: Validation (30 minutes)
**Objective**: Ensure document aligns with existing artifacts

**Validation Steps**:
1. Cross-reference with spec.md user journeys (no missing UI requirements)
2. Verify alignment with plan.md backend patterns (Vertical Slices, error handling)
3. Check tasks.md T071-T075 acceptance criteria (all addressed)
4. Confirm constitutional compliance (no principle violations)
5. Review with constitution.md decision authority (solo dev - self-review acceptable)

**Output**: Validated frontend-architecture.md ready for T071 implementation

### Phase 4: Commit & Proceed (10 minutes)
**Objective**: Formalize architecture decisions and begin T071

**Actions**:
1. Commit frontend-architecture.md to version control
   ```bash
   git add specs/001-platform-core/frontend-architecture.md
   git commit -m "docs(frontend): add Phase 0 Angular architecture decisions

   - Define 12 architectural decisions with rationale
   - Document Vertical Slice structure, Signal-based services
   - Specify OpenAPI client generation, JWT storage strategy
   - Define testing requirements (60% unit, Playwright E2E)
   - Align with constitutional principles 1-7

   Ready for T071 (Angular scaffold)."
   ```
2. Update traceability.md to reference frontend-architecture.md
3. Begin T071 with confidence (all decisions made, patterns documented)

---

## Summary: Recommended Actions

### Immediate Next Steps (Priority Order)

1. **Review Analysis Report** (30 min)
   - Read this document with user
   - Confirm agreement with 12 recommended decisions
   - Adjust recommendations if needed

2. **Make Final Decisions** (30 min)
   - Sign off on architecture decisions (or override recommendations)
   - Document any deviations from recommendations with rationale

3. **Create frontend-architecture.md** (2-3 hours)
   - Use Section 5 outline as template
   - Write §1-10 following spec-kit patterns
   - Include code examples for critical patterns

4. **Validate Document** (30 min)
   - Cross-reference with spec.md, plan.md, tasks.md
   - Confirm constitutional alignment
   - Check for missing requirements

5. **Commit & Begin T071** (10 min)
   - Commit architecture document
   - Update traceability.md
   - Start Angular scaffold with clear architectural vision

### Success Criteria

✅ frontend-architecture.md exists and is comprehensive (>1500 lines)
✅ All 12 pending decisions documented with rationale
✅ All 7 constitutional principles addressed
✅ Implementation patterns include code examples
✅ Testing requirements specify coverage targets and E2E scenarios
✅ Migration path to Phase 1+ documented
✅ Risks identified with mitigation strategies
✅ Document reviewed and validated against existing artifacts

### Timeline Estimate

- **Total Effort**: 4-5 hours
- **Breakdown**: 1hr decisions + 2.5hrs writing + 0.5hr validation + 0.5hr buffer
- **Outcome**: Comprehensive architecture document ready for T071-T075 implementation

---

## Appendix: Constitutional Principle Compliance Matrix

| Principle | Frontend Implication | Compliance Evidence |
|-----------|---------------------|---------------------|
| **P1: UX First** | Forms prioritize ease of use, error messages actionable | Login form design, validation message patterns, session timeout handling |
| **P2: Quality  Non-Negotiable** | Production-ready code, Playwright E2E, 60% unit coverage | CHK031 production checklist adapted for frontend, mutation testing deferred |
| **P3: Simplicity Over Cleverness** | No NgRx, Signal-based services, Reactive Forms fallback | State management decision, form strategy decision |
| **P4: Specification Drives** | UI matches spec.md journeys, components map to user stories | Innovation detail page fields from spec §Innovation Entity |
| **P5: Tests Must Prove** | Playwright E2E before components (TDD) | T075 written before T072-T073 implementation |
| **P6: AI Augments** | Angular CLI for boilerplate, human reviews critical logic | OpenAPI generator for API client, manual auth service |
| **P7: Architecture Supports Evolution** | Vertical Slices enable Phase 1+ additions | Feature folder structure accommodates future bid submission, partner selection UI |

---

## Appendix: Decision Dependency Graph

```
State Management (A1)
  └─> Form Strategy (A2)
  └─> Component Communication (C3)

API Client Generation (B1)
  └─> HTTP Interceptor Strategy (B2)
  └─> API Error Handling (B3)

JWT Token Storage (A3)
  └─> HTTP Interceptor Strategy (B2)

Folder Structure (C1)
  └─> Routing Strategy (C2)
  └─> Component Testing Library (D1)

E2E Test Data Strategy (D2)
  └─> Code Coverage Targets (D3)

Critical Path: A1 → A2, B1 → B2, C1 → C2
```

**Critical Path Decisions** (must resolve first):
1. A1: State Management (affects Forms, Component Communication)
2. B1: API Client Generation (affects Interceptors, Error Handling)
3. C1: Folder Structure (affects Routing, Testing)

**Non-Blocking Decisions** (can defer to implementation):
- C3: Component Communication (simple app, minimal nesting)
- D3: Code Coverage Targets (can adjust after T071)

---

**End of Analysis Report**
