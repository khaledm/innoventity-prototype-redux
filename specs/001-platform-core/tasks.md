# Tasks: Platform Core (v1.0) - Phase 0

**Input**: Design documents from `/specs/001-platform-core/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Phase 0 Scope**: User Registration & Authentication + View Single Innovation (backend + minimal frontend)
**MVP Goal**: Prove end-to-end: Register → Activate → Login → View Innovation via Angular client

## Phase 1: Setup

- [X] T001 Create solution structure src/Innoventity.API/, tests/Innoventity.API.Tests/
- [X] T002 Initialize ASP.NET Core 8 project with Minimal APIs in src/Innoventity.API/
- [X] T003 [P] Configure EF Core 8, BCrypt.Net, IdentityModel.Tokens.Jwt NuGet packages
- [X] T004 [P] Initialize xUnit test project in tests/Innoventity.API.Tests/
- [X] T005 [P] Configure .editorconfig, .gitignore, appsettings.json structure

---

## Phase 2: Foundational (Blocking Prerequisites)

**⚠️ CRITICAL**: User stories cannot begin until foundation is complete

- [X] T006 Create AppDbContext in src/Innoventity.API/Infrastructure/Persistence/AppDbContext.cs
- [X] T007 Configure EF Core connection string management in src/Innoventity.API/Program.cs
- [X] T008 Create initial migration for empty database in Infrastructure/Persistence/Migrations/
- [X] T009 [P] Implement JWT token generation service in Infrastructure/Authentication/JwtTokenService.cs
- [X] T010 [P] Configure JWT authentication middleware in Program.cs
- [X] T011 [P] Implement password hashing service using BCrypt in Infrastructure/Authentication/PasswordHasher.cs
- [X] T012 Create base ProblemDetails error handling middleware in Infrastructure/ErrorHandling/
- [X] T013 Configure Application Insights telemetry in Program.cs
- [X] T066 Implement `/health` endpoint in Features/Health/HealthCheck.cs validating database and configuration dependencies
- [X] T067 Add integration tests for `/health` happy-path and failure-path behavior in tests/Integration/Features/Health/HealthCheckTests.cs

---

## Phase 2b: Infrastructure Phase 0 (FR7.1/FR7.2)

**Goal**: Minimal IaC and scripts to create/destroy a dev environment matching FR7.1/FR7.2.

- [ ] T068 Create infrastructure/environments/dev template (e.g., Terraform) provisioning Resource Group, App Service, Azure SQL, and Application Insights as per infrastructure.md
- [ ] T069 Add scripts to create and destroy the dev environment (`apply`/`destroy`) and verify completion within 10 minutes
- [ ] T070 Validate infrastructure idempotency and characterisation failure modes: (1) run second `terraform apply` on `core/` and `data/` — assert "No changes" exit code 0 [automated by `create-environment.ps1` Step 5/5 idempotency gate — H5]; (2) deliberately omit DB connection string from App Service `app_settings`, run `GET /health`, record actual HTTP status in `infrastructure/README.md`; (3) restore config, assert health returns 200; (4) run Pester suite (`Invoke-Pester -CI ./infrastructure/tests/pester/`) — Pester `$script:` scope bug fixed in commit `6cb2109` (C3); (5) run Terratest locally (`go test ./infrastructure/tests/terratest/ -timeout 30m`) — requires `go mod tidy` to be run first with Go 1.21+ (H3 blocker: `go.sum` not yet generated); **Gate**: before closing T070, run `/speckit.analyze` and resolve any CRITICAL/HIGH findings against `.specify/memory/infra-review-prompt.md`

---

## Phase 3: User Story 1 - User Registration (Priority: P1) 🎯 MVP

**Goal**: User can register as Idea Generator and activate account via email token

**Independent Test**: Register new user → verify PendingActivation → activate with token → verify Active

### Domain for User Story 1

- [X] T014 [P] [US1] Create ActorType enum (IdeaGenerator, RD, Manufacturing, SalesMarketing, Investor) in Domain/Entities/ActorType.cs
- [X] T015 [P] [US1] Create AccountStatus enum (PendingActivation, Active, Suspended) in Domain/Entities/AccountStatus.cs
- [X] T016 [US1] Create Actor entity in Domain/Entities/Actor.cs with validation rules R1.1-R1.3

### Tests for User Story 1 (Write FIRST, verify FAIL)

- [X] T017 [P] [US1] Unit test: Actor entity validates required fields in tests/Unit/Domain/Entities/ActorTests.cs
- [X] T018 [P] [US1] Unit test: Actor entity enforces email uniqueness per ActorType in ActorTests.cs
- [X] T019 [P] [US1] Unit test: PasswordHasher verifies BCrypt work factor 12 in tests/Unit/Infrastructure/PasswordHasherTests.cs
- [X] T020 [US1] Integration test: POST /auth/register creates actor with PendingActivation in tests/Integration/Features/Authentication/RegisterActorTests.cs
- [X] T021 [US1] Integration test: POST /auth/register rejects duplicate email+ActorType in RegisterActorTests.cs
- [X] T022 [US1] Integration test: POST /auth/activate changes status to Active in ActivateAccountTests.cs
- [X] T023 [US1] Integration test: POST /auth/activate rejects invalid token in ActivateAccountTests.cs

### Implementation for User Story 1

- [X] T024 [US1] Add Actor entity to AppDbContext and create migration in Infrastructure/Persistence/
- [X] T025 [US1] Implement POST /auth/register endpoint in Features/Authentication/Register.cs
- [X] T026 [US1] Implement activation token generation in Features/Authentication/Register.cs
- [X] T027 [US1] Implement POST /auth/activate endpoint in Features/Authentication/Activate.cs
- [X] T028 [US1] Add validation for R1.3 (email unique per ActorType) in Features/Authentication/Register.cs
- [X] T029 [US1] Add validation for R8.4 (password complexity) in Features/Authentication/Register.cs

---

## Phase 4: User Story 2 - User Authentication (Priority: P1) 🎯 MVP

**Goal**: Active user can obtain JWT access token (1hr) and refresh token (7d)

**Independent Test**: Login with email+ActorType+password → receive tokens → refresh before expiry

### Tests for User Story 2 (Write FIRST, verify FAIL)

- [X] T030 [P] [US2] Unit test: JwtTokenService generates valid HS256 tokens in tests/Unit/Infrastructure/JwtTokenServiceTests.cs
- [X] T031 [P] [US2] Unit test: JwtTokenService sets access token expiry to 1 hour in JwtTokenServiceTests.cs
- [X] T032 [P] [US2] Unit test: JwtTokenService sets refresh token expiry to 7 days in JwtTokenServiceTests.cs
- [X] T033 [US2] Integration test: POST /auth/login returns tokens for valid credentials in tests/Integration/Features/Authentication/LoginTests.cs
- [X] T034 [US2] Integration test: POST /auth/login rejects PendingActivation account in LoginTests.cs
- [X] T035 [US2] Integration test: POST /auth/login rejects invalid password in LoginTests.cs
- [X] T036 [US2] Integration test: POST /auth/refresh-token returns new access token in RefreshTokenTests.cs
- [X] T037 [US2] Integration test: POST /auth/refresh-token rejects expired refresh token in RefreshTokenTests.cs
- [ ] T081 [P] [US2] Unit test: Login handler increments FailedLoginAttempts on wrong password and resets counter to 0 on successful login in tests/Unit/Infrastructure/LoginHandlerTests.cs
- [ ] T082 [US2] Integration test: POST /auth/login returns 423 Locked with RFC 7807 body "Too many failed login attempts. Account temporarily locked for 15 minutes." after 5 consecutive failed attempts; assert LockoutUntil is ~15 min in future in tests/Integration/Features/Authentication/LoginTests.cs

### Implementation for User Story 2

- [X] T038 [US2] Implement POST /auth/login endpoint in Features/Authentication/Login.cs
- [X] T039 [US2] Implement password verification using BCrypt in Features/Authentication/Login.cs
- [X] T040 [US2] Implement AccountStatus check (only Active can login) in Features/Authentication/Login.cs
- [X] T041 [US2] Implement POST /auth/refresh-token endpoint in Features/Authentication/RefreshToken.cs
- [X] T042 [US2] Add JWT claims (actorId, actorType, email) in JwtTokenService.cs
- [ ] T083 [US2] Add FailedLoginAttempts (int, default 0) and LockoutUntil (DateTimeOffset?, nullable) columns to Actor entity in Domain/Entities/Actor.cs (R8.4)
- [ ] T084 [US2] Create EF Core migration for Actor lockout fields in Infrastructure/Persistence/Migrations/
- [ ] T085 [US2] Implement lockout logic in Features/Authentication/Login.cs: increment FailedLoginAttempts on wrong password; set LockoutUntil = UtcNow + 15 min on 5th failure; block login with 423 when LockoutUntil > UtcNow; reset counter on successful login (R8.4)

---

## Phase 5: User Story 3 - View Innovation (Priority: P1) 🎯 MVP

**Goal**: Authenticated user can retrieve single innovation by ID

**Independent Test**: Seed innovation → login → GET /innovations/{id} → verify response matches

### Domain for User Story 3

- [X] T043 [P] [US3] Create ResearchCategory enum (Management, Engineering, NaturalScience) in Domain/Entities/ResearchCategory.cs
- [X] T044 [P] [US3] Create InnovationStatus enum (Draft, Published, etc.) in Domain/Entities/InnovationStatus.cs
- [X] T045 [US3] Create Innovation entity in Domain/Entities/Innovation.cs with required fields for Phase 0
- [X] T046 [P] [US3] Create Industry entity in Domain/Entities/Industry.cs

### Tests for User Story 3 (Write FIRST, verify FAIL)

- [X] T047 [P] [US3] Unit test: Innovation entity validates required fields in tests/Unit/Domain/Entities/InnovationTests.cs
- [X] T048 [US3] Integration test: GET /innovations/{id} returns innovation data in tests/Integration/Features/Innovations/GetInnovationTests.cs
- [X] T049 [US3] Integration test: GET /innovations/{id} returns 404 for non-existent ID in GetInnovationTests.cs
- [X] T050 [US3] Integration test: GET /innovations/{id} returns 401 without auth token in GetInnovationTests.cs
- [X] T050a [US3] Integration test: Cross-actor access validation - User of ActorType Manufacturing reads innovation owned by ActorType IdeaGenerator, assert 200 status (validates Phase 0 open-discovery semantics: any authenticated user can view any innovation)
- [X] T051 [US3] E2E test: Register → Activate → Login → ViewInnovation journey in tests/E2E/Journeys/Phase0JourneyTests.cs

### Implementation for User Story 3

- [X] T052 [US3] Add Innovation and Industry entities to AppDbContext in Infrastructure/Persistence/AppDbContext.cs
- [X] T053 [US3] Create migration for Innovation and Industry tables in Infrastructure/Persistence/Migrations/
- [X] T054 [US3] Implement GET /innovations/{id} endpoint in Features/Innovations/GetInnovation.cs
- [X] T055 [US3] Add [Authorize] attribute to innovation endpoint in Features/Innovations/GetInnovation.cs
- [X] T056 [US3] Add validation for non-existent innovation ID in Features/Innovations/GetInnovation.cs
- [X] T057 [US3] Create seed data script implementing spec.md §6 Test Data Requirements (Quantum Battery Prototype with fixed GUIDs, seeded actor, 2 industries) in Infrastructure/Persistence/SeedData.cs
- [ ] T057a [Future-Phase6+] Integration test: Partner selection irreversibility - verify attempting to modify accepted partner selection returns 403 Forbidden with error "Partner selection is final and cannot be changed" in tests/Integration/Features/PartnerSelection/PartnerSelectionTests.cs (CRITICAL per Constitution Principle 5: human-written test required before implementing partner selection endpoint)

---

## Phase 6: Polish & Deployment

- [X] T058 [P] Create README.md with setup instructions at repository root
- [ ] T059 [P] Verify quickstart.md validation steps in specs/001-platform-core/quickstart.md
- [X] T060 [P] Document API endpoints in Scalar/OpenAPI at /scalar route
- [X] T061 Configure CORS policy for development in Program.cs
- [X] T062 Add structured logging with correlation IDs in Infrastructure/Logging/
- [X] T063 Verify all integration tests pass with clean database
- [X] T064 Verify E2E test passes end-to-end journey
- [X] T065 Create deployment configuration for Azure App Service in infrastructure/ (includes App Service Configuration for JWT signing key, database connection string; SendGrid API key config deferred to Phase 1+ when email notifications implemented)
- [X] T079 Run Stryker.NET mutation tests on Infrastructure/Authentication/JwtTokenService.cs and PasswordHasher.cs, verify ≥70% mutation score per CHK031 requirement
  <!-- Result (2026-02-19): Score = 80% (16 killed / 18 tested). 2 survivors both in JwtTokenService.cs — null-coalescing ?? throw paths at lines 19 and 49 (missing SigningKey guard) not exercised. ≥70% threshold MET. -->
- [X] T080 Update contracts/openapi.yaml to document all Phase 0.6 endpoints: GET /health, GET /industries, POST /innovations, GET /innovations, PUT /innovations/{id}, PATCH /innovations/{id}/submit, GET /innovations/{innovationId}/bids, POST /innovations/{innovationId}/bids, PUT /bids/{bidId} — including auth requirements, request/response schemas, and all documented status codes (200/201/400/401/403/404/409/500 where applicable)

---

## Phase 6b: CI/CD Pipeline & Operations (CHK031)

**Goal**: Implement automated deployment pipeline satisfying CHK031 production-ready requirements.

- [ ] T076 Author three GitHub Actions workflows: (1) `.github/workflows/infra.yml` with jobs `terraform-plan-core` → `terraform-apply-core` → `terraform-plan-data` → `approve-data` (manual gate, prod only) → `terraform-apply-data` → `pester-infra`; (2) `.github/workflows/deploy.yml` with jobs `preflight` → `build-test` → `migrate` → `deploy` → `pester-health` → `slot-swap`; (3) `.github/workflows/drift.yml` with `cron: "0 2 * * *"` trigger only, jobs `drift-check-core` + `drift-check-data` using `terraform plan -detailed-exitcode` (detection only, never applies); **Note**: `infra.yml` `terraform-apply-core` step must inject `-var sql_server_id=$(cd environments/dev/data && terraform output -raw sql_server_id)` to enable SQL diagnostic settings (C5, FR7.6); **Gate**: before merging T076 PR, run `/speckit.analyze` and confirm zero new CRITICAL findings
- [ ] T077 Configure pipeline gates across three workflows: `infra.yml` fails if `terraform apply` exits non-zero or `Invoke-Pester -CI` fails; `deploy.yml` fails if `dotnet test` exits non-zero, if `pester-health` (`HealthCheck.Tests.ps1`) returns non-200 from `GET /health`, or if `migrate` job errors; `drift.yml` emits `::error::` annotation and exits non-zero if `terraform plan -detailed-exitcode` returns exit code 2 (drift detected) — `drift.yml` never calls `terraform apply`
- [ ] T078 Validate green runs across all three pipelines: trigger `infra.yml` via `workflow_dispatch` on dev — confirm all 6 jobs pass and `pester-infra` exits 0; trigger `deploy.yml` by pushing to `src/` — confirm `pester-health` passes (`GET /health` 200); trigger `drift.yml` manually — confirm exit code 0 (no drift) on dev; document screenshot evidence in `infrastructure/README.md`

---

## Phase 7: Frontend Phase 0 Shell (Angular)

**Goal**: Minimal Angular client to exercise the Phase 0 end-to-end journey against the real API.

- [ ] T071 Initialize Angular 18 app in src/Innoventity.Client/ with routing and basic layout
- [ ] T072 Implement login page calling POST /auth/login, storing access/refresh tokens, and handling error messages
- [ ] T073 Implement minimal innovation detail page that calls GET /innovations/{id}` using stored access token and renders required Phase 0 fields
- [ ] T074 Configure Angular environment files with API base URL for dev/staging
- [ ] T075 Add Playwright E2E test that drives the browser through Register → Activate (mock email token) → Login → View innovation, using the Angular client

