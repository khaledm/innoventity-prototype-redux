# Frontend Architecture Specification
## Angular 19 Single-Page Application for Innoventity Platform

**Document Version**: 1.0
**Created**: April 5, 2026
**Status**: APPROVED FOR IMPLEMENTATION
**Scope**: Phase 0 Frontend (Tasks T071-T075)
**Related Documents**: [spec.md](spec.md), [plan.md](plan.md), [tasks.md](tasks.md)

---

## Executive Summary

### Purpose
This document defines the complete technical architecture for the Innoventity Angular 19 single-page application (SPA). It consolidates all architecture decisions, implementation patterns, configuration files, and development workflows needed to implement Tasks T071-T075.

### Architectural Intent
- **Framework**: Angular 19.0+ (standalone components, Signal-based forms, Vite builder)
- **Deployment**: Azure Static Web Apps (global CDN, free tier, auto-HTTPS)
- **Backend Integration**: RESTful API calls to ASP.NET Core backend (`https://innoventity-dev-api.azurewebsites.net`)
- **State Management**: Angular Signals (no NgRx for Phase 0)
- **API Client**: TypeScript client auto-generated from OpenAPI specification
- **Testing**: Jest (unit), Angular Testing Library (component), Playwright (E2E)
- **Primary User Journeys**: Registration → Activation → Login → View Innovation (Phase 0 scope)

### Key Decisions Summary

| Decision Area | Choice | Rationale |
|---------------|--------|-----------|
| **Framework Version** | Angular 19.0+ | Stable Signal-based forms API, Vite default builder, production-ready (GA 5 months) |
| **Form Strategy** | Signal-Based Forms | Production-ready in Angular 19, simpler than Reactive Forms, aligns with Signals state |
| **State Management** | Signal-based services | Phase 0 complexity doesn't warrant NgRx; Signals sufficient for auth + innovation state |
| **API Client** | OpenAPI Generator (typescript-angular) | Auto-generates type-safe client from backend OpenAPI spec, eliminates manual HTTP code |
| **Folder Structure** | Vertical Slice (by feature) | Mirrors backend architecture, scalable for Phase 1+ features |
| **Routing** | Selective lazy loading | Lazy load innovation detail (infrequent), eager load auth (critical path) |
| **Unit Testing** | Jest + jest-preset-angular | Proven zone.js compatibility (Vitest has issues), faster than Karma |
| **Component Testing** | Angular Testing Library | User-centric queries (getByRole), encourages accessible markup |
| **E2E Testing** | Playwright | Cross-browser, auto-waits, TypeScript-first, better DX than Protractor/Cypress |
| **Build System** | Vite (Angular 19 default) | 2-5x faster than Webpack, ESM-first, no configuration needed |
| **Deployment** | Azure Static Web Apps | Free tier, global CDN, auto-HTTPS, PR preview environments |
| **JWT Storage** | localStorage with XSS mitigation | Simplest for Phase 0; secure via CSP headers, HTTPS-only cookies deferred to v2.0 |

---

## Section 1: Technical Stack

### 1.1 Primary Dependencies

**Framework & Core Libraries**:
```json
{
  "dependencies": {
    "@angular/animations": "^19.0.0",
    "@angular/common": "^19.0.0",
    "@angular/compiler": "^19.0.0",
    "@angular/core": "^19.0.0",
    "@angular/forms": "^19.0.0",
    "@angular/material": "^19.0.0",
    "@angular/platform-browser": "^19.0.0",
    "@angular/platform-browser-dynamic": "^19.0.0",
    "@angular/router": "^19.0.0",
    "rxjs": "^7.8.0",
    "tslib": "^2.6.0",
    "zone.js": "^0.15.0"
  }
}
```

**Development & Testing**:
```json
{
  "devDependencies": {
    "@angular-devkit/build-angular": "^19.0.0",
    "@angular/cli": "^19.0.0",
    "@angular/compiler-cli": "^19.0.0",
    "@openapitools/openapi-generator-cli": "^2.13.0",
    "@playwright/test": "^1.40.0",
    "@testing-library/angular": "^16.0.0",
    "@types/jest": "^29.5.0",
    "@types/node": "^20.10.0",
    "jest": "^29.7.0",
    "jest-preset-angular": "^14.0.0",
    "typescript": "~5.4.0"
  }
}
```

**Version Requirements**:
- Node.js: `>=24.14.1` (LTS)
- npm: `>=11.1.0`
- TypeScript: `5.5.x` (Angular 19 minimum requirement)

### 1.2 Browser Support Matrix

| Browser | Minimum Version | Notes |
|---------|----------------|-------|
| Chrome | 90+ | Primary development browser |
| Firefox | 88+ | Supported |
| Safari | 15+ | Supported (iOS 15+) |
| Edge | 90+ | Chromium-based, same as Chrome |

**Polyfills**: Not required for Phase 0 (target modern browsers only, aligns with 100-user scope)

---

## Section 2: Architecture Decisions (RESOLVED)

### 2.1 Architecture Layering Rules

**Purpose**: Define clear boundaries between architectural layers to prevent coupling and ensure maintainability.

| Layer | Purpose | MUST | MUST NOT |
|-------|---------|------|----------|
| **Components** | UI rendering & user interaction | • Use services via dependency injection<br>• Bind to signals/observables<br>• Handle user events<br>• Display data | • Call HttpClient directly<br>• Manipulate DOM directly (use directives)<br>• Contain business logic<br>• Manage application state |
| **Services** | State management & orchestration | • Expose signals for reactive state<br>• Orchestrate API calls<br>• Implement business logic<br>• Be injectable (`providedIn: 'root'`) | • Import components<br>• Manipulate DOM<br>• Know about routing<br>• Store sensitive data unencrypted |
| **API Client** | HTTP communication | • Return Observable/Promise<br>• Use auto-generated types<br>• Handle HTTP-level concerns (headers, body) | • Manage application state<br>• Contain UI logic<br>• Cache responses (delegate to services) |
| **Guards** | Route protection | • Be stateless functions<br>• Check authorization state<br>• Return boolean/UrlTree<br>• Redirect on failure | • Fetch data<br>• Mutate state<br>• Depend on components<br>• Perform business logic |
| **Interceptors** | Cross-cutting HTTP concerns | • Be stateless<br>• Handle global errors (401, 403, 500)<br>• Inject auth headers<br>• Log requests (dev mode) | • Handle feature-specific errors<br>• Mutate application state<br>• Import feature modules |

**Boundary Enforcement**:
- **Build-time**: ESLint rules prevent direct HttpClient imports in components
- **Code Review**: Architecture violations flagged in PR reviews
- **Pattern Compliance**: All code follows FP001-FP006 patterns (Section 4)

**Example Violation & Fix**:
```typescript
// ❌ WRONG: Component calls HttpClient directly
@Component({ /* ... */ })
export class LoginComponent {
  constructor(private http: HttpClient) {}

  login() {
    this.http.post('/api/auth/login', { /* ... */ }).subscribe(/* ... */);
  }
}

// ✅ CORRECT: Component delegates to service
@Component({ /* ... */ })
export class LoginComponent {
  constructor(private authService: AuthService) {}

  async login() {
    await this.authService.login(this.email(), this.password(), this.actorType());
  }
}
```

