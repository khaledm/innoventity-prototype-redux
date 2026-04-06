# Data Model: Registration User Interface

**Feature**: Registration User Interface (Phase 1+)
**Date**: April 6, 2026
**Spec**: [spec.md Section 8](./spec.md#8-registration-user-interface-phase-1)

---

## Overview

This document defines the data models, TypeScript interfaces, validation rules, and state transitions for the browser-based registration workflow. All models are **frontend-only** (no database schema changes—backend APIs already implemented in Phase 0).

---

## 1. TypeScript Interfaces

### RegistrationFormModel

**Purpose**: Type-safe representation of the registration form data structure.

**Interface**:

```typescript
export interface RegistrationFormModel {
  email: string;
  password: string;
  confirmPassword: string;
  actorType: ActorType;
  fullName: string;
  organizationName: string | null; // null for Idea Generator
}
```

**Usage**:

- Component: `FormGroup<RegistrationFormModel>`
- Service: `register(data: RegistrationFormModel): Observable<RegistrationResponse>`

---

### ActorType (Enum)

**Purpose**: Strongly-typed actor type selection.

**Enum**:

```typescript
export enum ActorType {
  IdeaGenerator = 'IdeaGenerator',
  RDOrganization = 'RDOrganization',
  ManufacturingOrganization = 'ManufacturingOrganization',
  SalesMarketingOrganization = 'SalesMarketingOrganization',
  InvestorOrganization = 'InvestorOrganization'
}
```

**Dropdown Options**:

```typescript
export const ACTOR_TYPE_OPTIONS = [
  { value: ActorType.IdeaGenerator, label: 'Idea Generator', description: 'Individual with innovative ideas' },
  { value: ActorType.RDOrganization, label: 'R&D Organization', description: 'Research and development partner' },
  { value: ActorType.ManufacturingOrganization, label: 'Manufacturing Organization', description: 'Production partner' },
  { value: ActorType.SalesMarketingOrganization, label: 'Sales & Marketing Organization', description: 'Market reach partner' },
  { value: ActorType.InvestorOrganization, label: 'Investor Organization', description: 'Funding provider' }
];
```

**Usage**:

```html
<mat-select formControlName="actorType">
  <mat-option *ngFor="let option of actorTypeOptions" [value]="option.value">
    {{ option.label }}
    <span class="option-description">{{ option.description }}</span>
  </mat-option>
</mat-select>
```

---

### RegistrationResponse (API Response)

**Purpose**: Backend response after successful registration.

**Interface**:

```typescript
export interface RegistrationResponse {
  message: string; // "Registration successful. Check your email for activation link."
}
```

**HTTP Status**: 201 Created

---

### ActivationRequest

**Purpose**: Activation component data structure.

**Interface**:

```typescript
export interface ActivationRequest {
  token: string; // From query parameter ?token=...
}
```

**Usage**: `activateAccount(request: ActivationRequest): Observable<ActivationResponse>`

---

### ActivationResponse

**Purpose**: Backend response after activation.

**Interface**:

```typescript
export interface ActivationResponse {
  message: string; // "Account activated successfully! You can now log in."
}
```

**HTTP Status**: 200 OK

---

### PendingActivationState (Router State)

**Purpose**: Transient state passed from RegisterComponent to PendingActivationComponent.

**Interface**:

```typescript
export interface PendingActivationState {
  email: string; // User's registered email address
}
```

**Usage**:

```typescript
// RegisterComponent
this.router.navigate(['/register/pending-activation'], {
  state: { email: this.registrationForm.value.email } as PendingActivationState
});

// PendingActivationComponent
ngOnInit() {
  const navigation = this.router.getCurrentNavigation();
  const state = navigation?.extras.state as PendingActivationState | undefined;
  this.email = state?.email || null;
}
```

---

## 2. Validation Rules

### Email Field

**Validators**:

- `Validators.required`
- `Validators.email` (RFC 5322 subset)

**Constraints**:

- **Format**: Standard email format (validated by browser + Angular)
- **Max Length**: 254 characters (RFC 5321 limit)

**Error Messages**:

```typescript
<mat-error *ngIf="email.hasError('required')">Email is required</mat-error>
<mat-error *ngIf="email.hasError('email')">Invalid email format</mat-error>
<mat-error *ngIf="email.hasError('emailExists')">
  {{ email.getError('emailExists') }} <!-- From API 409 response -->
</mat-error>
```

---

### Password Field

**Validators**:

- `Validators.required`
- `passwordStrengthValidator()` (custom)

**Constraints** (FR8.1):

- **Minimum Length**: 8 characters
- **Uppercase**: At least one uppercase letter (A-Z)
- **Lowercase**: At least one lowercase letter (a-z)
- **Digit**: At least one digit (0-9)
- **Special Character**: At least one special character (!@#$%^&*()_+-=[]{}|;:',.<>?/)

**Custom Validator**:

```typescript
export function passwordStrengthValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value || '';

    const hasMinLength = value.length >= 8;
    const hasUppercase = /[A-Z]/.test(value);
    const hasLowercase = /[a-z]/.test(value);
    const hasDigit = /\d/.test(value);
    const hasSpecial = /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(value);

    const valid = hasMinLength && hasUppercase && hasLowercase && hasDigit && hasSpecial;

    return valid ? null : {
      passwordStrength: {
        hasMinLength,
        hasUppercase,
        hasLowercase,
        hasDigit,
        hasSpecial
      }
    };
  };
}
```

**Error Messages**:

```html
<mat-error *ngIf="password.hasError('required')">Password is required</mat-error>
<mat-error *ngIf="password.hasError('passwordStrength')">
  Password must include:
  <ul>
    <li *ngIf="!password.getError('passwordStrength').hasMinLength">At least 8 characters</li>
    <li *ngIf="!password.getError('passwordStrength').hasUppercase">One uppercase letter</li>
    <li *ngIf="!password.getError('passwordStrength').hasLowercase">One lowercase letter</li>
    <li *ngIf="!password.getError('passwordStrength').hasDigit">One digit</li>
    <li *ngIf="!password.getError('passwordStrength').hasSpecial">One special character</li>
  </ul>
</mat-error>
```

---

### Confirm Password Field

**Validators**:

- `Validators.required`
- `passwordMatchValidator()` (form-level validator)

**Cross-Field Validator**:

```typescript
export function passwordMatchValidator(passwordKey: string, confirmPasswordKey: string): ValidatorFn {
  return (formGroup: AbstractControl): ValidationErrors | null => {
    const password = formGroup.get(passwordKey);
    const confirmPassword = formGroup.get(confirmPasswordKey);

    if (!password || !confirmPassword) {
      return null;
    }

    if (confirmPassword.value === '') {
      return null; // Don't show error until user types
    }

    return password.value === confirmPassword.value
      ? null
      : { passwordMismatch: true };
  };
}
```

**Error Messages**:

```html
<mat-error *ngIf="confirmPassword.hasError('required')">Confirm password is required</mat-error>
<mat-error *ngIf="registrationForm.hasError('passwordMismatch')">Passwords do not match</mat-error>
```

---

### Actor Type Field

**Validators**:

- `Validators.required`

**Constraints**:

- **Options**: Must be one of 5 ActorType enum values
- **Default**: No default (user must select)

**Error Messages**:

```html
<mat-error *ngIf="actorType.hasError('required')">Please select your role</mat-error>
```

---

### Full Name Field

**Validators**:

- `Validators.required`
- `Validators.minLength(2)`
- `Validators.maxLength(100)`

**Constraints**:

- **Min Length**: 2 characters (prevents single-letter names)
- **Max Length**: 100 characters (prevents excessively long names)
- **Format**: Any Unicode characters (supports international names)

**Error Messages**:

```html
<mat-error *ngIf="fullName.hasError('required')">Full name is required</mat-error>
<mat-error *ngIf="fullName.hasError('minlength')">Full name must be at least 2 characters</mat-error>
<mat-error *ngIf="fullName.hasError('maxlength')">Full name cannot exceed 100 characters</mat-error>
```

---

### Organization Name Field

**Validators** (Dynamic):

- **Idea Generator**: `Validators.minLength(2)`, `Validators.maxLength(200)` (optional)
- **Other Actor Types**: `Validators.required`, `Validators.minLength(2)`, `Validators.maxLength(200)`

**Constraints**:

- **Min Length**: 2 characters
- **Max Length**: 200 characters
- **Conditional Required**: Required for all actor types EXCEPT Idea Generator (per Clarification Q3)

**Dynamic Validator Update**:

```typescript
ngOnInit() {
  this.registrationForm.get('actorType')!.valueChanges.subscribe(actorType => {
    const orgNameControl = this.registrationForm.get('organizationName')!;

    if (actorType === ActorType.IdeaGenerator) {
      orgNameControl.clearValidators();
      orgNameControl.setValidators([Validators.minLength(2), Validators.maxLength(200)]);
    } else {
      orgNameControl.setValidators([
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(200)
      ]);
    }

    orgNameControl.updateValueAndValidity();
  });
}
```

**Error Messages**:

```html
<mat-label>
  Organization Name
  <span *ngIf="actorType.value === 'IdeaGenerator'" class="optional-label">(Optional)</span>
</mat-label>
<input matInput formControlName="organizationName">
<mat-error *ngIf="organizationName.hasError('required')">Organization name is required</mat-error>
<mat-error *ngIf="organizationName.hasError('minlength')">Organization name must be at least 2 characters</mat-error>
<mat-error *ngIf="organizationName.hasError('maxlength')">Organization name cannot exceed 200 characters</mat-error>
```

**Clarification Applied**: Field ALWAYS VISIBLE, marked "(Optional)" for Idea Generator (not hidden dynamically).

---

## 3. Password Strength Indicator Model

### PasswordStrength (Type)

**Purpose**: Represents password strength level.

**Type**:

```typescript
export type PasswordStrength = 'weak' | 'medium' | 'strong';
```

**Calculation Logic**:

```typescript
calculateStrength(password: string): PasswordStrength {
  // Must meet all requirements to be evaluated (8+ chars, uppercase, lowercase, digit, special)
  if (!this.meetsRequirements(password)) {
    return 'weak';
  }

  if (password.length >= 16) return 'strong';
  if (password.length >= 12) return 'medium';
  return 'weak'; // 8-11 characters
}
```

---

### PasswordStrengthIndicator (Display Model)

**Purpose**: Mapping strength to UI display properties.

**Interface**:

```typescript
export interface PasswordStrengthIndicator {
  strength: PasswordStrength;
  label: string; // "Weak", "Medium", "Strong"
  color: 'warn' | 'accent' | 'primary'; // Material theme colors
  percentage: number; // 33, 66, 100 (for progress bar)
}
```

**Mapping**:

```typescript
getStrengthIndicator(strength: PasswordStrength): PasswordStrengthIndicator {
  switch (strength) {
    case 'weak':
      return { strength: 'weak', label: 'Weak', color: 'warn', percentage: 33 };
    case 'medium':
      return { strength: 'medium', label: 'Medium', color: 'accent', percentage: 66 };
    case 'strong':
      return { strength: 'strong', label: 'Strong', color: 'primary', percentage: 100 };
  }
}
```

**Template Usage**:

```html
<mat-progress-bar
  mode="determinate"
  [value]="strengthIndicator.percentage"
  [color]="strengthIndicator.color">
</mat-progress-bar>
<span [class]="'strength-label strength-' + strengthIndicator.strength">
  {{ strengthIndicator.label }}
</span>
```

---

## 4. API Error Response Models

### ValidationErrorResponse (HTTP 400)

**Purpose**: Map backend validation errors to form field errors.

**Interface**:

```typescript
export interface ValidationErrorResponse {
  type: string; // "https://tools.ietf.org/html/rfc7807"
  title: string; // "One or more validation errors occurred."
  status: 400;
  errors: {
    [fieldName: string]: string[]; // e.g., { "Email": ["Invalid format"], "Password": ["Too weak"] }
  };
}
```

**Mapping Logic**:

```typescript
private mapApiErrorsToForm(apiErrors: ValidationErrorResponse) {
  Object.keys(apiErrors.errors || {}).forEach(field => {
    const control = this.registrationForm.get(field.toLowerCase());
    if (control) {
      control.setErrors({ serverError: apiErrors.errors[field][0] });
    }
  });
}
```

---

### DuplicateEmailErrorResponse (HTTP 409)

**Purpose**: Handle duplicate email error.

**Interface**:

```typescript
export interface DuplicateEmailErrorResponse {
  message: string; // "Email already registered for this actor type."
}
```

**Handling**:

```typescript
if (httpError.status === 409) {
  this.registrationForm.get('email')!.setErrors({
    emailExists: 'Email already registered for this actor type. Try logging in.'
  });
}
```

---

### ActivationErrorResponse (HTTP 400 or 404)

**Purpose**: Handle activation errors (invalid/expired token).

**Interface**:

```typescript
export interface ActivationErrorResponse {
  message: string; // "Invalid or expired activation token."
}
```

**Display**:

```html
<div *ngIf="activationError" class="error-message">
  <mat-icon>error</mat-icon>
  <p>{{ activationError }}</p>
  <button mat-raised-button color="primary" (click)="retry()">Retry</button>
</div>
```

---

## 5. Component State Models

### RegisterComponent State

**Properties**:

```typescript
export class RegisterComponent {
  registrationForm: FormGroup<RegistrationFormModel>;
  isSubmitting: boolean = false;
  globalError: string | null = null;
  passwordStrength: PasswordStrength | null = null;
  passwordStrengthIndicator: PasswordStrengthIndicator | null = null;
  hidePassword: boolean = true;
  hideConfirmPassword: boolean = true;
  actorTypeOptions = ACTOR_TYPE_OPTIONS;
}
```

**State Transitions**:

1. **Initial**: Form pristine, isSubmitting = false, no errors
2. **User Input**: Form dirty/touched, validation runs, password strength updates
3. **Submitting**: isSubmitting = true, submit button disabled, spinner visible
4. **Success**: Navigate to /register/pending-activation (clear form)
5. **Error**: isSubmitting = false, globalError or field errors set, form state retained

---

### PendingActivationComponent State

**Properties**:

```typescript
export class PendingActivationComponent {
  email: string | null = null; // From router state
}
```

**State Transitions**:

1. **Page Load**: Extract email from router state
2. **No Email**: Redirect to /register (guard logic)
3. **Email Present**: Display success message with email

---

### ActivateComponent State

**Properties**:

```typescript
export class ActivateComponent {
  token: string | null = null; // From query parameter
  isActivating: boolean = true;
  activationSuccess: boolean = false;
  activationError: string | null = null;
  networkError: string | null = null;
}
```

**State Transitions**:

1. **Page Load**: Extract token from query parameter
2. **No Token**: Show error "Missing activation token"
3. **Activating**: isActivating = true, call API
4. **Success**: isActivating = false, activationSuccess = true, show success message + "Continue to Login" button
5. **Error (Invalid Token)**: isActivating = false, activationError set, show retry option
6. **Error (Network)**: isActivating = false, networkError set, show retry button

**Clarification Applied**: No auto-redirect after successful activation; user must click "Continue to Login" button (manual navigation).

---

## 6. Form Submission Payload

### POST /api/auth/register

**Request Body** (maps from RegistrationFormModel):

```json
{
  "email": "user@example.com",
  "password": "TestPassword123!",
  "actorType": "IdeaGenerator",
  "fullName": "John Doe",
  "organizationName": null
}
```

**Notes**:

- `confirmPassword` is NOT sent to backend (client-side validation only)
- `organizationName` is `null` for Idea Generator (not empty string)

**Response** (201 Created):

```json
{
  "message": "Registration successful. Check your email for activation link."
}
```

---

### POST /api/auth/activate

**Request Body**:

```json
{
  "token": "abc123xyz..."
}
```

**Response** (200 OK):

```json
{
  "message": "Account activated successfully! You can now log in."
}
```

---

## 7. Data Flow Summary

### Registration Flow

```
User Input (RegisterComponent)
  ↓
RegistrationFormModel (validated)
  ↓
RegistrationService.register(data)
  ↓
HTTP POST /api/auth/register
  ↓
201 Created / 400 Validation / 409 Duplicate
  ↓
Success: Navigate to /register/pending-activation with router state { email }
Error: Set form errors or global error message
```

---

### Activation Flow

```
User Clicks Email Link (/activate?token=abc123)
  ↓
ActivateComponent extracts token from query parameter
  ↓
ActivationService.activate({ token })
  ↓
HTTP POST /api/auth/activate
  ↓
200 OK / 400 Invalid Token / 404 Not Found
  ↓
Success: Show success message + "Continue to Login" button
Error: Show error message + "Retry" button
```

---

## 8. Validation Error Matrix

| Field | Validation | Error Message |
|-------|------------|---------------|
| **Email** | Required | "Email is required" |
| **Email** | Format | "Invalid email format" |
| **Email** | Duplicate (API 409) | "Email already registered for this actor type. Try logging in." |
| **Password** | Required | "Password is required" |
| **Password** | Min length (8) | "Password must include: At least 8 characters" |
| **Password** | Uppercase | "Password must include: One uppercase letter" |
| **Password** | Lowercase | "Password must include: One lowercase letter" |
| **Password** | Digit | "Password must include: One digit" |
| **Password** | Special char | "Password must include: One special character" |
| **Confirm Password** | Required | "Confirm password is required" |
| **Confirm Password** | Match | "Passwords do not match" |
| **Actor Type** | Required | "Please select your role" |
| **Full Name** | Required | "Full name is required" |
| **Full Name** | Min length (2) | "Full name must be at least 2 characters" |
| **Full Name** | Max length (100) | "Full name cannot exceed 100 characters" |
| **Organization Name** | Required (non-Idea Generator) | "Organization name is required" |
| **Organization Name** | Min length (2) | "Organization name must be at least 2 characters" |
| **Organization Name** | Max length (200) | "Organization name cannot exceed 200 characters" |

---

## 9. Testing Data Fixtures

### Valid Registration Data

```typescript
export const VALID_REGISTRATION_DATA: RegistrationFormModel = {
  email: 'test@example.com',
  password: 'TestPassword123!',
  confirmPassword: 'TestPassword123!',
  actorType: ActorType.IdeaGenerator,
  fullName: 'Test User',
  organizationName: null
};
```

---

### Invalid Registration Data (Test Cases)

```typescript
export const INVALID_EMAIL = {
  ...VALID_REGISTRATION_DATA,
  email: 'invalid-email' // Missing @
};

export const WEAK_PASSWORD = {
  ...VALID_REGISTRATION_DATA,
  password: 'weak', // Too short, no uppercase, no digit, no special
  confirmPassword: 'weak'
};

export const PASSWORD_MISMATCH = {
  ...VALID_REGISTRATION_DATA,
  confirmPassword: 'DifferentPassword123!'
};

export const MISSING_ORG_NAME = {
  ...VALID_REGISTRATION_DATA,
  actorType: ActorType.RDOrganization, // Requires organization name
  organizationName: null // Missing
};
```

---

### API Response Mocks

```typescript
export const MOCK_SUCCESS_RESPONSE: RegistrationResponse = {
  message: 'Registration successful. Check your email for activation link.'
};

export const MOCK_DUPLICATE_ERROR: DuplicateEmailErrorResponse = {
  message: 'Email already registered for this actor type.'
};

export const MOCK_ACTIVATION_SUCCESS: ActivationResponse = {
  message: 'Account activated successfully! You can now log in.'
};

export const MOCK_INVALID_TOKEN_ERROR: ActivationErrorResponse = {
  message: 'Invalid or expired activation token.'
};
```

---

## Conclusion

**Data Model Complete**: All TypeScript interfaces, validation rules, error models, and state transitions documented. Application ready for implementation.

**Next Steps**: Generate quickstart.md (developer setup guide) and contracts/ (component interface contracts).