---

## Phase 8: Notification System (Journey 2+3 Prerequisite)

**Goal**: DB-persisted in-app notification feed so innovation owners are alerted when bids arrive and the sufficient-bids threshold is crossed (spec.md §Clarifications Q3, R5.5).
**Prerequisite**: Phase 5 complete (Bid entity + submission endpoint from specs/003-api-completion/ must exist).

- [ ] T086 [P] Create Notification entity in Domain/Entities/Notification.cs: Id (Guid), RecipientActorId (Guid FK → Actor), Message (string max 500), CreatedAt (DateTimeOffset), IsRead (bool default false), LinkedEntityId (Guid?), LinkedEntityType (string? max 50)
- [ ] T087 Add Notification to AppDbContext and create EF Core migration in Infrastructure/Persistence/Migrations/
- [ ] T088 Integration test: GET /notifications returns array of unread NotificationDto sorted descending by CreatedAt for authenticated actor in tests/Integration/Features/Notifications/GetNotificationsTests.cs
- [ ] T089 Integration test: POST /innovations/{id}/bids creates a Notification record for the innovation owner with message "A new bid was received for your innovation." in tests/Integration/Features/Bids/SubmitBidNotificationTests.cs
- [ ] T090 Integration test: sufficient-bids trigger fires exactly once — a second bid in the same final-missing category does NOT create a second threshold notification in SubmitBidNotificationTests.cs
- [ ] T091 Implement GET /notifications endpoint in Features/Notifications/GetNotifications.cs with [Authorize], returning NotificationDto[] (Id, Message, CreatedAt, IsRead, LinkedEntityId, LinkedEntityType)
- [ ] T092 Add notification creation in bid-submission handler: (a) always create "new bid received" notification to innovation owner; (b) if this bid completes the last missing required category per R5.5 query-time check, create "sufficient bids reached" threshold notification once
- [ ] T093 Update contracts/openapi.yaml with GET /notifications schema

