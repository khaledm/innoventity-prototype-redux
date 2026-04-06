# Registration UI Component Contracts

**Feature**: Registration User Interface (Phase 1+)
**Date**: April 6, 2026
**Purpose**: Component public interface contracts

---

## Overview

This directory contains TypeScript interface contracts for Registration UI components. These contracts define the public APIs, inputs, outputs, and interfaces that components expose.

---

## RegisterComponent Contract

**File**: `src/app/features/registration/register/register.component.ts`

### Public Interface

```typescript
export class RegisterComponent implements OnInit, OnDestroy {
  // Public Properties (Template Access)
  registrationForm: FormGroup<RegistrationFormModel>;
  isSubmitting: boolean;
  globalError: string | null;
  passwordStrength: PasswordStrength | null;
  passwordStrengthIndicator: PasswordStrengthIndicator | null;
  hide Password: boolean;
  hideConfirmPassword: boolean;
  actorTypeOptions: ActorTypeOption[];

  // Public Methods
  onSubmit(): void;
  togglePasswordVisibility(): void;
  toggleConfirmPasswordVisibility(): void;
}
```

### Inputs/Outputs

**None** (standalone component, no @Input/@Output needed)

### Form Model

```typescript
interface RegistrationFormModel {
  email: string;
  password: string;
  confirmPassword: string;
  actorType: ActorType;
  fullName: string;
  organizationName: string | null;
}
```

### Dependencies (Injected)

```typescript
constructor(
  private fb: FormBuilder,
  private router: Router,
  private registrationService: RegistrationService,
  private passwordStrengthService: PasswordStrengthService,
  private destroyRef: DestroyRef
) {}
```

### Template Contract

**Selector**: `<app-register></app-register>`

**Exported Form Controls** (for template access):
- `email` (FormControl<string>)
- `password` (FormControl<string>)
- `confirmPassword` (FormControl<string>)
- `actorType` (FormControl<ActorType>)
- `fullName` (FormControl<string>)
- `organizationName` (FormControl<string | null>)

**Template Bindings**:
- `[formGroup]="registrationForm"`
- `(ngSubmit)="onSubmit()"`
- `[disabled]="!registrationForm.valid || isSubmitting"`
- `*ngIf="globalError"` (error banner)
- `*ngFor="let option of actorTypeOptions"` (dropdown)

---

## PendingActivationComponent Contract

**File**: `src/app/features/registration/pending-activation/pending-activation.component.ts`

### Public Interface

```typescript
export class PendingActivationComponent implements OnInit {
  // Public Properties (Template Access)
  email: string | null;

  // Public Methods
  navigateToLogin(): void;
}
```

### Inputs/Outputs

**None** (state passed via router state)

### Dependencies (Injected)

```typescript
constructor(
  private router: Router
) {}
```

### Template Contract

**Selector**: `<app-pending-activation></app-pending-activation>`

**Template Bindings**:
- `{{ email }}` (display registered email)
- `(click)="navigateToLogin()"` (button click)

### Router State Dependency

**Expected State**:
```typescript
interface PendingActivationState {
  email: string;
}
```

**Access**:
```typescript
const navigation = this.router.getCurrentNavigation();
const state = navigation?.extras.state as PendingActivationState | undefined;
this.email = state?.email || null;
```

---

## ActivateComponent Contract

**File**: `src/app/features/registration/activate/activate.component.ts`

### Public Interface

```typescript
export class ActivateComponent implements OnInit {
  // Public Properties (Template Access)
  token: string | null;
  isActivating: boolean;
  activationSuccess: boolean;
  activationError: string | null;
  networkError: string | null;

  // Public Methods
  retry(): void;
  navigateToLogin(): void;
}
```

### Inputs/Outputs

**None** (token from query parameter)

### Dependencies (Injected)

```typescript
constructor(
  private route: ActivatedRoute,
  private router: Router,
  private activationService: ActivationService
) {}
```

### Template Contract

**Selector**: `<app-activate></app-activate>`

