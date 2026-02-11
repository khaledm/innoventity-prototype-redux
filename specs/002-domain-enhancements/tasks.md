---
description: "Implementation tasks for Phase 0.5 Domain Model Refactoring"
---

# Tasks: Legacy Domain Model Enhancements (Phase 0.5)

**Input**: Design documents from `/specs/002-domain-enhancements/`
**Prerequisites**: plan.md, spec.md, checklists (planning.md, migration.md, requirements.md)

**Tests**: Tests are INCLUDED as part of this feature (TDD approach). All test tasks must be completed as specified.

**Organization**: Tasks are grouped by user story to enable independent validation. However, due to tight coupling in Actor entity schema, US1-US3 are implemented together after foundational US4.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

- Main project: `src/Innoventity.API/`
- Tests: `tests/Innoventity.API.Tests/`
- Domain: `src/Innoventity.API/Domain/`
- Features: `src/Innoventity.API/Features/`
- Infrastructure: `src/Innoventity.API/Infrastructure/`

---

## Phase 1: Setup (Infrastructure Validation)

**Purpose**: Verify project structure and tooling are ready

- [ ] T001 Verify project builds and all 32 existing tests pass with `dotnet test`

---

## Phase 2: Foundational - Entity Equality Semantics (US4)

**Purpose**: Implement EntityBase<TId> infrastructure that ALL entities depend on

**User Story 4**: As a developer working with domain entities, I need all entities to inherit from EntityBase<TId> so that entity equality works correctly in collections, comparisons, and transient entity detection.

**⚠️ CRITICAL**: This phase MUST be complete before ANY user story implementation can begin. All entities (Actor, Innovation, Industry) depend on EntityBase.

**Independent Test**: Create transient entities, add to HashSet, compare by Id, verify GetHashCode stability.

### Implementation for User Story 4

- [ ] T002 [P] [US4] Create EntityBase<TId> abstract class in src/Innoventity.API/Domain/Common/EntityBase.cs
- [ ] T003 [P] [US4] Create EntityOfGuid specialization in src/Innoventity.API/Domain/Common/EntityOfGuid.cs
- [ ] T004 [P] [US4] Create EntityOfInt32 specialization in src/Innoventity.API/Domain/Common/EntityOfInt32.cs
- [ ] T005 [US4] Update Actor entity to inherit from EntityOfGuid in src/Innoventity.API/Domain/Entities/Actor.cs
- [ ] T006 [US4] Update Innovation entity to inherit from EntityOfGuid in src/Innoventity.API/Domain/Entities/Innovation.cs
- [ ] T007 [US4] Update Industry entity to inherit from EntityBase<string> in src/Innoventity.API/Domain/Entities/Industry.cs

### Tests for User Story 4

- [ ] T008 [P] [US4] Create EntityBaseTests with 7 tests in tests/Innoventity.API.Tests/Unit/Domain/Common/EntityBaseTests.cs
- [ ] T009 [P] [US4] Update ActorTests to test equality semantics (2 new tests) in tests/Innoventity.API.Tests/Unit/Domain/Entities/ActorTests.cs
- [ ] T010 [US4] Run EntityBase test suite: `dotnet test --filter "FullyQualifiedName~EntityBaseTests"` (expect 7 passing)

**Checkpoint**: At this point, EntityBase infrastructure is ready. All entities have identity-based equality. Proceed to Actor schema refactoring.

---

## Phase 3: Actor Schema Refactoring (US1, US2, US3 Combined)

**Purpose**: Implement security hardening (PasswordSalt), name decomposition (FirstName/LastName), and structured address (Address value object)

**User Story 1**: Secure Password Management - Actor entity stores password salt separately
**User Story 2**: Structured Actor Names - Split FullName into FirstName/LastName
**User Story 3**: Structured Contact Addresses - Model Address as value object