---

## Phase 9: Partner Selection (Journey 3)

**Goal**: Innovation owner selects exactly one bid per required actor type; selection is permanent (R5.1–R5.5). T057a (Future-Phase6+) in Phase 5 constitutes the constitution-mandated human-written irreversibility test anchor for this phase.
**Prerequisite**: Phase 8 complete (notification system must fire post-selection workspace notifications).

- [ ] T094 Integration test: POST /innovations/{id}/select-partners returns 409 when sufficient bids threshold not met (R5.4) in tests/Integration/Features/PartnerSelection/SelectPartnersTests.cs
- [ ] T095 Integration test: POST /innovations/{id}/select-partners returns 422 when required actor type missing from selection payload (R5.2) in SelectPartnersTests.cs
- [ ] T096 Integration test: POST /innovations/{id}/select-partners returns 200 and transitions selected bids to Accepted, remaining bids to Rejected (R4.4 atomic transition) in SelectPartnersTests.cs
- [ ] T097 Integration test: POST /innovations/{id}/select-partners returns 403 when partner selection already completed — error "Partner selection is final and cannot be changed" (R5.3 irreversibility; see T057a) in SelectPartnersTests.cs
- [ ] T098 Implement POST /innovations/{id}/select-partners endpoint in Features/PartnerSelection/SelectPartners.cs with FluentValidation enforcing R5.1–R5.5
- [ ] T099 Implement irreversibility guard: check existing Accepted bids; return 403 if already selected (R5.3)
- [ ] T100 Update contracts/openapi.yaml with partner selection endpoint schema (request: { selectedBidIds: Guid[] }, response: 200/403/409/422)

