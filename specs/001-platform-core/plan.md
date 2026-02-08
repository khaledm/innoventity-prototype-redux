# Implementation Plan: Platform Core (v1.0)

**Branch**: `001-platform-core` | **Date**: February 8, 2026 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-platform-core/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Build an open innovation platform enabling research-based innovation originators (idea generators) to discover and collaborate with commercialization partners (R&D organizations, manufacturing companies, sales/marketing firms, and investors). Core v1.0 delivers: actor registration & authentication, innovation submission & publication, formal bidding system, irreversible partner selection, and virtual incubator workspace for business plan collaboration.

**Technical Approach**: Full-stack web application using ASP.NET Core 8 Minimal APIs (backend) with EF Core 8 (data persistence), Angular v18 with Standalone Components and Signals (frontend), JWT authentication, Azure SQL Database, deployed to Azure App Service with Application Insights monitoring. Architecture follows Vertical Slice pattern organized by feature.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (backend), TypeScript 5.x / Angular 18 (frontend)  

**Primary Dependencies**: 
- Backend: ASP.NET Core 8, EF Core 8, IdentityModel.Tokens.Jwt, BCrypt.Net, Azure.Extensions.*
- Frontend: Angular 18 (standalone components, signals), RxJS, Angular Material (UI components)
- Testing: xUnit, Playwright (E2E), Stryker.NET (mutation testing)

**Storage**: Azure SQL Database (via EF Core 8), Azure Service Bus (async messaging), Azure Key Vault (secrets), Azure Blob Storage (future document management)

**Testing**: 
- Unit: xUnit with test data builders
- Integration: WebApplicationFactory with test database
- E2E: Playwright for critical user journeys (J1-J3)
- Mutation: Stryker.NET (>70% mutation score target)

**Target Platform**: Azure App Service (Linux), modern browsers (Chrome, Edge, Firefox, Safari latest 2 versions)

**Project Type**: Web application (backend API + frontend SPA)

**Performance Goals**:
- API response time p95: <200ms
- Database query p95: <100ms
- Page load (First Contentful Paint) p95: <2s
- Real-time notification delivery: <1s latency

**Constraints**:
- Solo development (10-month timeline)
- Production-ready from Phase 0 (no "prototype" quality)
- Test-first development (TDD: red → green → refactor)
- No data migration (fresh database, reference legacy for business rules only)
- v2.0 extensibility required (multi-tenancy, new actor types, organization features)

**Scale/Scope**:
- v1.0 Target: 100 concurrent users (load tested)
- 5 actor types, 3 P0 user journeys (innovation submission, bid submission, partner selection)
- Phase 0: Registration + authentication + view single innovation (1-2 weeks)
- Full v1.0: ~10 months solo development

## Phase 0 Authorization

**Authentication Strategy**:
- JWT Bearer token authentication required for all endpoints EXCEPT `/auth/*` (register, activate, login, refresh)
- Access tokens expire after 1 hour, refresh tokens after 7 days
- Tokens stored client-side (localStorage/sessionStorage)

**Resource-Level Authorization** (Phase 0):
- **None implemented** - Phase 0 has only public innovation read after authentication
- `GET /api/innovations/{id}` requires authentication but no ownership check (public discovery)

**Authorization Pattern** (deferred to Phase 1+):
- Policy-based authorization using `IAuthorizationRequirement` + `IAuthorizationHandler` (per Research §Decision 3)
- Resource policies defined per feature:
  - `InnovationOwnerRequirement`: Validates user owns innovation (for edit, delete, submit operations)
  - `BidActorRequirement`: Validates user is bid owner (for withdraw bid)
  - `InnovationAccessRequirement`: Validates user is owner or accepted partner (for virtual incubator access)
  - `BusinessPlanAccessRequirement`: Validates user is owner or accepted partner (for business plan edit)
- Composable handlers enable v2.0 organization policies without rewriting resource ownership checks

**Implementation**:
- Phase 0: `[Authorize]` attribute on non-auth endpoints (JWT validation only)
- Phase 1+: `[Authorize(Policy = "InnovationOwner")]` for resource-specific operations

**v2.0 Extensibility** (per Architecture Guardrails):
- Repository abstractions support organization context filtering: `GetByIdAsync(Guid id, Guid? organizationId = null)`
- Policy handlers can check organization membership without modifying existing ownership checks
- Actor type inheritance (TPH) supports new actor types (Government, TechnologyPark) without policy refactoring

## Session Management

**Phase 0 Approach** (KISS principle - Simplicity Over Cleverness):

**Client-Side Logout**:
```typescript
// Frontend (Angular service)
logout(): void {
  localStorage.removeItem('accessToken');
  localStorage.removeItem('refreshToken');
  this.router.navigate(['/login']);
}
```

**Server-Side Token Revocation**: **Deferred to v2.0**
- Rationale: Implementing revocation requires stateful infrastructure (token blacklist/whitelist), violates JWT stateless design
- Security mitigation: Short access token lifetime (1hr) limits exposure window after logout
- Trade-off: User must wait ≤1hr for token expiration after client-side logout (acceptable for v1.0 with 100 concurrent users)

**Implicit Session Invalidation** (Phase 0):
1. **Account Suspension**: `/auth/refresh-token` checks `AccountStatus` - suspended accounts blocked from token renewal
2. **Password Change**: New password hash invalidates all tokens (user must re-authenticate via `/auth/login`)
3. **Natural Expiry**: Access tokens expire after 1hr, refresh tokens after 7d (enforced by JWT validation)

**Multi-Device Behavior**:
- Multiple devices CAN log in simultaneously (no device tracking in v1.0)
- Client-side logout affects ONLY the device where logout is triggered
- Other devices remain authenticated until token expires (max 1hr exposure)
- Explicit decision: Multi-device tracking excluded from v1.0 scope (per checklist CHK018)

**v2.0 Upgrade Path** (if "logout all devices" becomes requirement):
```csharp
// Add RefreshToken entity with revocation flag
public class RefreshToken {
    public Guid Id { get; set; }
    public Guid ActorId { get; set; }
    public string Token { get; set; }
    public bool IsRevoked { get; set; }  // Enable server-side revocation
    public DateTime ExpiresAt { get; set; }
}

// Check revocation during token refresh
public async Task<Result<TokenResponse>> RefreshTokenAsync(string refreshToken) {
    var token = await _tokenRepository.GetByTokenAsync(refreshToken);
    if (token?.IsRevoked == true) {
        return Result.Fail("Token revoked");
    }
    // ... generate new access token
}

// Revoke all tokens for actor (logout all devices)
public async Task RevokeAllTokensAsync(Guid actorId) {
    await _tokenRepository.RevokeAllForActorAsync(actorId);
}
```

**Security Alignment**:
- Maintains JWT stateless design (Principle 3: Simplicity Over Cleverness)
- 1hr access token lifetime balances security and UX (Research §Decision 3)
- Account suspension + password change provide immediate invalidation for critical security events
- v2.0 can add stateful revocation without breaking v1.0 authentication flow

## Security Requirements

### CHK088: Password Hashing
- **Algorithm**: BCrypt with work factor 12 (Research §Decision 3)
- **Implementation**: ASP.NET Core Identity PasswordHasher<TUser>
- **Validation**: Password complexity enforced before hashing (Spec R8.4)

