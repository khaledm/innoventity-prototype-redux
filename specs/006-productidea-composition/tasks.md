---

description: "Task list for ProductIdea Composition Pattern"

---

# Tasks: ProductIdea Composition Pattern

**Input**: Design documents from `/specs/006-productidea-composition/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/innovations-api.md](./contracts/innovations-api.md), [quickstart.md](./quickstart.md)

**Tests**: Included — spec.md's Definition of Done and constitution Principle 5 (Tests Must Prove They Work) require test-first validation for this feature's completeness-rule and data-preservation guarantees.

**Organization**: Tasks are grouped by user story (US1–US4, matching spec.md priorities). Backend tasks (US1–US3) are marked `[X]` — implemented, committed, and verified passing (268/268 `dotnet test`) as of this update. US4 (Angular client) is genuinely unstarted and marked `[ ]`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- **[X]**: Already completed and verified (per this update) — **[ ]**: not yet done

## Path Conventions

Web app per plan.md: `src/Innoventity.API/` (backend), `src/Innoventity.Client/src/app/` (frontend), `tests/Innoventity.API.Tests/` (backend tests).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: No new project scaffolding needed — this feature modifies an existing entity and its consumers within the established Vertical Slice Architecture.

- [X] T001 Confirm branch `006-productidea-composition` checked out and backend/frontend projects build (`dotnet build -c Release --no-restore`, `npm install` in `src/Innoventity.Client/`)

**Checkpoint**: No blocking setup — proceed directly to Foundational.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Owned entity types and their EF Core mapping MUST exist before any user story's endpoints or tests can compile.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T002 [P] Create `IdeaSummary` owned entity with `Validate()` and `IsComplete()` in `src/Innoventity.API/Domain/Entities/IdeaSummary.cs`
- [X] T003 [P] Create `Product` owned entity with `Validate()` and `IsComplete()` in `src/Innoventity.API/Domain/Entities/Product.cs`
- [X] T004 [P] Create `Market` owned entity with `Validate()` and `IsComplete()` in `src/Innoventity.API/Domain/Entities/Market.cs`
- [X] T005 [P] Create `CollaborationRequirement` owned entity in `src/Innoventity.API/Domain/Entities/CollaborationRequirement.cs`
- [X] T006 Compose `Innovation` root with `IdeaSummary`/`Product`/`Market`/`CollaborationRequirement` navigation properties and the four completeness methods (`IsIdeaSummaryComplete`, `IsProductDetailsComplete`, `IsMarketDetailsSectionComplete`, `IsReadyForSubmission`) in `src/Innoventity.API/Domain/Entities/Innovation.cs` (depends on T002-T005)
- [X] T007 Map all four owned types via `OwnsOne` with explicit `HasColumnName`/length/precision matching original flat columns in `src/Innoventity.API/Infrastructure/Persistence/AppDbContext.cs` (depends on T006)
- [X] T008 Generate EF Core migration `ComposeInnovationSections` and verify empty `Up()`/`Down()` bodies (zero data loss) in `src/Innoventity.API/Infrastructure/Persistence/Migrations/20260823142911_ComposeInnovationSections.cs` (depends on T007)
- [X] T009 Update `SeedData.cs` to construct innovations via the four owned-entity objects in `src/Innoventity.API/Infrastructure/Persistence/SeedData.cs` (depends on T006)

**Checkpoint**: Foundation ready — verified via `dotnet build -c Release --no-restore` (0 warnings, 0 errors) and `dotnet ef migrations list` showing `ComposeInnovationSections` after `AddFormalResponseHierarchy`.

---

## Phase 3: User Story 1 - Innovation Data Organized into Logical Sections (Priority: P1) 🎯 MVP

**Goal**: Innovation create/retrieve/update responses group every field under `ideaSummary`/`product`/`market`/`collaborationRequirement`, with no field lost or renamed beyond its section placement.

**Independent Test**: Create an innovation, retrieve it, verify the response groups every field under its correct section per the field-mapping table (spec.md), with no field lost.

### Tests for User Story 1

- [X] T010 [P] [US1] Unit tests for `IdeaSummary.Validate()`/`IsComplete()` covering placeholder-title, required-field, and boundary-length cases in `tests/Innoventity.API.Tests/Unit/Domain/Entities/OwnedSections/IdeaSummaryTests.cs`
- [X] T011 [P] [US1] Unit tests for `Product.Validate()` AND `IsComplete()` (11 cases added: happy path, per-required-field empty/whitespace/null via `[Theory]`, optional-fields-ignored case) in `tests/Innoventity.API.Tests/Unit/Domain/Entities/OwnedSections/ProductTests.cs`
- [X] T012 [P] [US1] Unit tests for `Market.Validate()` AND `IsComplete()` (boundary cases for `RelevantMarketSize`/`PotentialMarketSize`: null/0/negative/smallest-positive; `decimal?` `[Theory]` data passed as `string` and parsed in-test, since xUnit's `InlineData` cannot bind `int`/`double` literals to a `decimal?` parameter) in `tests/Innoventity.API.Tests/Unit/Domain/Entities/OwnedSections/MarketTests.cs`
- [X] T013 [P] [US1] Unit tests for `CollaborationRequirement` construction/defaults (default value is `string.Empty`, not `null`; explicit-null override; comma-separated parsing) in `tests/Innoventity.API.Tests/Unit/Domain/Entities/OwnedSections/CollaborationRequirementTests.cs`
- [X] T014 [US1] Update `InnovationTests.cs` to construct innovations via the four owned-entity objects and assert `IsIdeaSummaryComplete`/`IsProductDetailsComplete`/`IsMarketDetailsSectionComplete`/`IsReadyForSubmission`, including per-section isolation cases (incomplete `Product`, zero `RelevantMarketSize`, whitespace-only `PartnersNeeded`) so a regression in one section's wiring can't hide behind another section's pass in `tests/Innoventity.API.Tests/Unit/Domain/Entities/InnovationTests.cs`
- [X] T015 [US1] Integration test asserting `GET /innovations/{id}` returns nested `ideaSummary`/`product`/`market`/`collaborationRequirement` in `tests/Innoventity.API.Tests/Integration/Features/Innovations/GetInnovationTests.cs`
- [X] T016 [US1] Integration test asserting `PUT /innovations/{id}` response nests `ideaSummary` in `tests/Innoventity.API.Tests/Integration/Features/Innovations/UpdateInnovationTests.cs`

### Implementation for User Story 1

- [X] T017 [US1] Update `CreateInnovation.cs` to construct the four owned entities from the flat create request in `src/Innoventity.API/Features/Innovations/CreateInnovation.cs`
- [X] T018 [US1] Restructure `GetInnovation.cs` response into nested `ideaSummary`/`product`/`market`/`collaborationRequirement` objects, including previously-missing fields (`technologyDescription`, `targetBeneficiaries`, market sizes, `partnersNeeded`) in `src/Innoventity.API/Features/Innovations/GetInnovation.cs` (depends on T007)
- [X] T019 [US1] Restructure `UpdateInnovation.cs` partial-update logic to reconstruct owned entities per-field with existing-value fallback, and nest `ideaSummary` in the response in `src/Innoventity.API/Features/Innovations/UpdateInnovation.cs` (depends on T007)
- [X] T020 [US1] Restructure `ListInnovations.cs` list items to nest `ideaSummary` and derive `partnersNeeded` from `CollaborationRequirement` in `src/Innoventity.API/Features/Innovations/ListInnovations.cs` (depends on T007)
- [ ] T021 [P] [US1] Remove dead commented-out flat-field update code and orphaned `ResearchCategory` parse block left over from the mechanical migration in `src/Innoventity.API/Features/Innovations/UpdateInnovation.cs` (code-quality follow-up identified in review; not spec-blocking)

**Checkpoint**: User Story 1 verified independently — `dotnet test --filter "FullyQualifiedName~GetInnovationTests|FullyQualifiedName~ListInnovationsTests|FullyQualifiedName~UpdateInnovationTests|FullyQualifiedName~CreateInnovationTests|FullyQualifiedName~OwnedSections|FullyQualifiedName~InnovationTests"` passes; manual `GET /innovations/{id}` via Swagger confirms nested shape per SC-004. Full suite: 268/268 passing (was 241 before T011-T014's `IsComplete()` coverage gap closure).

---

## Phase 4: User Story 2 - Submission Gate Behavior Preserved via Section Validation (Priority: P1)

**Goal**: The publish step enforces the same 13 completeness rules as before, now delegated to `IsReadyForSubmission()`, with identical accept/reject outcomes and per-field error reporting.

**Independent Test**: Attempt to publish innovations in various incomplete states; verify accept/reject decision and identified incomplete fields are equivalent to prior behavior for all 13 rules.

### Tests for User Story 2

- [X] T022 [US2] Integration test asserting all 13 rules' failure keys appear in `POST .../submit` 400 response in `tests/Innoventity.API.Tests/Integration/Features/Innovations/SubmitInnovationTests.cs` (`SubmitInnovation_Incomplete_Returns400WithErrors`)
- [X] T023 [US2] Integration test asserting a fully-complete innovation publishes successfully in `tests/Innoventity.API.Tests/Integration/Features/Innovations/SubmitInnovationTests.cs` (`SubmitInnovation_Complete_Returns200AndPublishes`)
- [X] T024 [US2] Integration test asserting already-published and non-owner submissions are rejected unchanged in `tests/Innoventity.API.Tests/Integration/Features/Innovations/SubmitInnovationTests.cs` (`SubmitInnovation_AlreadyPublished_Returns409Conflict`, `SubmitInnovation_AsNonOwner_Returns403Forbidden`)
- [X] T025 [US2] Regression test proving the endpoint's accept/reject decision always agrees with `innovation.IsReadyForSubmission()` (guards against the two logic paths silently diverging) in `tests/Innoventity.API.Tests/Integration/Features/Innovations/SubmitInnovationTests.cs` (`SubmitInnovation_PlaceholderTitleOnly_Returns400AndMatchesIsReadyForSubmission`)

### Implementation for User Story 2

- [X] T026 [US2] Fix inverted placeholder-title check (`||` → `&&`) in `IdeaSummary.IsComplete()` in `src/Innoventity.API/Domain/Entities/IdeaSummary.cs` (bug found via T010's test-first pass)
- [X] T027 [US2] Update `SubmitInnovation.cs`'s 13 field-level checks to read through the nested owned entities (`innovation.IdeaSummary.Title`, etc.) for error-detail reporting in `src/Innoventity.API/Features/Innovations/SubmitInnovation.cs`
- [X] T028 [US2] Delegate the publish accept/reject decision to `innovation.IsReadyForSubmission()` instead of `validationErrors.Any()`, per FR-005, in `src/Innoventity.API/Features/Innovations/SubmitInnovation.cs` (depends on T006, T026, T027)
- [ ] T029 [US2] Section-qualify validation error keys (e.g., `ideaSummary.title` instead of bare `Title`) per FR-008's "field + section identifiable" requirement in `src/Innoventity.API/Features/Innovations/SubmitInnovation.cs` (open item noted in contracts/innovations-api.md; current bare keys satisfy FR-008 only via the external rule-mapping table, not self-descriptively)

**Checkpoint**: User Story 2 verified independently — `dotnet test --filter "FullyQualifiedName~SubmitInnovationTests"` passes (5/5); the placeholder-title bug (T026) was caught precisely because T010/T025 were written test-first before T028's delegation change.

---

## Phase 5: User Story 3 - Existing Innovation Data Survives the Upgrade (Priority: P1)

**Goal**: Every existing innovation record retains all field values through the structural upgrade — zero data loss, zero re-entry, reversible migration.

**Independent Test**: Capture all innovation rows before the upgrade, apply it, verify row counts match and every field value is readable at its new location with identical content.

### Tests for User Story 3

- [X] T030 [US3] Verify migration `ComposeInnovationSections` has empty `Up()`/`Down()` bodies (proving zero physical schema change) — validated by inspection during T008, re-confirmed in `src/Innoventity.API/Infrastructure/Persistence/Migrations/20260823142911_ComposeInnovationSections.cs`
- [X] T031 [US3] Full regression suite run confirming zero data-shape regressions across Bids/formal-response/auth/discovery flows in `tests/Innoventity.API.Tests/` (`dotnet test` — 268/268 passing)

### Implementation for User Story 3

- [X] T032 [US3] Correct `AppDbContext.cs` `OwnsOne` column configuration to exactly match the last-committed `AppDbContextModelSnapshot.cs` lengths/precision/conversions (`ResearchCategory` string conversion, `TechnologyDescription` 5000, `PartnersNeeded` 500, `RelevantMarketSize`/`PotentialMarketSize` precision(28,2), restored `ProductKeywords`/`AdvantageKeywords` config) in `src/Innoventity.API/Infrastructure/Persistence/AppDbContext.cs` (depends on T007; required to eliminate the "operation may result in loss of data" warning `dotnet ef migrations add` initially raised)

**Checkpoint**: User Story 3 verified independently — migration inspected and confirmed no-op; `dotnet ef migrations add` produced no data-loss warning on the corrected configuration.

---

## Phase 6: User Story 4 - Existing Client Screens Keep Working (Priority: P2)

**Goal**: Existing Angular innovation screens keep functioning after the backend restructure — invisible to the end user.

**Independent Test**: Run existing client unit tests and E2E journeys against the updated client; verify they pass without reducing coverage or removing assertions.

**Status**: NOT STARTED. The backend now returns a nested payload shape (Phase 3); the Angular client still expects the old flat shape. This is real, scoped, pending work — confirmed by inspection: `innovation.model.ts` declares `InnovationDetail`/`InnovationListItem` as flat interfaces, and exactly two components consume innovation fields directly.

### Tests for User Story 4

- [ ] T033 [P] [US4] Update `innovations.service.spec.ts` fixtures/mocks to the nested response shape in `src/Innoventity.Client/src/app/core/services/innovations.service.spec.ts`
- [ ] T034 [P] [US4] Update `innovation-detail.component.spec.ts` fixtures to nested shape and confirm existing assertions still hold in `src/Innoventity.Client/src/app/features/innovations/innovation-detail/innovation-detail.component.spec.ts`
- [ ] T035 [P] [US4] Update `innovations-list.component.spec.ts` fixtures to nested shape in `src/Innoventity.Client/src/app/features/innovations/innovations-list/innovations-list.component.spec.ts`

### Implementation for User Story 4

- [ ] T036 [US4] Update `InnovationDetail` and `InnovationListItem` interfaces to nest `ideaSummary`/`product`/`market`/`collaborationRequirement` per contracts/innovations-api.md in `src/Innoventity.Client/src/app/core/models/innovation.model.ts` (depends on T018, T020 — must match the actual API shape)
- [ ] T037 [US4] Fix all now-broken field accesses (`innovation.title` → `innovation.ideaSummary.title`, etc.) surfaced by the TypeScript compiler in `src/Innoventity.Client/src/app/features/innovations/innovation-detail/innovation-detail.component.ts` (depends on T036)
- [ ] T038 [US4] Fix all now-broken field accesses in the list view in `src/Innoventity.Client/src/app/features/innovations/innovations-list/innovations-list.component.ts` (depends on T036)
- [ ] T039 [US4] Confirm `innovations.service.ts` requires no logic changes (pass-through typed HTTP calls only) — verify via `npm run build` in `src/Innoventity.Client/src/app/core/services/innovations.service.ts` (depends on T036)

**Checkpoint**: User Story 4 will be verified via `npm test` (all three spec files green, no reduced assertion count) and `npm run e2e` (existing journeys touching innovation screens still pass) — not yet run, pending T033-T039.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements spanning multiple user stories; spec.md Success Criteria not yet explicitly checkpointed.

- [ ] T040 [P] Run `dotnet stryker` from `src/Innoventity.API/` to confirm mutation score ≥80% (floor >70%) on the owned-entity/`Innovation` completeness methods per SC-005 — `stryker-config.json`'s `mutate` list now includes `Innovation.cs`/`IdeaSummary.cs`/`Product.cs`/`Market.cs`/`CollaborationRequirement.cs` (previously scoped only to Auth/Bids from an earlier feature; updated but not yet executed — Stryker.NET is not installed in this environment)
- [ ] T041 Run full `quickstart.md` validation end-to-end (backend + frontend sections) once Phase 6 completes
- [ ] T042 [P] Update `CLAUDE.md`'s active-feature pointer to reference this `tasks.md` now that it exists (currently references only spec.md)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — DONE
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories — DONE
- **User Story 1 (Phase 3)**: Depends on Foundational — DONE except T013 (test gap), T021 (cleanup)
- **User Story 2 (Phase 4)**: Depends on Foundational; reads owned entities from US1's endpoints but is independently testable via the `submit` endpoint alone — DONE except T029 (error-key follow-up)
- **User Story 3 (Phase 5)**: Depends on Foundational (specifically T007) — DONE
- **User Story 4 (Phase 6)**: Depends on User Story 1's response shape being final (T018, T020) — NOT STARTED
- **Polish (Phase 7)**: Depends on all desired user stories being complete

### User Story Dependencies

- **US1, US2, US3**: All backend, all complete; US2 and US3 both build on US1's foundational entity work but were verified independently per their own checkpoints
- **US4**: Depends on US1's finalized nested response shape (cannot start meaningfully before T018/T020 exist) — this is why it's sequenced last despite being P2, not P3: it's a downstream consumer of P1 work, not a lower-priority independent story

### Parallel Opportunities

- T002-T005 (four owned entity files) — different files, no dependencies — ran in parallel
- T010-T013 (owned-entity unit test files) — different files — ran in parallel; all now complete
- T033-T035 (client spec file updates) — different files — can run in parallel once T036 lands
- T040, T042 — independent of each other and of T041

---

## Parallel Example: User Story 4 (remaining work)

```bash
# After T036 (model update) lands, these can proceed in parallel:
Task: "Fix field accesses in innovation-detail.component.ts"
Task: "Fix field accesses in innovations-list.component.ts"