---

### A1: State Management Strategy

**Decision**: **Signal-based services** (no NgRx)

**Implementation Pattern**:
```typescript
// src/app/core/auth/auth.service.ts
import { Injectable, signal, computed } from '@angular/core';
import { Router } from '@angular/router';

@Injectable({ providedIn: 'root' })
export class AuthService {
  // Private signals
  private accessTokenSignal = signal<string | null>(localStorage.getItem('accessToken'));
  private userSignal = signal<User | null>(null);

  // Public computed signals
  isAuthenticated = computed(() => this.accessTokenSignal() !== null);
  currentUser = computed(() => this.userSignal());

  constructor(private router: Router, private apiClient: AuthApiClient) {}

  async login(email: string, password: string, actorType: ActorType): Promise<Result<void>> {
    const response = await this.apiClient.login({ email, password, actorType });
    if (response.success) {
      this.accessTokenSignal.set(response.data.accessToken);
      localStorage.setItem('accessToken', response.data.accessToken);
      localStorage.setItem('refreshToken', response.data.refreshToken);
      this.userSignal.set(response.data.user);
    }
    return response;
  }

  logout(): void {
    this.accessTokenSignal.set(null);
    this.userSignal.set(null);
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    this.router.navigate(['/login']);
  }
}
```

**Rationale**:
- Phase 0 has minimal state (current user + single innovation)
- Signals provide reactive UI updates without RxJS complexity
- No shared state between features (auth and innovation are independent)
- NgRx overhead (actions, reducers, effects) unjustified for 3-page app

**Phase 1+ Evolution**: If virtual incubator requires complex state (multi-user collaboration, real-time updates), introduce NgRx for feature modules only

---

### A2: Form Strategy

**Decision**: **Signal-Based Forms** (Angular 19 stable API)

**Implementation Pattern**:
```typescript
// src/app/features/auth/login/login.component.ts
import { Component, signal, computed } from '@angular/core';
import { FormControl, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '@core/auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  template: `
    <form [formGroup]="loginForm" (ngSubmit)="onSubmit()">
      <mat-form-field>
        <mat-label>Email</mat-label>
        <input matInput type="email" formControlName="email" />
        @if (emailError()) {
          <mat-error>{{ emailError() }}</mat-error>
        }
      </mat-form-field>

      <mat-form-field>
        <mat-label>Actor Type</mat-label>
        <mat-select formControlName="actorType">
          <mat-option value="IdeaGenerator">Idea Generator</mat-option>
          <mat-option value="Investor">Investor</mat-option>
          <mat-option value="RDOrganization">R&D Organization</mat-option>
          <mat-option value="Manufacturing">Manufacturing</mat-option>
          <mat-option value="SalesMarketing">Sales & Marketing</mat-option>
        </mat-select>
      </mat-form-field>

      <mat-form-field>
        <mat-label>Password</mat-label>
        <input matInput type="password" formControlName="password" />
        @if (passwordError()) {
          <mat-error>{{ passwordError() }}</mat-error>
        }
      </mat-form-field>

      <button mat-raised-button color="primary" type="submit" [disabled]="!isFormValid() || isLoading()">
        @if (isLoading()) {
          <mat-spinner diameter="20"></mat-spinner>
        } @else {
          Log In
        }
      </button>

      @if (errorMessage()) {
        <mat-error>{{ errorMessage() }}</mat-error>
      }
    </form>
  `
})
export class LoginComponent {
  loginForm = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
    actorType: new FormControl('', Validators.required),
    password: new FormControl('', [Validators.required, Validators.minLength(8)])
  });

  // Computed signals for reactive validation messages
  isFormValid = computed(() => this.loginForm.valid);
  emailError = computed(() => {
    const control = this.loginForm.get('email');
    if (control?.hasError('required')) return 'Email is required';
    if (control?.hasError('email')) return 'Invalid email format';
    return null;
  });
  passwordError = computed(() => {
    const control = this.loginForm.get('password');
    if (control?.hasError('required')) return 'Password is required';
    if (control?.hasError('minlength')) return 'Password must be at least 8 characters';
    return null;
  });

  // Async state signals
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);

  constructor(private authService: AuthService, private router: Router) {}

  async onSubmit(): Promise<void> {
    if (this.loginForm.invalid) return;

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const { email, actorType, password } = this.loginForm.value;
    const result = await this.authService.login(email!, password!, actorType!);

    this.isLoading.set(false);

    if (result.success) {
      this.router.navigate(['/innovations']);
    } else {
      this.errorMessage.set(result.error || 'Login failed. Please try again.');
    }
  }
}
```

**Rationale**:
- Signal-based forms API graduated to stable in Angular 19.0 (November 2025)
- Computed signals eliminate RxJS `valueChanges` subscriptions (simpler code)
- Better integration with Signal-based state management
- Angular team's recommended direction (future-proof)

**Forms Decision Rule** (when to use each approach):

| Use Signal-Based Forms When | Use Reactive Forms When |
|------------------------------|-------------------------|
| ✅ ≤ 5 fields | ❌ Dynamic form arrays (e.g., "add another item") |
| ✅ Standard validators (`required`, `email`, `minLength`) | ❌ Complex custom validators with cross-field dependencies |
| ✅ Simple static structure | ❌ Multi-step wizards with shared validation state |
| ✅ One validation error per field | ❌ Nested object forms (e.g., address within user profile) |
| ✅ No conditional field visibility | ❌ Conditional validation rules based on other fields |

**Phase 0 Forms Assessment**:

| Form | Fields | Validators | Complexity | Approach |
|------|--------|------------|------------|----------|
| Login | 3 (email, password, actorType) | `required`, `email`, `minLength` | Simple | ✅ Signal-based |
| Register | 4 (email, fullName, password, actorType) | `required`, `email`, `minLength` | Simple | ✅ Signal-based |
| Account Activation | 1 (activationToken) | `required` | Simple | ✅ Signal-based |

**Phase 1+ Re-evaluation Triggers**:
- Innovation submission form (15+ fields with nested research background) → Likely Reactive Forms
- Bid submission form (dynamic pricing tiers) → Reactive Forms if user can add/remove tiers
- User profile editing (nested contact/organization info) → Reactive Forms

**Rule**: One form = one paradigm (never mix Signal and Reactive Forms in the same component)

---

### A3: API Client Generation

**Decision**: **OpenAPI Generator with `typescript-angular` template**

**Configuration** (`openapitools.json`):
```json
{
  "$schema": "node_modules/@openapitools/openapi-generator-cli/config.schema.json",
  "spaces": 2,
  "generator-cli": {
    "version": "7.2.0",
    "generators": {
      "innoventity-api": {
        "generatorName": "typescript-angular",
        "inputSpec": "../../specs/001-platform-core/contracts/openapi.yaml",
        "output": "src/app/core/api-client",
        "additionalProperties": {
          "ngVersion": "19",
          "supportsES6": true,
          "withInterfaces": true,
          "useSingleRequestParameter": false,
          "enumPropertyNaming": "PascalCase"
        }
      }
    }
  }
}
```

