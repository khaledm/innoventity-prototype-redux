# Requirements Quality Checklist: Web Application (Backend API + Frontend SPA)

**Feature**: Platform Core (v1.0)
**Domain**: Open Innovation Platform - Full-Stack Web Application
**Purpose**: Validate requirements completeness, clarity, consistency, and measurability
**Created**: February 8, 2026
**Audience**: Author (self-review)
**Depth**: Comprehensive (50+ requirement quality checks)

---

## Checklist Purpose

This checklist acts as **unit tests for requirements writing** — validating that the specification, plan, data model, and API contracts are complete, clear, consistent, and implementable. Each item tests the QUALITY of requirements documentation, NOT implementation behavior.

**What This Checklist Tests**:

- ✅ Are requirements clearly specified and unambiguous?
- ✅ Do requirements cover all necessary scenarios?
- ✅ Can requirements be objectively verified?
- ✅ Are requirements consistent across documents?

**What This Checklist Does NOT Test**:

- ❌ Whether code works correctly (that's QA testing)
- ❌ Whether implementation matches spec (that's verification)
- ❌ Whether buttons click or APIs respond (that's functional testing)

---

## Resolution Summary

**Date Updated**: February 9, 2026 (Phase 3 User Story 1 Complete)
**Purpose**: Document how identified gaps were addressed before and during implementation

**Phase 3 Implementation Updates** (February 9, 2026):

- ✅ CHK007-009, CHK011: Actor entity implemented with validation rules, composite unique index (Email+ActorType), audit timestamp fields
- ✅ CHK016: Account activation (R1.1) fully implemented with PendingActivation→Active status transition, token validation, integration tests
- ✅ CHK046: Actor registration requirements (R1) align with authentication flow (J1 Steps 1a-1b)
- ✅ CHK049: Email activation requirement objectively verified with integration tests T022-T023
- ✅ CHK058-060: User journey J1 Steps 1a-1b fully implemented with success outcomes and API endpoint mapping
- ✅ CHK121: Business rules R1.1, R1.3, R8.4 traceable to data model constraints (AccountStatus enum, composite unique index, BCrypt work factor 12)
- ✅ CHK125: API endpoints POST /auth/register and POST /auth/activate documented in OpenAPI spec with examples

**Implementation Evidence**:

- 24/24 tests passing (7 Actor unit tests, 6 PasswordHasher unit tests, 4 Registration integration tests, 3 Activation integration tests, 4 Health endpoint tests)
- Migrations: AddActorEntity (composite unique index), RemoveAccountStatusDefault
- TDD discipline maintained: Red→Green→Refactor cycle followed throughout Phase 3

### High-Priority Gaps Resolved

**1. Security Requirements (CHK088-093)** - ✅ COMPLETE
**Resolution**: Added comprehensive "Security Requirements" section in [plan.md](../plan.md) (lines 145-200) + Functional Requirements in [spec.md](../spec.md) §7 FR7.5 with acceptance criteria
**Coverage**:

- CHK088: Password hashing (BCrypt work factor 12) - *Spec §R8.4, Plan §Security Requirements, Spec §7 FR7.5*
- CHK089: JWT signing key management (HS256, 256-bit, App Service Configuration) - *Plan §CHK089, Spec §7 FR7.5*
- CHK090: HTTPS enforcement (production only, HSTS 1-year) - *Plan §CHK090, Spec §7 FR7.5*
- CHK091: SQL injection prevention (EF Core parameterized queries) - *Plan §CHK091, Spec §7 FR7.5*
- CHK092: CORS policy (specific origins, no wildcards) - *Plan §CHK092, Spec §7 FR7.5*
- CHK093: Secret management (App Service Configuration) - *Plan §CHK093, Spec §7 FR7.5*

**2. Frontend/UX Requirements (CHK019-024)** - ✅ COMPLETE
**Resolution**: Added "Frontend/UX Requirements" section in [plan.md](../plan.md) (lines 202-280)
**Coverage**:

- CHK019: Loading states (mat-spinner, 300ms minimum, skeleton screens) - *Plan §CHK019*
- CHK020: Error message content (inline, banner, ProblemDetails mapping) - *Plan §CHK020*
- CHK021: Form validation (duplicate client/server, DRY violation accepted) - *Plan §CHK021*
- CHK022: Accessibility (WCAG 2.1 AA, 4.5:1 contrast, Lighthouse ≥90) - *Plan §CHK022*
- CHK023: Responsive design (5 breakpoints, mobile-first, 44x44px touch targets) - *Plan §CHK023*
- CHK024: Navigation (AuthGuard/RoleGuard, routerLinkActive, breadcrumbs) - *Plan §CHK024*

**3. Exception Flow Coverage (CHK065-073)** - ✅ COMPLETE
**Resolution**: Added "Exception Flow Coverage" section in [plan.md](../plan.md) (lines 282-430)
**Coverage**:

- CHK065: Duplicate email on registration (400 with ValidationProblemDetails) - *Plan §CHK065*
- CHK066: Invalid activation token (400, resend flow) - *Plan §CHK066*
- CHK067: Unactivated login (401, Contracts notActivated example) - *Plan §CHK067*
- CHK068: Unauthorized access (Phase 0: N/A, Phase 1+: 403) - *Plan §CHK068*
- CHK069: Innovation not found (404 with ProblemDetails) - *Plan §CHK069*
- CHK070: Database/network errors (500, generic message) - *Plan §CHK070*
- CHK071: Resend activation email (Gap: Deferred to Phase 1) - *Plan §CHK071*
- CHK072: Refresh token expiry (400, redirect to login) - *Plan §CHK072*
- CHK073: Form validation recovery (inline errors, dynamic clearing) - *Plan §CHK073*

**4. Edge Case Requirements (CHK074-083)** - ✅ COMPLETE
**Resolution**: Added "Edge Case Requirements" section in [plan.md](../plan.md) (lines 432-606)
**Coverage**:

- CHK074: Zero-state scenarios (Phase 0: N/A, Phase 1: empty state UI) - *Plan §CHK074*
- CHK075: Minimum bid threshold (1 bid per category sufficient) - *Plan §CHK075*
- CHK076: Max concurrent bids (Phase 0: Uncapped, Phase 1: Rate limit) - *Plan §CHK076*
- CHK077: String length validations (comprehensive table in Data Model) - *Plan §CHK077*
- CHK078: Null vs empty string (nullable use `string?`, reject empty "") - *Plan §CHK078*
- CHK079: Concurrent bid submissions (UNIQUE constraint, Read Committed) - *Plan §CHK079*
- CHK080: Concurrent partner selection (Phase 0: N/A, Phase 1: Optimistic concurrency) - *Plan §CHK080*
- CHK081: Transaction isolation (Read Committed default, explicit for Phase 1) - *Plan §CHK081*
- CHK082: Orphaned records (ON DELETE RESTRICT, prefer suspension) - *Plan §CHK082*
- CHK083: Referential integrity (comprehensive cascade rules documented) - *Plan §CHK083*

### Medium-Priority Items Resolved

**5. Operational & Testing Requirements (CHK014, CHK029, CHK097-100, CHK101-103, CHK114, CHK116)** - ✅ COMPLETE
**Resolution**: Added "Operational & Testing Requirements" section in [plan.md](../plan.md) (lines 608-785) + Infrastructure & Operational Requirements in [spec.md](../spec.md) §7 (FR7.5-FR7.8)
**Coverage**:

- CHK014: Token expiration durations (1hr access, 7d refresh) - *Quickstart §Configuration, Plan §Session Management, Contracts /auth/refresh-token* ✅ **NEW RESOLUTION**
- CHK029: Performance test requirements (JMeter load/stress tests, 100 users) - *Plan §CHK029*
- CHK097: Structured logging requirements (Correlation IDs, 30-90 day retention) - *Research §Decision 9, Spec §7 FR7.6* ✅ **UPDATED**
- CHK098: Monitoring alert thresholds (error rate >1%, p95 <200ms) - *Research §Decision 9, Spec §7 FR7.6* ✅ **UPDATED**
- CHK099: Distributed tracing (Application Insights integration) - *Spec §7 FR7.6* ✅ **UPDATED**
- CHK100: Metrics collection (Application Insights) - *Plan §Technical Context, Spec §7 FR7.6* ✅ **UPDATED**
- CHK101: Backup/recovery (Out of scope - ephemeral infrastructure approach) - *Architecture simplification decision* ✅ **UPDATED**
- CHK102: Deployment rollback (Slot swaps, migration rollback strategies) - *Plan §CHK102, Spec §7 FR7.8* ✅ **UPDATED**
- CHK103: Migration failure recovery (Pre-validation checklist, 5 failure scenarios) - *Plan §CHK103, Spec §7 FR7.8* ✅ **UPDATED**
- CHK114: Email per actor type vs single login (No conflict, login requires actorType) - *Plan §CHK114*
- CHK116: JWT expiration security (Already addressed in Session Management) - *Plan §CHK116*

### Additional Resolutions

**6. Password Complexity Quantification (CHK013)** - ✅ COMPLETE
**Resolution**: Updated [spec.md](../spec.md) R8.4 Account Security (lines 1079-1088)
**Details**: Measurable criteria added (8+ chars, uppercase, lowercase, digit, special char, max 128, lockout after 5 failures)

**7. API Contract Validation (CHK004-005)** - ✅ COMPLETE
**Investigation Date**: February 2026
**Scope**: Validated all 5 Phase 0 API endpoints in [contracts/openapi.yaml](../contracts/openapi.yaml)

**CHK004 - Error Response Schema Completeness** - ✅ COMPLETE:

- **POST /auth/register**: ✅ Complete (201, 400 ValidationProblemDetails with examples, 500)
- **POST /auth/activate**: ✅ Complete (200, 400 ProblemDetails with examples, 500)
- **POST /auth/login**: ✅ Complete (200, 400 ValidationProblemDetails, 401, 500)
- **POST /auth/refresh-token**: ✅ Complete (200, 400 ValidationProblemDetails, 401, 500)
- **GET /innovations/{id}**: ✅ Complete (200, 401 Unauthorized reference, 404 ProblemDetails with example, 500)

**Verification**: `contracts/openapi.yaml` includes 400 `ValidationProblemDetails` responses for both `/auth/login` and `/auth/refresh-token`, closing the remaining CHK004 gaps.

**CHK005 - Authentication Requirements Specification** - ✅ COMPLETE:

- **Global security scheme**: BearerAuth defined with JWT description ✅
- **POST /auth/register**: security: [] (explicitly no auth) ✅
- **POST /auth/activate**: security: [] (explicitly no auth) ✅
- **POST /auth/login**: security: [] (explicitly no auth) ✅
- **POST /auth/refresh-token**: security: [] (explicitly no auth) ✅
- **GET /innovations/{id}**: Uses global security: BearerAuth + description "Requires authentication" ✅

**Conclusion**: Authentication requirements and error response schemas are fully specified for all Phase 0 authentication endpoints.

**8. Cross-Document Consistency Validation (CHK041-045)** - ✅ ALL ITEMS VALIDATED
**Investigation Date**: February 2026
**Scope**: Systematic comparison of [spec.md](../spec.md) functional requirements, [data-model.md](../data-model.md) entity schemas, and [contracts/openapi.yaml](../contracts/openapi.yaml) API contracts

**CHK041 - API Contract Schemas Match Data Model** - ✅ VALIDATED:

- **IdeaSummary Schema** (contracts lines 588-618) vs **Innovation Entity** (data-model lines 219-259):
  - Contracts expose 6 fields: title, productType, researchBackground, researchCategory, hasIPR, iprDetails
  - Data model defines 25+ fields including product details, market details, collaboration requirements, lifecycle status
  - **Design Decision**: Contracts expose Phase 0-scoped subset for innovation viewing; full entity used for future phases (draft creation, editing)
  - **Validation**: Field types, constraints (minLength/maxLength), and required status align correctly ✅

- **Actor Schemas** (RegisterActorRequest, ActorInfo) vs **Actor Entity** (data-model lines 138-175):
  - RegisterActorRequest: email, fullName, contactAddress, actorType, password
  - ActorInfo: actorId, fullName, actorType
  - Actor entity: Id, Email, FullName, ContactAddress, ActorType, AccountStatus, ActivationToken, PasswordHash, CreatedAt, UpdatedAt
  - **Security Validation**: PasswordHash never exposed in API responses (R8.3 compliance) ✅
  - **Internal Fields**: ActivationToken, AccountStatus, timestamps omitted from public responses (security best practice) ✅

**CHK042 - User Journey Steps Align with API Endpoints** - ✅ VALIDATED:

- **Journey 1 (Innovation Submission & Publication)** - spec.md lines 235-323:
  - J1 Step 1a: Account Creation → POST /auth/register ✅
  - J1 Step 1b: Email Activation → POST /auth/activate ✅
  - J1 Step 1c: Login (implicit) → POST /auth/login ✅
  - J1 Steps 2-7: Draft creation, editing, submission → **NOT IN PHASE 0** (deferred to Phase 1)

- **Journey 2 (Innovation Discovery & Bid Submission)** - spec.md lines 324-402:
  - J2 Step 1: Account Creation → POST /auth/register ✅ (same as J1)
  - J2 Step 3: View Innovation Details → GET /innovations/{id} ✅
  - J2 Steps 4-9: Bid submission → **NOT IN PHASE 0** (deferred to Phase 1)

- **Token Management** (implicit in both journeys):
  - Token refresh → POST /auth/refresh-token ✅

**Conclusion**: All Phase 0-scoped journey steps have corresponding API endpoints. Steps beyond Phase 0 scope correctly excluded from contracts.

**CHK043 - Business Rules Map to Database Constraints & API Validation** - ✅ VALIDATED:

- **R1.1 (Email activation required)**:
  - Data Model: AccountStatus enum (PendingActivation, Active, Suspended) ✅
  - Contracts: POST /auth/activate endpoint + PendingActivation status in RegisterActorResponse ✅
  - Journey: J1 Step 1 describes activation flow ✅

- **R1.3 (Email unique per ActorType)**:
  - Data Model: Unique index IX_Actor_Email_ActorType (lines 431-438) ✅
  - Contracts: 400 ValidationProblemDetails with "Email must be unique per ActorType" example ✅
  - Spec: Business rule documented (R1.3) ✅

- **R2.1 (Innovation validation constraints)**:
  - Data Model: Title non-default constraint, research background required ✅
  - Contracts: IdeaSummary minLength: 10 for title, minLength: 50 for researchBackground ✅
  - Spec: Validation requirements in R2.1 ✅

- **R8.3 (PasswordHash never exposed)**:
  - Data Model: PasswordHash field stored, BCrypt hashing ✅
  - Contracts: No password fields in any response schema (RegisterActorResponse, LoginResponse, ActorInfo) ✅
  - Spec: Security requirement R8.3 documented ✅

- **R8.4 (Password complexity)**:
  - Data Model: Enforced before hashing (application validation) ✅
  - Contracts: 400 ValidationProblemDetails example shows complexity error messages ✅
  - Spec: Measurable criteria in R8.4 (8+ chars, uppercase, lowercase, digit, special char) ✅

**Phase 1+ Rules**: R4 (bid rules), R5 (partner selection), R6 (workspace access), R7 (business plan) not yet in API contracts - correctly deferred.

**CHK044 - Actor Type Enums Consistent** - ✅ CONSISTENT:

- **Data Model** (lines 398-408): ActorType enum = IdeaGenerator, RD, Manufacturing, SalesMarketing, Investor
- **Contracts** (lines 655-667): ActorType enum = IdeaGenerator, RD, Manufacturing, SalesMarketing, Investor
- **Spec** (line 106): Uses "RDOrganization" as display name/persona label
- **Analysis**: "RD" is technical enum value, "RDOrganization" is full display name in persona descriptions - **NOT AN INCONSISTENCY** ✅
- **Other Enums Validated**:
  - AccountStatus (3 values): PendingActivation, Active, Suspended - consistent across all documents ✅
  - ResearchCategory (3 values): Management, Engineering, NaturalScience - consistent across all documents ✅
  - BidType (4 values): Only in data-model (not in Phase 0 contracts) - expected for future phases ✅

**CHK045 - Innovation Status Values Consistent** - ✅ CONSISTENT:

- **Data Model** (lines 416-427): InnovationStatus enum = Draft, AwaitingBids, ReceivingBids, SufficientBids, PartnersSelected, BusinessPlanInProgress, BusinessPlanComplete (7 values)
- **Contracts** (lines 702-717): InnovationStatus enum = Draft, AwaitingBids, ReceivingBids, SufficientBids, PartnersSelected, BusinessPlanInProgress, BusinessPlanComplete (7 values) with state machine descriptions
- **Spec**: Status transitions documented in R2.2, J1 (innovation submission), J2 (bid reception), J3 (partner selection), J4 (business plan collaboration)
- **Usage Verified**: 20+ references to "Draft" status in spec.md user journeys, acceptance tests, success metrics ✅

**Conclusion**: All 5 cross-document consistency checks PASSED. Specifications are aligned and ready for implementation. No schema mismatches, enum inconsistencies, or journey-endpoint gaps identified.

**9. Vague Terms Quantification Validation (CHK030-040)** - ✅ 9 RESOLVED | ⚠️ 1 PARTIAL
**Investigation Date**: February 2026
**Scope**: Validated clarity of potentially ambiguous terms across [spec.md](../spec.md), [plan.md](../plan.md), and [data-model.md](../data-model.md)

**CHK030 - "Fast" Response Time Quantification** - ✅ RESOLVED:

- **Plan §Performance Goals** (lines 35-39) provides measurable metrics:
  - API response time p95: <200ms ✅
  - Database query p95: <100ms ✅
  - Page load (First Contentful Paint) p95: <2s ✅
  - Real-time notification delivery: <1s latency ✅
- **Additional Context**: Plan line 727-728 specifies 95th percentile ≤500ms for authentication/reads, ≤2000ms for writes
- **Conclusion**: "Fast" quantified with industry-standard percentile-based metrics

**CHK031 - "Production-Ready" Definition** - ⚠️ PARTIAL:

- **Plan §Constraints** (lines 43, 910) mentions "Production-ready from Phase 0 (no 'prototype' quality)" but lacks measurable acceptance criteria
- **What's Missing**:
  - Security checklist (HTTPS, secrets management, SQL injection prevention) - partially covered in CHK088-093 ✅
  - Test coverage thresholds (unit/integration/E2E minimum coverage %)
  - Performance benchmarks must be met before release
  - Observability requirements (logging, monitoring, tracing) - covered in Spec §7 FR7.6 ✅
  - Deployment automation requirements - covered in Spec §7 FR7.1, FR7.8 ✅
- **Recommendation**: Define "production-ready" checklist in plan.md with pass/fail criteria before TASKS phase
- **Impact**: MEDIUM - Team knows intent (no shortcuts), but explicit checklist prevents scope creep debates

**CHK032 - "Sufficient Bids" Quantification** - ✅ RESOLVED:

- **Spec R5.5** (lines 942-964) explicitly defines threshold:
  - ≥1 Manufacturing bid ✅
  - ≥1 Sales/Marketing bid ✅
  - ≥1 R&D bid ✅
  - Investor bid OPTIONAL (not required for threshold) ✅
- **Gherkin Acceptance Test** validates enforcement (lines 951-964)
- **User-Facing Message**: "Your innovation has received sufficient bids. You can now select partners."
- **Conclusion**: Completely quantified with clear business rules

**CHK033 - "Qualified Actor" Definition** - ✅ RESOLVED:

- **Spec §2 Personas** (lines 69-209) describe actor capabilities and needs but don't define measurable "qualification" criteria
- **Usage Context**: Line 73 mentions "qualified partnership proposals"
- **What Exists**: Actor type taxonomy (IdeaGenerator, RD, Manufacturing, SalesMarketing, Investor) defines roles ✅
- **Design Decision (Confirmed February 2026)**:
  - No credential verification or pre-approval required for actors ✅
  - Platform is intentionally permissive - any registered actor can submit bids
  - Bid quality evaluation delegated to innovation owner's judgment (R4.2 requires 200+ char description for context)
  - "Qualified" means "appropriately typed actor who submits detailed proposal" not "pre-verified credentials"
- **Rationale**:
  - Reduces registration friction (no lengthy verification process)
  - Innovation owners are domain experts best positioned to evaluate bid quality
  - Phase 0 scope prioritizes marketplace creation over gatekeeping
  - Future reputation/rating system (Phase 2+) can provide quality signals if needed
- **Conclusion**: No credential verification requirements needed - design decision finalized ✅

**CHK034 - "Active Account" Status Definition** - ✅ RESOLVED:

- **Data Model §Enums** (lines 409-415) defines AccountStatus: PendingActivation, Active, Suspended
- **Spec R1.1** (lines 657-678) specifies activation flow:
  - New accounts start: PendingActivation ✅
  - After email activation: Active ✅
  - Admin action: Suspended ✅
- **Business Rule**: Only Active accounts can submit innovations/bids (line 662)
- **Cross-Validation**: CHK044 confirmed AccountStatus enum consistent across all documents ✅
- **Conclusion**: "Active account" clearly defined as AccountStatus = Active (distinct from PendingActivation and Suspended)

**CHK035 - "Critical User Journeys" Identification** - ✅ RESOLVED:

- **Plan §Technical Context** (line 28) explicitly identifies: "E2E: Playwright for critical user journeys (J1-J3)"
- **Spec §6 Critical Path Testing** (lines 1323-1341) defines priority:
  - ✅ **Journey 1**: Innovation Submission & Publication (Idea Generator) - CRITICAL
  - ✅ **Journey 2**: Innovation Discovery & Bid Submission (Non-Idea-Generator Actors) - CRITICAL
  - ✅ **Journey 3**: Partner Evaluation & Selection (Idea Generator) - CRITICAL
  - 🟡 **Journey 4**: Virtual Incubator Collaboration (Multi-Party) - Important but can use integration tests if E2E resources constrained
  - 🟡 **Journey 5**: Actor Profile & Industry Affiliation Management - Important but can use integration tests
- **Test Organization**: Plan line 1086 shows folder structure: `Journeys/ # User journey tests (J1-J3 priority)`
- **Conclusion**: Critical journeys clearly identified and prioritized for E2E testing

**CHK036 - "Innovation Publication" Trigger Definition** - ✅ RESOLVED:

- **Spec R2.1** (lines 726-748) defines validation requirements and submission action
- **Spec J1 Step 7** (lines 285-299) specifies explicit trigger:
  1. User clicks "Submit for publication" button ✅
  2. System validates all sections complete (Idea Summary, Product Details, Market Details, ≥1 target industry, IPR declaration, Right-to-use confirmed) ✅
  3. Submission timestamp recorded ✅
  4. Innovation becomes visible to ecosystem actors ✅
  5. Status changes to AwaitingBids (from Draft) ✅
- **Error Handling**: Incomplete sections prevent submission with specific error messages (line 746)
- **Spec R2.2**: Only owner can submit (line 751)
- **Conclusion**: Publication trigger explicitly defined as user-initiated submit action with comprehensive validation gate

**CHK037 - "Collaboration Requirements" Validation Rules** - ✅ RESOLVED:

- **Spec J1 Step 6** (lines 277-283) lists collaboration requirements section:
  - Actor types selection: R&D Organization, Manufacturing Company, Sales & Marketing Company, Investor ✅
- **Spec R2.1 Market Details** (line 273, 292): "≥1 target industry selected" explicitly quantified ✅
- **Spec R5.5** (lines 942-950): Sufficient bids threshold enforces ≥1 bid per required actor type ✅
- **Implicit Validation**: Journey step shows ≥1 actor type must be marked as "needed" for collaboration (owner selects from 4 options)
- **Phase 0 Scope**: Innovation viewing (GET /innovations/{id}) doesn't expose collaboration requirements in Phase 0 contracts - deferred to Phase 1 draft creation
- **Conclusion**: Validation rule is "at least one actor type must be specified as needed" - documented in journey workflow

**CHK038 - "Irreversible Partner Selection" Mechanism** - ✅ RESOLVED:

- **Spec R5.3** (lines 912-930) explicitly specifies irreversibility mechanism:
  - Selection is PERMANENT and cannot be undone ✅
  - Confirmation prompt required: "Partner selection is PERMANENT and cannot be undone. Selected partners: [list]. Are you sure you want to proceed?" ✅
  - Error on retry: "Partner selection has been completed and cannot be changed" ✅
  - Gherkin acceptance test validates enforcement (lines 918-930) ✅
- **Journey J3 Step 4** (line 442): "**This operation is IRREVERSIBLE**" emphasized in journey description
- **Error Scenario J3** (lines 482-484): Hesitation before selection triggers confirmation prompt
- **Business Logic Validation**: CHK115 investigated potential conflict with optional business plan (R7.1) - NO CONFLICT, both requirements coexist logically ✅
- **Conclusion**: Irreversibility mechanism thoroughly specified with UI warnings, confirmation flows, technical enforcement, and acceptance tests

**CHK039 - "Virtual Incubator Formation" Trigger** - ✅ RESOLVED:

- **Spec J3 Step 5** (lines 456-462) defines trigger:
  - User confirms partner selection (irreversible action) →
  - "Virtual Incubator Formation" occurs →
  - Virtual incubator team formed with committed partners ✅
- **Spec R6.1 Acceptance Test** (lines 903-909) shows state transition:
  - `Given Idea Generator selects Manufacturing A, Sales B, R&D C` →
  - `When the selection is confirmed` →
  - `Then the selected bids are marked accepted` →
  - `And the virtual incubator workspace is created` ✅
- **Innovation Status Transition**: InnovationStatus enum (Data Model lines 416-427) shows progression: PartnersSelected → BusinessPlanInProgress (implicit workspace active)
- **Access Control**: R6.1 (lines 970-996) defines workspace access restrictions (owner + selected partners only)
- **Conclusion**: Formation trigger explicitly defined as partner selection confirmation → workspace creation + status transition

**CHK040 - Email Notification Content Requirements** - ✅ RESOLVED:

- **Templates created at** `specs/001-platform-core/email-templates/` (adapted from legacy app originals):
  - `Account Activation.txt` - Activation email with link ✅
  - `Welcome Email upon Activation - IdeaAuthor.txt` - IdeaGenerator onboarding ✅
  - `Welcome Email upon Activation - R&D.txt` - R&D organization onboarding ✅
  - `Welcome Email upon Activation - Mfg.txt` - Manufacturing company onboarding ✅
  - `Welcome Email upon Activation - Sales&Mktg.txt` - Sales & Marketing onboarding ✅
  - `Welcome Email upon Activation - Investor.txt` - Investor onboarding ✅
- **Content Structure Provided**:
  - Subject lines: "Account Activation", "Welcome Email upon Activation - [ActorType]" ✅
  - Body structure: Personalized greeting ("Dear [Name]"), instructional content, call-to-action links, closing signature ("Innoventity team") ✅
  - Activation link format: `http://www.innovemtuty.com/activateaccount/{token}` ✅
  - Tone: Professional, encouraging, process-oriented ✅
  - Actor-specific onboarding: Tailored instructions for each actor type's workflow ✅
- **Adaptation Required for Phase 0**:
  - Remove monetization features (£20 submission fee, £100 membership mentioned in legacy templates - not in Phase 0 scope)
  - Simplify workflow instructions (legacy 3-step process → Phase 0 simplified: register, activate, view innovation)
  - Update domain from innovemtuty.com/innoventity.com to Phase 0 deployment domain
  - Modernize tone and fix typos (e.g., "plateform" → "platform")
  - HTML formatting (legacy templates are plain text) - add responsive design, logo, branded styling
- **Phase 0 Emails Needed**:
  1. Account Activation (blocking for registration) - legacy template provides baseline ✅
  2. Welcome Email per ActorType (post-activation) - legacy templates provide baselines ✅
  3. Sufficient Bids Notification (mentioned in R5.5) - **NEW for Phase 1+**, no legacy template
- **Recommendation**:
  - Create `specs/001-platform-core/email-templates/` directory
  - Adapt legacy templates for Phase 0 scope (remove payments, update links, modernize)
  - Add "Sufficient Bids Notification" template (Phase 1+)
  - Document in plan.md: HTML/plain text dual format, responsive design, logo placement, from address (<noreply@innoventity.example.com>)
- **Conclusion**: Email content requirements NOW SPECIFIED via legacy templates. Core structure and content reusable with Phase 0 adaptations.

**Summary by Status**:

- ✅ **10 Resolved**: CHK030 (response time metrics), CHK032 (sufficient bids threshold), CHK033 (qualified actor design decision), CHK034 (active account status), CHK035 (critical journeys), CHK036 (publication trigger), CHK037 (collaboration validation), CHK038 (irreversibility mechanism), CHK039 (incubator formation trigger), CHK040 (email content via legacy templates)
- ⚠️ **1 Partial**: CHK031 (production-ready lacks checklist)
- ❌ **0 Gaps**: All items resolved or partially resolved

**Recommendations Before TASKS Phase**:

1. **CHK031**: Define "production-ready" checklist in plan.md (security, testing, observability, performance benchmarks)
2. **CHK040**: ✅ COMPLETE - Email templates created in `specs/001-platform-core/email-templates/` with Phase 0 adaptations

### Identified Gaps Requiring Phase 1+ Implementation

**1. Resend Activation Email (CHK071)**
**Status**: Spec mentions feature (R1.0 Error Scenario 1), but NO API endpoint in Phase 0 contracts
**Decision**: Defer to Phase 1 (complexity vs value trade-off)
**Workaround**: User can re-register with same email/ActorType (overwrite PendingActivation accounts)
**Phase 1 Requirements**: Documented in [plan.md](../plan.md) §CHK071

**2. Zero-State UI (CHK074)**
**Status**: Phase 0 has no innovation list/browse page (only GET /api/innovations/{id})
**Decision**: Phase 1 will add discovery page with empty state handling
**Design**: Documented in [plan.md](../plan.md) §CHK074

**3. Authorization (403 Forbidden) (CHK068)**
**Status**: Phase 0 has authentication only (JWT validation), no resource-level authorization
**Decision**: Phase 1 will implement policy-based authorization (InnovationOwnerRequirement, etc.)
**Design**: Documented in [plan.md](../plan.md) §Phase 0 Authorization (lines 54-78)

### Minor Gaps Requiring Documentation Fix (Before TASKS Phase)

**1. API Error Schema Documentation (CHK004)** - ✅ CLOSED
**Resolution**: `contracts/openapi.yaml` now documents 400 `ValidationProblemDetails` for both `POST /auth/login` and `POST /auth/refresh-token`.
**Impact**: API consumer contract is now aligned with ASP.NET Core model-binding validation behavior.

### Statistics

**Initial Gaps Identified**: 43 items requiring clarification/documentation
**Gaps Resolved**: 56 items (43 fully addressed, 13 clarity validations) - **Updated with Spec §7 references + CHK115 conflict investigation + CHK004-005 contract validation + CHK041-045 cross-document consistency + CHK030-040 vague terms quantification + CHK033 design decision confirmed**
**Gaps Partially Resolved**: 1 item (CHK031 - production-ready lacks checklist)
**New Gaps Identified**: 1 item (CHK144 - Innovation lifecycle terminal states, discovered during CHK115 investigation)
**Gaps Deferred to Phase 1+**: 3 items (Resend Activation, Zero-State UI, 403 Authorization)
**Documentation Added**: ~820 lines across [plan.md](../plan.md), [spec.md](../spec.md) §7 (FR7.1-FR7.8), and [infrastructure.md](../infrastructure.md)
**Most Recent Investigation**: CHK030-040 (February 2026) - Vague terms quantification validated: 10 resolved (including CHK033 qualified actor design decision confirmed, CHK040 via legacy email templates and template creation), 1 partial

**Recent Updates**:

- ✅ CHK014: Token expiration durations marked as RESOLVED (1hr access, 7d refresh documented in Quickstart, Plan, Contracts)
- ✅ CHK088-093: Security requirements updated with Spec §7 FR7.5 references (secret management acceptance criteria)
- ✅ CHK097-100: Observability requirements updated with Spec §7 FR7.6 references (structured logging, monitoring, tracing)
- ✅ CHK101-103: Reliability requirements updated with Spec §7 FR7.7-FR7.8 references (migration idempotency, lifecycle documentation)
- ✅ CHK115: Conflict investigated and RESOLVED - No conflict exists; GAP identified (CHK144: innovation lifecycle terminal states)
- ✅ CHK129-143: New infrastructure category added (15 items validating FR7.1-FR7.8)
- ✅ CHK004: RESOLVED - login and refresh-token now include 400 ValidationProblemDetails responses in OpenAPI
- ✅ CHK005: RESOLVED - All endpoints have clear authentication specifications
- ✅ CHK041-045: RESOLVED - Cross-document consistency validated (schema alignment, journey-endpoint mapping, enum consistency, business rule enforcement)
- ✅ CHK030-040: 10 RESOLVED (response time, sufficient bids, qualified actor design decision, active account, critical journeys, publication trigger, collaboration validation, irreversibility, incubator formation, email content via legacy templates), 1 PARTIAL (production-ready checklist)
- 📝 CHK144: New item added - Innovation lifecycle terminal states missing definition

**Traceability**: 128/139 items (92.1%) include [Spec §X], [Plan §Y], [Contracts /path], or [Gap] markers

---

## Category 1: Requirement Completeness

### API Endpoint Requirements

- [ ] CHK001 - Are all Phase 0 API endpoints explicitly listed with HTTP methods? [Completeness, Spec §6]
- [ ] CHK002 - Are request body schemas defined for all POST/PUT endpoints? [Completeness, Contracts]
- [ ] CHK003 - Are response schemas defined for all success cases (200, 201, 204)? [Completeness, Contracts]
- [x] CHK004 - Are error response schemas defined for all failure cases (400, 401, 403, 404, 500)? ✅ RESOLVED - Updated March 2026: All authentication endpoints include complete error schema coverage; `/auth/login` and `/auth/refresh-token` now document 400 `ValidationProblemDetails` in `contracts/openapi.yaml`. [Completeness, Contracts]
- [x] CHK005 - Are authentication requirements specified for each endpoint? ✅ RESOLVED - Updated 2026-02-19: All Phase 0/0.6 endpoints have auth specifications: Auth endpoints (register/activate/login/refresh-token) use security: [] (explicitly no auth required). Health and Industries use security: [] (public reference data). All Innovation and Bid endpoints use BearerAuth (RequireAuthorization()). **Full re-validation required after T080 documents all 14 endpoints in openapi.yaml.** [Completeness, Contracts Security]

### Data Model Requirements

- [x] CHK007 - Are all entities from business rules (R1-R8) represented in data model? ✅ PHASE 3: Actor entity (R1.1-R1.3) implemented with all properties [Completeness, Data Model]
- [x] CHK008 - Are field-level validation rules specified for all entity properties? ✅ PHASE 3: Actor entity validation rules complete (Email 255 chars, FullName 200 chars, ContactAddress 500 chars, PasswordHash 60 chars, AccountStatus enum, ActorType enum) [Completeness, Data Model §Validation Rules]
- [x] CHK009 - Are database indexes specified for performance-critical queries? ✅ PHASE 3: Composite unique index IX_Actor_Email_ActorType implemented (R1.3) [Completeness, Data Model §Indexes]
- [ ] CHK010 - Are foreign key cascade behaviors defined for all relationships? [Completeness, Data Model §Constraints]
- [x] CHK011 - Are audit fields (CreatedAt, UpdatedAt) requirements documented? ✅ PHASE 3: Timestamps implemented with GETUTCDATE() defaults in AppDbContext [Completeness, Data Model]

### Authentication & Authorization Requirements

- [x] CHK013 - Are password complexity requirements quantified? ✅ RESOLVED - Spec §R8.4 updated with measurable criteria (8+ chars, 4 types, max 128) [Clarity, Spec R8.4]
- [x] CHK014 - Are token expiration durations specified (access + refresh)? ✅ RESOLVED - 1hr access, 7d refresh tokens documented in Quickstart §Configuration, Plan §Session Management, Contracts /auth/refresh-token [Completeness]
- [x] CHK016 - Are account activation requirements completely specified? ✅ PHASE 3: R1.1 fully implemented with PendingActivation→Active status transition, token validation, integration tests (T022-T023) [Completeness, Spec R1.1]

### Frontend / UX Requirements

- [x] CHK019 - Are loading state requirements defined for all asynchronous operations? ✅ RESOLVED - Plan §Frontend/UX Requirements §CHK019 [Gap]
- [x] CHK020 - Are error message content requirements specified for validation failures? ✅ RESOLVED - Plan §Frontend/UX Requirements §CHK020 [Gap]
- [x] CHK021 - Are form validation requirements (client-side vs server-side) clarified? ✅ RESOLVED - Plan §Frontend/UX Requirements §CHK021 [Ambiguity]
- [x] CHK022 - Are accessibility requirements (WCAG level) specified? ✅ RESOLVED - Plan §Frontend/UX Requirements §CHK022 [Gap]
- [x] CHK023 - Are responsive design breakpoints and behavior requirements documented? ✅ RESOLVED - Plan §Frontend/UX Requirements §CHK023 [Gap]
- [x] CHK024 - Are navigation requirements consistent across all user journeys? ✅ RESOLVED - Plan §Frontend/UX Requirements §CHK024 [Consistency, Spec §3]

### Testing Requirements

- [ ] CHK025 - Are unit test coverage targets quantified (>80% per Plan)? [Clarity, Plan §Technical Context]
- [ ] CHK026 - Are integration test coverage targets quantified (100% endpoints per Plan)? [Clarity, Plan §Technical Context]
- [ ] CHK027 - Are E2E test scope requirements clearly defined (J1-J3 per Plan)? [Completeness, Plan §Technical Context]
- [ ] CHK028 - Are mutation testing thresholds specified (>70% per Plan)? [Clarity, Plan §Technical Context]
- [x] CHK029 - Are performance test requirements (load, stress) documented? ✅ RESOLVED - Plan §Operational & Testing Requirements §CHK029 (JMeter, 100 users, success criteria) [Gap]

---

## Category 2: Requirement Clarity

### Vague Terms Requiring Quantification

- [x] CHK030 - Is "fast" response time quantified with specific metrics (p95 <200ms per Plan)? ✅ RESOLVED - Plan §Performance Goals specifies p95 <200ms API response, <100ms DB queries, <2s page load. Industry-standard percentile-based metrics. [Clarity, Plan §Performance Goals]
- [x] CHK031 - Is "production-ready" defined with measurable criteria? ✅ RESOLVED - Updated 2026-02-19: Plan §CHK031 Production-Ready Checklist documents all pass/fail criteria: test coverage thresholds (unit/integration/E2E), mutation score ≥70% (T079), CI/CD pipeline gates (T076-T078), performance benchmarks (p95 <200ms), security scan, health check validation. Security/observability/deployment all covered (CHK088-093, FR7.5, FR7.6, FR7.1, FR7.8). [Ambiguity, Plan §CHK031]
- [x] CHK032 - Is "sufficient bids" quantified (≥1 per required actor type per Spec R5.1)? ✅ RESOLVED - Spec R5.5 explicitly defines: ≥1 Manufacturing + ≥1 Sales/Marketing + ≥1 R&D bid (Investor optional). Gherkin acceptance test validates enforcement. [Clarity, Spec R5.1]
- [x] CHK033 - Is "qualified actor" defined with measurable criteria? ⚠️ PARTIAL - Spec §2 describes personas but no credential verification requirements. Design decision: "qualified" means "typed actor with detailed proposal" (R4.2 requires 200+ chars) not "pre-verified credentials". Phase 0 intentionally permissive, owner evaluates qualifications from bid content. [Ambiguity, Spec §2]
- [x] CHK034 - Is "active account" status clearly defined vs suspended/pending? ✅ RESOLVED - Data Model §Enums defines AccountStatus: PendingActivation, Active, Suspended. Spec R1.1 specifies activation flow. Validated in CHK044 cross-document consistency check. [Clarity, Data Model §Enums]
- [x] CHK035 - Are "critical user journeys" explicitly identified (J1-J3 per Plan)? ✅ RESOLVED - Plan line 28, 915, 1086 explicitly identifies J1-J3 for E2E Playwright testing. Spec §6 (lines 1323-1341) prioritizes J1-J3 as critical path, J4-J5 as important. [Clarity, Plan §Technical Context]

### Ambiguous Functional Requirements

- [x] CHK036 - Is "innovation publication" trigger explicitly defined (status transition)? ✅ RESOLVED - Spec J1 Step 7 + R2.1: User clicks "Submit for publication" → System validates all sections → Status Draft→AwaitingBids → Innovation visible to ecosystem. Validation gate prevents incomplete submissions. [Ambiguity, Spec R2.2]
- [x] CHK037 - Are "collaboration requirements" validation rules quantified (≥1 actor type required)? ✅ RESOLVED - Spec J1 Step 6 shows actor type selection (RD, Manufacturing, Sales/Marketing, Investor). R2.1 requires "≥1 target industry selected". R5.5 enforces ≥1 bid per required actor type. Validation rule: ≥1 actor type must be marked as needed. [Clarity, Spec R2.1]
- [x] CHK038 - Is "irreversible partner selection" mechanism clearly specified? ✅ RESOLVED - Spec R5.3 thoroughly specifies: PERMANENT operation, confirmation prompt with partner list, "cannot be undone" UI warnings, error on retry, Gherkin acceptance tests validate enforcement. CHK115 validated no conflict with optional business plan. [Clarity, Spec R6.2]
- [x] CHK039 - Is "virtual incubator formation" trigger and state transition defined? ✅ RESOLVED - Spec J3 Step 5 + R6.1 acceptance test: Partner selection confirmed → Bids marked accepted → "Virtual incubator workspace is created" (line 909) → Status becomes PartnersSelected → Access control enforced (owner + selected partners only). [Ambiguity, Spec §3 J4]
- [x] CHK040 - Are email notification content requirements specified? ✅ RESOLVED - Legacy app email templates provide reference content structure: Account Activation.txt (subject, body with activation link, greeting, signature), Welcome Email templates for each actor type (IdeaGenerator, RD, Manufacturing, SalesMarketing, Investor) with personalized onboarding instructions. Templates created in specs/001-platform-core/email-templates/: activation-email.md, welcome-idea-generator.md, welcome-investor.md, welcome-manufacturing.md, welcome-rd-organization.md, welcome-sales-marketing.md. Payment/monetization references (£20/£100 fees) removed. Domain updated. Phase 0 activation token workaround documented (token returned in response body, not emailed — see plan.md §CHK071). [Gap — RESOLVED 2026-02-19]

---

## Category 3: Requirement Consistency

### Cross-Document Consistency

- [x] CHK041 - Do API contract schemas match data model entity definitions? ✅ VALIDATED - Contracts expose appropriate subsets of entity fields for Phase 0 scope. IdeaSummary (6 fields) is subset of Innovation entity (25+ fields). Actor schemas (RegisterActorRequest, ActorInfo) expose only necessary fields, never PasswordHash (R8.3). Schema design follows API security best practices. [Consistency, Contracts vs Data Model]
- [x] CHK042 - Do user journey steps (Spec §3) align with API endpoints (Contracts)? ⚠️ STALE - Updated 2026-02-19: Phase 0.6 implemented J1 Steps 2-7 (CreateInnovation, UpdateInnovation, SubmitInnovation, ListInnovations) and J2 bid management (GetBids, SubmitBid, UpdateBid). The previous validation "Journey steps beyond Phase 0 correctly not in contracts" is now factually incorrect — all J1 steps and J2 bid steps are implemented. **Re-validate after T080 (openapi.yaml update) is complete.** [Consistency — needs re-validation post-T080]
- [x] CHK043 - Do business rules (Spec R1-R8) map to database constraints (Data Model)? ✅ VALIDATED - All Phase 0 business rules enforced: R1.1 (activation) → AccountStatus enum + activation endpoint, R1.3 (email uniqueness per ActorType) → validation error documented, R2.1 constraints → IdeaSummary minLength/maxLength, R8.3 (password never exposed) → no password in response schemas, R8.4 (complexity) → validation error examples. Future phase rules (R4-R7) not yet in API as expected. [Consistency]
- [x] CHK044 - Are actor type enums consistent across spec, data model, and contracts? ✅ CONSISTENT - ActorType enum values identical: IdeaGenerator, RD, Manufacturing, SalesMarketing, Investor. Note: spec.md line 106 uses "RDOrganization" as display name for Persona 2, while technical enum value is "RD" - this is correct design (display name vs enum value). AccountStatus (3 values) and ResearchCategory (3 values) also consistent. [Consistency]
- [x] CHK045 - Are innovation status values consistent across all documents? ✅ CONSISTENT - InnovationStatus enum matches exactly across all documents: Draft, AwaitingBids, ReceivingBids, SufficientBids, PartnersSelected, BusinessPlanInProgress, BusinessPlanComplete (7 values). Usage verified in spec.md user journeys, data-model.md entity definition, contracts/openapi.yaml response schemas. [Consistency, Data Model §Enums vs Spec R2.2]

### Internal Specification Consistency

- [x] CHK046 - Are actor registration requirements (Spec R1) consistent with authentication flow (J1)? ✅ PHASE 3: Registration (POST /auth/register) matches J1 Step 1a, Activation (POST /auth/activate) matches J1 Step 1b, email uniqueness enforced per ActorType (R1.3), activation required (R1.1) [Consistency, Spec R1 vs J1]
- [ ] CHK047 - Are bid submission requirements (R4) consistent with partner selection rules (R6)? [Consistency, Spec R4 vs R6]
- [ ] CHK048 - Do persona needs (Spec §2) align with provided feature set? [Consistency]

---

## Category 4: Acceptance Criteria Quality

### Measurability & Testability

- [x] CHK049 - Can "email activation required" (R1.1) be objectively verified with test? ✅ PHASE 3: Integration tests T022-T023 validate activation flow (token validation, status transition PendingActivation→Active, reject invalid token, reject already activated) [Measurability, Spec R1.1]
- [ ] CHK050 - Can "minimum bid proposal length 200 chars" (R4.2) be automated tested? [Measurability, Spec R4.2]
- [ ] CHK051 - Can "p95 API response <200ms" be monitored in production? [Measurability, Plan §Performance Goals]
- [ ] CHK052 - Can "TPH inheritance for actors" be validated in migration? [Measurability, Data Model §Entities]
- [ ] CHK053 - Are Gherkin acceptance tests (Spec §4) executable or documentation-only? [Ambiguity, Spec §4]

### Success Criteria Completeness

- [ ] CHK054 - Are Phase 0 success criteria measurable (Spec §6.3)? [Measurability, Spec §6.3]
- [ ] CHK055 - Are user satisfaction metrics (Spec §5.1) instrumented? [Gap]
- [ ] CHK056 - Are business metrics (Spec §5.2) queryable from database? [Gap]
- [ ] CHK057 - Are technical metrics (Spec §5.3) exported to Application Insights? [Gap]

---

## Category 5: Scenario Coverage

### Primary Flow Coverage

- [x] CHK058 - Are requirements defined for all 5 user journeys (J1-J5)? ✅ PHASE 0.6 EXPANDED - Updated 2026-02-19: J1 Steps 1-7 fully implemented (registration → activation → login → create draft → edit draft → submit for bidding). J2 implemented (list innovations, view innovation, bid placement/update). J2 bid viewing (owner perspective) implemented. J3-J5 deferred to Phase 1+. All implemented journey steps have corresponding subcutaneous integration tests in specs/003-api-completion/. [Coverage, Spec §3]
- [x] CHK059 - Are success outcomes specified for each journey step? ✅ PHASE 3: J1 Step 1a success outcome (actor created with PendingActivation, activation token generated), J1 Step 1b success outcome (account status changed to Active) validated via integration tests [Completeness, Spec §3]
- [x] CHK060 - Are API endpoints mapped to journey steps? ✅ PHASE 0.6 COMPLETE - Updated 2026-02-19: J1 Step 1a→POST /auth/register ✅, J1 Step 1b→POST /auth/activate ✅, J1 Step 1c→POST /auth/login ✅, J1 token refresh→POST /auth/refresh-token ✅, J1 Step 2→POST /innovations ✅, J1 Steps 3-6→PUT /innovations/{id} ✅, J1 Step 7→PATCH /innovations/{id}/submit ✅, J2 Step 1→GET /innovations ✅, J2 Step 3→GET /innovations/{id} ✅, J2 bid→POST /innovations/{id}/bids ✅, J2 bid update→PUT /bids/{bidId} ✅. J3-J5 steps deferred. [Traceability, Contracts vs Spec §3]

### Alternate Flow Coverage

- [ ] CHK061 - Are requirements specified for editing draft innovations before submission? [Coverage, Alternate Flow]
- [ ] CHK062 - Are requirements defined for withdrawing bids before acceptance? [Coverage, Gap]
- [ ] CHK063 - Are requirements specified for viewing rejected bids (actor perspective)? [Coverage, Gap]
- [ ] CHK064 - Are password reset flow requirements documented? [Coverage, Gap]

### Exception Flow Coverage

- [x] CHK065 - Are error handling requirements defined for duplicate email registration? ✅ RESOLVED - Plan §Exception Flow Coverage §CHK065 [Coverage, Exception, Spec R1.3]
- [x] CHK066 - Are requirements specified for invalid activation token scenarios? ✅ RESOLVED - Plan §Exception Flow Coverage §CHK066 [Coverage, Exception, Contracts /auth/activate]
- [x] CHK067 - Are requirements defined for login with unactivated account? ✅ RESOLVED - Plan §Exception Flow Coverage §CHK067 [Coverage, Exception, Spec R1.1]
- [x] CHK068 - Are error requirements specified for unauthorized resource access? ✅ RESOLVED - Plan §Exception Flow Coverage §CHK068 (Phase 0: N/A, Phase 1+: 403) [Coverage, Exception, Contracts 401/403]
- [x] CHK069 - Are requirements defined for innovation not found scenarios? ✅ RESOLVED - Plan §Exception Flow Coverage §CHK069 [Coverage, Exception, Contracts 404]
- [x] CHK070 - Are database connection failure requirements specified? ✅ RESOLVED - Plan §Exception Flow Coverage §CHK070 [Coverage, Exception, Gap]

### Recovery Flow Coverage

- [x] CHK071 - Are requirements defined for resending activation emails? ⚠️ DEFERRED to Phase 1 - Plan §Exception Flow Coverage §CHK071 [Coverage, Recovery, Gap]
- [x] CHK072 - Are refresh token expiry recovery requirements specified? ✅ RESOLVED - Plan §Exception Flow Coverage §CHK072 [Coverage, Recovery, Contracts /auth/refresh-token]
- [x] CHK073 - Are form validation failure recovery requirements defined? ✅ RESOLVED - Plan §Exception Flow Coverage §CHK073 [Coverage, Recovery, Gap]

---

## Category 6: Edge Case Coverage

### Boundary Conditions

- [x] CHK074 - Are requirements defined for zero-state scenarios (no innovations exist)? ✅ RESOLVED - Updated 2026-02-19: GET /api/innovations (Phase 0.6) returns empty array [] when no innovations exist — zero-state API response handled correctly. Zero-state UI rendering (empty state component) deferred to Phase 1 frontend (T071-T075). [Edge Case, Plan §Edge Case Requirements]
- [x] CHK075 - Are requirements specified for exactly 1 bid (minimum sufficient)? ✅ RESOLVED - Plan §Edge Case Requirements §CHK075 [Edge Case, Spec R5.1]
- [x] CHK076 - Are requirements defined for maximum concurrent bids per innovation? ✅ RESOLVED - Plan §Edge Case Requirements §CHK076 (uncapped in Phase 0, rate limit in Phase 1) [Edge Case, Gap]
- [x] CHK077 - Are string length boundary validations specified (min/max)? ✅ RESOLVED - Plan §Edge Case Requirements §CHK077 (comprehensive table with Data Model references) [Edge Case, Data Model]
- [x] CHK078 - Are requirements defined for empty optional fields (null vs empty string)? ✅ RESOLVED - Plan §Edge Case Requirements §CHK078 (nullable use string?, reject empty "") [Edge Case, Ambiguity]

### Concurrent Operations

- [x] CHK079 - Are requirements specified for concurrent bid submissions to same innovation? ✅ RESOLVED - Plan §Edge Case Requirements §CHK079 [Edge Case, Concurrency]
- [x] CHK080 - Are requirements defined for concurrent partner selection attempts? ✅ RESOLVED - Plan §Edge Case Requirements §CHK080 (Phase 0: N/A, Phase 1: Optimistic concurrency) [Edge Case, Concurrency]
- [x] CHK081 - Are database transaction isolation requirements specified? ✅ RESOLVED - Plan §Edge Case Requirements §CHK081 (Read Committed default, explicit for Phase 1) [Edge Case, Gap]

### Data Integrity

- [x] CHK082 - Are requirements defined for orphaned records (deleted actors with innovations)? ✅ RESOLVED - Plan §Edge Case Requirements §CHK082 (ON DELETE RESTRICT, prefer suspension) [Edge Case, Data Model §Constraints]
- [x] CHK083 - Are referential integrity requirements consistent with cascade rules? ✅ RESOLVED - Plan §Edge Case Requirements §CHK083 [Edge Case, Data Model §Constraints]

---

## Category 7: Non-Functional Requirements

### Performance Requirements

- [ ] CHK084 - Are performance targets quantified for all metrics (response time, throughput, latency)? [Clarity, Plan §Performance Goals]
- [ ] CHK085 - Are performance requirements specified under expected load (100 concurrent users)? [Completeness, Plan §Scale/Scope]
- [ ] CHK086 - Are database query performance targets defined (p95 <100ms per Plan)? [Clarity, Plan §Performance Goals]
- [ ] CHK087 - Are page load performance requirements quantified (FCP p95 <2s per Plan)? [Clarity, Plan §Performance Goals]

### Security Requirements

- [x] CHK088 - Are password hashing algorithm requirements specified (BCrypt per Research)? ✅ RESOLVED - Plan §Security Requirements §CHK088, Spec §7 FR7.5 [Completeness, Research §Decision 3]
- [x] CHK089 - Are JWT signing key management requirements documented? ✅ RESOLVED - Plan §Security Requirements §CHK089, Spec §7 FR7.5 [Gap]
- [x] CHK090 - Are HTTPS requirements for all endpoints specified? ✅ RESOLVED - Plan §Security Requirements §CHK090, Spec §7 FR7.5 [Gap]
- [x] CHK091 - Are SQL injection prevention requirements documented? ✅ RESOLVED - Plan §Security Requirements §CHK091, Spec §7 FR7.5 [Gap]
- [x] CHK092 - Are CORS policy requirements clearly defined? ✅ RESOLVED - Plan §Security Requirements §CHK092, Spec §7 FR7.5 [Gap]
- [x] CHK093 - Are secret management requirements specified (App Service Configuration Phase 0)? ✅ RESOLVED - Plan §Security Requirements §CHK093, Spec §7 FR7.5 with rotation acceptance criteria [Completeness]

### Scalability Requirements

- [ ] CHK094 - Are horizontal scaling requirements defined (stateless API per Plan)? [Completeness, Plan §Technical Context]
- [ ] CHK095 - Are database connection pooling requirements specified? [Gap]
- [ ] CHK096 - Are caching requirements defined or explicitly excluded? [Ambiguity]

### Observability Requirements

- [x] CHK097 - Are logging requirements specified (structured logging per Research)? ✅ RESOLVED - Research §Decision 9, Spec §7 FR7.6 with correlation ID acceptance criteria [Completeness]
- [x] CHK098 - Are monitoring alert thresholds quantified (error rate >1% per Research)? ✅ RESOLVED - Research §Decision 9, Spec §7 FR7.6 with 5-min validation acceptance criteria [Clarity]
- [x] CHK099 - Are distributed tracing requirements documented? ✅ RESOLVED - Spec §7 FR7.6 Application Insights integration [Gap]
- [x] CHK100 - Are metrics collection requirements defined (Application Insights per Plan)? ✅ RESOLVED - Plan §Technical Context, Spec §7 FR7.6 with p95 <200ms acceptance criteria [Completeness]

### Reliability Requirements

- [x] CHK101 - Are backup and recovery requirements specified? ❌ OUT OF SCOPE - Ephemeral infrastructure approach: recreate environments instead of backup/restore. EF Core migrations + test data seeding provide schema/data reproducibility [Architecture Decision] ✅ CONFIRMED OUT OF SCOPE (2026-03-08)
- [x] CHK102 - Are deployment rollback requirements documented? ✅ RESOLVED - Plan §Operational & Testing Requirements §CHK102, Spec §7 FR7.8 destruction workflow [Gap]
- [x] CHK103 - Are database migration failure recovery requirements defined? ✅ RESOLVED - Plan §Operational & Testing Requirements §CHK103, Spec §7 FR7.7 migration idempotency [Gap]

---

## Category 8: Dependencies & Assumptions

### External Dependencies

- [ ] CHK104 - Are Azure service dependencies explicitly documented (SQL, App Service, Application Insights per Research)? [Completeness, Research §Decision 8]
- [ ] CHK105 - Are email service provider requirements specified? [Gap]
- [ ] CHK106 - Are third-party library version requirements pinned (Angular 19, .NET 8 per Plan)? [Completeness, Plan §Technical Context]

### Assumptions Requiring Validation

- [ ] CHK107 - Is the assumption "actors have valid email addresses" validated? [Assumption, Spec R1.1]
- [ ] CHK108 - Is the assumption "innovations always have ≥1 target industry" enforced? [Assumption, Spec R2.1, Data Model]
- [ ] CHK109 - Is the assumption "bid proposals min 200 chars are meaningful" justified? [Assumption, Spec R4.2]
- [ ] CHK110 - Is the assumption "100 concurrent users" sufficient based on usage projections? [Assumption, Plan §Scale/Scope]

---

## Category 9: Ambiguities & Conflicts

### Unclear Requirements

- [ ] CHK111 - Is "Phase 0 timeline 1-2 weeks" achievable given scope (registration + auth + view innovation)? [Ambiguity, Plan §Scale/Scope]
- [ ] CHK112 - Is "test-first development (TDD: red → green → refactor)" enforced or aspirational? [Ambiguity, Plan §Constraints]
- [ ] CHK113 - Is "production-ready from Phase 0" compatible with iterative development? [Ambiguity, Plan §Constraints]

### Potential Conflicts

- [x] CHK114 - Does "email uniqueness per actor type" (R1.3) conflict with "single login" UX? ✅ NO CONFLICT - Plan §Operational & Testing Requirements §CHK114 [Potential Conflict, Spec R1.3 vs Contracts /auth/login]
- [x] CHK115 - Does "irreversible partner selection" (R5.3) conflict with "business plan optional completion" (R7.1)? ✅ NO CONFLICT - Investigated February 2026: Partner selection creates permanent team (R5.3), business plan completion is optional (R7.1). Both logically compatible: teams can exist indefinitely without completing business plan (may work offline, skip planning, or abandon innovation). **GAP IDENTIFIED**: Spec lacks innovation lifecycle terminal states (completed/abandoned/archived) and workspace closure mechanism. Recommend adding R7.4 to define innovation end states. [Spec R5.3 vs R7.1, Journey J5]
- [x] CHK116 - Do "JWT 1hr expiration" and "no explicit logout" requirements align with security best practices? ✅ ALREADY ADDRESSED - Plan §Session Management [Potential Conflict, Research §Decision 3]

### Missing Definitions

- [ ] CHK117 - Is "innovation discovery" mechanism defined (search, filtering, matching)? [Gap]
- [ ] CHK118 - Is "bid evaluation criteria" specified for idea generators? [Gap]
- [ ] CHK119 - Is "business plan structure" defined (sections, required fields, validation)? [Gap]
- [ ] CHK120 - Is "virtual incubator workspace" scope clearly bounded? [Ambiguity, Spec §3 J5]
- [ ] CHK144 - Are innovation lifecycle terminal states defined (completed/abandoned/archived)? **NEW from CHK115 investigation** - Currently only "business plan completed" is mentioned as success outcome, but no requirements for: workspace closure mechanism, innovation abandonment, archiving inactive innovations, or defining when an innovation "ends". [Gap, Spec §3 J5, R7.1]

---

## Category 10: Traceability & Documentation

### Requirement Traceability

- [x] CHK121 - Are all business rules (R1-R8) traceable to data model constraints? ✅ PHASE 0.6 EXPANDED - Updated 2026-03-08: R1.1 (activation)→AccountStatus enum + Activate endpoint ✅. R1.3 (email uniqueness per ActorType)→composite unique index IX_Actor_Email_ActorType ✅. R2.1 (innovation completeness)→13-rule validation in SubmitInnovation + field constraints ✅. R2.2 (ownership)→OwnerId claim validation in CreateInnovation/UpdateInnovation/SubmitInnovation ✅. R4.1-R4.4 (bid rules)→SubmitBid/UpdateBid/GetBids endpoint enforcement ✅. R8.4 (password complexity + lockout)→BCrypt work factor 12 + Register validation + login lockout enforcement (5 failures → 15-minute lockout, HTTP 423) with T081-T085 tests ✅. R5 (partner selection), R6 (virtual incubator), R7 (business plan) remain Phase 1+. [Traceability, Spec §4 vs Data Model §Validation Rules]
- [ ] CHK122 - Are all user journey steps (J1-J5) traceable to API endpoints? [Traceability, Spec §3 vs Contracts] ⚠️ **Note 2026-02-19**: Phase 0.6 significantly expands coverage (J1 Steps 1-7 + J2 bid flow). Validate against 14-endpoint baseline after T080 completes openapi.yaml.
- [ ] CHK123 - Are all acceptance tests (Gherkin scenarios) traceable to test implementations? [Traceability, Spec §4]
- [ ] CHK124 - Are Phase 0 requirements (Spec §6) traceable to Phase 0 API contracts? [Traceability, Spec §6 vs Contracts] ⚠️ **Note 2026-02-19**: Validate against full Phase 0.6 scope (14 endpoints) after T080 completes openapi.yaml — do not assess against 5-endpoint Phase 0 baseline.

### Documentation Completeness

- [x] CHK125 - Are all API endpoints documented in OpenAPI spec (Contracts)? ⚠️ PARTIAL - Updated 2026-02-19: All original Phase 0 endpoints now documented including POST /auth/login ✅ and POST /auth/refresh-token ✅ (implemented, no longer pending). **NEW GAP (S01)**: 9 Phase 0.6 endpoints implemented but not yet in openapi.yaml: GET /health, GET /industries, POST/GET /innovations, PUT /innovations/{id}, PATCH /innovations/{id}/submit, GET/POST /innovations/{innovationId}/bids, PUT /bids/{bidId}. Tracked as T080. [Completeness, Contracts]
- [ ] CHK126 - Are developer setup instructions complete and testable (Quickstart)? [Completeness, Quickstart]
- [ ] CHK127 - Are migration instructions documented for Phase 0 → Phase 1 schema? [Completeness, Data Model §Migration Strategy]
- [ ] CHK128 - Are deployment prerequisites explicitly listed (Quickstart §Prerequisites)? [Completeness, Quickstart]

---

## Category 11: Infrastructure & Operational Requirements (Section 7)

### Functional Requirement Quality (FR7.1-FR7.8)

- [ ] CHK129 - Are all 8 infrastructure functional requirements (FR7.1-FR7.8) testable with clear acceptance criteria? [Measurability, Spec §7]
- [ ] CHK130 - Do FR7.1 acceptance scenarios validate infrastructure creation completes within 10 minutes? [Acceptance Criteria, Spec §7 FR7.1]
- [ ] CHK131 - Do FR7.2 acceptance scenarios validate component health endpoints and idempotency? [Acceptance Criteria, Spec §7 FR7.2]
- [ ] CHK132 - Do FR7.3 acceptance scenarios demonstrate test failure before success (epistemic validation)? [Acceptance Criteria, Spec §7 FR7.3]
- [ ] CHK133 - Do FR7.4 acceptance scenarios validate SOLID/KISS/YAGNI principles are enforced? [Acceptance Criteria, Spec §7 FR7.4]
- [ ] CHK134 - Do FR7.5 acceptance scenarios cover secret rotation without deployment? [Acceptance Criteria, Spec §7 FR7.5]
- [ ] CHK135 - Do FR7.6 acceptance scenarios validate correlation IDs in structured logs? [Acceptance Criteria, Spec §7 FR7.6]
- [ ] CHK136 - Do FR7.7 acceptance scenarios validate migration idempotency and seed data reproducibility? [Acceptance Criteria, Spec §7 FR7.7]
- [ ] CHK137 - Do FR7.8 acceptance scenarios validate environment creation/destruction workflows? [Acceptance Criteria, Spec §7 FR7.8]

### Cross-Document Infrastructure Consistency

- [ ] CHK138 - Do spec.md FR7.5 secret management requirements align with plan.md CHK088-093 security details? [Consistency, Spec §7 FR7.5 vs Plan §Security]
- [ ] CHK139 - Do spec.md FR7.6 observability requirements conflict with or duplicate plan.md CHK097-100? [Consistency, Spec §7 FR7.6 vs Plan]
- [ ] CHK140 - Do spec.md FR7.8 lifecycle requirements cover plan.md CHK101-103 operational scenarios? [Consistency, Spec §7 FR7.8 vs Plan §Operational]
- [ ] CHK141 - Are infrastructure.md Terraform modules traceable to FR7.1 ephemeral environment requirements? [Traceability, Infrastructure vs Spec §7 FR7.1]
- [ ] CHK142 - Are infrastructure.md provisioning workflows traceable to FR7.8 lifecycle requirements (10-min creation, 5-min destruction)? [Traceability, Infrastructure vs Spec §7 FR7.8]
- [ ] CHK143 - Are FR7.7 data persistence requirements (schema as code, migrations) consistent with data-model.md migration strategy? [Consistency, Spec §7 FR7.7 vs Data Model]

---

## Summary Statistics

- **Total Checklist Items**: 139 (123 original + 15 infrastructure requirements + 1 from CHK115 investigation)
- **Resolved Items**: 80 (57.6% of 139 items) - **Updated March 8, 2026** — CHK004 moved from partial to resolved after OpenAPI 400 error schema completion
  - CHK005, CHK013-024: Authentication specs, password complexity, token expiration, UX requirements ✅
  - CHK007-009, CHK011: Data model entities, validation rules, indexes, audit fields ✅ **NEW**
  - CHK016: Account activation requirements fully specified ✅ **NEW**
  - CHK030-032, CHK034-040: Vague terms quantified (response time, sufficient bids, active account, critical journeys, publication trigger, irreversibility, email content via legacy templates) ✅
  - CHK041-045: Cross-document consistency (schema alignment, journey-endpoint mapping, enum consistency) ✅
  - CHK046: Actor registration consistent with authentication flow ✅ **NEW**
  - CHK049: Email activation objectively verified ✅ **NEW**
  - CHK058-060: User journey requirements, success outcomes, API endpoint mapping ✅ **NEW**
  - CHK065-073: Exception flow coverage ✅
  - CHK074-083: Edge case requirements ✅
  - CHK088-093: Security requirements (Plan + Spec §7 FR7.5) ✅
  - CHK097-100: Observability requirements (Spec §7 FR7.6) ✅
  - CHK101-103, CHK114-116: Reliability & operational requirements (Spec §7 FR7.7-FR7.8) ✅
  - CHK121: Business rules traceable to data model (R1.1, R1.3, R8.4) ✅ **NEW**
  - CHK125: API endpoints documented in OpenAPI (register, activate) ✅ **NEW**
- **Partially Resolved Items**: 2 (1.4% of 139 items)
  - CHK031: Production-ready mentioned but lacks consolidated checklist ⚠️
  - CHK033: Qualified actor defined operationally but lacks credential verification criteria ⚠️
- **Coverage by Category**:
  - Requirement Completeness: 24 items (12 resolved **+5 NEW**, 1 partial)
  - Requirement Clarity: 11 items (11 resolved - CHK030-040 ✅, CHK031 partial for production-ready checklist)
  - Requirement Consistency: 8 items (9 resolved - CHK041-048 ✅, **+1 NEW CHK046**)
  - Acceptance Criteria Quality: 9 items (1 resolved **+1 NEW CHK049**)
  - Scenario Coverage: 16 items (12 resolved **+3 NEW CHK058-060**)
  - Edge Case Coverage: 10 items (10 resolved ✅)
  - Non-Functional Requirements: 20 items (11 resolved)
  - Dependencies & Assumptions: 7 items (0 resolved)
  - Ambiguities & Conflicts: 11 items (4 resolved - CHK114-116 ✅, CHK040 ✅)
  - Traceability & Documentation: 8 items (2 resolved **+2 NEW CHK121, CHK125**)
  - Infrastructure & Operational Requirements: 15 items (0 resolved - NEW category, needs validation)

- **Traceability**: 128/139 items (92.1%) include specification references `[Spec §X]` or markers `[Gap]`, `[Ambiguity]`, `[Conflict]`
- **Remaining Open Items**: 66 items requiring validation or clarification (down from 82, **-16 items completed in Phase 3**)
  - **High-Priority**: ~3 items (CHK001-003 - API endpoint listing validation, request/response schema completeness)
  - **Medium-Priority**: ~20 items (CHK010, CHK025-028, CHK050-057, CHK084-087, CHK094-096, CHK144 - Phase 0 clarity, test coverage, performance)
  - **Infrastructure Validation**: 15 items (CHK129-143 - validate Spec §7 FR7.1-FR7.8 acceptance criteria)
  - **Low-Priority/Deferred**: ~28 items (CHK117-120, Phase 1+ features, dependencies, traceability audits)
- **New Coverage**: Section 7 Infrastructure & Operational Requirements (FR7.1-FR7.8) validated with 15 items

---

## Next Actions

### ✅ Recently Completed (February 2026)

1. **✅ Security Requirements** (CHK088-CHK093): HTTPS, CORS, secret management, SQL injection prevention now documented in Plan + Spec §7 FR7.5
2. **✅ Error Handling** (CHK065-CHK073): Exception and recovery flow requirements complete in Plan
3. **✅ UX Details** (CHK019-CHK024): Loading states, error messages, accessibility, responsive behavior documented in Plan
4. **✅ Edge Cases** (CHK074-CHK083): Zero-state, boundary conditions, concurrent operations addressed in Plan
5. **✅ Observability** (CHK097-CHK100): Structured logging, monitoring, tracing requirements in Spec §7 FR7.6
6. **✅ Reliability** (CHK101-CHK103): Deployment rollback and migration failure handling in Spec §7 FR7.7-FR7.8 (CHK101 backup/recovery out of scope per ephemeral architecture)
7. **✅ Infrastructure Requirements** (CHK129-CHK143): Added Category 11 with 15 items validating Spec §7 FR7.1-FR7.8 acceptance criteria
8. **✅ Token Expiration** (CHK014): Marked RESOLVED - 1hr access, 7d refresh tokens documented in Quickstart, Plan, Contracts
9. **✅ Business Logic Conflict Resolution (CHK115)**: Investigated "irreversible partner selection vs optional business plan" - NO CONFLICT - Both requirements can coexist logically; Virtual Incubator workspaces can exist indefinitely without completed business plans. **GAP IDENTIFIED**: Added CHK144 for innovation lifecycle terminal states definition.
10. **✅ API Contract Validation** (CHK004-005): CHK004 and CHK005 both resolved; `/auth/login` and `/auth/refresh-token` now include 400 `ValidationProblemDetails` coverage in OpenAPI
11. **✅ Cross-Document Consistency** (CHK041-045): All 5 consistency checks PASSED - Schema alignment (contracts match data model subsets), journey-endpoint mapping (Phase 0 steps have corresponding APIs), business rule enforcement (R1-R8 map to database constraints + API validation), enum consistency (ActorType, InnovationStatus, AccountStatus, ResearchCategory consistent across all documents). **Specifications are aligned and ready for implementation**.
12. **✅ Vague Terms Quantification** (CHK030-040): 9 RESOLVED (response time p95 <200ms, sufficient bids ≥1 per actor type, active account status defined, critical journeys J1-J3 identified, publication trigger explicit, collaboration requirements ≥1 actor type, irreversibility mechanism thorough, incubator formation trigger defined, email content via legacy templates with Phase 0 adaptations), 2 PARTIAL (production-ready lacks checklist, qualified actor lacks credential criteria).
13. **✅ Token Expiration** (CHK014): 1hr access, 7d refresh tokens documented in Quickstart, Plan, Contracts
14. **✅ Infrastructure Requirements** (CHK129-143): New category added validating Spec §7 FR7.1-FR7.8
15. **✅ Business Logic Conflict Resolution (CHK115)**: Investigated "irreversible partner selection vs optional business plan" - NO CONFLICT - Both requirements can coexist logically; Virtual Incubator workspaces can exist indefinitely without completed business plans. **GAP IDENTIFIED**: Added CHK144 for innovation lifecycle terminal states definition.
16. **✅ Authentication Requirements (CHK005)**: Validated all 5 Phase 0 endpoints have clear authentication specifications - Auth endpoints use security: [], Innovation endpoint uses BearerAuth with description
17. **✅ Error Response Schemas (CHK004)**: RESOLVED - 400 `ValidationProblemDetails` responses documented for `/auth/login` and `/auth/refresh-token`
18. **✅ Phase 0.6 API Completion** (February 2026, specs/003-api-completion/): Innovation CRUD (R2.1, R2.2 — CreateInnovation, UpdateInnovation, SubmitInnovation, ListInnovations), Bid management (R4.1-R4.4 — SubmitBid, UpdateBid, GetBids), Industries reference data. 97 tests (94 passing, 3 skipped). J1 Steps 1-7 and J2 bid flow fully implemented via subcutaneous integration tests. CHK042/CHK058/CHK060/CHK074/CHK121 status updated accordingly.

### High-Priority Gaps (Address Before TASKS Phase)

1. **API Contract Minor Gaps** (CHK004): ✅ CLOSED - 400 `ValidationProblemDetails` responses are now documented for both login and refresh-token endpoints

2. **API Contract Completeness** (CHK001-003):
   - Verify all Phase 0/0.6 endpoints listed (14 endpoints implemented: register, activate, login, refresh-token, health, industries, GET/POST innovations, GET/PUT innovations/{id}, PATCH innovations/{id}/submit, GET/POST innovations/{id}/bids, PUT bids/{bidId}) — **T080 tracks this work**
   - Confirm request body schemas complete for all POST endpoints (validated in CHK004-005 investigation - all complete ✅)
   - Validate success response schemas (200, 201) documented (validated in CHK041 investigation - all complete ✅)

3. **Production-Ready Checklist** (CHK031 - partial):
   - Define consolidated "production-ready" checklist in plan.md with measurable pass/fail criteria
   - Components covered: Security (CHK088-093 ✅), Observability (FR7.6 ✅), Deployment automation (FR7.1, FR7.8 ✅)
   - **Missing**: Test coverage thresholds (unit/integration/E2E minimum %), performance benchmark requirements before release
   - **Impact**: MEDIUM - Team knows intent, but explicit checklist prevents scope creep debates

4. **Infrastructure Acceptance Criteria Validation** (CHK129-143):
   - Validate FR7.1-FR7.8 Gherkin scenarios are testable and complete
   - Confirm cross-document consistency between Spec §7, Plan, Infrastructure.md, Data Model

### Medium-Priority Ambiguities (Clarify During Phase 0)

1. **Data Model Details** (CHK008-010): Field validations, indexes, cascade behaviors
2. **Acceptance Criteria Measurability** (CHK049-057): Validate Gherkin tests executable, metrics instrumented
3. **Performance/Scalability** (CHK084-087, CHK094-096): Connection pooling, caching strategy decisions
4. **Innovation Lifecycle Definition (CHK144)** - **NEW from CHK115 investigation**: Define terminal states (completed/abandoned/archived) and workspace closure mechanism
5. **Email Template Adaptation (CHK040)** - **RESOLVED (2026-02-19)**: Templates created at `specs/001-platform-core/email-templates/` (activation-email.md + 5 actor-type welcome templates). Payment/monetization references (£20/£100 fees) removed. Domain and tone updated. Activation token workaround documented in plan.md §CHK071.
6. **Qualified Actor Criteria (CHK033)** - **PARTIAL from CHK030-040**: Document design decision explicitly in plan.md - "qualified" means "typed actor with detailed proposal" (R4.2 requires 200+ chars) not "pre-verified credentials". Defer credential verification to Phase 1+ if needed.

### Low-Priority (Defer to Phase 1+)

1. **Innovation Discovery** (CHK117): Search/filtering/matching mechanism (Phase 1+)
2. **Business Plan Structure** (CHK119): Detailed workspace fields and validation (Phase 1+)
3. **Caching Strategy** (CHK096): Determine if needed based on Phase 0 performance data

---

## Governance Compliance

### Assumptions Made

1. **Explicit**: Feature directory is `specs/001-platform-core/` (from context)
2. **Explicit**: Available documents are spec.md, plan.md, data-model.md, contracts/openapi.yaml, quickstart.md (from file reads)
3. **Explicit**: User selected "Comprehensive" depth (50+ items) and "All areas" focus (from clarification questions)
4. **Inferred**: Phase 0 is highest priority for requirement validation (from spec.md §6 and plan.md)

### Simpler Alternatives Considered

1. **Lightweight checklist (15-20 items)**: Rejected - user selected "Comprehensive" depth
2. **Single-domain focus (e.g., API only)**: Rejected - user selected "All of the above" areas
3. **Generic web app checklist**: Rejected - tailored to open innovation platform domain per specification

### SOLID/KISS/YAGNI Compliance

- **KISS**: Each checklist item asks ONE specific question about requirement quality
- **YAGNI**: No hypothetical scenarios invented - all items derived from specification, plan, data model, or contracts
- **Single Responsibility**: Each item tests one aspect of requirement quality (completeness, clarity, consistency, measurability, coverage)

### YAGNI Violations Removed

- ❌ Did not include checklist items for features mentioned in "Out of Scope" (Spec §7)
- ❌ Did not invent requirements not present in source documents
- ❌ Did not create implementation-testing items (e.g., "Verify API returns 200 OK")

### What Could Be Deleted Without Loss

- Items CHK106-CHK110 (Dependencies & Assumptions) could be reduced - many are already validated in plan/research documents
- Items CHK121-CHK128 (Traceability) could be automated via tooling instead of manual checklist
- If focusing only on Phase 0, items related to Phase 1+ features (bids, business plans) could be deferred

---

**END OF CHECKLIST**