# Spec updates can start in parallel with the above, then be finalized once compiler errors are resolved:
Task: "Update innovations.service.spec.ts fixtures"
Task: "Update innovation-detail.component.spec.ts fixtures"
Task: "Update innovations-list.component.spec.ts fixtures"
```

---

## Implementation Strategy

### Status Summary

- **MVP (US1)**: ✅ DONE — backend nested composition shipped, tested, committed; `IsComplete()` coverage gap on `Product`/`Market`/`CollaborationRequirement` (originally tracked as T013 and flagged in code review) has since been closed — 268/268 tests passing
- **US2 (submission gate)**: ✅ DONE — delegation to `IsReadyForSubmission()` verified via regression test
- **US3 (data preservation)**: ✅ DONE — migration verified as a no-op; zero data-loss warning
- **US4 (client)**: ❌ NOT STARTED — next actionable increment

### Recommended Next Increment

1. T036 (client model) → unblocks T037, T038 in parallel
2. T037, T038 (component fixes) → unblocks T033-T035 (spec fixture updates, parallel)
3. T039 (service sanity check) can run any time after T036
4. Validate via `npm test` + `npm run e2e`
5. T029 (error-key section-qualification) and T021 (dead-code cleanup) are independent, low-risk polish items that can be picked up whenever — not on the US4 critical path
6. T040 (Stryker mutation testing) has two prerequisites now both met: (a) `Product.IsComplete()`/`Market.IsComplete()`/`CollaborationRequirement` all have direct unit coverage as of T011-T013, and (b) `stryker-config.json`'s `mutate` list now includes the five owned-entity/Innovation files (it previously only covered Auth/Bids). The actual run has not been executed in this environment (Stryker.NET not installed) — run it locally or in CI to get the real score before treating SC-005 as satisfied

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- [X] = implemented and test-verified as of this update. T002-T032 (Setup through US3) are committed on commits `4af0775`..`85ca4a3`..`37d0f35`. T011-T014's `IsComplete()` coverage additions (Product/Market/CollaborationRequirement units + Innovation-level isolation cases) and this plan/tasks artifact set are implemented and passing locally (268/268) but **not yet committed** — pending a commit per constitution Principle 8.
- Remaining [ ] tasks are genuine, unstarted work — verified by direct inspection of the client codebase, not assumed
- Commit after each task or logical group, per constitution Principle 8 (Conventional Commits)