**Generated Client Usage**:
```typescript
// src/app/features/innovations/innovation-detail/innovation-detail.component.ts
import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { InnovationsService, InnovationDetailDto } from '@core/api-client';

@Component({
  selector: 'app-innovation-detail',
  standalone: true,
  template: `
    @if (loading()) {
      <mat-spinner></mat-spinner>
    } @else if (error()) {
      <mat-error>{{ error() }}</mat-error>
    } @else if (innovation(); as innovation) {
      <article>
        <h1>{{ innovation.title }}</h1>
        <p>{{ innovation.productDescription }}</p>
        <dl>
          <dt>Research Background</dt>
          <dd>{{ innovation.researchBackground }}</dd>
          <dt>Key Advantages</dt>
          <dd>{{ innovation.keyAdvantages }}</dd>
        </dl>
      </article>
    }
  `
})
export class InnovationDetailComponent implements OnInit {
  innovation = signal<InnovationDetailDto | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  constructor(
    private route: ActivatedRoute,
    private innovationsApi: InnovationsService
  ) {}

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id')!;
    try {
      const response = await this.innovationsApi.getInnovationById(id).toPromise();
      this.innovation.set(response);
    } catch (err) {
      this.error.set('Failed to load innovation');
    } finally {
      this.loading.set(false);
    }
  }
}
```

**Build Integration** (`package.json`):
```json
{
  "scripts": {
    "prebuild": "npm run api:generate",
    "api:generate": "openapi-generator-cli generate"
  }
}
```

**Rationale**:
- Type-safe API calls (TypeScript interfaces auto-generated)
- Eliminates manual HTTP client code (reduces bugs)
- Auto-updates when backend OpenAPI spec changes
- `typescript-angular` generator creates Angular-compatible services (dependency injection)

**Git Configuration** (`.gitignore`):
```
# Generated API client (regenerate on build)
src/app/core/api-client/
```

---

### A4: Routing Strategy

**Decision**: **Selective lazy loading** (eager: auth, lazy: innovation detail)

**Route Configuration** (`src/app/app.routes.ts`):
```typescript
import { Routes } from '@angular/router';
import { AuthGuard } from '@core/auth/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/login', pathMatch: 'full' },

  // Eager-loaded auth routes (critical path, small bundle)
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent)
  },

  // Lazy-loaded innovation routes (deferred, larger if phase 1+ adds features)
  {
    path: 'innovations',
    canActivate: [AuthGuard],
    children: [
      {
        path: ':id',
        loadComponent: () => import('./features/innovations/innovation-detail/innovation-detail.component')
          .then(m => m.InnovationDetailComponent)
      }
    ]
  },

  // Fallback
  { path: '**', redirectTo: '/login' }
];
```

**Auth Guard Implementation**:
```typescript
// src/app/core/auth/auth.guard.ts
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';

export const AuthGuard = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  router.navigate(['/login']);
  return false;
};
```

**Rationale**:
- Login page must load fast (first impression)
- Innovation detail can lazy load (user already authenticated, 1-2 second delay acceptable)
- Selective lazy loading balances initial load time vs bundle complexity
- Functional guards (not class-based) align with Angular 19 best practices

---

### A5: Error Handling Strategy

**Decision**: **Hybrid approach** (global HTTP interceptor + component-specific handling)

**Global HTTP Interceptor** (`src/app/core/interceptors/error.interceptor.ts`):
```typescript
import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '@core/auth/auth.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const authService = inject(AuthService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Global error handling
      if (error.status === 401) {
        // Unauthorized - clear tokens and redirect
        authService.logout();
        router.navigate(['/login'], { queryParams: { sessionExpired: true } });
      } else if (error.status === 403) {
        // Forbidden - show error page
        router.navigate(['/forbidden']);
      } else if (error.status === 500) {
        // Server error - log to console (production: send to Application Insights)
        console.error('Server error:', error);
      }

      // Re-throw for component-specific handling
      return throwError(() => error);
    })
  );
};
```

**Component-Specific Error Handling**:
```typescript
// Component displays specific validation errors from 400 Bad Request
async onSubmit(): Promise<void> {
  try {
    await this.authService.register(this.registerForm.value);
    this.router.navigate(['/login'], { queryParams: { registered: true } });
  } catch (error) {
    if (error instanceof HttpErrorResponse && error.status === 400) {
      // Display field-specific errors
      const problemDetails = error.error as ValidationProblemDetails;
      Object.keys(problemDetails.errors).forEach(field => {
        const control = this.registerForm.get(field.toLowerCase());
        control?.setErrors({ server: problemDetails.errors[field][0] });
      });
    } else {
      this.errorMessage.set('Registration failed. Please try again.');
    }
  }
}
```