**Why Combined**: All three changes modify the same Actor entity schema and require a single database migration. Implementing separately would require multiple migrations and increase deployment complexity.

**Independent Test**: Register actors with structured data, verify database schema, query by name/location, validate password with salt.

### Implementation for User Stories 1, 2, 3

- [ ] T011 [P] [US3] Create Address value object class in src/Innoventity.API/Domain/Entities/Address.cs
- [ ] T012 [US1] [US2] [US3] Refactor Actor entity (add FirstName, LastName, PasswordSalt, ContactAddress as Address, Phone) in src/Innoventity.API/Domain/Entities/Actor.cs
- [ ] T013 [US1] [US2] [US3] Update AppDbContext with Actor configuration (owned entity for Address, PasswordSalt column) in src/Innoventity.API/Infrastructure/Persistence/AppDbContext.cs
- [ ] T014 [US1] [US2] Create database migration AddEntityBaseAndRefactorActor with FullName split + PasswordSalt generation in src/Innoventity.API/Migrations/
- [ ] T015 [US1] [US2] [US3] Update SeedData actors with FirstName, LastName, PasswordSalt, structured Address in src/Innoventity.API/Infrastructure/Persistence/SeedData.cs

### Tests for User Stories 1, 2, 3

- [ ] T016 [P] [US3] Create AddressTests with 3 tests (valid creation, CountryCode validation, required fields) in tests/Innoventity.API.Tests/Unit/Domain/Entities/AddressTests.cs
- [ ] T017 [P] [US1] [US2] Update ActorTests for FirstName/LastName/DisplayName properties (update 4 existing tests) in tests/Innoventity.API.Tests/Unit/Domain/Entities/ActorTests.cs
- [ ] T018 [US1] [US2] [US3] Apply migration to test database and verify FullName split, PasswordSalt backfill, Address columns created

**Checkpoint**: At this point, Actor schema is refactored. Database has FirstName/LastName/PasswordSalt/ContactAddress_* columns. Tests verify domain model correctness. Proceed to API updates.

---

## Phase 4: API Contract Updates (US1, US2, US3 Endpoints)

**Purpose**: Update all API endpoints and integration tests to use new Actor schema

**Why This Phase**: API contracts depend on completed domain model schema. All endpoints returning Actor data must be updated to use FirstName/LastName/DisplayName.

**Independent Test**: Call all API endpoints with new request structure, verify responses include firstName/lastName/displayName.

### Implementation for User Stories 1, 2, 3

- [ ] T019 [US1] [US2] [US3] Update Register endpoint with firstName/lastName/contactAddress DTO, PasswordSalt generation in src/Innoventity.API/Features/Authentication/Register.cs
- [ ] T020 [P] [US2] Update Login endpoint response with firstName/lastName/displayName in src/Innoventity.API/Features/Authentication/Login.cs
- [ ] T021 [P] [US2] Update RefreshToken endpoint response with firstName/lastName/displayName in src/Innoventity.API/Features/Authentication/RefreshToken.cs
- [ ] T022 [P] [US2] Update GetInnovation endpoint OwnerDto with firstName/lastName/displayName in src/Innoventity.API/Features/Innovations/GetInnovation.cs

### Tests for User Stories 1, 2, 3

- [ ] T023 [US1] [US2] [US3] Update RegisterActorTests (6 tests) with new request body structure in tests/Innoventity.API.Tests/Integration/Features/Authentication/RegisterActorTests.cs
- [ ] T024 [P] [US2] Update LoginTests (5 tests) with new response assertions in tests/Innoventity.API.Tests/Integration/Features/Authentication/LoginTests.cs
- [ ] T025 [P] [US2] Update ActivateAccountTests (3 tests) for seed data changes in tests/Innoventity.API.Tests/Integration/Features/Authentication/ActivateAccountTests.cs
- [ ] T026 [P] [US2] Update RefreshTokenTests (2 tests) with new response assertions in tests/Innoventity.API.Tests/Integration/Features/Authentication/RefreshTokenTests.cs
- [ ] T027 [P] [US2] Update GetInnovationTests (4 tests) with new owner response assertions in tests/Innoventity.API.Tests/Integration/Features/Innovations/GetInnovationTests.cs
- [ ] T028 [P] [US2] Update Phase0JourneyTests (2 tests) with new Actor structure in tests/Innoventity.API.Tests/Integration/E2E/Phase0JourneyTests.cs