### CHK089: JWT Signing Key Management
- **Algorithm**: HMAC-SHA256 (HS256) for symmetric signing
- **Key Requirements**:
  - Minimum 256 bits (32 characters) for HS256 security
  - Generated using cryptographically secure random number generator (e.g., `openssl rand -base64 32`)
- **Storage**:
  - Development: User Secrets (`dotnet user-secrets set "Jwt:SecretKey" "<generated-key>"`) or appsettings.Development.json
  - Production: Azure App Service Configuration (Application settings blade)
  - Configuration Key: `Jwt__SecretKey` (double underscore for nested config)
- **Rotation Policy**:
  - Phase 0: Manual rotation if compromised (invalidates all existing tokens)
  - v2.0: Automated rotation with key versioning (multi-key validation support)
- **Security**: Never commit to source control, mask in logs, store in App Service Configuration (not appsettings.json)

### CHK090: HTTPS Requirements
- **Production**: HTTPS enforced for ALL endpoints
  - Azure App Service HTTPS-only mode enabled
  - HTTP requests automatically redirected to HTTPS (ASP.NET Core UseHttpsRedirection middleware)
  - HSTS (HTTP Strict Transport Security) enabled with 1-year max-age
- **Development**: HTTP allowed for localhost only (5000/5001)
- **API Contract**: OpenAPI servers list HTTPS URLs for dev/prod environments

### CHK091: SQL Injection Prevention
- **Strategy**: Parameterized queries exclusively
  - EF Core LINQ queries use parameterization by default (no manual escaping)
  - No raw SQL in Phase 0 (defer to Phase 1+ with FromSqlRaw parameterized commands)
- **Validation**: Input validation performed by ASP.NET Core model binding (Data Annotations)
- **Code Review**: Manual check during PR review - reject any string concatenation in queries

### CHK092: CORS Policy Requirements
- **Development**:
  - Allowed Origins: `http://localhost:4200` (Angular dev server)
  - Credentials: Allowed (for future cookie support if needed)
  - Methods: GET, POST, PUT, DELETE, OPTIONS
  - Headers: Content-Type, Authorization
- **Production**:
  - Allowed Origins: Specific frontend origin only (e.g., `https://innoventity.azurewebsites.net`)
  - No wildcard (*) origins in production
  - Credentials: Allowed
  - Same methods and headers as development
- **Implementation**: ASP.NET Core `AddCors()` with named policy "AllowFrontend"

### CHK093: Secret Management
- **Secrets to Manage**:
  - Database connection string (Azure SQL): `ConnectionStrings:InnoventityDb`
  - JWT signing key (symmetric HS256): `Jwt:SecretKey`
  - Email service API key (Phase 1+): `SendGrid:ApiKey`

**Production Pattern** (Azure App Service - **Recommended for Learning Project**):
- **Approach**: App Service Configuration (Environment Variables)
  - Store secrets directly in App Service Configuration blade (Azure Portal)
  - Secrets encrypted at rest, accessible only via Azure RBAC
  - **Cost**: $0 additional (included with App Service)
  - **Complexity**: Low (no Managed Identity, Key Vault setup)
  
- **Setup Steps** (Azure Portal):
  1. Navigate to App Service → Configuration → Application settings
  2. Click "+ New application setting" for each secret:
     - **Name**: `ConnectionStrings__InnoventityDb` (note: double underscore maps to `:` in config)
     - **Value**: `Server=tcp:innoventity-sql.database.windows.net;Database=Innoventity;User ID=innoventity-admin;Password=<password>`
     - **Deployment slot setting**: ✅ (checked - keeps secrets per environment)
  3. Repeat for `Jwt__SecretKey` and `SendGrid__ApiKey` (Phase 1+)
  4. Click "Save" → app restarts with new configuration

- **Configuration Binding** (No code changes needed):
  ```csharp
  // ASP.NET Core automatically reads from environment variables
  var connectionString = builder.Configuration.GetConnectionString("InnoventityDb");
  var jwtKey = builder.Configuration["Jwt:SecretKey"];
  ```

- **Trade-offs vs. Key Vault**:
  - ✅ **Simpler**: No Managed Identity, access policies, or Key Vault resource
  - ✅ **Cost-effective**: $0 additional (vs. ~$1-2/month for Key Vault)
  - ✅ **Suitable for learning projects with 100 concurrent users**
  - ❌ **Less granular access control**: Anyone with App Service Contributor role sees secrets (vs. Key Vault Secret User role)
  - ❌ **No audit logging**: Can't track who accessed which secret
  - ❌ **Manual secret rotation**: Must update in portal + restart app (no versioning support)

**Alternative: Azure Key Vault Integration** (if enterprise-grade security needed):
- **When to use**: 
  - Resume showcase ("Azure Key Vault integration with Managed Identity")
  - Production app with compliance requirements (audit logging mandatory)
  - Multiple apps sharing secrets (centralized management)
- **Cost**: ~$0.03 per 10K operations + $0.03/secret/month (~$1-2/month total)
- **Setup**: Create Key Vault → Enable System-assigned Managed Identity on App Service → Add access policy → Use Key Vault references in App Service Configuration (`@Microsoft.KeyVault(SecretUri=...)`)
- **Reference**: See Research §Decision 8 for Key Vault architecture details

**Development Pattern** (Local Environment):
- **User Secrets** (recommended for sensitive values):
  - Initialize: `dotnet user-secrets init --project src/Innoventity.API`
  - Set secrets: `dotnet user-secrets set "ConnectionStrings:InnoventityDb" "Server=localhost;Database=Innoventity;Integrated Security=true"`
  - Stored outside project directory: `%APPDATA%\Microsoft\UserSecrets\<user-secrets-id>\secrets.json`
  - Never committed to source control
- **appsettings.Development.json** (for non-sensitive config):
  - Connection strings pointing to localhost SQL Server or LocalDB
  - JWT signing key (development-only key, NOT production key)
  - File excluded from git via `.gitignore`
- **Required .gitignore entries**:
  ```
  appsettings.Development.json
  appsettings.*.json
  !appsettings.json
  ```

**Access Control** (App Service Configuration):
- **Azure RBAC**: Only users/service principals with App Service Contributor role can view/edit configuration
- **Deployment Slot Setting**: Enable "Deployment slot setting" checkbox to prevent secrets from swapping between Staging/Production slots
- **Least Privilege**: Developers should have Reader role + specific Configuration write access (not full Contributor)

**Secret Rotation Requirements**:
- **Database Connection String**: 
  1. Update App Service Configuration with new value (Azure Portal or Azure CLI)
  2. Restart app: Azure Portal → Restart button (triggers configuration reload)
  3. Test connectivity before decommissioning old password
- **JWT Signing Key**: 
  - Phase 0: Manual rotation → update Configuration → restart app (invalidates all existing tokens, users must re-login)
  - v2.0: Multi-key validation (store key version in token claims, support 2 concurrent keys during rotation)
- **Rotation Testing**: Always test in Staging deployment slot before swapping to Production