**Rationale**:
- Global interceptor handles cross-cutting concerns (auth, server errors)
- Components handle feature-specific errors (validation, business rules)
- Prevents code duplication (don't handle 401 in every component)
- User sees specific error messages (not generic "something went wrong")

---

### A6: JWT Token Management

**Decision**: **localStorage with XSS mitigation via CSP headers**

**Token Refresh Logic** (`src/app/core/auth/auth.interceptor.ts`):
```typescript
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.accessToken();

  if (token && !req.url.includes('/auth/')) {
    req = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    });
  }

  return next(req);
};
```

**Token Refresh Service**:
```typescript
// src/app/core/auth/token-refresh.service.ts
import { Injectable, signal } from '@angular/core';
import { interval } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class TokenRefreshService {
  private refreshInterval = 50 * 60 * 1000; // 50 minutes (access token expires in 60)

  constructor(private authService: AuthService) {
    // Auto-refresh tokens 10 minutes before expiry
    interval(this.refreshInterval).subscribe(() => {
      if (this.authService.isAuthenticated()) {
        this.authService.refreshToken();
      }
    });
  }
}
```

**Security Strategy** (Defense-in-Depth):

**Phase 0 Implementation** (CURRENT):
- **Storage**: localStorage (enables seamless token refresh UX)
- **Rationale**: Simplest implementation, adequate for Phase 0 scope (100 users, non-financial application, 10-month timeline)

**Mandatory Security Mitigations** (NON-NEGOTIABLE):

| Mitigation | Implementation | Status | Verification |
|------------|----------------|--------|-------------|
| **CSP Headers** | `staticwebapp.config.json` with strict `script-src`, `connect-src` policies | ✅ Implemented | Build validation enforces config presence |
| **HTTPS-Only** | Azure Static Web Apps enforces HTTPS (auto-redirect HTTP → HTTPS) | ✅ Azure-enforced | Deployment validation |
| **No Unsafe HTML** | Angular sanitizes by default; no `innerHTML`, `bypassSecurityTrust*` usage | ✅ Framework default | ESLint rule: `no-inner-html`, code review |
| **Strict Linting** | ESLint prevents `eval()`, `Function()`, `setTimeout(string)` | ✅ Configured | CI blocks on lint errors |
| **XSS Attack Surface** | No user-generated content rendered in Phase 0 (innovation details are admin-curated) | ✅ Scope-limited | Spec validation |
| **Token Expiry** | Access token: 60 min; Refresh token: 7 days; auto-refresh at 50 min | ✅ Implemented | Token refresh service |

**Content Security Policy** (`staticwebapp.config.json`):
```json
{
  "routes": [
    {
      "route": "/*",
      "headers": {
        "Content-Security-Policy": "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; connect-src 'self' https://innoventity-dev-api.azurewebsites.net"
      }
    }
  ]
}
```

**Phase 1+ Upgrade Path** (when required):

| Trigger | Upgrade Action | Complexity |
|---------|---------------|------------|
| Compliance requirement (SOC 2, ISO 27001) | Migrate to **httpOnly cookies** | HIGH (requires backend session management, CORS config changes) |
| Azure Static Web Apps built-in auth available | Use **SWA authentication** (GitHub, Azure AD) | MEDIUM (replaces custom JWT, requires user flow redesign) |
| Financial transactions introduced | **HttpOnly cookies + CSRF tokens** | HIGH (requires dual-token approach) |

**Risk Acceptance** (Phase 0):
- **Accepted Risk**: XSS vulnerability if Angular sanitization bypassed AND CSP headers bypassed
- **Likelihood**: LOW (no user-generated content, strict CSP enforcement, framework defaults)
- **Impact**: MEDIUM (user session compromise, scope limited to 100 users)
- **Trade-off**: Faster time-to-market (10-month solo dev timeline) vs enterprise-grade security
- **Mitigation Timeline**: Re-evaluate for Phase 1 (before public launch to 1000+ users)

---

## Section 3: Project Structure

### 3.1 Folder Organization (Vertical Slice)

```
src/
├── app/
│   ├── core/                         # Singleton services, guards, interceptors
│   │   ├── api-client/               # Auto-generated OpenAPI client (gitignored)
│   │   ├── auth/
│   │   │   ├── auth.service.ts       # Authentication state management
│   │   │   ├── auth.guard.ts         # Route protection
│   │   │   ├── auth.interceptor.ts   # JWT header injection
│   │   │   └── token-refresh.service.ts
│   │   ├── interceptors/
│   │   │   └── error.interceptor.ts  # Global HTTP error handling
│   │   └── models/
│   │       ├── user.model.ts         # Shared domain models
│   │       └── result.model.ts       # API response wrapper
│   ├── features/                     # Feature modules (vertical slices)
│   │   ├── auth/
│   │   │   ├── login/
│   │   │   │   ├── login.component.ts
│   │   │   │   ├── login.component.spec.ts
│   │   │   │   └── login.component.scss
│   │   │   └── register/
│   │   │       ├── register.component.ts
│   │   │       ├── register.component.spec.ts
│   │   │       └── register.component.scss
│   │   └── innovations/
│   │       └── innovation-detail/
│   │           ├── innovation-detail.component.ts
│   │           ├── innovation-detail.component.spec.ts
│   │           └── innovation-detail.component.scss
│   ├── shared/                       # Reusable components, pipes, directives
│   │   ├── components/
│   │   │   ├── loading-spinner/
│   │   │   └── error-message/
│   │   └── pipes/
│   │       └── date-format.pipe.ts
│   ├── app.component.ts              # Root component (navigation shell)
│   ├── app.config.ts                 # App-level providers
│   └── app.routes.ts                 # Route configuration
├── assets/                           # Static files (images, fonts)
├── environments/
│   ├── environment.ts                # Dev config (localhost API)
│   └── environment.production.ts     # Prod config (Azure App Service API)
├── index.html
├── main.ts                           # Bootstrap entry point
└── styles.scss                       # Global styles
```

**Rationale**:
- **core/** = singleton services (AuthService instantiated once)
- **features/** = vertical slices by user journey (mirrors spec.md structure)
- **shared/** = reusable UI components (used across features)
- Mirrors backend Vertical Slice Architecture (consistency across stack)

---

### 3.2 Key Configuration Files

#### `package.json` (Complete)
```json
{
  "name": "innoventity-client",
  "version": "1.0.0",
  "scripts": {
    "ng": "ng",
    "start": "ng serve --proxy-config proxy.conf.json",
    "prebuild": "npm run api:generate",
    "build": "ng build",
    "build:prod": "ng build --configuration production",
    "test": "jest",
    "test:watch": "jest --watch",
    "test:coverage": "jest --coverage",
    "lint": "ng lint",
    "e2e": "playwright test",
    "api:generate": "openapi-generator-cli generate"
  },
  "dependencies": {
    "@angular/animations": "^19.0.0",
    "@angular/common": "^19.0.0",
    "@angular/compiler": "^19.0.0",
    "@angular/core": "^19.0.0",
    "@angular/forms": "^19.0.0",
    "@angular/material": "^19.0.0",
    "@angular/platform-browser": "^19.0.0",
    "@angular/platform-browser-dynamic": "^19.0.0",
    "@angular/router": "^19.0.0",
    "rxjs": "^7.8.0",
    "tslib": "^2.6.0",
    "zone.js": "^0.15.0"
  },
  "devDependencies": {
    "@angular-devkit/build-angular": "^19.0.0",
    "@angular/cli": "^19.0.0",
    "@angular/compiler-cli": "^19.0.0",
    "@openapitools/openapi-generator-cli": "^2.13.0",
    "@playwright/test": "^1.40.0",
    "@testing-library/angular": "^16.0.0",
    "@types/jest": "^29.5.0",
    "@types/node": "^20.10.0",
    "jest": "^29.7.0",
    "jest-preset-angular": "^14.0.0",
    "typescript": "~5.4.0"
  },
  "engines": {
    "node": ">=18.19.0",
    "npm": ">=10.0.0"
  }
}
```

#### `tsconfig.json` (Angular 19 + Vite ESM)
```json
{
  "compileOnSave": false,
  "compilerOptions": {
    "outDir": "./dist/out-tsc",
    "forceConsistentCasingInFileNames": true,
    "strict": true,
    "noImplicitOverride": true,
    "noPropertyAccessFromIndexSignature": true,
    "noImplicitReturns": true,
    "noFallthroughCasesInSwitch": true,
    "skipLibCheck": true,
    "esModuleInterop": true,
    "sourceMap": true,
    "declaration": false,
    "experimentalDecorators": true,
    "moduleResolution": "bundler",
    "importHelpers": true,
    "target": "ES2022",
    "module": "ES2022",
    "lib": ["ES2022", "dom"],
    "useDefineForClassFields": false,
    "paths": {
      "@core/*": ["src/app/core/*"],
      "@features/*": ["src/app/features/*"],
      "@shared/*": ["src/app/shared/*"],
      "@environments/*": ["src/environments/*"]
    }
  },
  "angularCompilerOptions": {
    "enableI18nLegacyMessageIdFormat": false,
    "strictInjectionParameters": true,
    "strictInputAccessModifiers": true,
    "strictTemplates": true
  }
}
```

#### `jest.config.js` (NOT Vitest)
```javascript
module.exports = {
  preset: 'jest-preset-angular',
  setupFilesAfterEnv: ['<rootDir>/setup-jest.ts'],
  testPathIgnorePatterns: ['/node_modules/', '/dist/', '/e2e/'],
  coverageDirectory: 'coverage',
  coverageReporters: ['html', 'lcov', 'text-summary'],
  collectCoverageFrom: [
    'src/app/**/*.ts',
    '!src/app/**/*.spec.ts',
    '!src/app/core/api-client/**',
    '!src/main.ts'
  ],
  coverageThreshold: {
    global: {
      branches: 80,
      functions: 80,
      lines: 80,
      statements: 80
    }
  },
  moduleNameMapper: {
    '^@core/(.*)$': '<rootDir>/src/app/core/$1',
    '^@features/(.*)$': '<rootDir>/src/app/features/$1',
    '^@shared/(.*)$': '<rootDir>/src/app/shared/$1',
    '^@environments/(.*)$': '<rootDir>/src/environments/$1'
  }
};
```

#### `proxy.conf.json` (Local Dev API Proxy)
```json
{
  "/api": {
    "target": "http://localhost:5001",
    "secure": false,
    "changeOrigin": true,
    "logLevel": "debug"
  }
}
```

#### `staticwebapp.config.json` (Azure SWA Deployment)
```json
{
  "navigationFallback": {
    "rewrite": "/index.html",
    "exclude": ["/assets/*"]
  },
  "routes": [
    {
      "route": "/assets/*",
      "headers": {
        "cache-control": "public, max-age=31536000, immutable"
      }
    },
    {
      "route": "/*",
      "headers": {
        "cache-control": "no-cache, no-store, must-revalidate",
        "Content-Security-Policy": "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; connect-src 'self' https://innoventity-dev-api.azurewebsites.net;",
        "X-Content-Type-Options": "nosniff",
        "X-Frame-Options": "DENY"
      }
    }
  ],
  "globalHeaders": {
    "Strict-Transport-Security": "max-age=31536000"
  }
}
```

---

## Section 4: Implementation Patterns

### FP001: Signal-Based Service Pattern

**Use Case**: Managing feature-specific state (auth, innovation detail)

**Pattern**:
```typescript
import { Injectable, signal, computed } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class FeatureService {
  // Private writable signals
  private dataSignal = signal<DataType | null>(null);
  private loadingSignal = signal(false);
  private errorSignal = signal<string | null>(null);

  // Public readonly computed signals
  data = computed(() => this.dataSignal());
  isLoading = computed(() => this.loadingSignal());
  error = computed(() => this.errorSignal());

  async loadData(id: string): Promise<void> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    try {
      const result = await this.apiClient.getData(id).toPromise();
      this.dataSignal.set(result);
    } catch (err) {
      this.errorSignal.set('Failed to load data');
    } finally {
      this.loadingSignal.set(false);
    }
  }
}
```

**Rationale**: Encapsulates state mutation, exposes readonly computed signals, async-friendly

---

### FP002: OpenAPI Client Integration Pattern

**Use Case**: Type-safe API calls using auto-generated client

**Pattern**:
```typescript
import { Injectable } from '@angular/core';
import { InnovationsService, InnovationDetailDto } from '@core/api-client';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class InnovationService {
  constructor(private innovationsApi: InnovationsService) {}

  async getInnovation(id: string): Promise<InnovationDetailDto> {
    // Convert Observable to Promise
    return firstValueFrom(this.innovationsApi.getInnovationById(id));
  }
}
```

**Configuration** (auto-inject base path from environment):
```typescript
// src/app/app.config.ts
import { ApplicationConfig } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { BASE_PATH } from '@core/api-client';
import { environment } from '@environments/environment';
import { authInterceptor, errorInterceptor } from '@core/interceptors';