**Template Bindings**:
- `*ngIf="isActivating"` (loading spinner)
- `*ngIf="activationSuccess"` (success message)
- `*ngIf="activationError"` (error message)
- `*ngIf="networkError"` (network error + retry)
- `(click)="retry()"` (retry button)
- `(click)="navigateToLogin()"` (continue button)

### Query Parameter Dependency

**Expected Parameter**: `?token=abc123xyz...`

**Access**:
```typescript
this.route.queryParamMap.subscribe(params => {
  this.token = params.get('token');
});
```

---

## RegistrationService Contract

**File**: `src/app/features/registration/services/registration.service.ts`

### Public Interface

```typescript
@Injectable({ providedIn: 'root' })
export class RegistrationService {
  // Public Methods
  register(data: RegistrationFormModel): Observable<RegistrationResponse>;
}
```

### Method Signatures

**register()**:
```typescript
register(data: RegistrationFormModel): Observable<RegistrationResponse>
```

**Parameters**:
- `data`: RegistrationFormModel (email, password, actorType, fullName, organizationName)

**Returns**: Observable<RegistrationResponse>
- Success (201 Created): `{ message: string }`
- Error (400): ValidationErrorResponse
- Error (409): DuplicateEmailErrorResponse
- Error (500): Server error

**HTTP Call**:
- Method: POST
- URL: `/api/auth/register`
- Body: RegistrationFormModel (without confirmPassword)

---

## ActivationService Contract

**File**: `src/app/features/registration/services/activation.service.ts`

### Public Interface

```typescript
@Injectable({ providedIn: 'root' })
export class ActivationService {
  // Public Methods
  activate(request: ActivationRequest): Observable<ActivationResponse>;
}
```

### Method Signatures

**activate()**:
```typescript
activate(request: ActivationRequest): Observable<ActivationResponse>
```

**Parameters**:
- `request`: ActivationRequest `{ token: string }`

**Returns**: Observable<ActivationResponse>
- Success (200 OK): `{ message: string }`
- Error (400): ActivationErrorResponse (invalid token)
- Error (404): Token not found

**HTTP Call**:
- Method: POST
- URL: `/api/auth/activate`
- Body: `{ token: string }`

---

## PasswordStrengthService Contract

**File**: `src/app/features/registration/services/password-strength.service.ts`

### Public Interface

```typescript
@Injectable({ providedIn: 'root' })
export class PasswordStrengthService {
  // Public Methods
  calculateStrength(password: string): PasswordStrength;
  getStrengthIndicator(strength: PasswordStrength): PasswordStrengthIndicator;
  getStrengthColor(strength: PasswordStrength): 'warn' | 'accent' | 'primary';
  getStrengthPercentage(strength: PasswordStrength): number;
  getStrengthLabel(strength: PasswordStrength): string;
}
```

### Method Signatures

**calculateStrength()**:
```typescript
calculateStrength(password: string): PasswordStrength
```

**Parameters**:
- `password`: string (user-entered password)

**Returns**: `'weak' | 'medium' | 'strong'`

**Logic**:
- Weak: 8-11 characters (meets minimum requirements)
- Medium: 12-15 characters (meets minimum requirements)
- Strong: 16+ characters (meets minimum requirements)

---

**getStrengthIndicator()**:
```typescript
getStrengthIndicator(strength: PasswordStrength): PasswordStrengthIndicator
```

**Parameters**:
- `strength`: PasswordStrength

**Returns**: PasswordStrengthIndicator
```typescript
{
  strength: PasswordStrength;
  label: string; // "Weak", "Medium", "Strong"
  color: 'warn' | 'accent' | 'primary';
  percentage: number; // 33, 66, 100
}
```

---

**getStrengthColor()**:
```typescript
getStrengthColor(strength: PasswordStrength): 'warn' | 'accent' | 'primary'
```

**Returns**: Material theme color for progress bar

---

**getStrengthPercentage()**:
```typescript
getStrengthPercentage(strength: PasswordStrength): number
```

**Returns**: Progress bar percentage (33 | 66 | 100)