### Phase 6 Infrastructure — Spec Analysis Remediation (commits 88242fb–6319022)

- [X] T101 Add `azurerm_monitor_diagnostic_setting` for App Service to `modules/app-service/main.tf` with inputs `log_analytics_workspace_id` wired from `module.monitoring.workspace_id` in `core/main.tf` (FR7.6 MUST — C5 finding from spec analysis)
- [X] T102 Add `azurerm_monitor_diagnostic_setting` for SQL Server to `environments/dev/core/main.tf` using `var.sql_server_id` (sourced from `data/` output at apply time) and `module.monitoring.workspace_id`; inject via `-var sql_server_id=...` in CI workflow T076 (FR7.6 MUST — C5 finding)
- [X] T103 Create `infrastructure/scripts/destroy-environment.ps1` with correct destroy order (core first, data second), `prevent_destroy` guard reminder, prod confirmation gate, and post-destroy `az group show` resource-existence validation (FR7.8 acceptance criterion — C6 finding)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Start immediately
- **Foundational (Phase 2)**: Depends on Setup → BLOCKS all user stories
- **Infrastructure Phase 0 (Phase 2b)**: Can run parallel with Phase 2 (after T008 migration)
- **US1 Registration (Phase 3)**: Depends on Foundational
- **US2 Authentication (Phase 4)**: Depends on US1 (requires Actor entity)
- **US3 View Innovation (Phase 5)**: Depends on US2 (requires authentication)
- **Polish (Phase 6)**: Depends on US1+US2+US3 complete
- **CI/CD Pipeline (Phase 6b)**: Depends on Phase 6 complete (needs T068-T070; T066 and T067 are ✅ complete)
- **Notification System (Phase 8)**: Depends on Bid submission endpoint (specs/003-api-completion/) and Actor entity (Phase 3)
- **Partner Selection (Phase 9)**: Depends on Phase 8 (notification triggers) and sufficient-bids threshold logic (R5.5)
- **Frontend Phase 0 (Phase 7)**: Can run parallel with Phase 6b