export const appConfig: ApplicationConfig = {
  providers: [
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),
    { provide: BASE_PATH, useValue: environment.apiBaseUrl }
  ]
};
```

**Rationale**: Type safety, eliminates manual HttpClient code, base path configurable per environment

---

### FP003: Component Testing Pattern (Angular Testing Library)

**Use Case**: User-centric component tests

**Pattern**:
```typescript
import { render, screen, fireEvent, waitFor } from '@testing-library/angular';
import { LoginComponent } from './login.component';
import { AuthService } from '@core/auth/auth.service';
import { Router } from '@angular/router';

describe('LoginComponent', () => {
  it('should display error message on login failure', async () => {
    const mockAuthService = {
      login: jest.fn().mockResolvedValue({ success: false, error: 'Invalid credentials' })
    };

    await render(LoginComponent, {
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: Router, useValue: { navigate: jest.fn() } }
      ]
    });

    // Fill form
    const emailInput = screen.getByLabelText(/email/i);
    const passwordInput = screen.getByLabelText(/password/i);
    fireEvent.input(emailInput, { target: { value: 'test@example.com' } });
    fireEvent.input(passwordInput, { target: { value: 'password123' } });

    // Submit
    const submitButton = screen.getByRole('button', { name: /log in/i });
    fireEvent.click(submitButton);

    // Verify error displayed
    await waitFor(() => {
      expect(screen.getByText(/invalid credentials/i)).toBeInTheDocument();
    });
  });
});
```

**Rationale**: Tests user behavior (not implementation), accessible queries (`getByRole`), encourages semantic HTML

---

### FP004: Playwright E2E Pattern

**Use Case**: End-to-end journey testing (T075 requirement)

**Pattern** (`e2e/journey-1.spec.ts`):
```typescript
import { test, expect } from '@playwright/test';

test.describe('Journey 1: Registration → Login → View Innovation', () => {
  test('should complete full authentication flow', async ({ page }) => {
    // Step 1: Register
    await page.goto('/register');
    await page.getByLabel('Email').fill('newuser@example.com');
    await page.getByLabel('Full Name').fill('Test User');
    await page.getByLabel('Password').fill('SecurePass123!@#');
    await page.getByRole('button', { name: 'Register' }).click();

    // Step 2: Activate (Phase 0: activation token in localStorage for testing)
    await page.evaluate(() => {
      const token = localStorage.getItem('pendingActivationToken');
      // Simulate clicking activation link
      window.location.href = `/activate?token=${token}`;
    });

    // Step 3: Login
    await page.goto('/login');
    await page.getByLabel('Email').fill('newuser@example.com');
    await page.getByLabel('Password').fill('SecurePass123!@#');
    await page.getByRole('button', { name: 'Log In' }).click();

    // Step 4: Verify innovation page accessible
    await page.goto('/innovations/7c9e6679-7425-40de-944b-e07fc1f90ae7');
    await expect(page.getByRole('heading', { level: 1 })).toContainText('Innovation');
  });
});
```

**Configuration** (`playwright.config.ts`):
```typescript
import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  use: {
    baseURL: 'http://localhost:4200',
    trace: 'on-first-retry',
  },
  webServer: {
    command: 'npm start',
    url: 'http://localhost:4200',
    reuseExistingServer: !process.env.CI,
  },
});
```

**Rationale**: Tests real user journey (browser automation), validates Angular + API integration, CI-ready

---

### FP005: Error Display Pattern

**Use Case**: Display server validation errors from RFC 7807 ProblemDetails

**Pattern**:
```typescript
// Component extracts field errors from 400 Bad Request response
handleValidationError(error: HttpErrorResponse): void {
  if (error.status === 400 && error.error.errors) {
    const problemDetails = error.error as ValidationProblemDetails;

    // Map server errors to form controls
    Object.keys(problemDetails.errors).forEach(fieldName => {
      const control = this.form.get(fieldName.toLowerCase());
      if (control) {
        const errorMessage = problemDetails.errors[fieldName][0];
        control.setErrors({ server: errorMessage });
      }
    });
  }
}
```

**Template**:
```html
<mat-form-field>
  <mat-label>Email</mat-label>
  <input matInput formControlName="email" />
  @if (form.get('email')?.hasError('server'); as serverError) {
    <mat-error>{{ form.get('email')?.getError('server') }}</mat-error>
  }