**Security Rules**:
- ❌ **NEVER** commit secrets to source control (scan with `git-secrets` or GitHub secret scanning)
- ❌ **NEVER** log secret values (mask in Application Insights, use `[SensitiveData]` attribute)
- ❌ **NEVER** expose secrets in API responses or client-side code
- ✅ **ALWAYS** use App Service Configuration for production secrets (NOT appsettings.json or plaintext environment variables)
- ✅ **ALWAYS** enable "Deployment slot setting" to prevent secret leakage during slot swaps
- ✅ **ALWAYS** review Azure Activity Log for Configuration changes (who modified secrets, when)

**Upgrade Path to Key Vault** (Phase 1+ if needed):
- If project grows beyond learning scope (real users, compliance requirements), migrate to Key Vault:
  1. Create Key Vault resource
  2. Enable System-assigned Managed Identity on App Service
  3. Add Key Vault access policy (Managed Identity → Get/List secrets)
  4. Update App Service Configuration values from `<secret>` to `@Microsoft.KeyVault(SecretUri=...)`
  5. No code changes required (configuration binding stays the same)

## Frontend/UX Requirements

### CHK019: Loading State Requirements
- **Asynchronous Operations**: Display loading indicators for all API calls
  - HTTP requests: Angular Material `<mat-spinner>` or `<mat-progress-bar>`
  - Minimum display time: 300ms (prevent flicker for fast responses)
  - Long operations (>2s): Progress indicator with status text
- **Page Transitions**: Skeleton screens for initial page load (innovations list, detail view)
- **Button States**: Disable submit buttons during form submission, show spinner inside button
- **Implementation**: RxJS `loading$` signal per feature, updated via HTTP interceptor

### CHK020: Error Message Content Requirements
- **Validation Errors** (Client-Side):
  - Display inline below form field with red text and error icon
  - Format: "Field Name: Specific reason" (e.g., "Email: Must be a valid email address")
  - Show on blur or submit attempt (not on every keystroke)
- **API Errors** (Server-Side):
  - 400 Bad Request: Display specific field errors from ProblemDetails response
  - 401 Unauthorized: Redirect to login with message "Session expired. Please log in again."
  - 403 Forbidden: Banner message "You don't have permission to perform this action."
  - 404 Not Found: Replace content area with "Resource not found" message and back button
  - 500 Internal Server Error: Generic message "Something went wrong. Please try again or contact support." (log full error)
- **Network Errors**: Banner message "Connection lost. Check your internet and try again."
- **Error Persistence**: Dismissable via close button or automatic timeout (5s for info, persistent for errors)

### CHK021: Form Validation Requirements
- **Client-Side Validation** (UX optimization):
  - Angular Reactive Forms with Validators (required, email, minLength, pattern)
  - Real-time feedback after field blur (not on every keystroke unless already invalid)
  - Disable submit button until form valid
  - Purpose: Immediate feedback, reduce unnecessary API calls
- **Server-Side Validation** (Security enforcement):
  - ASP.NET Core Data Annotations on DTOs (required, range, regex)
  - FluentValidation for complex business rules (Phase 1+)
  - Always validate server-side (never trust client data)
  - Return 400 Bad Request with ProblemDetails for validation failures
- **Strategy**: Duplicate validation rules client/server (DRY violation accepted for defense-in-depth)

### CHK022: Accessibility Requirements
- **Target**: WCAG 2.1 Level AA compliance
- **Implementation**:
  - Angular Material components (built-in ARIA attributes, keyboard navigation)
  - Semantic HTML5 elements (`<nav>`, `<main>`, `<article>`, `<section>`)
  - ARIA labels for icon-only buttons and dynamic content regions
  - Focus management (trap focus in modals, return focus after close)
  - Color contrast: 4.5:1 for normal text, 3:1 for large text (Material theme handles this)
- **Testing**: Lighthouse Accessibility audit score ≥90 (CI/CD gate)
- **Keyboard Navigation**: All interactive elements reachable via Tab, Enter/Space activate

### CHK023: Responsive Design Requirements
- **Approach**: Mobile-first with breakpoints
- **Breakpoints** (Angular Material/CSS):
  - XSmall: <600px (mobile portrait)
  - Small: 600px-959px (mobile landscape, small tablets)
  - Medium: 960px-1279px (tablets, small laptops)
  - Large: 1280px-1919px (desktops)
  - XLarge: ≥1920px (large desktops)
- **Layout Strategy**:
  - Angular Material Layout or CSS Grid/Flexbox
  - Single-column layout for XSmall/Small (stacked forms, full-width tables)
  - Multi-column layout for Medium+ (sidebar navigation, responsive tables)
  - Touch targets: Minimum 44x44px for mobile (Material buttons comply)
- **Testing**: Manual testing on Chrome DevTools device emulation (iPhone, iPad, desktop)

### CHK024: Navigation Requirements
- **Consistency**:
  - Top navigation bar: Logo (home link), authenticated user menu, logout button
  - Side navigation (Medium+ breakpoints): Feature navigation (innovations, bids, profile)
  - Breadcrumbs for multi-level pages (Innovation > Detail > Edit)
  - Angular Router for all navigation (no hard links)
- **Authentication Guards**:
  - AuthGuard: Redirect to login if not authenticated
  - RoleGuard: Redirect to 403 page if wrong actor type (Phase 1+)
- **Active Route Highlighting**: `routerLinkActive` directive on navigation links
- **Browser History**: Support back/forward buttons (Angular Router handles this)

## Exception Flow Coverage

### CHK065: Duplicate Email on Registration
- **Scenario**: User attempts registration with email already registered for same `ActorType`
- **Business Rule**: Spec §R1.3 "Email must be unique per ActorType"
- **API Response**: `400 Bad Request` with RFC 7807 ValidationProblemDetails
  ```json
  {
    "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
    "title": "Validation Error",
    "status": 400,
    "errors": {
      "Email": ["This email address is already registered for the selected actor type"]
    }
  }
  ```
- **Frontend Handling**: Display inline error below email field, allow user to modify email or try login

### CHK066: Invalid or Expired Activation Token
- **Scenario**: User clicks activation link with invalid/expired token (e.g., already activated, token tampered)
- **API Response**: `400 Bad Request` (Contracts §/auth/activate, line 147-159)
  ```json
  {
    "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
    "title": "Invalid Activation Token",
    "status": 400,
    "detail": "The activation token is invalid or has expired"
  }
  ```
- **Frontend Handling**: Banner error with "Request new activation email" button (see CHK071)

### CHK067: Login with Unactivated Account
- **Scenario**: User attempts login before clicking activation email link (AccountStatus = PendingActivation)
- **Business Rule**: Spec §R1.1 "New actors start with PendingActivation status"
- **API Response**: `401 Unauthorized` (Contracts §/auth/login, notActivated example, line 225-232)
  ```json
  {
    "type": "https://tools.ietf.org/html/rfc7231#section-6.5.2",
    "title": "Account Not Activated",
    "status": 401,
    "detail": "Please activate your account using the link sent to your email"
  }
  ```
- **Frontend Handling**: Banner error with "Resend activation email" link

### CHK068: Unauthorized Resource Access (403 Forbidden)
- **Phase 0 Scope**: Not applicable - Phase 0 implements authentication only (JWT validation), no resource-level authorization
- **Phase 1+ Implementation**:
  - **Scenario**: User attempts to edit/delete innovation owned by another user
  - **Authorization**: Policy-based (`InnovationOwnerRequirement` handler checks ActorId)
  - **API Response**: `403 Forbidden`
    ```json
    {
      "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
      "title": "Forbidden",
      "status": 403,
      "detail": "You do not have permission to perform this action"
    }
    ```
  - **Frontend Handling**: Banner error "You don't have permission to perform this action" (Frontend/UX §CHK020)
