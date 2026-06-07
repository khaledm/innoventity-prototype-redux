# Tasks: Partner Selection Workflow

**Feature**: Partner Selection Workflow
**Branch**: `004-partner-selection`
**Generated**: 2026-06-07
**Status**: ✅ COMPLETE — all tasks executed, all verification scenarios passing
**Note**: Reverse-engineered post-implementation to codify the task history.
**Input**: Design documents from `/specs/004-partner-selection/` (plan.md, spec.md)

**Tests**: Integration tests included (12 test methods covering all 8 spec scenarios + 4 guard paths). Tests validate HTTP response AND database state for each scenario.

**Organization**: Tasks grouped by implementation phase. Phases follow the natural dependency order of a Vertical Slice feature: domain → persistence → endpoint → tests → quality → spec encoding.

---

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to
- Include exact file paths in descriptions

---

## Phase 1: Domain Layer

**Purpose**: Add the sentinel field and navigation property that the entire feature depends on.

**⚠️ CRITICAL**: These entity changes are prerequisites for the EF migration and the endpoint.

- [X] T090 [P] [US SelectPartner-4] Add `PartnerSelectionCompletedOn` nullable `DateTimeOffset?` property to `Innovation` entity in `src/Innoventity.API/Domain/Entities/Innovation.cs`
- [X] T091 [P] [US SelectPartner-2] Add `Bids` inverse navigation property (`ICollection<Bid>?`) to `Innovation` entity in `src/Innoventity.API/Domain/Entities/Innovation.cs`

**Checkpoint**: Domain entity updated — EF configuration and migration can now proceed

---

## Phase 2: Persistence Layer

**Purpose**: Configure EF Core for the new property and relationship, then generate and validate the migration.

- [X] T092 [US SelectPartner-4] Configure `PartnerSelectionCompletedOn` as optional (`IsRequired(false)`) in `AppDbContext.OnModelCreating` in `src/Innoventity.API/Infrastructure/Persistence/AppDbContext.cs`
- [X] T093 [US SelectPartner-2] Update `Innovation → Bid` relationship to use `WithMany(i => i.Bids)` (enabling `Include(i => i.Bids).ThenInclude(b => b.Actor)` eager loading) in `src/Innoventity.API/Infrastructure/Persistence/AppDbContext.cs`
- [X] T095 Generate EF Core migration `AddPartnerSelectionCompletedOn` and verify the `Up()` migration adds `PartnerSelectionCompletedOn datetimeoffset NULL` to the `Innovations` table — `src/Innoventity.API/Infrastructure/Persistence/Migrations/20260606231900_AddPartnerSelectionCompletedOn.cs`
- [X] T096 [P] Update `appsettings.Development.json` connection string database name from `InnoventityDev` to `InnoventityDevelopment` for consistency with provisioned dev database

**Checkpoint**: Persistence layer ready — endpoint implementation can proceed

---

## Phase 3: Feature Endpoint (T094 — US SelectPartner-2, US SelectPartner-3, US SelectPartner-4)

**Goal**: Implement the `POST /innovations/{innovationId}/select-partners` endpoint with all validation steps and atomic mutation.

**Independent Test**: `POST /innovations/{id}/select-partners` with valid credentials, Published innovation, full readiness, and correct payload returns 200 with `PartnersSelected` status and accepted bid list.

- [X] T094 Register `app.MapSelectPartners()` in `src/Innoventity.API/Program.cs` (maps `POST /innovations/{innovationId}/select-partners` → `SelectPartners.Handle`)
- [X] T097 Create `src/Innoventity.API/Features/Bids/SelectPartners.cs` with:
  - `RequiredActorTypes` compile-time constant (`Manufacturing`, `SalesMarketing`, `RD`)
  - `SelectPartnersRequest` record (`Guid[] SelectedBidIds`)
  - `AcceptedBidSummary` record (`BidId`, `ActorId`, `ActorType`)
  - `SelectPartnersResponse` record (`InnovationId`, `Status`, `PartnerSelectionCompletedOn`, `AcceptedBids`)
  - `MapSelectPartners()` extension method with OpenAPI metadata and ProducesProblem declarations
- [X] T098 Implement `Handle` method validation pipeline in `src/Innoventity.API/Features/Bids/SelectPartners.cs`:
  - Step 1-2: JWT sub/NameIdentifier claim → Actor lookup → 401 if missing
  - Step 3: Load `Innovation` with `Include(i => i.Bids!).ThenInclude(b => b.Actor)` → 404 if not found
  - Step 4: Ownership check (FR-002/FR-011) → 403 "Only the innovation owner can select partners"
  - Step 5: Immutability check `PartnerSelectionCompletedOn != null` (FR-007) → 403 "Partner selection is final and cannot be changed"
  - Step 6: State check `Status == Published` (FR-016) → 409
  - Step 7: Readiness check — at least one Pending bid per required type (FR-005/FR-015) → 409 with missing type list
  - Step 8: Payload count == 3 (FR-004) → 422 ValidationProblem
  - Step 9: No duplicate bid IDs → 422 ValidationProblem
  - Step 10: All bid IDs belong to this innovation (FR-003) → 422 ValidationProblem
  - Step 11: No unsupported actor types (FR-014) → 422 ValidationProblem
  - Step 12: No duplicate actor types (FR-004) → 422 ValidationProblem
  - Step 14: Atomic mutation — selected bids → Accepted + AcceptedAt, remaining Pending → Rejected, innovation → PartnersSelected + PartnerSelectionCompletedOn = UtcNow; single `SaveChangesAsync` (FR-006/FR-012) → 200