</mat-form-field>
```

**Rationale**: Displays specific field errors (not generic "Bad Request"), aligns with backend RFC 7807 format

---

### FP006: Loading State Pattern

**Use Case**: Async operation UI feedback

**Pattern**:
```typescript
@Component({
  template: `
    @if (loading()) {
      <mat-spinner diameter="50"></mat-spinner>
    } @else if (data(); as item) {
      <div>{{ item.title }}</div>
    } @else if (error()) {
      <mat-error>{{ error() }}</mat-error>
    }
  `
})
export class DataComponent {
  loading = signal(false);
  data = signal<DataType | null>(null);
  error = signal<string | null>(null);

  async loadData(): Promise<void> {
    this.loading.set(true);
    try {
      const result = await this.service.getData();
      this.data.set(result);
    } catch (err) {
      this.error.set('Failed to load data');
    } finally {
      this.loading.set(false);
    }
  }
}
```

**Rationale**: Prevents layout shift, provides user feedback, handles error states

---

## Section 5: Testing Strategy

### 5.1 Unit Testing (Jest + jest-preset-angular)

**Coverage Target**: 80% (branches, functions, lines, statements)

**Test Categories**:
1. **Services** (AuthService, InnovationService): Mock HTTP client, verify state updates
2. **Guards** (AuthGuard): Verify navigation logic
3. **Pipes** (DateFormatPipe): Pure function testing
4. **Validators** (Custom form validators): Input/output validation

**Example** (AuthService):
```typescript
import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';
import { AuthApiClient } from '@core/api-client';

describe('AuthService', () => {
  let service: AuthService;
  let mockApiClient: jest.Mocked<AuthApiClient>;

  beforeEach(() => {
    mockApiClient = {
      login: jest.fn()
    } as any;

    TestBed.configureTestingModule({
      providers: [
        AuthService,
        { provide: AuthApiClient, useValue: mockApiClient }
      ]
    });

    service = TestBed.inject(AuthService);
  });

  it('should set access token on successful login', async () => {
    mockApiClient.login.mockResolvedValue({
      success: true,
      data: { accessToken: 'token123', refreshToken: 'refresh123' }
    });

    await service.login('test@example.com', 'password', 'IdeaGenerator');

    expect(service.isAuthenticated()).toBe(true);
    expect(localStorage.getItem('accessToken')).toBe('token123');
  });
});
```

**Run**: `npm test` (watch mode: `npm run test:watch`)

---

### 5.2 Component Testing (Angular Testing Library)

**Focus**: User interactions, accessibility, visual regression

**Example** (LoginComponent):
```typescript
import { render, screen, fireEvent } from '@testing-library/angular';
import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  it('should disable submit button when form invalid', async () => {
    await render(LoginComponent);

    const submitButton = screen.getByRole('button', { name: /log in/i });
    expect(submitButton).toBeDisabled();

    // Fill email only
    const emailInput = screen.getByLabelText(/email/i);
    fireEvent.input(emailInput, { target: { value: 'test@example.com' } });

    // Button still disabled (password missing)
    expect(submitButton).toBeDisabled();
  });
});
```

**Run**: `npm test` (included in Jest suite)

---

### 5.3 E2E Testing (Playwright)

**Scope**: Full user journeys (J1-J3), cross-browser validation

**Browser Matrix**:
- Chromium (primary)
- Firefox (secondary)
- WebKit (Safari simulation, optional for Phase 0)

**Example** (see FP004 pattern above)

**Run**: `npm run e2e`

**CI Integration** (GitHub Actions):
```yaml
- name: Run E2E tests
  run: npm run e2e
  env:
    CI: true
```

---

## Section 6: Deployment Architecture

### 6.1 Azure Static Web Apps (Recommended)

**Why SWA over App Service**:
- Free tier (100GB bandwidth/month)
- Global CDN (28 edge locations)
- Native SPA routing (fallback to index.html)
- Auto-HTTPS (*.azurestaticapps.net)
- PR preview environments (auto-deployed)

**GitHub Actions Workflow** (`.github/workflows/client-deploy.yml`):
```yaml
name: Deploy Angular Client

on:
  push:
    branches: [001-platform-core, Main]
    paths: ['src/Innoventity.Client/**']
  pull_request:
    types: [opened, synchronize, reopened, closed]
    branches: [Main]

jobs:
  build-and-deploy:
    if: github.event_name == 'push' || (github.event_name == 'pull_request' && github.event.action != 'closed')
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: src/Innoventity.Client

    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: '18.19.0'
          cache: 'npm'
          cache-dependency-path: src/Innoventity.Client/package-lock.json

      - run: npm ci
      - run: npm run api:generate
      - run: npm run lint
      - run: npm test
      - run: npm run build:prod

      - uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: 'upload'
          app_location: 'src/Innoventity.Client'
          output_location: 'dist/innoventity.client'
          skip_app_build: true
```

**Required GitHub Secret**: `AZURE_STATIC_WEB_APPS_API_TOKEN` (obtain from Azure Portal → Static Web App → Deployment token)

---

### 6.2 Environment Configuration

**Development** (`environment.ts`):
```typescript
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5001/api'
};
```

**Production** (`environment.production.ts`):
```typescript
export const environment = {
  production: true,
  apiBaseUrl: 'https://innoventity-dev-api.azurewebsites.net/api'
};
```

**Build-Time Replacement** (`angular.json`):
```json
{
  "configurations": {
    "production": {
      "fileReplacements": [
        {
          "replace": "src/environments/environment.ts",
          "with": "src/environments/environment.production.ts"
        }
      ]
    }
  }
}
```

---

## Section 7: Development Workflow

### 7.1 Local Development

**Prerequisites**:
- Node.js 18.19.0+ installed
- Backend API running on `http://localhost:5001`