**Checkpoint**: At this point, all API endpoints work with new Actor schema. All integration tests pass. User Stories 1, 2, 3 are complete and independently testable.

---

## Phase 5: Polish & Validation

**Purpose**: Comprehensive validation and quality assurance

- [ ] T029 Run full test suite with `dotnet test --verbosity normal` (expect 42+ tests passing)
- [ ] T030 Verify migration checklist (migration.md) - all MIG items resolved
- [ ] T031 Verify breaking changes documentation (breaking-changes.md) complete

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - start immediately
- **Foundational (Phase 2)**: Depends on Setup - BLOCKS all user stories
- **Actor Refactoring (Phase 3)**: Depends on Foundational completion (EntityBase must exist)
- **API Updates (Phase 4)**: Depends on Actor Refactoring completion (schema must be migrated)
- **Polish (Phase 5)**: Depends on API Updates completion

### User Story Dependencies

- **User Story 4 (EntityBase)**: No dependencies - FOUNDATIONAL for all entities
- **User Story 1 (PasswordSalt)**: Depends on US4 (Actor inherits EntityOfGuid)
- **User Story 2 (Names)**: Depends on US4 (Actor inherits EntityOfGuid)
- **User Story 3 (Address)**: Depends on US4 (Actor inherits EntityOfGuid)
- **US1 + US2 + US3**: Implemented together (single Actor schema migration)

### Within Phase 2 (Foundational)

- T002, T003, T004 marked [P] - can run in parallel (different files)
- T005-T007 sequential - entity updates depend on base classes
- T008, T009 marked [P] - test creation can run in parallel
- T010 sequential - runs after T008, T009

### Within Phase 3 (Actor Refactoring)

- T011 marked [P] - Address class independent of Actor changes
- T012-T015 sequential - migration depends on entity, seed data depends on migration
- T016, T017 marked [P] - test files independent
- T018 sequential - migration application depends on all implementation

### Within Phase 4 (API Updates)

- T020, T021, T022 marked [P] - independent endpoint files
- T023 sequential - depends on T019 (Register endpoint must be updated first)
- T024-T028 marked [P] - independent test files
- All depend on Phase 3 completion

### Parallel Opportunities

**Phase 2 Parallel Tasks** (after T005-T007 complete):
```bash
# Create all base classes in parallel:
Task T002: EntityBase.cs
Task T003: EntityOfGuid.cs
Task T004: EntityOfInt32.cs

# Create test files in parallel:
Task T008: EntityBaseTests.cs
Task T009: ActorTests updates
```

**Phase 3 Parallel Tasks**:
```bash
# Independent test files:
Task T016: AddressTests.cs
Task T017: ActorTests updates
```

**Phase 4 Parallel Tasks** (after T019 complete):
```bash
# Independent endpoint updates:
Task T020: Login.cs
Task T021: RefreshToken.cs
Task T022: GetInnovation.cs

# Independent test updates:
Task T024: LoginTests.cs
Task T025: ActivateAccountTests.cs
Task T026: RefreshTokenTests.cs
Task T027: GetInnovationTests.cs
Task T028: Phase0JourneyTests.cs
```

---

## Implementation Strategy

### MVP First (Phase 0.5 Only)

Phase 0.5 IS the MVP for domain model refactoring:

1. Complete Phase 1: Setup (verify ready)
2. Complete Phase 2: Foundational (EntityBase - CRITICAL blocker)
3. Complete Phase 3: Actor Refactoring (schema changes + migration)
4. Complete Phase 4: API Updates (all endpoints working)
5. Complete Phase 5: Validation (42+ tests passing)
6. **STOP and VALIDATE**: Test full API surface independently
7. Deploy to staging for validation

### Incremental Delivery Not Possible

**Note**: User Stories 1, 2, 3 CANNOT be delivered incrementally because they are tightly coupled in the Actor entity schema. A single database migration handles all three changes. Attempting to split would require multiple migrations and increase rollback complexity.

### Sequential Execution (Solo Developer)

Recommended execution order:

1. **Day 1 Morning** (2 hours): Phase 2 - EntityBase infrastructure
   - T002-T010: Create base classes, update entities, write tests
   - **Checkpoint**: EntityBase tests passing (7 tests)

2. **Day 1 Afternoon** (3 hours): Phase 3 - Actor Schema
   - T011-T018: Address VO, Actor refactor, migration, seed data, tests
   - **Checkpoint**: Actor schema refactored, migration applied, domain tests passing

3. **Day 2 Morning** (2.5 hours): Phase 4 - API Updates
   - T019-T028: Update all endpoints and integration tests
   - **Checkpoint**: All API endpoints working with new schema

4. **Day 2 Afternoon** (0.5 hours): Phase 5 - Validation
   - T029-T031: Full test suite, checklist validation
   - **Checkpoint**: 42+ tests passing, ready for deployment

**Total Effort**: 8 hours (aligns with plan.md estimate of 7.5 hours + 0.5 buffer)

---

## Validation Checkpoints

### After Phase 2 (Entity Equality)

**Verify**:
- [ ] EntityBase tests: 7/7 passing
- [ ] Actor/Innovation/Industry inherit from correct base
- [ ] Entities can be added to HashSet without duplicates
- [ ] IsTransient() returns true for new entities

**Command**: `dotnet test --filter "FullyQualifiedName~EntityBase"`

### After Phase 3 (Actor Refactoring)

**Verify**:
- [ ] Database columns: FirstName, LastName, PasswordSalt, ContactAddress_Address1, etc.
- [ ] Migration executed successfully (FullName split, PasswordSalt backfilled)
- [ ] Seed data: 3 actors with FirstName/LastName/PasswordSalt
- [ ] Domain tests: AddressTests (3/3), ActorTests (6/6)

**Command**: `dotnet test --filter "Category=Unit&FullyQualifiedName~Entities"`

### After Phase 4 (API Updates)

**Verify**:
- [ ] Registration: Accepts firstName/lastName/contactAddress (structured)
- [ ] Login: Returns firstName/lastName/displayName
- [ ] GetInnovation: Owner includes firstName/lastName/displayName
- [ ] Integration tests: 25/25 passing

**Command**: `dotnet test --filter "Category=Integration"`

### Final Validation (Phase 5)

**Verify**:
- [ ] Full test suite: 42+ tests passing
- [ ] Build: Zero warnings, zero errors
- [ ] Migration: Down() rollback works (data preserved)
- [ ] Checklists: migration.md (SEC001, DATA001 resolved), planning.md (CHK021, CHK022 resolved)

**Command**: `dotnet test --verbosity normal`

---

## Notes

- [P] tasks = different files, no dependencies within phase
- [Story] label maps task to specific user story for traceability
- US1+US2+US3 combined due to tight coupling in Actor entity schema (single migration strategy)
- Tests are PART of feature delivery (TDD approach - write tests with implementation)
- Each phase has clear checkpoint validation
- Total task count: 31 tasks (1 setup + 9 foundational + 8 refactoring + 10 API updates + 3 validation)
- Estimated delivery: 2 days (solo developer, 8 hours total)
- Breaking changes: YES (Actor schema, API contracts) - see breaking-changes.md
- Rollback tested: YES (migration Down() method preserves data)