---

**getStrengthLabel()**:
```typescript
getStrengthLabel(strength: PasswordStrength): string
```

**Returns**: Human-readable label ("Weak", "Medium", "Strong")

---

## Password Validators Contract

**File**: `src/app/shared/validators/password-validators.ts`

### Exported Functions

**passwordStrengthValidator()**:
```typescript
export function passwordStrengthValidator(): ValidatorFn
```

**Returns**: ValidatorFn that validates password requirements

**Error Object** (when invalid):
```typescript
{
  passwordStrength: {
    hasMinLength: boolean;
    hasUppercase: boolean;
    hasLowercase: boolean;
    hasDigit: boolean;
    hasSpecial: boolean;
  }
}
```

**Valid State**: Returns `null`

---

**passwordMatchValidator()**:
```typescript
export function passwordMatchValidator(
  passwordKey: string,
  confirmPasswordKey: string
): ValidatorFn
```

**Parameters**:
- `passwordKey`: FormControl key for password field (e.g., 'password')
- `confirmPasswordKey`: FormControl key for confirm password field (e.g., 'confirmPassword')

**Returns**: ValidatorFn (applied to FormGroup, not individual controls)

**Error Object** (when passwords don't match):
```typescript
{
  passwordMismatch: true
}
```

**Valid State**: Returns `null`

---

## Registration Guard Contract

**File**: `src/app/guards/registration.guard.ts`

### Exported Function

**registrationGuard**:
```typescript
export const registrationGuard: CanActivateFn = (route, state) => {
  const router = inject(Router);
  const navigation = router.getCurrentNavigation();

  // Check if navigation state contains email
  if (navigation?.extras.state?.['email']) {
    return true; // Allow navigation
  }

  // Redirect to register if no email in state
  router.navigate(['/register']);
  return false; // Block navigation
};
```

**Purpose**: Prevent direct access to `/register/pending-activation` without registration flow

**Return**: `boolean | UrlTree`
- `true`: Allow navigation
- `false`: Block navigation (redirect handled in guard)

---

## Route Configuration Contract

**File**: `src/app/app.routes.ts`

### Registration Routes

```typescript
export const routes: Routes = [
  {
    path: 'register',
    component: RegisterComponent,
    title: 'Register - Innoventity'
  },
  {
    path: 'register/pending-activation',
    component: PendingActivationComponent,
    title: 'Activation Pending - Innoventity',
    canActivate: [registrationGuard]
  },
  {
    path: 'activate',
    component: ActivateComponent,
    title: 'Activate Account - Innoventity'
  }
];
```

**Navigation Paths**:
- Register: `/register`
- Pending Activation: `/register/pending-activation` (guarded)
- Activate: `/activate?token={token}` (no guard, allows email link access)

---

## Model Contracts

**File**: `src/app/shared/models/registration.model.ts`

### Exported Interfaces

**RegistrationFormModel**:
```typescript
export interface RegistrationFormModel {
  email: string;
  password: string;
  confirmPassword: string;
  actorType: ActorType;
  fullName: string;
  organizationName: string | null;
}
```

---

**RegistrationResponse**:
```typescript
export interface RegistrationResponse {
  message: string;
}
```

---

**ActivationRequest**:
```typescript
export interface ActivationRequest {
  token: string;
}
```

---

**ActivationResponse**:
```typescript
export interface ActivationResponse {
  message: string;
}
```

---

**PendingActivationState**:
```typescript
export interface PendingActivationState {
  email: string;
}
```

---

**PasswordStrength**:
```typescript
export type PasswordStrength = 'weak' | 'medium' | 'strong';
```

---

**PasswordStrengthIndicator**:
```typescript
export interface PasswordStrengthIndicator {
  strength: PasswordStrength;
  label: string;
  color: 'warn' | 'accent' | 'primary';
  percentage: number;
}
```

---

**ActorType**:
```typescript
export enum ActorType {
  IdeaGenerator = 'IdeaGenerator',
  RDOrganization = 'RDOrganization',
  ManufacturingOrganization = 'ManufacturingOrganization',
  SalesMarketingOrganization = 'SalesMarketingOrganization',
  InvestorOrganization = 'InvestorOrganization'
}
```

---

**ActorTypeOption**:
```typescript
export interface ActorTypeOption {
  value: ActorType;
  label: string;
  description: string;
}
```

---

## Error Response Contracts

**ValidationErrorResponse** (HTTP 400):
```typescript
export interface ValidationErrorResponse {
  type: string;
  title: string;
  status: 400;
  errors: {
    [fieldName: string]: string[];
  };
}
```

---

**DuplicateEmailErrorResponse** (HTTP 409):
```typescript
export interface DuplicateEmailErrorResponse {
  message: string;
}
```

---

**ActivationErrorResponse** (HTTP 400/404):
```typescript
export interface ActivationErrorResponse {
  message: string;
}
```

---

## Testing Contracts

### Component Test Setup

**RegisterComponent Test**:
```typescript
describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let fixture: ComponentFixture<RegisterComponent>;
  let registrationService: jasmine.SpyObj<RegistrationService>;

  beforeEach(() => {
    const spy = jasmine.createSpyObj('RegistrationService', ['register']);

    TestBed.configureTestingModule({
      imports: [RegisterComponent, NoopAnimationsModule],
      providers: [{ provide: RegistrationService, useValue: spy }]
    });

    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
    registrationService = TestBed.inject(RegistrationService) as jasmine.SpyObj<RegistrationService>;
  });
});
```

---

### Service Test Setup

**RegistrationService Test**:
```typescript
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
    httpMock.verify();
  });
});
```

---

## Integration Contracts

### RegistrationService → Backend API

**POST /api/auth/register**:

**Request**:
```json
{
  "email": "user@example.com",
  "password": "TestPassword123!",
  "actorType": "IdeaGenerator",
  "fullName": "John Doe",
  "organizationName": null
}
```

**Success Response** (201 Created):
```json
{
  "message": "Registration successful. Check your email for activation link."
}
```

**Error Response** (400 Bad Request):
```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Email": ["Invalid email format"],
    "Password": ["Password does not meet requirements"]
  }
}
```

**Error Response** (409 Conflict):
```json
{
  "message": "Email already registered for this actor type."
}
```

---

### ActivationService → Backend API

**POST /api/auth/activate**:

**Request**:
```json
{
  "token": "abc123xyz..."
}
```

**Success Response** (200 OK):
```json
{
  "message": "Account activated successfully! You can now log in."
}
```

**Error Response** (400 Bad Request):
```json
{
  "message": "Invalid or expired activation token."
}
```

**Error Response** (404 Not Found):
```json
{
  "message": "Activation token not found."
}
```

---

## Contract Compliance Checklist

### Component Implementation

- [ ] RegisterComponent implements all public methods
- [ ] RegisterComponent exposes all public properties for template
- [ ] PendingActivationComponent reads router state correctly
- [ ] ActivateComponent reads query parameter correctly

### Service Implementation

- [ ] RegistrationService.register() returns Observable<RegistrationResponse>
- [ ] ActivationService.activate() returns Observable<ActivationResponse>
- [ ] PasswordStrengthService implements all calculation methods

### Validator Implementation

- [ ] passwordStrengthValidator() returns correct error object structure
- [ ] passwordMatchValidator() validates cross-field correctly

### Routing Implementation

- [ ] All routes configured in app.routes.ts
- [ ] registrationGuard protects /register/pending-activation
- [ ] /activate route allows direct access (no guard)

### Model Implementation

- [ ] All TypeScript interfaces exported from registration.model.ts
- [ ] ActorType enum matches backend values

### Testing Implementation

- [ ] All components have .spec.ts files
- [ ] All services have .spec.ts files
- [ ] All validators have .spec.ts files
- [ ] E2E tests cover all 10 scenarios from spec.md

---

**Contracts Complete** - All component public interfaces documented. Ready for implementation and testing.