**Checkpoint**: Endpoint fully implemented — integration tests can now be written and executed

---

## Phase 4: Integration Tests

**Goal**: Verify all 8 spec verification scenarios and 4 guard paths against real EF In-Memory database with `WebApplicationFactory<Program>`.

**Test file**: `tests/Innoventity.API.Tests/Integration/Features/Bids/SelectPartnersTests.cs`

- [X] T099 Create `SelectPartnersTests` class with `WebApplicationFactory<Program>`, in-memory EF database, JWT configuration, and `SeedTestData()` method seeding:
  - 6 actors (owner/IdeaGenerator, mfg, sales, rd, mfg2, investor)
  - 6 innovations (_publishedId/5 bids, _target1Id/3 bids, _target2Id/3 bids, _insufficientId/1 bid, _completedId/4 settled bids, _draftId/no bids)
  - `PostWithAuthAsync` helper via `TestFixtures`
- [X] T100 [P] [US SelectPartner-2] Implement Scenario 1: insufficient readiness → 409 with "Insufficient Readiness" and named missing types; assert innovation status unchanged
- [X] T101 [P] [US SelectPartner-3] Implement Scenario 2: missing required actor type in payload → 422 with "3 bid IDs" and `SelectedBidIds` error key; assert no mutations
- [X] T102 [US SelectPartner-2] Implement Scenario 3: valid selection → 200; assert response shape (`InnovationId`, `Status: PartnersSelected`, `PartnerSelectionCompletedOn`, 3 `AcceptedBids`); assert DB state (innovation status + all three bids Accepted with AcceptedAt)
- [X] T103 [US SelectPartner-4] Implement Scenario 4: repeated selection → 403 with "partner selection is final" and "immutable"; assert innovation status unchanged; assert existing Accepted/Rejected bids remain unchanged (AC3 bid-state preservation — seeded via `_cMfgBidId`, `_cSalesBidId`, `_cRdBidId`, `_cRejectedBidId`)
- [X] T104 [P] [US SelectPartner-3] Implement Scenario 5: duplicate actor type in payload → 422 with "duplicate actor types"; assert no mutations
- [X] T105 [P] [US SelectPartner-3] Implement Scenario 6: unsupported actor type (Investor) in payload → 422 with "not required for partner selection"; assert no mutations
- [X] T106 [US SelectPartner-2] Implement Scenario 7: exactly one eligible bid per required type → readiness passes and selection succeeds; assert innovation transitions to PartnersSelected
- [X] T107 [P] [US SelectPartner-2] Implement Scenario 8: innovation not in Published state (Draft) → 409 with "Published status"; assert innovation status unchanged
- [X] T108 [P] [US SelectPartner-2] Implement guard test: non-owner caller → 403 with "Forbidden" and "owner"
- [X] T109 [P] [US SelectPartner-3] Implement guard test: bid IDs from a different innovation → 422 with "do not belong to this innovation"
- [X] T110 [P] [US SelectPartner-3] Implement guard test: duplicate bid IDs in payload → 422 with "Duplicate bid IDs"; assert no mutations
- [X] T111 [US SelectPartner-2] Implement guard test: non-selected Pending bids are Rejected after successful selection — seeds _publishedId with 5 bids, selects 3, asserts _pMfg2BidId and _pInvestorBidId transition to Rejected

**Checkpoint**: All 12 test methods implemented and passing — feature verified against all spec scenarios

---

## Phase 5: Quality & Constitution Compliance

**Purpose**: Ensure mutation testing and threshold configuration comply with the project constitution.

- [X] T112 Add `"Features/Bids/SelectPartners.cs"` to the `mutate` list in `src/Innoventity.API/stryker-config.json`
- [X] T113 Restore Stryker `low` threshold to `70` (constitution §5 requirement: mutation score > 70%); keep `break: 60` as absolute floor; keep `high: 80` unchanged

**Checkpoint**: Quality gates constitution-compliant

---

## Phase 6: Spec Encoding (Post-Implementation Reconciliation)

**Purpose**: Reconcile the specification with the actual implementation to eliminate drift. Findings from `/speckit-analyze` drove all edits.