### Critical Path for Phase 0 MVP

```
T001-T005 (Setup)
  ↓
T006-T013, T066-T067 (Foundational + Health) [BLOCKING]
  ↓
T014-T029 (US1: Registration)
  ↓
T030-T042 (US2: Authentication) [needs Actor entity from US1]
  ↓
T043-T057 (US3: View Innovation) [needs auth from US2]
  ↓
T058-T065, T079 (Polish & Deploy + Mutation Testing)
  ↓
T068-T070 (IaC Phase 0) [prerequisite for CI/CD — see Phase Dependencies]
  ↓
T076-T078 (CI/CD Pipeline) [CHK031 requirement; depends on T066-T070]
(Parallel with T068-T070 and above: T071-T075 Frontend)
```

### Parallel Opportunities

**Phase 1 (Setup)**: T003, T004, T005 can run parallel
**Phase 2 (Foundational)**: T009, T010, T011 can run parallel after T006-T008
**Phase 3 (US1 Domain)**: T014, T015 can run parallel
**Phase 3 (US1 Tests)**: T017, T018, T019 can run parallel (after T016)
**Phase 4 (US2 Tests)**: T030, T031, T032 can run parallel
**Phase 5 (US3 Domain)**: T043, T044, T046 can run parallel
**Phase 5 (US3 Tests)**: T047, T048, T049, T050, T050a can run parallel (after T045)
**Phase 6 (Polish)**: T058, T059, T060 can run parallel; T079 runs after core auth implementation complete
**Phase 6b (CI/CD)**: T076, T077 can run parallel (both editing pipeline file)
**Phase 7 (Frontend)**: T071, T072, T073, T074 can run parallel (different files)
**Phase 8 (Notifications)**: T086 entity + T088/T089/T090 tests can run parallel (after T087 migration); T091 + T092 implementation can run parallel
**Phase 9 (Partner Selection)**: T094, T095, T096, T097 can run parallel (all tests, different scenarios; after Phase 8 complete)

