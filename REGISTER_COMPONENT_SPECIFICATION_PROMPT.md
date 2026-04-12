# Registration Component Specification Prompt for /speckit.specify

## Context for AI Agent

This prompt is for the `/speckit.specify` command to add comprehensive registration UI requirements to the existing `specs/001-platform-core/spec.md` file. The registration API endpoints (POST /auth/register, POST /auth/activate) already exist and are fully implemented in the backend. Phase 0 deliberately excluded registration UI—users registered via API calls only. This specification adds the missing UI layer for Phase 1+.

---

## Feature Addition Request

**Feature Name**: Registration User Interface (Phase 1+ Enhancement)

**Feature Type**: Enhancement to existing authentication feature

**Target Spec File**: `specs/001-platform-core/spec.md`

**Target Phase**: Phase 1+ (NOT Phase 0)

**Integration Point**: Extends existing User Story 1 (User Registration) from spec.md

---

## Functional Requirements to Add

### New Section: FR8 - Registration User Interface (Phase 1+)

**Context**: Phase 0 implemented registration via API endpoints only (POST /auth/register, POST /auth/activate). Phase 1+ adds Angular UI components to provide browser-based registration workflow for manual user onboarding.

#### FR8.1: Registration Form Component

**WHO**: All prospective platform users (across all 5 actor types)

**WHAT**: A web-based registration form accessible via browser that collects required registration information and submits to the existing POST /auth/register endpoint.

**WHY**: Enable non-technical users to register without API tools (Postman/curl). Improve user acquisition by removing technical barriers. Support marketing campaigns with shareable registration URLs.

**Required Form Fields**:
1. **Email** (text input, required)
   - Validation: RFC 5322 email format
   - Real-time validation feedback (invalid format error)
   - Unique per ActorType (server-side validation via API)

2. **Password** (password input, required)
   - Validation: Minimum 8 characters, at least one uppercase, one lowercase, one digit, one special character
   - Real-time strength indicator (weak/medium/strong visual feedback)
   - Toggle visibility button (show/hide password)

3. **Confirm Password** (password input, required)
   - Validation: Must exactly match Password field
   - Real-time mismatch error feedback
   - Toggle visibility button (show/hide password)

4. **Actor Type** (dropdown/select, required)
   - Options: Idea Generator, R&D Organization, Manufacturing Company, Sales & Marketing Company, Investor
   - Default: No pre-selection (user must choose)
   - Help text explaining each actor type (tooltip or info icon)

5. **Full Name** (text input, required)
   - Validation: 2-100 characters, allows letters, spaces, hyphens, apostrophes
   - Used for display purposes across platform

6. **Organization Name** (text input, conditionally required)
   - Required for: R&D Organization, Manufacturing Company, Sales & Marketing Company, Investor
   - Optional for: Idea Generator
   - Validation: 2-200 characters when provided
   - Conditional display: Show/hide based on Actor Type selection

**Form Behavior**:
- All validation errors displayed inline below respective fields
- Submit button disabled until all required fields valid
- Loading spinner during API call (prevent duplicate submissions)
- Success: Redirect to activation pending page with instructions
- Error: Display API error messages inline (e.g., "Email already registered for this actor type")

**Acceptance Criteria**:
1. User can access registration form at `/register` route
2. Form validates all fields client-side before submission
3. Form submits to POST /auth/register with correct request body format
4. Successful registration displays activation instructions with user's email
5. Duplicate email error displays user-friendly message (not raw API error)
6. Password strength indicator updates in real-time as user types
7. Confirm Password field shows mismatch error immediately (on blur or real-time)
8. Organization Name field shows/hides based on Actor Type selection
9. Form is keyboard-accessible (tab navigation, Enter to submit)
10. Form works on mobile viewports (responsive design, 320px minimum width)

#### FR8.2: Activation Pending Page

**WHO**: Users who just completed registration

**WHAT**: Informational page displayed after successful registration explaining next steps (email activation).

**WHY**: Set user expectations, reduce support requests about "why can't I log in yet?"

**Required Content**:
1. Success icon/graphic
2. Heading: "Registration Successful!"
3. Body text:
   - "We've sent an activation link to **{user's email}**"
   - "Please check your inbox and click the link to activate your account"
   - "The activation link is valid for 24 hours"
4. Help text:
   - "Didn't receive the email? Check your spam folder"
   - Link to resend activation email (Phase 2+ feature, show "Coming Soon" for Phase 1)
5. Back to login link