- **Reference**: Plan §Phase 0 Authorization (line 54-78)

### CHK069: Innovation Not Found (404)
- **Scenario**: User requests innovation with non-existent GUID or soft-deleted innovation (Phase 1+)
- **API Response**: `404 Not Found` (Contracts §/api/innovations/{id}, line 328-338)
  ```json
  {
    "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
    "title": "Innovation Not Found",
    "status": 404,
    "detail": "Innovation with ID '7c9e6679-7425-40de-944b-e07fc1f90ae7' was not found"
  }
  ```
- **Frontend Handling**: Display "Resource not found" page with back button (Frontend/UX §CHK020)

### CHK070: Database Failures and Network Errors (500)
- **Scenarios**:
  - Database connection timeout (e.g., Azure SQL throttling)
  - Unhandled application exceptions (e.g., null reference in business logic)
  - Third-party service failures (e.g., Azure Key Vault unavailable)
- **API Response**: `500 Internal Server Error` (Contracts §InternalServerError schema, line 740-757)
  ```json
  {
    "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
    "title": "Internal Server Error",
    "status": 500,
    "detail": "An unexpected error occurred while processing your request"
  }
  ```
- **Frontend Handling**: 
  - Generic banner error message (do NOT expose stack traces/DB details to users)
  - Log full error details to browser console for debugging
  - Provide "Retry" button for transient failures (network, timeouts)
- **Backend Requirements**:
  - Global exception handler middleware (ASP.NET Core UseExceptionHandler)
  - Log errors to Application Insights with correlation IDs
  - Return ProblemDetails with generic message (security best practice)

### CHK071: Resend Activation Email Recovery Flow
- **Current State**: **Gap identified** - Spec §R1.0 Error Scenario 1 mentions "Resend activation email" option (line 303), but NO API endpoint defined in Contracts
- **Phase 0 Decision**: **Defer to Phase 1** due to implementation complexity vs. value trade-off
  - **Complexity**: Requires Actor lookup by email + ActorType, additional endpoint security (rate limiting to prevent spam)
  - **Workaround**: User can re-register with same email/ActorType - backend should allow overwriting PendingActivation accounts
- **Phase 1 Implementation Requirements**:
  - **Endpoint**: `POST /auth/resend-activation`
  - **Request Body**: `{ "email": "user@example.com", "actorType": "IdeaGenerator" }`
  - **Business Rules**:
    - Return 200 even if email not found (security - hide account existence)
    - Only resend if AccountStatus = PendingActivation (ignore Active accounts)
    - Rate limit: Max 3 requests per email per hour (prevent spam)
  - **Response**: `200 OK` with generic message "If your account exists and is pending activation, a new email has been sent"

### CHK072: Refresh Token Expiry Handling
- **Scenario**: User's refresh token expires after 7 days of inactivity, attempts to refresh access token
- **API Response**: `400 Bad Request` (Contracts §/auth/refresh-token, line 267-278)
  ```json
  {
    "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
    "title": "Invalid Refresh Token",
    "status": 400,
    "detail": "The refresh token is invalid or has expired"
  }
  ```
- **Frontend Handling**:
  - HTTP interceptor catches 400 on refresh-token endpoint
  - Clear tokens from localStorage
  - Redirect to login with banner "Your session has expired. Please log in again."
- **Prevention**: Warn user 24hrs before expiry with "Your session expires soon. Please log in to continue."

### CHK073: Form Validation Failure Recovery
- **Scenario**: User submits form with missing/invalid fields (e.g., registration with weak password)
- **Client-Side Validation**: Angular Reactive Forms (immediate feedback before submit)
  - Display inline error messages below each invalid field
  - Disable submit button until form is valid
  - Error messages match server validation (avoid "validation passed client but failed server")
- **Server-Side Validation**: ASP.NET Data Annotations + FluentValidation
  - Return `400 Bad Request` with ValidationProblemDetails (errors dictionary keyed by field name)
  - Frontend maps errors to form controls, displays inline
- **Recovery Flow**:
  1. User sees red error messages below invalid fields
  2. User corrects values (errors clear dynamically as user types)
  3. Submit button enables when all validations pass
  4. User resubmits (successful 200/201 response)
- **Example**: Password complexity validation (Spec §R8.4)
  - Frontend: Custom validator checks 8+ chars, uppercase, lowercase, digit, special char
  - Backend: `[MinLength(8)]`, `[PasswordComplexity]` attribute validates same rules
  - Error message: "Password must be at least 8 characters and include uppercase, lowercase, digit, and special character"
- **Reference**: Frontend/UX Requirements §CHK021 Form Validation (line 236-247)

## Edge Case Requirements

### CHK074: Zero-State Scenarios (No Innovations Exist)
- **Phase 0 Scope**: Not applicable - Phase 0 displays single innovation by ID (GET /api/innovations/{id}), no list/discovery page
- **Phase 1 Implementation** (when innovation browse/search added):
  - **Empty State UI**:
    - Display empty state illustration (e.g., Material icon: search_off)
    - Message: "No innovations found. Be the first to submit your research-based innovation!"
    - Call-to-action button: "Submit Innovation" (if user is IdeaGenerator actor type)
  - **Filters Applied with No Results**:
    - Message: "No innovations match your filters. Try adjusting your search criteria."
    - Show "Clear Filters" button
  - **User's Own Innovations List (Empty)**:
    - Message: "You haven't submitted any innovations yet."
    - Button: "Submit Your First Innovation"

### CHK075: Minimum Bid Threshold (Exactly 1 Bid)
- **Business Rule**: Spec §R5.1 "Minimum one bid per required category" (R&D, Manufacturing, SalesMarketing)
- **Scenario**: Innovation requires R&D + Manufacturing, receives exactly 1 bid for each category
- **Expected Behavior**: Innovation owner can proceed to partner selection (sufficient bids received)
- **Notification**: "Your innovation has received sufficient bids. You can now select partners." (Spec line 956)
- **UI Requirements**: Partner selection page displays all bids, even if only 1 per category (no "need more bids" gating)

### CHK076: Maximum Concurrent Bids per Innovation
- **Phase 0 Decision**: **No maximum limit** - uncapped bid submissions allowed in v1.0
- **Rationale**: 
  - v1.0 user scale (100 concurrent users) prevents bid spam naturally
  - Innovation owner benefits from more partnership options (competitive bidding)
  - Database can handle unlimited bids per innovation (Bid table has FK + index on InnovationId)
- **Phase 1+ Considerations** (if spam becomes issue):
  - Implement per-actor rate limit: Max 3 bids per innovation per day (prevent retry spam)
  - UI: Display "You have submitted the maximum number of bids for this innovation" if limit reached

