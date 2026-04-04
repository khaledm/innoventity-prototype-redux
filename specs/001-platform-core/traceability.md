# Phase 0 Implementation Traceability Matrix

**Branch**: `001-platform-core`
**Last Updated**: April 4, 2026
**Purpose**: Map requirements → tasks → implementation artifacts → test evidence

---

## Requirements Coverage

### User Story 1: User Registration (R1.1-R1.3, R8.4)

| Requirement | Tasks | Implementation | Tests | Status |
|-------------|-------|----------------|-------|--------|
| R1.1: Actor must have valid email | T014-T016, T024-T029 | `src/Innoventity.API/Domain/Entities/Actor.cs` | `tests/Unit/Domain/Entities/ActorTests.cs` | ✅ |
| R1.2: Actor must select type | T014-T016, T024-T029 | `src/Innoventity.API/Domain/Entities/ActorType.cs` | `tests/Unit/Domain/Entities/ActorTests.cs` | ✅ |
| R1.3: Email unique per ActorType | T016, T021, T028 | `Features/Authentication/Register.cs` | `tests/Integration/Features/Authentication/RegisterActorTests.cs` | ✅ |
| R8.4: Password complexity + lockout | T029, T081-T085 | `Features/Authentication/Login.cs`, `Infrastructure/Authentication/PasswordHasher.cs` | `tests/Integration/Features/Authentication/LoginTests.cs` | ✅ |

### User Story 2: User Authentication (R2.1-R2.4, R8.1-R8.3)

| Requirement | Tasks | Implementation | Tests | Status |
|-------------|-------|----------------|-------|--------|
| R2.1: JWT access token (1hr) | T030-T032, T038-T042 | `Infrastructure/Authentication/JwtTokenService.cs` | `tests/Unit/Infrastructure/JwtTokenServiceTests.cs` | ✅ |
| R2.2: Refresh token (7d) | T032, T041 | `Features/Authentication/RefreshToken.cs` | `tests/Integration/Features/Authentication/RefreshTokenTests.cs` | ✅ |
| R2.3: Only Active can login | T034, T040 | `Features/Authentication/Login.cs` | `tests/Integration/Features/Authentication/LoginTests.cs` | ✅ |
| R8.1: HS256 signing | T030, T042 | `Infrastructure/Authentication/JwtTokenService.cs` | `tests/Unit/Infrastructure/JwtTokenServiceTests.cs` | ✅ |
| R8.2: BCrypt work factor 12 | T019, T039 | `Infrastructure/Authentication/PasswordHasher.cs` | `tests/Unit/Infrastructure/PasswordHasherTests.cs` | ✅ |
| R8.3: JWT claims | T042 | `Infrastructure/Authentication/JwtTokenService.cs` | `tests/Unit/Infrastructure/JwtTokenServiceTests.cs` | ✅ |

### User Story 3: View Innovation (R3.1-R3.9)

| Requirement | Tasks | Implementation | Tests | Status |
|-------------|-------|----------------|-------|--------|
| R3.1: Innovation must have title | T045, T047 | `src/Innoventity.API/Domain/Entities/Innovation.cs` | `tests/Unit/Domain/Entities/InnovationTests.cs` | ✅ |
| R3.2-R3.9: Innovation fields | T043-T046, T052-T057 | `Domain/Entities/Innovation.cs`, `Features/Innovations/GetInnovation.cs` | `tests/Integration/Features/Innovations/GetInnovationTests.cs` | ✅ |
| R3.x: Auth required | T050, T055 | `Features/Innovations/GetInnovation.cs` | `tests/Integration/Features/Innovations/GetInnovationTests.cs` | ✅ |
| R3.x: 404 for non-existent | T049, T056 | `Features/Innovations/GetInnovation.cs` | `tests/Integration/Features/Innovations/GetInnovationTests.cs` | ✅ |

### Infrastructure Requirements (FR7.1, FR7.2, FR7.6, FR7.8)

| Requirement | Tasks | Implementation | Tests | Status |
|-------------|-------|----------------|-------|--------|
| FR7.1: Dev environment < 10 min | T068-T069 | `infrastructure/scripts/create-environment.ps1` | DEV validated 2026-04-04 | ✅ |
| FR7.2: SQL + App Service + Insights | T068 | `infrastructure/environments/dev/{core,data}/` | `infrastructure/tests/pester/AppService.Tests.ps1`, `SqlDatabase.Tests.ps1` | ✅ |
| FR7.6: Diagnostic settings | T101-T102 | `infrastructure/modules/app-service/main.tf` (commit `7ba7686`) | Manual verification in Azure Portal | ✅ |
| FR7.8: Lifecycle documentation | T070, T103 | `infrastructure/README.md`, `destroy-environment.ps1` | Documented failure modes + destroy order | ✅ |

### CI/CD Requirements (CHK031)