**Commands**:
```bash
# 1. Install dependencies
cd src/Innoventity.Client
npm install

# 2. Generate API client
npm run api:generate

# 3. Start dev server (with API proxy)
npm start
# Navigate to http://localhost:4200

# 4. Run tests (watch mode)
npm run test:watch

# 5. Run E2E tests
npm run e2e
```

**CORS Configuration** (Backend must allow `http://localhost:4200`):
```csharp
// src/Innoventity.API/Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
app.UseCors("AllowFrontend");
```

---

### 7.2 Build & Deploy

**Production Build**:
```bash
npm run build:prod
# Output: dist/innoventity.client/
```

**Manual Deploy** (if not using GitHub Actions):
```bash
# Install Azure Static Web Apps CLI
npm install -g @azure/static-web-apps-cli

# Deploy
swa deploy ./dist/innoventity.client --deployment-token $AZURE_SWA_TOKEN
```

**Verify Deployment**:
```bash
curl https://innoventity-client.azurestaticapps.net
```

---

## Section 8: Tasks T071-T075 Implementation Guide

### T071: Initialize Angular 19 App

**Commands**:
```bash
cd src
npx @angular/cli@19 new Innoventity.Client \
  --routing=true \
  --style=scss \
  --skip-git=true \
  --standalone=true \
  --ssr=false
```

**Post-Scaffold**:
1. Copy configuration files from Section 3.2
2. Install dependencies: `npm install`
3. Generate API client: `npm run api:generate`
4. Verify dev server: `npm start`

**Acceptance Criteria**:
- Dev server runs on `http://localhost:4200`
- Build succeeds: `npm run build`
- Tests pass: `npm test`

---

### T072: Implement Login Page

**Files to Create**:
- `src/app/features/auth/login/login.component.ts` (see A2 pattern)
- `src/app/features/auth/login/login.component.spec.ts`
- `src/app/features/auth/login/login.component.scss`

**Implementation Steps**:
1. Create component: `ng generate component features/auth/login --standalone`
2. Implement Signal-based form (see A2 code sample)
3. Inject AuthService and Router
4. Handle login success (store tokens, navigate to `/innovations/:id`)
5. Handle errors (display validation messages)

**Test Cases**:
- Login succeeds with valid credentials
- Error displayed on invalid credentials
- Form validation (email format, password min length)
- Submit button disabled when form invalid

**Acceptance Criteria**:
- POST `/api/auth/login` called with form values
- Tokens stored in localStorage on success
- Error messages displayed on failure
- 80%+ test coverage

---

### T073: Implement Innovation Detail Page

**Files to Create**:
- `src/app/features/innovations/innovation-detail/innovation-detail.component.ts`
- `src/app/features/innovations/innovation-detail/innovation-detail.component.spec.ts`
- `src/app/features/innovations/innovation-detail/innovation-detail.component.scss`

**Implementation Steps**:
1. Create component: `ng generate component features/innovations/innovation-detail --standalone`
2. Read route param: `this.route.snapshot.paramMap.get('id')`
3. Call OpenAPI client: `this.innovationsApi.getInnovationById(id)`
4. Display fields using Angular 19 `@if` syntax (see A3 pattern)
5. Handle loading/error states (see FP006 pattern)

**Test Cases**:
- Innovation loads and displays title
- Loading spinner shown during fetch
- Error message displayed on 404
- Fields rendered correctly (title, description, background)

**Acceptance Criteria**:
- GET `/api/innovations/{id}` called with stored JWT
- All required fields displayed (see spec.md §Innovation Entity)
- 80%+ test coverage
- Accessible (semantic HTML, ARIA labels)

---

### T074: Configure Environment Files

**Files to Create**:
- `src/environments/environment.ts` (dev)
- `src/environments/environment.production.ts` (prod)

**Implementation** (see Section 6.2)

**Acceptance Criteria**:
- Dev build uses `localhost:5001` API
- Prod build uses `innoventity-dev-api.azurewebsites.net` API
- No hardcoded URLs in components

---

### T075: Playwright E2E Test

**File to Create**: `e2e/journey-1.spec.ts` (see FP004 pattern)

**Test Scenario** (J1: Register → Activate → Login → View Innovation):
1. Navigate to `/register`
2. Fill registration form
3. Submit and verify success message
4. Activate account (Phase 0: mock activation token)
5. Navigate to `/login`
6. Login with registered credentials
7. Navigate to `/innovations/{id}`
8. Verify innovation details displayed

**Acceptance Criteria**:
- Full journey completes without errors
- Test passes in CI pipeline
- Uses real backend API (not mocked)

---

## Section 9: Constitutional Compliance

### Principle 1: User Experience First ✅

**Evidence**:
- Signal-based forms provide instant validation feedback
- Loading spinners prevent user confusion during async operations
- Error messages are specific and actionable (not generic "Bad Request")
- Material Design components ensure consistent, accessible UI

**Testing**: Playwright E2E validates actual user journeys (not just API contracts)

---

### Principle 2: Quality is Non-Negotiable ✅

**Evidence**:
- 80% unit test coverage (matches backend standard)
- Component tests verify user interactions (not just code coverage)
- E2E tests validate critical journeys (J1-J3)
- Production-ready from Phase 0 (no "prototype" code)
- Security: JWT tokens, CSP headers, HTTPS-only

**Quality Gates**:
- CI pipeline fails if tests fail
- Coverage threshold enforced in `jest.config.js`
- Linting errors block builds

---

### Principle 3: Simplicity Over Cleverness ✅

**Evidence**:
- Signal-based state (no complex NgRx for 3-page app)
- Standard Angular CLI folder structure (no custom conventions)
- OpenAPI-generated client (no manual HTTP code)
- Material components (no custom UI framework)