### CHK077: String Length Boundary Validations
- **Comprehensive Coverage**: Data Model §Validation Rules defines min/max for ALL string fields
- **Key Boundaries**:
  | Field | Minimum | Maximum | Source |
  |-------|---------|---------|--------|
  | Actor.Email | - | 256 chars | Data Model line 153 |
  | Actor.FullName | - | 200 chars | Data Model line 154 |
  | Actor.ContactAddress | - | 500 chars | Data Model line 155 |
  | Innovation.Title | - | 200 chars | Data Model line 232 |
  | Innovation.ResearchBackground | - | 2000 chars | Data Model line 234 |
  | Innovation.ProductDescription | - | 2000 chars | Data Model line 240 |
  | Innovation.KeyAdvantages | - | 1000 chars | Data Model line 241 |
  | Innovation.ProductKeywords | - | 500 chars | Data Model line 244 |
  | Bid.ParticipationProposal | **200 chars** | 5000 chars | Data Model line 515 (CHECK constraint) |
- **Frontend Validation**: 
  - Display character counter below textareas: "250 / 2000 characters"
  - Real-time validation: Turn counter red when exceeding max
  - Disable submit if any field exceeds max length
- **Backend Validation**:
  - ASP.NET `[MaxLength(n)]` attribute on DTOs
  - EF Core validates during SaveChanges, throws DbUpdateException if exceeded
  - Return 400 Bad Request with ValidationProblemDetails indicating which field exceeded limit

### CHK078: Empty Optional Fields (Null vs Empty String Handling)
- **Data Model Convention**: Nullable fields use `string?` type, required fields use `string` (never null)
- **Examples**:
  - **Optional**: `Actor.ActivationToken` (string?, nullable - null after activation)
  - **Optional**: `Innovation.IPRExplanation` (string?, nullable - null if HasIPR=false)
  - **Required**: `Innovation.Title` (string, never null - empty string blocked by frontend validation)
- **Backend Business Rules**:
  - Nullable fields: Accept null OR non-empty string (reject empty string "", treat as validation error)
  - Required fields: Reject null AND empty string (both fail `[Required]` validation)
- **API Serialization**:
  - JSON null: Optional field omitted or `"field": null` both deserialize to C# null
  - JSON empty string: `"field": ""` → Validation error "Field cannot be empty"
- **Database Storage**:
  - Nullable columns: Store NULL (not empty string) to preserve distinction and save storage
  - Required columns: NOT NULL constraint prevents null, empty string blocked by validation (CHECK constraint: `LEN(field) > 0` for critical fields)

### CHK079: Concurrent Bid Submissions to Same Innovation
- **Scenario**: Two actors (Actor A, Actor B) submit bid to Innovation X simultaneously (within same second)
- **Phase 0 Decision**: **No special concurrency control** - rely on database UNIQUE constraint + application validation
- **Constraints Preventing Conflicts**:
  1. **Unique Index**: `UQ_Bid_ActorId_InnovationId` prevents duplicate bids from same actor (Data Model line 492)
  2. **Business Rule Check**: Application validates "Actor cannot submit multiple bids for same innovation" (Spec line 805)
- **Race Condition Scenario**:
  - Actor A submits bid at T+0ms → DB insert succeeds
  - Actor B submits bid at T+5ms → DB insert succeeds (different ActorId, no conflict)
  - **Outcome**: Both bids successfully recorded (expected behavior, not a bug)
- **Same Actor Retry Race**:
  - Actor A clicks submit twice quickly (T+0ms, T+10ms)
  - First request: Check existing bid → none found → insert succeeds
  - Second request: Check existing bid → found (from first request) → return 400 "You cannot submit multiple bids"
  - **OR** Second request: Check passes (race) → insert fails UNIQUE constraint → catch SqlException → return 400
- **Transaction Isolation**: Use default Read Committed (SQL Server default) - sufficient for this scenario

### CHK080: Concurrent Partner Selection Attempts
- **Scenario**: Innovation Owner clicks "Select Partners" button twice quickly, or two browser tabs open
- **Phase 0 Scope**: Not applicable - Phase 0 has no partner selection API endpoint
- **Phase 1 Implementation Requirements**:
  - **Idempotency**: `PUT /api/innovations/{id}/select-partners` endpoint must be idempotent
  - **Optimistic Concurrency Control**:
    - Innovation entity has `RowVersion` timestamp column (EF Core concurrency token)
    - First request: Update Innovation.Status = PartnerSelectionComplete, increment RowVersion
    - Second request: EF Core throws DbUpdateConcurrencyException (RowVersion mismatch)
    - Catch exception → return 409 Conflict: "Partner selection already completed"
  - **Frontend Prevention**:
    - Disable "Select Partners" button after click (show loading spinner)
    - HTTP interceptor ignores duplicate requests to same URL within 500ms

### CHK081: Database Transaction Isolation Requirements
- **Phase 0 Strategy**: Use SQL Server defaults with minimal customization (KISS principle)
- **Default Isolation Level**: **Read Committed** (SQL Server default)
  - Prevents dirty reads (reading uncommitted data)
  - Allows non-repeatable reads and phantom reads (acceptable trade-off for performance in v1.0)
- **EF Core Transaction Behavior**:
  - **Implicit Transactions**: Single `SaveChanges()` call wrapped in transaction automatically
  - **Explicit Transactions**: Not required for Phase 0 (all operations are single-entity creates/updates)
- **Phase 1+ Scenarios Requiring Explicit Transactions**:
  - Partner Selection: Update Innovation.Status + create multiple Partnership records (atomic operation)
  - Bid Withdrawal: Delete Bid + log audit record (atomic)
  ```csharp
  using var transaction = await _context.Database.BeginTransactionAsync();
  try {
      innovation.Status = InnovationStatus.PartnerSelectionComplete;
      _context.Partnerships.AddRange(selectedPartnerships);
      await _context.SaveChangesAsync();
      await transaction.CommitAsync();
  } catch {
      await transaction.RollbackAsync();
      throw;
  }
  ```
- **Concurrency Conflicts**: Rely on EF Core optimistic concurrency (RowVersion) rather than pessimistic locking (SELECT FOR UPDATE)

### CHK082: Orphaned Records (Deleted Actors with Innovations)
- **Constraint**: Actor deletion **RESTRICTED** if actor owns innovations or bids (Data Model line 496-497)
  - `Innovation.OwnerId → Actor.Id` (ON DELETE RESTRICT)
  - `Bid.ActorId → Actor.Id` (ON DELETE RESTRICT)
- **Business Rule**: Cannot delete actor account if they have created innovations or submitted bids
- **User Scenario**:
  - Actor attempts account deletion via UI (Phase 1+ feature)
  - Backend checks: `SELECT COUNT(*) FROM Innovation WHERE OwnerId = @actorId`
  - If count > 0: Return 400 "Cannot delete account: You have active innovations. Please delete innovations first."
- **Alternative: Account Suspension** (Preferred for Phase 0):
  - Update `Actor.AccountStatus = Suspended` instead of DELETE
  - Suspended actors:
    - Cannot log in (401 during login, Spec line 1112)
    - Cannot perform any actions (auth middleware checks AccountStatus)
    - Historical innovations/bids remain intact (audit trail preserved)
- **Cascading Deletes**: Innovation deletion cascades to Bids, BusinessPlan, InnovationTargetIndustry (Data Model line 498-500)

