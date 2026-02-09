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

- [ ] T068 Create infrastructure/environments/dev template (e.g., Terraform/Bicep) provisioning Resource Group, App Service, Azure SQL, and Application Insights as per infrastructure.md
- [ ] T069 Add scripts to create and destroy the dev environment (`apply`/`destroy`) and verify completion within 10 minutes
- [ ] T070 Add a basic infrastructure validation step (re-running apply to confirm idempotency and checking service health endpoints)

---

## Phase 3: User Story 1 - User Registration (Priority: P1) 🎯 MVP

**Goal**: User can register as Idea Generator and activate account via email token

**Independent Test**: Register new user → verify PendingActivation → activate with token → verify Active

### Domain for User Story 1

- [ ] T014 [P] [US1] Create ActorType enum (IdeaGenerator, RD, Manufacturing, SalesMarketing, Investor) in Domain/Entities/ActorType.cs
- [ ] T015 [P] [US1] Create AccountStatus enum (PendingActivation, Active, Suspended) in Domain/Entities/AccountStatus.cs
- [ ] T016 [US1] Create Actor entity in Domain/Entities/Actor.cs with validation rules R1.1-R1.3

### Tests for User Story 1 (Write FIRST, verify FAIL)

- [ ] T017 [P] [US1] Unit test: Actor entity validates required fields in tests/Unit/Domain/Entities/ActorTests.cs
- [ ] T018 [P] [US1] Unit test: Actor entity enforces email uniqueness per ActorType in ActorTests.cs
- [ ] T019 [P] [US1] Unit test: PasswordHasher verifies BCrypt work factor 12 in tests/Unit/Infrastructure/PasswordHasherTests.cs
- [ ] T020 [US1] Integration test: POST /auth/register creates actor with PendingActivation in tests/Integration/Features/Authentication/RegisterActorTests.cs
- [ ] T021 [US1] Integration test: POST /auth/register rejects duplicate email+ActorType in RegisterActorTests.cs
- [ ] T022 [US1] Integration test: POST /auth/activate changes status to Active in ActivateAccountTests.cs
- [ ] T023 [US1] Integration test: POST /auth/activate rejects invalid token in ActivateAccountTests.cs

### Implementation for User Story 1

- [ ] T024 [US1] Add Actor entity to AppDbContext and create migration in Infrastructure/Persistence/
- [ ] T025 [US1] Implement POST /auth/register endpoint in Features/Authentication/Register.cs
- [ ] T026 [US1] Implement activation token generation in Features/Authentication/Register.cs
- [ ] T027 [US1] Implement POST /auth/activate endpoint in Features/Authentication/Activate.cs
- [ ] T028 [US1] Add validation for R1.3 (email unique per ActorType) in Features/Authentication/Register.cs
- [ ] T029 [US1] Add validation for R8.4 (password complexity) in Features/Authentication/Register.cs

---

## Phase 4: User Story 2 - User Authentication (Priority: P1) 🎯 MVP

**Goal**: Active user can obtain JWT access token (1hr) and refresh token (7d)

**Independent Test**: Login with email+ActorType+password → receive tokens → refresh before expiry

### Tests for User Story 2 (Write FIRST, verify FAIL)

- [ ] T030 [P] [US2] Unit test: JwtTokenService generates valid HS256 tokens in tests/Unit/Infrastructure/JwtTokenServiceTests.cs
- [ ] T031 [P] [US2] Unit test: JwtTokenService sets access token expiry to 1 hour in JwtTokenServiceTests.cs
- [ ] T032 [P] [US2] Unit test: JwtTokenService sets refresh token expiry to 7 days in JwtTokenServiceTests.cs
- [ ] T033 [US2] Integration test: POST /auth/login returns tokens for valid credentials in tests/Integration/Features/Authentication/LoginTests.cs
- [ ] T034 [US2] Integration test: POST /auth/login rejects PendingActivation account in LoginTests.cs
- [ ] T035 [US2] Integration test: POST /auth/login rejects invalid password in LoginTests.cs
- [ ] T036 [US2] Integration test: POST /auth/refresh-token returns new access token in RefreshTokenTests.cs
- [ ] T037 [US2] Integration test: POST /auth/refresh-token rejects expired refresh token in RefreshTokenTests.cs

### Implementation for User Story 2

- [ ] T038 [US2] Implement POST /auth/login endpoint in Features/Authentication/Login.cs
- [ ] T039 [US2] Implement password verification using BCrypt in Features/Authentication/Login.cs
- [ ] T040 [US2] Implement AccountStatus check (only Active can login) in Features/Authentication/Login.cs
- [ ] T041 [US2] Implement POST /auth/refresh-token endpoint in Features/Authentication/RefreshToken.cs
- [ ] T042 [US2] Add JWT claims (actorId, actorType, email) in JwtTokenService.cs

---

## Phase 5: User Story 3 - View Innovation (Priority: P1) 🎯 MVP

**Goal**: Authenticated user can retrieve single innovation by ID

**Independent Test**: Seed innovation → login → GET /innovations/{id} → verify response matches

### Domain for User Story 3

- [ ] T043 [P] [US3] Create ResearchCategory enum (Management, Engineering, NaturalScience) in Domain/Entities/ResearchCategory.cs
- [ ] T044 [P] [US3] Create InnovationStatus enum (Draft, Published, etc.) in Domain/Entities/InnovationStatus.cs
- [ ] T045 [US3] Create Innovation entity in Domain/Entities/Innovation.cs with required fields for Phase 0
- [ ] T046 [P] [US3] Create Industry entity in Domain/Entities/Industry.cs