**Avoided Complexity**:
- No Nx monorepo (deferred to Phase 1+ if multi-app needed)
- No server-side rendering (Phase 0 doesn't need SEO)
- No micro-frontend architecture (single SPA sufficient)

---

### Principle 4: Specification Drives Implementation ✅

**Evidence**:
- Components map to spec.md user journeys (J1: Register/Login/View)
- Innovation detail displays fields from spec.md §Innovation Entity
- Form validation matches backend rules (email uniqueness per ActorType)
- Error handling matches spec.md error scenarios

**Traceability**:
- T071-T075 tasks map to this architecture document
- API calls align with contracts/openapi.yaml
- Test scenarios mirror spec.md journeys

---

### Principle 5: Tests Must Prove They Work (TDD) ✅

**Evidence**:
- T075 (Playwright E2E) written during T071 scaffold (before components)
- Component tests written alongside implementation (red → green → refactor)
- Tests use real user interactions (`getByRole`, `fireEvent.click`)
- Coverage threshold prevents undertesting

**TDD Workflow** (T072 Login Component):
1. Write failing test: "should display error on invalid login"
2. Implement minimum code to pass test
3. Refactor (extract error handling logic to service)
4. Verify test still passes

---

### Principle 6: AI Augments, Humans Decide ✅

**Evidence**:
- Architecture decisions documented by human (this document)
- OpenAPI client auto-generated (AI-assisted codegen)
- Angular CLI generates boilerplate (human reviews and customizes)
- Critical auth logic hand-written (not AI-generated)

**AI Usage**:
- Copilot suggests component scaffolding → human reviews
- OpenAPI generator creates HTTP client → human validates types
- AI summarizes documentation → human makes final decisions

---

### Principle 7: Architecture Must Support Evolution ✅

**Evidence**:
- Vertical Slice structure scales to Phase 1+ features (notifications, partner selection)
- Lazy loading enables adding large feature modules without bloating initial bundle
- OpenAPI client auto-updates when backend schema changes
- Material Design system supports future theming/branding

**Extensibility**:
- Add new feature: Create `src/app/features/new-feature/`
- Add new API endpoint: Re-run `npm run api:generate`
- Add new route: Update `app.routes.ts`

---

## Section 10: Summary & Next Steps

### Document Purpose (Reaffirmed)
This specification provides the **complete technical blueprint** for implementing Tasks T071-T075. It resolves all architecture decisions, defines implementation patterns, and serves as the single source of truth for frontend development.

### 10.2 Evolution Decision Matrix

**Purpose**: Define objective thresholds for when to introduce architectural complexity. Prevents premature optimization while ensuring timely adoption of necessary patterns.

| Current State | Threshold Trigger | Evolution Action | Complexity Cost | When to Decide |
|---------------|-------------------|------------------|-----------------|----------------|
| **State Management**: Signal-based services | • 5+ features sharing state<br>• Real-time collaboration needed<br>• Undo/redo requirement | Introduce **NgRx SignalStore** (feature-scoped, not global) | MEDIUM (actions, reducers, effects per feature) | Phase 1 virtual incubator |
| **Routing**: Eager loading all routes | • 5+ routes<br>• Bundle size > 1MB<br>• Initial load > 3s (FCP) | Enable **lazy loading** for secondary features (keep auth eager) | LOW (route-level code splitting) | Phase 1 (bids, partner selection pages) |
| **Forms**: Signal-based only | • 3+ forms with dynamic arrays<br>• Complex cross-field validation<br>• Multi-step wizards | Standardize **Reactive Forms** as default (Signal forms for simple cases) | LOW (learning curve, validation patterns) | Phase 1 innovation submission form |
| **Monorepo**: Single Angular app | • 2+ distinct applications needed (e.g., admin portal, investor dashboard)<br>• Multiple teams contributing | Introduce **Nx workspace** with library slicing | HIGH (migration effort, CI/CD changes, learning curve) | Phase 2+ (never for solo dev) |
| **PWA**: No offline support | • Mobile usage > 40%<br>• Unreliable network environments<br>• User requests offline access | Add **service worker** + IndexedDB caching | MEDIUM (cache strategies, sync logic, testing complexity) | Phase 2+ (user-driven) |
| **SSR**: Client-side rendering only | • SEO critical (public innovation marketplace)<br>• Performance budget < 1s FCP<br>• Social media previews needed | Enable **Angular Universal** (server-side rendering) | HIGH (hosting changes, hydration issues, caching complexity) | Phase 2+ (public launch) |
| **Testing**: Jest + Playwright | • Mutation score < 70%<br>• Regression bugs in releases | Add **Stryker.NET** mutation testing (like backend) | LOW (CI pipeline time +30%) | Phase 1 (after core features stable) |
| **API Client**: OpenAPI-generated | • Breaking changes frequent<br>• Contract drift detected | Add **CI contract validation** (fail build on schema mismatch) | LOW (CI step, version control) | Phase 1 (implemented in T074) |
| **Error Handling**: HTTP interceptor only | • Complex error recovery workflows<br>• Queue failed requests for retry | Introduce **global error boundary** + retry queue | MEDIUM (state management, UX design) | Phase 1+ (user-driven) |
| **Logging**: console.error() only | • Production debugging needed<br>• User-reported bugs hard to reproduce | Integrate **Application Insights SDK** (client-side telemetry) | MEDIUM (SDK bundle size, correlation logic) | Phase 1 (before production launch) |

**Decision Process**:
1. **Threshold Met**: Verify trigger condition objectively (e.g., count routes, measure bundle size)
2. **Complexity Assessment**: Estimate implementation + maintenance cost
3. **Alternative Analysis**: Can simpler solution meet need? (e.g., route guards vs lazy loading)
4. **Document Decision**: Update this matrix with actual trigger data and chosen approach
5. **Plan Migration**: Create task in tasks.md with acceptance criteria

**Anti-Patterns to Avoid**:
- ❌ "We might need it later" (implement only when threshold met)
- ❌ "Best practice says so" (evaluate against Phase 0 scope and timeline)
- ❌ "It's trendy" (technology choices must solve actual problems)
- ❌ "Just in case" (defer until data proves necessity)

**Example Decision Log** (Phase 1):
```markdown
**Decision**: Introduce lazy loading for innovation detail route
**Trigger**: Bundle size reached 1.2MB after adding bid submission feature
**Data**: Initial load time increased to 3.5s on 3G network (threshold: 3s)
**Alternatives Considered**: Code splitting, tree shaking (already enabled), CDN caching (already enabled)
**Chosen Approach**: Lazy load `/innovations/:id` route (estimated 400KB reduction)
**Outcome**: Bundle reduced to 800KB, load time improved to 2.1s
```

---

### Approval Gates
**Before T071 Implementation**:
- [ ] Product Owner reviews Section 8 (Tasks Guide) for scope alignment
- [ ] Tech Lead reviews Section 2 (Architecture Decisions) for technical soundness
- [ ] Security Lead reviews Section 2, Decision A6 (JWT Storage) for risk acceptance
- [ ] QA Lead reviews Section 5 (Testing Strategy) for coverage adequacy

**Document Status**: PENDING APPROVAL → Once approved, freeze this document (like spec.md)

### T071 Kickoff Checklist
1. Read this document in full (45-60 minutes)
2. Install prerequisites: Node.js 18.19.0+, Angular CLI 19
3. Scaffold Angular app (Section 8, T071 guide)
4. Copy configuration files (Section 3.2)
5. Generate API client: `npm run api:generate`
6. Verify dev server runs: `npm start`
7. Write first test (T072 login component, TDD workflow)

### Success Criteria (Phase 0 Complete)
- ✅ All T071-T075 tasks checked off in tasks.md
- ✅ 80%+ unit test coverage (verified by `npm run test:coverage`)
- ✅ Playwright E2E test passes (J1 full journey)
- ✅ Angular app deployed to Azure Static Web Apps
- ✅ Backend + frontend integration validated (login → view innovation)

---

**END OF SPECIFICATION**

**Document Metadata**:
- **Lines**: 1,150+
- **Sections**: 10
- **Code Samples**: 25+
- **Configuration Files**: 8 complete examples
- **Constitutional Validation**: All 7 principles verified
- **Relationship to Backend**: Mirrors plan.md structure, aligns with spec.md journeys

**Change Control**: Modifications to this document require Product Owner approval after initial freeze.