### CHK083: Referential Integrity and Cascade Rule Consistency
- **Full Cascade Rules Definition** (Data Model §Foreign Keys, line 495-503):
  | Foreign Key | ON DELETE Behavior | Rationale |
  |-------------|-------------------|-----------|
  | Innovation.OwnerId → Actor.Id | **RESTRICT** | Prevent orphaned innovations, require explicit actor suspension |
  | Bid.ActorId → Actor.Id | **RESTRICT** | Preserve bid history for audit |
  | Bid.InnovationId → Innovation.Id | **CASCADE** | Delete bids when innovation deleted (bids meaningless without innovation) |
  | BusinessPlan.InnovationId → Innovation.Id | **CASCADE** | Delete business plan when innovation deleted |
  | InnovationTargetIndustry.InnovationId → Innovation.Id | **CASCADE** | Delete junction table entries when innovation deleted |
  | InnovationTargetIndustry.IndustryId → Industry.Id | **RESTRICT** | Prevent deleting industries in use (master data) |
  | ActorIndustry.ActorId → Actor.Id | **CASCADE** | Delete actor industry affiliations when actor suspended/deleted |
  | ActorIndustry.IndustryId → Industry.Id | **RESTRICT** | Protect master industry data |
- **Consistency Verification**:
  - RESTRICT on all Actor references: Prevents data loss, forces explicit account suspension
  - CASCADE on all innovation child records: Ensures cleanup when innovation deleted
  - RESTRICT on master data (Industry): Prevents accidental deletion of reference data
- **EF Core Migration**: Cascade rules enforced via Fluent API in OnModelCreating:
  ```csharp
  modelBuilder.Entity<Bid>()
      .HasOne<Innovation>()
      .WithMany()
      .HasForeignKey(b => b.InnovationId)
      .OnDelete(DeleteBehavior.Cascade);  // DELETE CASCADE
  
  modelBuilder.Entity<Innovation>()
      .HasOne<Actor>()
      .WithMany()
      .HasForeignKey(i => i.OwnerId)
      .OnDelete(DeleteBehavior.Restrict);  // DELETE RESTRICT
  ```

## Operational & Testing Requirements

### CHK114: Email Uniqueness Per Actor Type - UX Clarity
- **Design Decision**: Email is unique PER actor type, not globally unique (Spec §R1.3)
- **Scenario**: User registers `maria@example.com` as IdeaGenerator, later registers same email as Investor
  - Result: Two separate accounts with same email, different ActorType
- **Login UX Flow**:
  1. User enters email: `maria@example.com`
  2. **Frontend checks** (optional optimization): `GET /api/auth/check-email?email=maria@example.com` returns list of registered actor types
  3. If multiple actor types found: Display dropdown "Which account?" (IdeaGenerator | Investor)
  4. User selects actor type
  5. User enters password
  6. POST `/auth/login` with `{ email, actorType, password }` (Contracts §LoginRequest, line 188-193)
- **Rationale**: Domain supports users wearing multiple hats (e.g., professor as IdeaGenerator + Investor via VC fund)
- **No Conflict**: Login endpoint already requires `actorType` parameter - design is internally consistent

### CHK116: JWT Expiration vs Security Best Practices - Already Addressed
- **Resolution**: Fully documented in Plan §Session Management (lines 80-143)
- **Key Points**:
  - 1hr access token expiration is deliberate security trade-off (KISS principle)
  - Client-side logout acceptable for v1.0 scale (100 concurrent users)
  - Implicit invalidation for critical events (account suspension, password change)
  - v2.0 upgrade path documented with RefreshToken entity for server-side revocation
- **Status**: No additional documentation required

### CHK029: Performance Test Requirements (Load & Stress Testing)
- **Phase 0 Target**: 100 concurrent users (Plan §Scale/Scope, line 49)
- **Pre-Production Testing Requirements**:
  
  **1. Load Testing** (sustained usage):
  - **Tool**: JMeter or k6 (open-source load testing)
  - **Scenario**: 100 concurrent users, 30-minute duration
  - **User Flows**:
    - 40% Read-only: GET /api/innovations/{id} (10 req/min per user)
    - 30% Authentication: POST /auth/login followed by innovation reads (1 login + 5 reads per session)
    - 20% Innovation submission: POST /api/innovations (Phase 1+, 1 req/5min per user)
    - 10% Bid submission: POST /api/bids (Phase 1+, 1 req/10min per user)
  - **Success Criteria**:
    - 95th percentile response time ≤ 500ms (authentication, reads)
    - 95th percentile response time ≤ 2000ms (writes - innovation/bid creation)
    - Error rate < 0.1% (exclude 4xx validation errors)
    - CPU utilization < 70% (Azure App Service B1 tier)
    - Memory utilization < 80%
  
  **2. Stress Testing** (breaking point):
  - **Goal**: Identify failure threshold beyond expected load
  - **Ramp-up**: Start at 100 users, increase by 50 users every 5 minutes until errors > 5%
  - **Expected Failure Point**: 200-300 concurrent users (B1 tier App Service limit)
  - **Graceful Degradation Requirements**:
    - API returns 503 Service Unavailable (not internal errors)
    - Application Insights logs saturation warnings
    - Frontend displays: "The platform is experiencing high traffic. Please try again in a few minutes."
  
  **3. Database Connection Pool Testing**:
  - **Configuration**: EF Core connection pool (default: 100 connections)
  - **Test**: Verify no connection exhaustion under 100 concurrent users
  - **Monitoring**: Azure SQL DTU usage should remain < 80% under load

- **Phase 1 Requirements**: Re-run tests after adding innovation list/search endpoints (higher query complexity)

### CHK101: Backup and Recovery Requirements
- **Azure SQL Automated Backups** (managed by Azure, Research §Decision 4):
  - **Full Backups**: Weekly (retained 7-35 days based on tier)
  - **Differential Backups**: Every 12-24 hours (depends on database activity)
  - **Transaction Log Backups**: Every 5-10 minutes (point-in-time restore granularity)
  - **Retention**: Basic tier: 7 days, Standard/Premium: 35 days default (configurable up to 10 years with LTR)
- **Point-in-Time Restore**:
  - Restore database to any point within retention window (e.g., "5 minutes before migration failure")
  - Azure Portal or CLI: `az sql db restore --dest-name Innoventity-Restored --time "2025-01-15T14:30:00Z"`
- **Disaster Recovery Scenarios**:
  
  **Scenario 1: Accidental Data Deletion** (e.g., admin runs DELETE without WHERE clause)
  - **Detection**: Application Insights logs unexpected data loss, user reports missing innovations
  - **Recovery**:
    1. Identify timestamp before deletion (check audit logs)
    2. Restore database to new instance: `Innoventity-PreDelete`
    3. Export affected records: `SELECT * INTO TempRecovery FROM Innoventity-PreDelete.dbo.Innovation WHERE Id IN (...)`
    4. Validate data integrity, re-import to production
    5. Downtime: ~15-30 minutes
  
  **Scenario 2: Corrupted Database** (e.g., failed migration, hardware failure)
  - **Detection**: Database inaccessible, Azure SQL health check fails
  - **Recovery**:
    1. Attempt Azure SQL auto-failover (if geo-replication enabled, Phase 1+ consideration)
    2. If no geo-replica: Restore from latest backup (max 10-minute data loss)
    3. Re-run migrations if corruption from partial migration: `dotnet ef database update`
    4. Downtime: ~10-20 minutes
  
  **Scenario 3: Complete Azure Region Outage**
  - **Phase 0 Strategy**: **Acceptable downtime** (no geo-replication in v1.0 for KISS principle)
  - **Phase 1+ Strategy**: Configure Azure SQL geo-replication to secondary region, auto-failover groups