### Tests for User Story 3 (Write FIRST, verify FAIL)

- [ ] T047 [P] [US3] Unit test: Innovation entity validates required fields in tests/Unit/Domain/Entities/InnovationTests.cs
- [ ] T048 [US3] Integration test: GET /innovations/{id} returns innovation data in tests/Integration/Features/Innovations/GetInnovationTests.cs
- [ ] T049 [US3] Integration test: GET /innovations/{id} returns 404 for non-existent ID in GetInnovationTests.cs
- [ ] T050 [US3] Integration test: GET /innovations/{id} returns 401 without auth token in GetInnovationTests.cs
- [ ] T050a [US3] Integration test: Cross-actor access validation - User of ActorType Manufacturing reads innovation owned by ActorType IdeaGenerator, assert 200 status (validates Phase 0 open-discovery semantics: any authenticated user can view any innovation)
- [ ] T051 [US3] E2E test: Register → Activate → Login → ViewInnovation journey in tests/E2E/Journeys/Phase0JourneyTests.cs

### Implementation for User Story 3

- [ ] T052 [US3] Add Innovation and Industry entities to AppDbContext in Infrastructure/Persistence/AppDbContext.cs
- [ ] T053 [US3] Create migration for Innovation and Industry tables in Infrastructure/Persistence/Migrations/
- [ ] T054 [US3] Implement GET /innovations/{id} endpoint in Features/Innovations/GetInnovation.cs
- [ ] T055 [US3] Add [Authorize] attribute to innovation endpoint in Features/Innovations/GetInnovation.cs
- [ ] T056 [US3] Add validation for non-existent innovation ID in Features/Innovations/GetInnovation.cs
- [ ] T057 [US3] Create seed data script for test innovation in Infrastructure/Persistence/SeedData.cs

---

## Phase 6: Polish & Deployment

- [ ] T058 [P] Create README.md with setup instructions at repository root
- [ ] T059 [P] Verify quickstart.md validation steps in specs/001-platform-core/quickstart.md
- [ ] T060 [P] Document API endpoints in Scalar/OpenAPI at /scalar route
- [ ] T061 Configure CORS policy for development in Program.cs
- [ ] T062 Add structured logging with correlation IDs in Infrastructure/Logging/
- [ ] T063 Verify all integration tests pass with clean database
- [ ] T064 Verify E2E test passes end-to-end journey
- [ ] T065 Create deployment configuration for Azure App Service in infrastructure/
- [ ] T079 Run Stryker.NET mutation tests on Infrastructure/Authentication/JwtTokenService.cs and PasswordHasher.cs, verify ≥70% mutation score per CHK031 requirement

---

## Phase 6b: CI/CD Pipeline & Operations (CHK031)

**Goal**: Implement automated deployment pipeline satisfying CHK031 production-ready requirements.

- [ ] T076 Author CI/CD pipeline definition (.github/workflows/deploy.yml or azure-pipelines.yml) with stages: Build → Unit Tests → Integration Tests → Deploy to Dev → Health Check
- [ ] T077 Configure pipeline gates: (1) Fail on test failures (exit code != 0), (2) Fail on health check returning non-200, (3) Fail on infrastructure drift detection
- [ ] T078 Validate green pipeline run: Trigger pipeline, verify successful deploy to dev environment, confirm all gates executed and artifact tagged

---

## Phase 7: Frontend Phase 0 Shell (Angular)

**Goal**: Minimal Angular client to exercise the Phase 0 end-to-end journey against the real API.

- [ ] T071 Initialize Angular 18 app in src/Innoventity.Client/ with routing and basic layout
- [ ] T072 Implement login page calling POST /auth/login, storing access/refresh tokens, and handling error messages
- [ ] T073 Implement minimal innovation detail page that calls GET /innovations/{id}` using stored access token and renders required Phase 0 fields
- [ ] T074 Configure Angular environment files with API base URL for dev/staging
- [ ] T075 Add Playwright E2E test that drives the browser through Register → Activate (mock email token) → Login → View innovation, using the Angular client

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
- **CI/CD Pipeline (Phase 6b)**: Depends on Phase 6 complete (needs T066-T070)
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
T076-T078 (CI/CD Pipeline) [CHK031 requirement]
  ↓
(Parallel: T068-T070 IaC + T071-T075 Frontend can run concurrently)
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
7. CI/CD Pipeline (T076-T078) → ~2 hours
8. (Parallel) IaC (T068-T070) → ~2 hours
9. (Parallel) Frontend (T071-T075) → ~3 hours

**Total MVP estimate**: ~20 hours (assumes TDD discipline, no debugging needed if tests written correctly)

### Validation Checkpoints

- After T013: Foundation ready → can seed database, migrations work
- After T029: User can register and activate → test via Scalar
- After T042: User can login → JWT tokens returned
- After T057: User can view innovation → E2E journey complete
- After T065: Deployable to Azure → smoke test in staging
- After T079: Mutation testing validated → test quality confirmed (CHK031)
- After T078: CI/CD pipeline operational → automated deploy/test/health validated (CHK031)

---

## Notes

- **Test Discipline**: Every implementation task depends on test tasks passing first
- **No Speculation**: Only Actor and Innovation entities for Phase 0 (no Bid, BusinessPlan, etc.)
- **Failure Reason**: Each test failure must be understood before implementation proceeds
- **File Paths**: All paths relative to repository root, follow Vertical Slice Architecture
- **Entity Validation**: Business rules R1.1-R8.4 enforced at entity and endpoint layers
- **Commit Frequency**: After each logical task or small task group
- **Epistemic Honesty**: If test passes unexpectedly, investigate before proceeding