| Requirement | Tasks | Implementation | Tests | Status |
|-------------|-------|----------------|-------|--------|
| CHK031: Automated deploy | T076 | `.github/workflows/infra.yml`, `deploy.yml` | DEV validated 2026-04-04 | ✅ |
| CHK031: Pipeline gates | T077 | Workflow job dependencies + test failures | `build-test` job, `pester-health` job | ✅ |
| CHK031: Drift detection | T076 | `.github/workflows/drift.yml` | Scheduled trigger pending Main merge | ⚠️ |
| CHK031: Mutation testing | T079 | Stryker.NET on JwtTokenService + PasswordHasher | 80% score (threshold: ≥70%) | ✅ |

---

## Task → Implementation Mapping

### Phase 2b: Infrastructure (T068-T070)

| Task | Commits | Files | Evidence |
|------|---------|-------|----------|
| T068 | `4064a63`, `88242fb`, `7ba7686`, `6319022` | `infrastructure/environments/dev/`, `modules/` | Terraform modules + environments |
| T069 | `6319022` | `infrastructure/scripts/*.ps1` | create/destroy/validate scripts |
| T070 | `b7293cf`, `3093a8f`, `2254a55`, `7281cf3` | `infrastructure/tests/pester/`, `terratest/` | Pester + Terratest suites |

### Phase 6b: CI/CD (T076-T078)

| Task | Commits | Files | Evidence |
|------|---------|-------|----------|
| T076 | `e69db0e`, `31b3c61`, `507b0ab`, `8ee6983`, `667a76b` | `.github/workflows/infra.yml`, `deploy.yml`, `drift.yml` | 3 workflows authored + hardened |
| T077 | `e69db0e` + hardening commits | Workflow job dependencies, fail-fast config | DEV runs validated 2026-04-04 |
| T078 | DEV runs 2026-04-04 | GitHub Actions history | infra.yml ✅, deploy.yml ✅, drift.yml ⚠️ Main merge pending |

### Phase 7: Frontend (T071-T075)

| Task | Status | Target Files | Evidence |
|------|--------|--------------|----------|
| T071 | ⏳ OPEN | `src/Innoventity.Client/` Angular app | No implementation yet |
| T072 | ⏳ OPEN | Login page component | No implementation yet |
| T073 | ⏳ OPEN | Innovation detail page component | No implementation yet |
| T074 | ⏳ OPEN | Angular environment configs | No implementation yet |
| T075 | ⏳ OPEN | Playwright E2E journey test | No implementation yet |

---

## Remediation Tasks (specs/003-api-completion)

| Task | Source Issue | Status | Notes |
|------|--------------|--------|-------|
| T001 | Database context lifecycle | ✅ COMPLETE | EF Core In-Memory isolation pattern (P001) |
| T002 | Database context lifecycle fix | ✅ COMPLETE | Closure-based database naming |
| T003 | JWT token attachment | ✅ COMPLETE | HttpClientExtensions.cs + config alignment |
| T004 | Test infrastructure validation | ✅ COMPLETE | Validated 112/115 tests passing |
| T005 | POST /innovations | ✅ COMPLETE | `Features/Innovations/CreateInnovation.cs` |

---

## Unmapped/Weakly Mapped Items

**Partner Selection (Phase 9 - Future)**:
- T057a: Irreversibility test anchor (deferred, awaiting partner selection implementation)
- T094-T100: Partner selection implementation tasks (Phase 9 dependency: notification system from Phase 8)

**Notification System (Phase 8 - Future)**:
- T086-T093: Notification entity + bid notification triggers (requires Bid entity from Phase 0.6)

**Angular Architecture Decisions (Phase 7 - Next Priority)**:
- Signal-Based Forms vs RxJS Reactive Forms selection pending
- Nx Monorepo management decision pending
- Vertical Feature Slicing structure pending
- OpenAPI-Generator for API client codegen pending

---

## Constitution Alignment (Principle 2 Production-Ready)

**Backend Complete** (Current State - 2026-04-04):
- ✅ 112/115 tests passing (97.4%)
- ✅ Infrastructure provisioned + validated on DEV
- ✅ CI/CD pipelines authored + validated (infra.yml, deploy.yml)
- ✅ API deployed to `innoventity-dev-api.azurewebsites.net`
- ✅ Mutation testing ≥70% threshold met (80%)

**Full Phase 0 Complete** (Scope Gates Pending):
- ⚠️ Frontend shell (Angular client) not yet built
- ⚠️ Browser E2E test (Playwright) not yet implemented
- ⚠️ Quickstart validation (T059) not executed
- ⚠️ Drift detection scheduled trigger requires Main merge

**Status Distinction** (per C2 finding resolution):
- **"Backend Complete"**: All API endpoints, tests, infrastructure, CI/CD validated
- **"Full Phase 0 Complete"**: Backend + Frontend + All acceptance criteria + Quickstart validation

---

## Notes

- This file serves as the single source of truth for requirement → implementation mapping
- Update this file when new tasks are completed or requirements added
- Cross-reference with `tasks.md` for task status, `spec.md` for requirements, `plan.md` for implementation patterns
- Evidence links should be verifiable (commit hashes, file paths, test names)