**Acceptance Criteria**:
1. Page displays immediately after successful registration
2. User's email address shown in confirmation message
3. Page accessible at `/register/pending-activation` route
4. Cannot access page directly without registration flow (redirect to /register if accessed directly)
5. "Back to Login" link navigates to `/login`

#### FR8.3: Email Activation Link Handling

**WHO**: Users with pending activation who click email link

**WHAT**: Web page that consumes activation token from email link, calls POST /auth/activate, and handles success/error states.

**WHY**: Complete browser-based registration workflow without requiring users to manually call API endpoints.

**Required Functionality**:
1. Route: `/activate?token={activationToken}` or `/activate/{activationToken}`
2. Automatically extract token from URL on page load
3. Call POST /auth/activate with extracted token
4. Success state:
   - Display success message: "Account activated successfully!"
   - Auto-redirect to /login after 3 seconds with countdown timer
   - Manual "Go to Login" button (don't force waiting)
5. Error states:
   - Invalid token: "Activation link is invalid"
   - Expired token: "Activation link has expired. Please register again."
   - Already activated: "Account already activated. You can log in now."
   - Network error: Retry button + error message

**Acceptance Criteria**:
1. Activation page accessible via URL with token parameter
2. Page displays loading spinner while calling API
3. Successful activation shows success message + countdown
4. Auto-redirect to /login after 3 seconds (or immediate if user clicks button)
5. Invalid token shows appropriate error message
6. Expired token error includes link back to /register
7. Network errors allow retry without navigating away
8. Already-activated accounts can proceed to login (not an error state)

#### FR8.4: Integration with Existing Login Flow

**WHO**: New users and returning users

**WHAT**: Login page includes link to registration page; registration flow redirects to login after activation.

**WHY**: Provide clear navigation between authentication workflows.

**Required Changes to Login Page**:
1. Add "Don't have an account? Register here" link below login form
2. Link navigates to `/register`
3. Link styled consistently with existing UI

**Navigation Flow**:
```
/register → (submit) → /register/pending-activation → (email) → /activate?token=... → /login
```

**Acceptance Criteria**:
1. Login page displays registration link
2. Registration link is visible and accessible on mobile
3. Clicking registration link preserves any error messages on login page (don't clear unnecessarily)
4. Full workflow accessible via browser without API tools

---

## User Story to Add

### User Story 8: Browser-Based Registration (Phase 1+)

**WHO**: Prospective platform user (any actor type)

**WHAT**: Complete registration workflow using web browser without API tools

**WHY**: Remove technical barriers to user acquisition; enable marketing campaigns with direct registration links

**Workflow**:
1. User navigates to `/register` (via link from login page or direct URL)
2. User fills registration form:
   - Email: jane.doe@example.com
   - Password: SecurePass123!
   - Confirm Password: SecurePass123!
   - Actor Type: R&D Organization
   - Full Name: Jane Doe
   - Organization Name: Innovatech Labs
3. User clicks "Register" button
4. Form validates all fields client-side
5. Form submits POST /auth/register to backend API
6. Success: User sees activation pending page with confirmation
7. User receives activation email (via SendGrid—Phase 1+ feature)
8. User clicks activation link in email
9. Browser opens `/activate?token={activationToken}`
10. Page automatically calls POST /auth/activate
11. Success: User sees "Account activated!" message
12. User auto-redirected to `/login` after 3 seconds (or clicks button)
13. User logs in with email + password

**Acceptance Criteria**:
1. User can complete registration without Postman/curl
2. All form validation errors visible before submission
3. Duplicate email error handled gracefully (user-facing message)
4. Activation link works from email client
5. Expired token shows helpful error with re-registration path
6. Entire workflow accessible via keyboard navigation
7. Workflow functions on mobile browsers (iOS Safari, Android Chrome)
8. All pages styled consistently with existing login page (Material Design)

**Edge Cases**:
- User registers, never activates, tries to login → Error: "Account pending activation"
- User clicks activation link twice → Second click succeeds silently (idempotent)
- User registers with existing email + same ActorType → Error: "Email already registered for this actor type. Try logging in or use different email."
- User registers with existing email + different ActorType → Success (same email, different actor types allowed per R1.3)

---

## Non-Functional Requirements

### NFR1: Performance
- Registration form submission completes in <2 seconds (p95)
- Activation page token validation completes in <1 second (p95)
- Form validation feedback displays in <100ms (real-time feedback)

### NFR2: Accessibility
- All form fields have visible labels and ARIA attributes
- Error messages announced to screen readers
- Keyboard navigation functional (tab order logical)
- Color contrast meets WCAG 2.1 AA standards
- Form usable with screen magnification (200% zoom)

### NFR3: Security
- Password never logged or sent to analytics
- Activation token not persisted in browser storage
- HTTPS required for all registration pages (redirect HTTP → HTTPS)
- Password field uses autocomplete="new-password" attribute
- Email field uses autocomplete="email" attribute

### NFR4: Browser Compatibility
- Chrome 120+ (latest 2 versions)
- Firefox 121+ (latest 2 versions)
- Safari 17+ (latest 2 versions)
- Edge 120+ (latest 2 versions)
- Mobile: iOS Safari 17+, Android Chrome 120+

### NFR5: Responsive Design
- Form usable on viewport widths 320px - 2560px
- Touch targets minimum 44x44px on mobile
- Form fields stack vertically on mobile (<768px)
- No horizontal scrolling required

---

## Testing Requirements for /speckit.tasks

### Unit Tests (Component-Level)

**RegisterComponent Unit Tests**:
1. Should render all form fields with correct initial states
2. Should disable submit button when form invalid
3. Should enable submit button when all fields valid
4. Should show/hide Organization Name based on Actor Type selection
5. Should validate email format and show error message
6. Should validate password strength (weak/medium/strong)
7. Should show password mismatch error when passwords don't match
8. Should call AuthService.register() with correct data on submit
9. Should navigate to /register/pending-activation on successful registration
10. Should display API error message on registration failure (409 duplicate email)
11. Should prevent duplicate submissions (disable button during API call)
12. Should toggle password visibility when show/hide clicked

**ActivationComponent Unit Tests**:
1. Should extract activation token from URL query parameter
2. Should call AuthService.activate() on component initialization
3. Should display success message on successful activation
4. Should redirect to /login after 3-second countdown
5. Should display invalid token error message
6. Should display expired token error message
7. Should allow retry on network error
8. Should handle already-activated accounts gracefully

**PendingActivationComponent Unit Tests**:
1. Should display user's email address from navigation state
2. Should redirect to /register if accessed without email data
3. Should render "Back to Login" link

### Integration Tests (Service-Level)

**AuthService Integration Tests**:
1. POST /auth/register returns 201 Created with activationToken (Phase 0 behavior—remove in Phase 1+ when email delivery added)
2. POST /auth/register returns 409 Conflict when email+ActorType duplicate
3. POST /auth/register validates password complexity server-side
4. POST /auth/activate returns 200 OK for valid token
5. POST /auth/activate returns 400 Bad Request for invalid token
6. POST /auth/activate returns 410 Gone for expired token (if implemented)
7. POST /auth/activate is idempotent (second activation with same token succeeds)

### End-to-End Tests (Browser-Level via Playwright)

**E2E Test Suite: Registration Workflow (Phase 1+)**:

**Test 1: Happy Path - Complete Registration Flow**
```gherkin
Given user navigates to /register
When user fills form:
  | Field              | Value                          |
  | Email              | e2e-test-user@example.com     |
  | Password           | TestPass123!                   |
  | Confirm Password   | TestPass123!                   |
  | Actor Type         | R&D Organization               |
  | Full Name          | E2E Test User                  |
  | Organization Name  | Test Labs Inc                  |
And user clicks "Register" button
Then user sees "Registration Successful!" heading
And user sees their email in confirmation message
And page URL is /register/pending-activation
When activation email arrives (mock token extraction from DB)
And user navigates to /activate?token={extractedToken}
Then user sees "Account activated successfully!" message
And user auto-redirected to /login after 3 seconds
When user enters email + password on login page
Then user successfully logs in and sees innovation detail page
```

**Test 2: Validation Errors - Client-Side**
```gherkin
Given user on /register page
When user enters invalid email "notanemail"
Then error message "Invalid email format" appears below email field
When user enters password "weak"
Then password strength indicator shows "Weak"
And submit button remains disabled
When user enters password "StrongPass123!"
And user enters confirm password "Mismatch456!"
Then error message "Passwords do not match" appears
And submit button remains disabled
When user fixes confirm password to "StrongPass123!"
Then passwords match error disappears
And submit button becomes enabled
```

**Test 3: Duplicate Email Error - Server-Side**
```gherkin
Given existing user with email "existing@example.com" and ActorType "IdeaGenerator"
When new user navigates to /register
And user fills form with email "existing@example.com" and ActorType "IdeaGenerator"
And user clicks "Register"
Then error message appears: "Email already registered for this actor type"
And user remains on /register page
And form fields retain entered values
```

**Test 4: Organization Name Conditional Display**
```gherkin
Given user on /register page
When user selects Actor Type "Idea Generator"
Then Organization Name field is hidden (or marked optional with clear indication)
When user selects Actor Type "R&D Organization"
Then Organization Name field appears with "Required" indicator
```

**Test 5: Activation Token - Invalid**
```gherkin
Given user navigates to /activate?token=INVALID_TOKEN
When page loads
Then error message "Activation link is invalid" displays
And user sees "Back to Registration" link
```

**Test 6: Activation Token - Expired**
```gherkin
Given expired activation token in URL
When user navigates to /activate?token={expiredToken}
Then error message "Activation link has expired" displays
And user sees "Please register again" link to /register
```

**Test 7: Mobile Responsive Design**
```gherkin
Given user on mobile device (viewport 375px width)
When user navigates to /register
Then all form fields are visible without horizontal scrolling
And touch targets are minimum 44x44px
And form is usable with on-screen keyboard
```

**Test 8: Keyboard Accessibility**
```gherkin
Given user on /register page
When user navigates using only keyboard (Tab, Shift+Tab, Enter)
Then tab order is logical (top to bottom, left to right)
And all form fields are reachable
And submit button activatable via Enter or Space
And password visibility toggle activatable via keyboard
```

**Test 9: Password Visibility Toggle**
```gherkin
Given user on /register page
When user enters password "SecretPass123!"
Then password field displays bullets/dots (type="password")
When user clicks "Show" icon/button
Then password field displays plain text "SecretPass123!"
And icon changes to "Hide"
When user clicks "Hide"
Then password field returns to bullets/dots
```

**Test 10: Navigation Between Auth Pages**
```gherkin
Given user on /login page
When user clicks "Don't have an account? Register here" link
Then user navigates to /register page
When user successfully registers
And clicks "Back to Login" on pending activation page
Then user navigates to /login page
```

---

## Dependencies and Constraints

**Backend Prerequisites (Already Complete in Phase 0)**:
- ✅ POST /auth/register endpoint (returns 201 + activationToken in Phase 0; deferred email delivery to Phase 1+)
- ✅ POST /auth/activate endpoint (200 OK for valid token, error codes for invalid/expired)
- ✅ Actor entity with email uniqueness per ActorType validation
- ✅ Password complexity validation (8 chars, upper/lower/digit/special)

**Frontend Prerequisites (Phase 1+ New Work)**:
- Angular 19 application initialized (completed in Phase 0 T071)
- Angular Material components available
- Angular Router configured
- HTTP Client configured with API base URL
- AuthService skeleton exists (from Phase 7 T072 LoginComponent work)

**Phase 1+ Email Integration** (not required for UI, but mentioned for completeness):
- SendGrid API integration for sending activation emails
- Email template design (activation email with link)
- Environment variable for SendGrid API key

**Design Consistency Requirements**:
- Match Material Design styling from existing LoginComponent (Phase 7 T072)
- Reuse color scheme, typography, spacing from login page
- Consistent error message styling (red text, error icons)
- Consistent button styling (primary action button, disabled state)

---

## Tasks Generation Guidance for /speckit.tasks

When `/speckit.tasks` processes this specification, it should generate tasks in this structure:

### Phase 1+: Registration UI (8-12 tasks estimated)

**Component Implementation Tasks**:
- T[X]: Create RegisterComponent scaffold with routing configuration
- T[X]: Implement registration form with Signal-based FormGroup and all 6 fields
- T[X]: Add client-side validation for all fields (email format, password strength, confirm password match)
- T[X]: Implement conditional Organization Name field (show/hide based on Actor Type)
- T[X]: Add password visibility toggle functionality
- T[X]: Integrate with AuthService.register() method (create if not exists)
- T[X]: Implement error handling and display (API errors, network errors)
- T[X]: Create PendingActivationComponent with email display and navigation
- T[X]: Create ActivationComponent with token extraction and API integration
- T[X]: Add navigation links between Login ↔ Register pages
- T[X]: Style all components with Material Design matching LoginComponent
- T[X]: Add responsive CSS for mobile viewports (320px-768px)

**Testing Tasks**:
- T[X]: Unit tests for RegisterComponent (12 test cases per Testing Requirements above)
- T[X]: Unit tests for ActivationComponent (7 test cases)
- T[X]: Unit tests for PendingActivationComponent (3 test cases)
- T[X]: Integration tests for AuthService registration methods (7 test cases)
- T[X]: Playwright E2E tests for registration workflow (10 test scenarios per Testing Requirements above)
- T[X]: Accessibility audit using axe-core (WCAG 2.1 AA compliance)
- T[X]: Cross-browser testing (Chrome, Firefox, Safari, Edge)
- T[X]: Mobile device testing (iOS Safari, Android Chrome)

**Documentation Tasks**:
- T[X]: Update quickstart.md with browser-based registration instructions
- T[X]: Update API documentation (if needed—backend already documented)
- T[X]: Add screenshots to docs showing registration flow

---

## Specification Checklist for /speckit.specify

When running `/speckit.specify`, ensure the output specification includes:

✅ Functional requirements clearly stated (FR8.1-FR8.4)
✅ User Story 8 with workflow steps and acceptance criteria
✅ Non-functional requirements (performance, accessibility, security, compatibility)
✅ Edge cases documented and handled
✅ Testing requirements comprehensive (unit, integration, E2E)
✅ Dependencies and prerequisites identified
✅ Design consistency requirements specified
✅ Phase explicitly marked as "Phase 1+" (NOT Phase 0)
✅ Traceability to existing User Story 1 (extends authentication feature)
✅ All validation rules specified (client-side and server-side)
✅ Error message text specified (user-facing language)
✅ Navigation flows documented
✅ Mobile/responsive requirements included
✅ Accessibility requirements (WCAG 2.1 AA)

---

## Success Criteria for Phase 1+ Implementation

**Specification Complete When**:
1. User can successfully register via browser without API tools
2. All form validation works client-side and server-side
3. Activation link workflow functions end-to-end
4. All 10 Playwright E2E tests pass
5. WCAG 2.1 AA accessibility audit passes
6. Works on all required browsers and mobile devices
7. Styling matches existing LoginComponent design
8. Documentation updated with browser-based registration instructions

---

## AI Agent Instructions for /speckit.specify

**Execution Instructions**:
1. Read existing `specs/001-platform-core/spec.md` to understand context
2. Insert new requirements AFTER existing User Story 1 (User Registration)
3. Add new section "## 8. Registration User Interface (Phase 1+)"
4. Include all functional requirements (FR8.1-FR8.4)
5. Add User Story 8 to user stories section
6. Append non-functional requirements to existing NFR section
7. Add E2E test scenarios to testing requirements section
8. Update Phase 1+ scope section if it exists (or create it)
9. Add traceability notation: "Extends US1 (User Registration) with browser UI layer"
10. Ensure no conflicts with Phase 0 scope (registration UI explicitly NOT in Phase 0)

**Quality Gates**:
- All requirements testable (concrete acceptance criteria)
- No implementation details (Angular-specific syntax, file paths, etc.)
- Language consistent with existing spec.md style
- No ambiguity about what "done" means
- Edge cases covered
- Accessibility not an afterthought (integrated into requirements)

---

## Example Output Format (for /speckit.specify)

```markdown
## 8. Registration User Interface (Phase 1+)

**Context**: Phase 0 implemented backend registration APIs (POST /auth/register, POST /auth/activate) tested via Postman/curl. Phase 1+ adds Angular UI components for browser-based user registration workflow.

**Traceability**: Extends User Story 1 (User Registration - spec.md:L243) with graphical user interface layer.

### FR8.1: Registration Form Component
[Content from "Functional Requirements to Add" section above]

### FR8.2: Activation Pending Page
[Content from above]

[... etc ...]

### User Story 8: Browser-Based Registration
[Content from "User Story to Add" section above]
```

---

## Ready to Execute

**Command**: `/speckit.specify`

**Prompt to Agent**:
"Add comprehensive specification for Registration UI components (RegisterComponent, ActivationComponent, PendingActivationComponent) to existing specs/001-platform-core/spec.md. Phase 1+ enhancement (not Phase 0). Backend APIs already exist. Need full browser-based registration workflow specification including functional requirements FR8.1-FR8.4, User Story 8, comprehensive E2E test scenarios (10 Playwright tests), accessibility (WCAG 2.1 AA), and mobile responsive requirements. See REGISTER_COMPONENT_SPECIFICATION_PROMPT.md for complete requirements."

**Follow-up Command**: `/speckit.tasks`

**Expected Output**: 20-25 tasks generated for Phase 1+ covering component implementation, styling, validation, E2E testing, accessibility audit, cross-browser testing, and documentation.