---

## Parallel Example: User Story 1 Tests

```bash
# Write all tests first (parallel, different files):
T017: Actor entity validation (ActorTests.cs)
T018: Email uniqueness validation (ActorTests.cs)
T019: Password hashing (PasswordHasherTests.cs)

# All tests MUST fail before any implementation begins
# Verify each failure reason is understood

# Then implement domain:
T024: Add Actor to DbContext
T025-T029: Implement register/activate endpoints
```

---

## Implementation Strategy

### TDD Workflow (Mandatory)

For each user story:
1. Write tests FIRST (marked with test task IDs)
2. Run tests → verify they FAIL for correct reason
3. Implement minimum code to pass (marked with implementation task IDs)
4. Refactor while keeping tests green
5. Commit

### MVP Path (Fastest to working demo)

1. Complete Setup (T001-T005) → ~30 min
2. Complete Foundational (T006-T013) → ~2 hours
3. Complete US1 Registration (T014-T029) → ~4 hours
4. Complete US2 Authentication (T030-T042) → ~3 hours
5. Complete US3 View Innovation (T043-T057) → ~3 hours
6. Polish & Deploy (T058-T065, T079) → ~3 hours
7. IaC Phase 0 (T068-T070) → ~2 hours
8. CI/CD Pipeline (T076-T078) → ~2 hours [requires T068-T070]
9. (Parallel with 7+8) Frontend (T071-T075) → ~3 hours