- [X] T114 [P] Fix C1/I2 — Replace all occurrences of `InCollaboration` with `PartnersSelected` in `specs/004-partner-selection/spec.md` (State Transitions section + US SelectPartner-2 AC3 + US SelectPartner-4 guard phrasing); update guard line to reference `PartnerSelectionCompletedOn` mechanism
- [X] T115 [P] Fix I1/I3 — Canonicalize `ResearchDevelopment` → `RD` across `specs/004-partner-selection/spec.md` (Clarifications, Clarified Scope, Preconditions, FR-013, Required Actor-Type Matrix) and across `src/Innoventity.API/Features/Bids/SelectPartners.cs` (L115 readiness error, L132 payload-count error, L161 unsupported-type error, L223 XML doc comment)
- [X] T116 Fix U3 — Add `## API Response Contract` section to `specs/004-partner-selection/spec.md` documenting the 200 response shape (JSON example + field descriptions) and the full error response table (401/403×2/404/409×2/422) with RFC 7807 Problem Details notes
- [X] T117 Fix U2 — Extend `SelectPartnersTests.cs` Scenario 4 fixture: add `_cMfgBidId`, `_cSalesBidId`, `_cRdBidId`, `_cRejectedBidId` fields with GUIDs in `cc000041–cc000044` range; seed 3 Accepted + 1 Rejected bids on `_completedId` in `SeedTestData()`; assert all 4 bids remain at their original status after the 403 response

**Checkpoint**: Spec fully encodes implementation reality — no drift between spec and code

---

## Phase 7: Plan & Tasks Codification

**Purpose**: Create this plan.md and tasks.md to codify the implementation as a durable artifact.

- [X] T118 [P] Generate `specs/004-partner-selection/plan.md` capturing Technical Context, Constitution Check, Architecture, Key Design Decisions, and Success Criteria
- [X] T119 [P] Generate `specs/004-partner-selection/tasks.md` (this file) capturing the full reverse-engineered task history

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Domain: T090-T091)
  ↓
Phase 2 (Persistence: T092-T096) — cannot configure EF without domain changes
  ↓
Phase 3 (Endpoint: T094, T097-T098) — endpoint requires both domain and navigation property
  ↓
Phase 4 (Tests: T099-T111) — tests require the endpoint to exist
  ↓
Phase 5 (Quality: T112-T113) — Stryker config updated after code exists
  │
  ├→ Phase 6 (Spec Encoding: T114-T117) — can run in parallel with Phase 5
  └→ Phase 7 (Codification: T118-T119) — run after all implementation complete
```

### Within Each Phase

- T090 and T091 are parallel (different concerns on the same file, but logically independent)
- T092 and T093 are sequential (both in `AppDbContext.OnModelCreating`, risk merge conflict if parallel)
- T097 and T098 split the file creation from the method body for traceability (atomic in practice)
- T100–T111 are all parallel once T099 (seed data) is complete
- T114, T115, T116, T117 are parallel (different sections of spec or different files)

### Parallel Opportunities

- **Phase 1**: T090 + T091 (different properties on Innovation)
- **Phase 4**: T100–T111 (all test methods independent once fixture is seeded)
- **Phase 6**: T114 + T115 + T116 + T117 (different files/sections)
- **Phase 7**: T118 + T119 (different files)

---

## Verification Scenarios → Test Method Map

| Spec Scenario | Test Method | Status |
|---------------|-------------|--------|
| 1: Insufficient readiness → 409 | `SelectPartners_InsufficientReadiness_Returns409` | ✅ |
| 2: Missing actor type → 422 | `SelectPartners_MissingRequiredActorType_Returns422` | ✅ |
| 3: Valid selection → 200 | `SelectPartners_ValidSelection_Returns200AndTransitions` | ✅ |
| 4: Repeated attempt → 403 + AC3 bid preservation | `SelectPartners_AlreadyCompleted_Returns403WithImmutableMessage` | ✅ |
| 5: Duplicate actor type → 422 | `SelectPartners_DuplicateActorType_Returns422` | ✅ |
| 6: Unsupported actor type → 422 | `SelectPartners_UnsupportedActorType_Returns422` | ✅ |
| 7: Exactly one eligible bid → success | `SelectPartners_ExactlyOneEligiblePerType_ReadinessPassesAndSelectionSucceeds` | ✅ |
| 8: Non-Published state → 409 | `SelectPartners_InnovationNotPublished_Returns409` | ✅ |
| Guard: Non-owner → 403 | `SelectPartners_NonOwner_Returns403` | ✅ |
| Guard: Foreign bids → 422 | `SelectPartners_BidsFromDifferentInnovation_Returns422` | ✅ |
| Guard: Duplicate bid IDs → 422 | `SelectPartners_DuplicateBidIds_Returns422` | ✅ |
| Guard: Non-selected Pending → Rejected | `SelectPartners_NonSelectedPendingBidsAreRejected` | ✅ |

---

**Total Tasks**: 30 (T090–T119, with T094 used for endpoint registration per `Program.cs` comment)
**Branch**: `004-partner-selection`
**Status**: All tasks complete. Feature ready for PR.