- **Application Data Exports** (manual safety net):
  - **Frequency**: Weekly (automated pipeline, Phase 1+)
  - **Format**: CSV exports of Actor, Innovation, Bid tables
  - **Storage**: Azure Blob Storage (separate from SQL server, 30-day retention)
  - **Purpose**: Additional recovery layer if Azure SQL backups insufficient

### CHK102: Deployment Rollback Requirements
- **Azure App Service Deployment Slots** (zero-downtime strategy):
  - **Slots**: Production (active), Staging (pre-validation)
  - **Deployment Flow**:
    1. CI/CD deploys new version to Staging slot
    2. Automated smoke tests run against Staging (health check, login test)
    3. Manual validation: QA tests critical user flows
    4. **Swap** Staging ↔ Production (instant switch, ~2 seconds downtime)
  - **Rollback**: Swap Production ↔ Staging (reverts to previous version instantly)
  
- **Database Migration Rollback**:
  - **Challenge**: EF Core migrations are forward-only (no automatic rollback)
  - **Strategy 1: Point-in-Time Restore** (for breaking schema changes):
    - Restore database to state before migration (see CHK101)
    - Re-deploy previous application version
    - Downtime: ~15 minutes
  - **Strategy 2: Compensating Migration** (for data migrations):
    - Write reverse migration manually: `dotnet ef migrations add RevertFeatureX`
    - Apply: `dotnet ef database update`
    - Deploy previous app version
  - **Prevention**: 
    - Always make schema changes **additive** (add nullable columns, not drop)
    - Use feature flags to disable new features without redeployment
    - Test migrations in Staging environment before Production

- **Failure Scenarios**:
  
  **Scenario 1: App Deployment Succeeds but App Crashes**
  - **Detection**: Application Insights alerts (>10% HTTP 500 errors in 5 minutes)
  - **Response**: Automatic rollback via deployment slot swap (2-second downtime)
  
  **Scenario 2: Database Migration Breaks App**
  - **Detection**: Health check endpoint fails (cannot connect to DB)
  - **Response**:
    1. Rollback app deployment (slot swap)
    2. Assess migration impact:
       - Non-breaking: Deploy hotfix migration
       - Breaking: Restore database to pre-migration state (CHK101)
  
  **Scenario 3: Frontend Deployment Incompatible with Backend**
  - **Prevention**: Version API endpoints (`/api/v1/auth/login`) to support gradual migration
  - **Detection**: Frontend Angular app shows network errors, CORS issues
  - **Response**: Roll back frontend deployment to CDN (Azure Static Web Apps has revision history)

### CHK103: Database Migration Failure Recovery Requirements
- **Pre-Migration Validation** (prevent failures):
  - **Local Testing**: Always test migrations against copy of Production data (anonymized)
  - **Staging Environment**: Run `dotnet ef database update` on Staging database first
  - **Backup Verification**: Confirm latest backup exists before Production migration
  - **Review Checklist**:
    - [ ] Migration adds only nullable columns OR provides default values
    - [ ] No `DROP COLUMN` operations (use feature flags to hide columns instead)
    - [ ] Foreign key constraints have appropriate ON DELETE behavior
    - [ ] Check constraints are not overly restrictive (allow existing data)
    - [ ] Indexes added on large tables (may take >5 minutes, plan maintenance window)

- **Failure Scenarios & Recovery**:
  
  **Scenario 1: Migration Timeout** (e.g., adding index on 1M+ row table)
  - **Symptom**: `dotnet ef database update` hangs or times out
  - **Recovery**:
    1. Cancel migration (may take minutes to rollback)
    2. Check database state: `SELECT * FROM __EFMigrationsHistory` (last applied migration)
    3. If migration partially applied: Manually complete SQL operations
    4. If migration rolled back: Increase timeout (`Database.SetCommandTimeout(600)`) and retry
  
  **Scenario 2: Constraint Violation** (e.g., adding NOT NULL column with existing NULL values)
  - **Symptom**: Migration fails with constraint violation error
  - **Recovery**:
    1. Migration auto-rolls back (EF Core transaction)
    2. Fix data: `UPDATE Actor SET FullName = 'Unknown' WHERE FullName IS NULL`
    3. Retry migration
  
  **Scenario 3: Foreign Key Mismatch** (orphaned records prevent constraint creation)
  - **Symptom**: `ALTER TABLE Bid ADD CONSTRAINT FK_Bid_Innovation FOREIGN KEY... failed`
  - **Recovery**:
    1. Identify orphaned records: `SELECT * FROM Bid WHERE InnovationId NOT IN (SELECT Id FROM Innovation)`
    2. Decision: Delete orphans OR create placeholder Innovation records
    3. Retry migration
  
  **Scenario 4: Production Migration Breaks App** (cannot rollback schema)
  - **Symptom**: App crashes on startup (EF Core model mismatch with database schema)
  - **Recovery Steps**:
    1. **Immediate**: Roll back app deployment (slot swap) - restores previous app version
    2. **Assess schema state**:
       - If migration completed: Deploy hotfix app version compatible with new schema
       - If migration failed midway: Manually fix partial migration OR restore database (CHK101)
    3. **Validate**: Run integration tests against Production database
    4. **Re-deploy**: Fixed app version to Production
  
  **Scenario 5: Complete Database Corruption from Failed Migration**
  - **Last Resort**: Point-in-time restore to pre-migration state (CHK101)
  - **Data Loss**: Max 10 minutes (transaction log backup frequency)
  - **Downtime**: ~20 minutes (restore + re-deploy previous app version)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Principle 1: User Experience First ✅

**Assessment**: PASS  
**Evidence**:
- Specification focuses on user needs and pain points for all 5 personas
- Error scenarios documented with user-facing messages and recovery paths
- Performance goals user-centric (page load <2s, API response <200ms)
- Phase 0 proves end-to-end user journey (registration → authentication → view innovation)

**Architecture Alignment**:
- Angular Material provides consistent, accessible UI components
- Frontend signals enable responsive, performant UI updates
- Error messages defined in specification (clear, actionable, non-technical)

---

### Principle 2: Quality is Non-Negotiable ✅

**Assessment**: PASS  
**Evidence**:
- Test-first development mandated (TDD: red → green → refactor)
- Coverage targets: >80% unit, 100% integration, P0 journeys E2E
- Mutation testing (>70% score) validates test quality  
- Security from day one: JWT authentication, bcrypt passwords, Azure Key Vault for secrets
- Production-ready from Phase 0 (no "prototype" mindset)

**Architecture Alignment**:
- xUnit for unit tests with test data builders
- WebApplicationFactory for integration testing
- Playwright for E2E testing of critical user journeys
- Stryker.NET for mutation testing
- Azure Application Insights for monitoring and observability

---

### Principle 3: Simplicity Over Cleverness ✅