**Total MVP estimate**: ~20 hours (assumes TDD discipline, no debugging needed if tests written correctly)

### Validation Checkpoints

- After T013: Foundation ready → can seed database, migrations work
- After T029: User can register and activate → test via Scalar
- After T042: User can login → JWT tokens returned
- After T057: User can view innovation → E2E journey complete
- After T065: Deployable to Azure → smoke test in staging
- After T079: Mutation testing validated → test quality confirmed (CHK031)
- After T078: CI/CD pipeline operational → automated deploy/test/health validated (CHK031)
- After T085: Login lockout operational → R8.4 fully enforced (5 failures → 15-min lockout)
- After T092: Notification system operational → bid notifications and sufficient-bids threshold alerts firing (Journey 2+3 unblocked)
- After T100: Partner selection operational → Journey 3 complete (irreversibility enforced per R5.3)

---

## Notes

- **Test Discipline**: Every implementation task depends on test tasks passing first
- **No Speculation**: Only Actor and Innovation entities for Phase 0 (no Bid, BusinessPlan, etc.)
- **Failure Reason**: Each test failure must be understood before implementation proceeds
- **File Paths**: All paths relative to repository root, follow Vertical Slice Architecture
- **Entity Validation**: Business rules R1.1-R8.4 enforced at entity and endpoint layers
- **Commit Frequency**: After each logical task or small task group
- **Epistemic Honesty**: If test passes unexpectedly, investigate before proceeding
- **Validation**: Data Annotations on DTOs for simple constraints; FluentValidation `AbstractValidator<T>` for complex business rules. Both layers active. See `plan.md §P004` and `plan.md §CHK073`.
- **⚠️ Industry IDs**: Use ICB taxonomy only (`TECH-001`, `HLTH-001`, `ENRG-001`, `AUTO-001`, `INDU-001`, `FIN-001`, `TCOM-001`, `CSVC-001`, `UTIL-001`, `MTRL-001`). `ELEC-001` is not a valid ID and does not exist in the production dataset.
- **⚠️ DbContext Isolation**: Tests must use a unique Guid-based database name shared between the test DbContext and WebApplicationFactory. See `plan.md §Implementation Patterns P001-P002` for required patterns.
- **Phase 0.6 Coverage**: `specs/003-api-completion/tasks.md` T001-T017 implement Innovation CRUD (R2.1), Bid management (R4.1-R4.4), and Journey 1-2 subcutaneous tests. Cross-reference before planning Phase 1+ tasks to avoid duplication.