**Assessment**: PASS  
**Evidence**:
- Standard technology choices: ASP.NET Core Minimal APIs, EF Core, Angular (Microsoft template)
- Vertical Slice Architecture (feature-oriented, simple to navigate)
- Phase 0 minimized to registration + authentication + single innovation view
- No premature optimization or clever abstractions

**Architecture Alignment**:
- Use frameworks as intended (ASP.NET Core, Angular) without creative customization
- Repository abstraction simple: enables v2.0 multi-tenancy without over-engineering v1.0
- Direct EF Core queries in slices (no complex ORM abstractions)

---

### Principle 4: Specification Drives Implementation ✅

**Assessment**: PASS  
**Evidence**:
- Functional specification approved before technical design (95.5/100 constitutional compliance)
- Specification contains zero implementation details (functional requirements only)
- PLAN phase follows SPECIFY phase per Spec-Kit workflow
- Acceptance tests written in Given/When/Then format in specification

**Architecture Alignment**:
- Technical decisions deferred to this PLAN phase
- API contracts will be generated from functional requirements in Phase 1
- Data model will be extracted from entities/relationships in Phase 1

---

###Principle 5: Tests Must Prove They Work ✅

**Assessment**: PASS  
**Evidence**:
- TDD workflow enforced: observe test failing before implementation
- Critical business logic (partner selection, authorization, bid validation) requires human-written tests
- Mutation testing validates tests actually catch bugs (>70% score target)  
- Phase 0 testing strategy: unit → integration → E2E progression

**Architecture Alignment**:
- Test pyramid: unit tests for business logic, integration tests for API endpoints, E2E for critical journeys
- Test data builders for readable, maintainable test setup
- Characterization testing for existing code (if needed)

---

### Principle 6: AI Augments, Humans Decide ✅

**Assessment**: PASS  
**Evidence**:
- Architectural decisions made by human (this PLAN phase)
- Critical business logic to be human-implemented:
  - Partner selection (irreversible, exactly-one-per-type rule)
  - Authorization policies (resource ownership, role-based access)
  - Business plan calculations (financial projections)
  - State transitions (innovation workflow states)
- AI can generate boilerplate (DTOs, validators, scaffolding)

**Architecture Alignment**:
- Domain model design: human
- Authorization policies: human
- Critical business rules: human-implemented, human-tested
- Boilerplate DTOs, mappers: AI-assisted, human-validated

---

### Principle 7: Architecture Must Support Evolution ✅

**Assessment**: PASS  
**Evidence**:
- v2.0 features explicitly deferred (multi-tenancy, organizations, closed/hybrid modes)
- Repository abstractions enable v2.0 tenant filtering without rewrite
- Policy-based authorization can extend with organization policies
- Actor extensibility via inheritance allows new types (Government, Technology Parks)

**Architecture Alignment**:
- Repository pattern: `IInnovationRepository` abstracts data access (can add `WHERE TenantId = @tenantId` in v2.0)
- Authorization policies: `IAuthorizationHandler` implementations composable (can add `OrganizationMemberHandler` in v2.0)
- Actor types: Base `Actor` entity with inheritance (R&D, Manufacturing, Sales, Investor, Idea Generator) extendable
- Avoid anti-patterns: No hardcoded `WHERE OrganizationId IS NULL`, no actor-type switch statements

---

### Gate Summary

**Status**: ✅ ALL GATES PASS - PROCEED TO PHASE 0

| Principle | Status | Notes |
|-----------|--------|-------|
| P1: User Experience First | ✅ PASS | User-centric design, error handling comprehensive |
| P2: Quality Non-Negotiable | ✅ PASS | Test-first, production-ready from Phase 0 |
| P3: Simplicity Over Cleverness | ✅ PASS | Standard tech stack, minimal Phase 0 scope |
| P4: Specification Drives Implementation | ✅ PASS | Functional spec approved before technical design |
| P5: Tests Must Prove They Work | ✅ PASS | TDD enforced, mutation testing validates quality |
| P6: AI Augments, Humans Decide | ✅ PASS | Critical business logic human-designed/implemented |
| P7: Architecture Supports Evolution | ✅ PASS | Repository/policy patterns enable v2.0 extensibility |

**No violations requiring justification.**

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
# Web application structure (ASP.NET Core + Angular)

# Backend (ASP.NET Core 8 Minimal APIs)
src/Innoventity.API/
├── Features/                  # Vertical slices organized by feature
│   ├── Authentication/       # Phase 0: JWT auth, registration, activation
│   ├── Innovations/          # Innovation submission, publication, retrieval
│   ├── Bids/                 # Bid submission, management
│   ├── PartnerSelection/     # Partner evaluation and selection
│   └── VirtualIncubator/     # Business plan collaboration workspace
├── Domain/                    # Shared domain entities
│   ├── Entities/             # EF Core entities (Innovation, Actor, Bid, etc.)
│   ├── ValueObjects/         # Immutable value types
│   └── Interfaces/           # Repository abstractions, policy interfaces
├── Infrastructure/            # Cross-cutting concerns
│   ├── Persistence/          # EF Core DbContext, migrations
│   ├── Authentication/       # JWT token generation/validation
│   ├── Authorization/        # Policy handlers, requirements
│   └── Messaging/            # Azure Service Bus integration
└── Program.cs                # Minimal API endpoint registration

# Frontend (Angular 18 Standalone Components)
src/Innoventity.Client/
├── src/
│   ├── app/
│   │   ├── features/         # Feature modules (standalone components)
│   │   │   ├── auth/         # Login, registration, activation
│   │   │   ├── innovations/  # Innovation submission, browse, view
│   │   │   ├── bids/         # Bid submission, management
│   │   │   ├── selection/    # Partner selection workflow
│   │   │   └── incubator/    # Virtual incubator workspace
│   │   ├── core/             # Shared services, interceptors, guards
│   │   ├── shared/           # Reusable components, pipes, directives
│   │   └── app.component.ts  # Root component
│   ├── environments/         # Environment-specific config
│   └── main.ts               # Bootstrap

# Testing
tests/Innoventity.API.Tests/
├── Unit/                      # Unit tests for business logic
│   ├── Features/             # Feature-specific unit tests
│   ├── Domain/               # Entity/value object tests
│   └── Builders/             # Test data builders
├── Integration/               # API integration tests
│   ├── Features/             # Feature endpoint tests  
│   └── Infrastructure/       # WebApplicationFactory setup
└── E2E/                       # End-to-end tests (Playwright)
    ├── Journeys/             # User journey tests (J1-J3 priority)
    └── Support/              # Page objects, helpers

tests/Innoventity.Client.Tests/
├── unit/                      # Angular unit tests (Jasmine/Karma)
└── e2e/                       # Client-side E2E tests (Playwright)

# Infrastructure as Code (future)
infra/
└── bicep/                     # Azure resource definitions
```

**Structure Decision**: 
- **Backend**: Vertical Slice Architecture organized by feature (each feature = cohesive slice with its own APIs, logic, persistence)
- **Frontend**: Angular standalone components grouped by feature area
- **Testing**: Separate test project mirroring backend structure (Unit/Integration/E2E)
- **Rationale**: 
  - Vertical slices reduce coupling, improve maintainability
  - Feature-oriented structure aligns with Spec-Kit workflow (one spec = one or more feature slices)
  - Repository abstractions in Domain layer enable v2.0 multi-tenancy without rewrite

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
