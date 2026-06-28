# Tasks: FormalResponse Polymorphic Hierarchy

**Input**: Design documents from `specs/005-formalresponse-hierarchy/`

**Prerequisites**: plan.md ✅ spec.md ✅ research.md ✅ data-model.md ✅ contracts/ ✅ quickstart.md ✅

**Tests**: Integration tests are included per Principle 5 (Tests Must Prove They Work). Write tests FIRST — they must fail before implementation begins (red → green → refactor).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

---

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies between marked tasks)
- **[Story]**: User story the task belongs to (US1–US6)
- Foundation phases have no story label

---

## Phase 1: Foundational — Domain Layer

**Purpose**: New entity types, enums, and value objects that every user story depends on. All user story work is blocked until this phase compiles cleanly.

**⚠️ CRITICAL**: No user story work can begin until Phase 2 (Persistence) is complete.

- [ ] T001 [P] Add `GeographicRegion` enum in `src/Innoventity.API/Domain/Entities/GeographicRegion.cs` — values: Asia, Americas, Europe, Africa, Oceania; stored as string in DB
- [ ] T002 [P] Add `ResponseStatus` enum in `src/Innoventity.API/Domain/Entities/ResponseStatus.cs` — values: Pending, Accepted, Rejected; stored as string in DB
- [ ] T003 [P] Add `YearlyManufacturingCost` value object (no EF identity key) in `src/Innoventity.API/Domain/Entities/YearlyManufacturingCost.cs` — properties: Year, ProductionVolume, ProductionVolumeRationale, UnitCost, UnitCostRationale, AverageGlobalDistributionExpense, AvgDistributionExpenseRationale
- [ ] T004 [P] Add `YearlySale` value object in `src/Innoventity.API/Domain/Entities/YearlySale.cs` — properties: Year, UnitsSold, UnitsSoldRationale, UnitPrice, UnitPriceRationale, SalesMarketingExpense, SalesMarketingExpenseRationale
- [ ] T005 [P] Add `YearlyDevelopmentCost` value object in `src/Innoventity.API/Domain/Entities/YearlyDevelopmentCost.cs` — properties: Year, InfrastructureCost, InfrastructureCostRationale, PeopleCost, PeopleCostRationale
- [ ] T006 Add abstract `FormalResponse` base class (EntityOfGuid) in `src/Innoventity.API/Domain/Entities/FormalResponse.cs` — properties: InnovationId, Innovation?, ActorId, Actor?, Location (GeographicRegion), ParticipationType [MaxLength(100)], ParticipationProposal [MaxLength(5000)], Status (ResponseStatus default Pending), SubmittedAt, UpdatedAt?, AcceptedAt?
- [ ] T007 [P] Add `ManufacturingResponse` in `src/Innoventity.API/Domain/Entities/ManufacturingResponse.cs` — inherits FormalResponse; adds `IList<YearlyManufacturingCost> YearlyManufacturingCosts`
- [ ] T008 [P] Add `SalesMarketingResponse` in `src/Innoventity.API/Domain/Entities/SalesMarketingResponse.cs` — inherits FormalResponse; adds `IList<YearlySale> YearlySales`
- [ ] T009 [P] Add `ResearchDevelopmentResponse` in `src/Innoventity.API/Domain/Entities/ResearchDevelopmentResponse.cs` — inherits FormalResponse; adds `int ProductDevelopmentDuration` and `IList<YearlyDevelopmentCost> YearlyDevelopmentCosts`
- [ ] T010 [P] Add `InvestorResponse` in `src/Innoventity.API/Domain/Entities/InvestorResponse.cs` — inherits FormalResponse; adds `string Feedback` [MaxLength(2000)]
- [ ] T011 Modify `Innovation.cs` in `src/Innoventity.API/Domain/Entities/Innovation.cs` — rename nav property `ICollection<Bid> Bids` → `ICollection<FormalResponse> FormalResponses`
- [ ] T012 Delete `src/Innoventity.API/Domain/Entities/Bid.cs` and `src/Innoventity.API/Domain/Entities/BidStatus.cs`
- [ ] T013 Run `dotnet build` from repo root — expect compilation errors ONLY in consumers (AppDbContext, SeedData, SelectPartners, GetBids, Program.cs); no errors in Domain/Entities/

**Checkpoint**: Domain layer complete — 10 new files, 1 modified, 2 deleted.

---

## Phase 2: Foundational — Persistence Layer

**Purpose**: Wire EF Core TPH, JSON columns for yearly projections, generate and apply the migration.

**⚠️ CRITICAL**: Completes the foundation. User story implementation begins after this phase.

- [ ] T014 Update `AppDbContext.cs` in `src/Innoventity.API/Infrastructure/Persistence/AppDbContext.cs` — remove `DbSet<Bid> Bids` and Bid config; add `DbSet<FormalResponse> FormalResponses`; configure TPH with `HasDiscriminator<string>("ResponseType")` and four `.HasValue()` mappings; add `OwnsMany(x => x.YearlyManufacturingCosts).ToJson()` for ManufacturingResponse, `OwnsMany(x => x.YearlySales).ToJson()` for SalesMarketingResponse, `OwnsMany(x => x.YearlyDevelopmentCosts).ToJson()` for ResearchDevelopmentResponse; replace `IX_Bid_ActorId_InnovationId` unique index with `IX_FormalResponse_ActorId_InnovationId`; update FK relationships to use `i => i.FormalResponses`
- [ ] T015 [P] Update `SeedData.cs` in `src/Innoventity.API/Infrastructure/Persistence/SeedData.cs` — replace all `new Bid { ... }` instances with typed FormalResponse subtypes (ManufacturingResponse, SalesMarketingResponse, ResearchDevelopmentResponse as needed); include at least one representative of each financial type for integration test seeds
- [ ] T016 Generate EF Core migration from `src/Innoventity.API/`: `dotnet ef migrations add AddFormalResponseHierarchy`
- [ ] T017 Review generated migration file in `src/Innoventity.API/Infrastructure/Persistence/Migrations/*AddFormalResponseHierarchy.cs` — verify Up() creates `FormalResponses` table with all columns and JSON columns; augment Up() with `migrationBuilder.Sql("INSERT INTO FormalResponses ...")` to copy existing Bid rows (map ActorType to ResponseType discriminator, set empty JSON `'[]'` for projection columns); verify Down() recreates Bids table and reverses data movement
- [ ] T018 Apply migration from `src/Innoventity.API/`: `dotnet ef database update`
- [ ] T019 Run `dotnet build` from repo root — verify zero compilation errors (all consumer files compile with new FormalResponse types)

**Checkpoint**: Foundation complete — FormalResponses table live, TPH configured, seeds updated. User story implementation can now proceed.

---

## Phase 3: User Story 1 — Manufacturing Response Submission (Priority: P1) 🎯 MVP

**Goal**: Manufacturing actor can submit a typed response with structured yearly cost projections.

**Independent Test**: Submit a 3-year ManufacturingResponse; retrieve via GET /bids as owner; verify all 3 `YearlyManufacturingCost` entries stored with correct rationale fields and `ResponseType = "ManufacturingResponse"`.

> **⚠️ Write tests FIRST — verify they FAIL before implementing the handler (Principle 5)**

- [ ] T020 [P] [US1] Write `SubmitManufacturingResponseTests.cs` in `tests/Innoventity.API.Tests/Integration/Features/Bids/SubmitManufacturingResponseTests.cs` — 11 tests: (1) happy path 3-year projection → 201 + stored discriminator; (2) wrong actor type (SalesMarketing) → 403; (3) actor owns innovation → 403; (4) duplicate response → 409; (5) innovation not Published (Draft) → 404; (6) rationale field under 20 chars → 422 with field name in error; (7) non-contiguous years [1,3] → 422; (8) year > 10 → 422; (9) participationProposal under 100 chars → 400; (10) rationale field over 500 chars → 422 with field name in error; (11) partial projection entry (a year supplying a metric but omitting its paired rationale, or vice versa) → 422 (rejected as a unit)
- [ ] T021 [US1] Implement `SubmitManufacturingResponse.cs` handler in `src/Innoventity.API/Features/Bids/SubmitManufacturingResponse.cs` — guard chain: extract actorId from JWT → look up Actor → reject non-Manufacturing (403) → look up Innovation → reject non-Published (404) → reject if actor owns innovation (403) → reject duplicate (409) → validate ParticipationProposal ≥ 100 chars (400) → validate year contiguity + max 10 (422) → reject any partial projection entry as a unit (every metric must be paired with its rationale) (422) → validate all rationale fields are 20–500 chars with field name in error (422) → persist ManufacturingResponse → return 201 with responseId/responseType/status/submittedAt
- [ ] T022 [US1] Register endpoint in `src/Innoventity.API/Program.cs` — `app.MapPost("/innovations/{innovationId}/bids/manufacturing", SubmitManufacturingResponse.Handle).RequireAuthorization()`
- [ ] T023 [US1] Run `dotnet test --filter SubmitManufacturingResponse` — verify all 8 tests pass

**Checkpoint**: Manufacturing actor can fully submit a typed response. US1 independently testable.

---

## Phase 4: User Story 2 — SalesMarketing Response Submission (Priority: P1)

**Goal**: SalesMarketing actor can submit a typed response with yearly revenue projections.

**Independent Test**: Submit a 2-year SalesMarketingResponse; verify `YearlySales` stored with UnitsSold, UnitPrice, SalesMarketingExpense fields and all six rationale strings.

> **⚠️ Write tests FIRST — verify they FAIL before implementing the handler**

- [ ] T024 [P] [US2] Write `SubmitSalesMarketingResponseTests.cs` in `tests/Innoventity.API.Tests/Integration/Features/Bids/SubmitSalesMarketingResponseTests.cs` — 9 tests: same guard coverage as US1 (wrong type = Manufacturing actor → 403, owns innovation → 403, duplicate → 409, not published → 404, UnitsSoldRationale under 20 chars → 422 with field name, year gap → 422, year > 10 → 422, participationProposal under 100 chars → 400, happy path → 201)
- [ ] T025 [US2] Implement `SubmitSalesMarketingResponse.cs` handler in `src/Innoventity.API/Features/Bids/SubmitSalesMarketingResponse.cs` — same guard chain as US1; actor type check: SalesMarketing only; validate ParticipationProposal ≥ 100 chars (400); validate YearlySales contiguity + max 10 + all rationale ≥ 20 chars; persist SalesMarketingResponse
- [ ] T026 [US2] Register endpoint in `src/Innoventity.API/Program.cs` — `app.MapPost("/innovations/{innovationId}/bids/sales", SubmitSalesMarketingResponse.Handle).RequireAuthorization()`
- [ ] T027 [US2] Run `dotnet test --filter SubmitSalesMarketingResponse` — verify all 8 tests pass

**Checkpoint**: SalesMarketing actor can submit. US2 independently testable alongside US1.

---

## Phase 5: User Story 3 — R&D Response Submission (Priority: P1)

**Goal**: R&D actor can submit a typed response with development duration and yearly development cost projections.

**Independent Test**: Submit a ResearchDevelopmentResponse with ProductDevelopmentDuration=2 and 2-year YearlyDevelopmentCosts; verify duration and both years' InfrastructureCost + PeopleCost stored with all rationale fields.

> **⚠️ Write tests FIRST — verify they FAIL before implementing the handler**

- [ ] T028 [P] [US3] Write `SubmitResearchDevelopmentResponseTests.cs` in `tests/Innoventity.API.Tests/Integration/Features/Bids/SubmitResearchDevelopmentResponseTests.cs` — 9 tests: happy path → 201 + ProductDevelopmentDuration stored; wrong actor type → 403; owns innovation → 403; duplicate → 409; not published → 404; InfrastructureCostRationale under 20 chars → 422 with field name; year gap → 422; ProductDevelopmentDuration = 0 → 422; participationProposal under 100 chars → 400
- [ ] T029 [US3] Implement `SubmitResearchDevelopmentResponse.cs` handler in `src/Innoventity.API/Features/Bids/SubmitResearchDevelopmentResponse.cs` — same guard chain; actor type check: RD only; validate ParticipationProposal ≥ 100 chars (400); validate ProductDevelopmentDuration 1–10 (422); validate YearlyDevelopmentCosts contiguity + max 10 + all rationale ≥ 20 chars; persist ResearchDevelopmentResponse
- [ ] T030 [US3] Register endpoint in `src/Innoventity.API/Program.cs` — `app.MapPost("/innovations/{innovationId}/bids/rd", SubmitResearchDevelopmentResponse.Handle).RequireAuthorization()`
- [ ] T031 [US3] Run `dotnet test --filter SubmitResearchDevelopmentResponse` — verify all 8 tests pass

**Checkpoint**: All three financial response types can be submitted. Core NPV data capture is functional.

---

## Phase 6: User Story 4 — Investor Response Submission (Priority: P2)

**Goal**: Investor actor can submit a lightweight feedback response distinguishable by type from the financial responses.

**Independent Test**: Submit an InvestorResponse; query GET /bids as owner; verify `ResponseType = "InvestorResponse"` in the list and Feedback field stored (no projection columns).

> **⚠️ Write tests FIRST — verify they FAIL before implementing the handler**

- [ ] T032 [P] [US4] Write `SubmitInvestorResponseTests.cs` in `tests/Innoventity.API.Tests/Integration/Features/Bids/SubmitInvestorResponseTests.cs` — 6 tests: happy path → 201 with responseType = InvestorResponse; wrong actor type (Manufacturing) → 403; owns innovation → 403; duplicate response → 409; feedback under 50 chars → 422; feedback over 2000 chars → 422
- [ ] T033 [US4] Implement `SubmitInvestorResponse.cs` handler in `src/Innoventity.API/Features/Bids/SubmitInvestorResponse.cs` — same guard chain; actor type check: Investor only; validate Feedback ≥ 50 chars ≤ 2000 chars (422); validate ParticipationProposal ≥ 100 chars (400); persist InvestorResponse
- [ ] T034 [US4] Register endpoint in `src/Innoventity.API/Program.cs` — `app.MapPost("/innovations/{innovationId}/bids/investor", SubmitInvestorResponse.Handle).RequireAuthorization()`
- [ ] T035 [US4] Run `dotnet test --filter SubmitInvestorResponse` — verify all 5 tests pass

**Checkpoint**: All 4 response types fully implemented. Full polymorphic hierarchy operational.

---

## Phase 7: User Story 5 — Response Retrieval with 3-Tier Visibility (Priority: P1)

**Goal**: Innovation owner sees full projection data and proposals; submitting actor sees their own full response; all other authenticated actors see only public summary fields.

**Independent Test**: Seed three typed responses; call GET /bids as (a) owner → full projections + proposals; (b) the manufacturing actor who submitted → own full data + others redacted; (c) a fourth actor who did not submit → all responses show only public summary fields.

> **⚠️ Write tests FIRST — verify they FAIL before updating the handler**

- [ ] T036 [P] [US5] Update `GetBidsTests.cs` in `tests/Innoventity.API.Tests/Integration/Features/Bids/GetBidsTests.cs` — add tests: (1) owner receives full projection data for all response types; (2) submitting actor receives own full response (projections + proposal) + public-only for others; (3) third-party actor receives only responseType/actorId/location/participationType/status/submittedAt for all; (4) ?type=manufacturing filter returns only ManufacturingResponse entries; (5) responseType discriminator present in all views (SC-008)
- [ ] T037 [US5] Update `GetBids.cs` in `src/Innoventity.API/Features/Bids/GetBids.cs` — query `db.FormalResponses.Where(r => r.InnovationId == innovationId)`; determine caller identity (owner / submitting actor / other); build typed DTOs per discriminator (ManufacturingResponseDto, SalesMarketingResponseDto, etc.); apply 3-tier visibility: owner gets full DTO; submitting actor gets full DTO for own response + base DTO for others; other actors get base DTO for all; apply `?type=` case-insensitive filter; return 200 with `{ innovationId, responses: [...] }`
- [ ] T038 [US5] Run `dotnet test --filter GetBids` — verify all visibility tests pass

**Checkpoint**: Owner can compare all response types. 3-tier visibility enforced (SC-004).

---

## Phase 8: User Story 6 — SelectPartners Compatibility (Priority: P1)

**Goal**: The existing partner selection workflow continues to operate using FormalResponse IDs and the renamed `selectedResponseIds` field.

**Independent Test**: Submit one Manufacturing, one SalesMarketing, one RD response; call POST /select-partners with `selectedResponseIds`; verify innovation transitions to PartnersSelected and all three responses have AcceptedAt set.

> **⚠️ Write/update tests FIRST — verify updated tests FAIL before updating the handler**

- [ ] T039 [P] [US6] Delete `src/Innoventity.API/Features/Bids/SubmitBid.cs` and `src/Innoventity.API/Features/Bids/UpdateBid.cs`
- [ ] T040 [P] [US6] Delete `tests/Innoventity.API.Tests/Integration/Features/Bids/SubmitBidTests.cs` and `tests/Innoventity.API.Tests/Integration/Features/Bids/UpdateBidTests.cs`
- [ ] T041 [P] [US6] Update `SelectPartnersTests.cs` in `tests/Innoventity.API.Tests/Integration/Features/Bids/SelectPartnersTests.cs` — replace `new Bid { ... }` seed data with typed FormalResponse subtypes (ManufacturingResponse, SalesMarketingResponse, ResearchDevelopmentResponse); rename `selectedBidIds` → `selectedResponseIds` in all request body constructions; verify 14 test cases still semantically correct
- [ ] T042 [P] [US6] Update `SelectPartners.cs` in `src/Innoventity.API/Features/Bids/SelectPartners.cs` — rename `SelectPartnersRequest.SelectedBidIds` → `SelectedResponseIds`; change `innovation.Bids` → `innovation.FormalResponses`; change `BidStatus` → `ResponseStatus` throughout; update required-type validation (ManufacturingResponse + SalesMarketingResponse + ResearchDevelopmentResponse)
- [ ] T043 [US6] Update `Program.cs` in `src/Innoventity.API/Program.cs` — remove `app.MapPost("/innovations/{id}/bids", SubmitBid.Handle)` and `app.MapPut("/bids/{bidId}", UpdateBid.Handle)` registrations; verify all 4 new submit endpoints are registered
- [ ] T044 [US6] Run `dotnet test --filter SelectPartners` — verify all 14 SelectPartners tests pass + zero regressions; run `dotnet test` to check overall count

**Checkpoint**: Breaking migration complete. All 6 user stories functional. SelectPartners regression-free (SC-002).

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: E2E journey update, Stryker config, and full quality gate validation.

- [ ] T045 [P] Update `Journey2_BiddingTests.cs` in `tests/Innoventity.API.Tests/E2E/Journeys/Journey2_BiddingTests.cs` — update full bidding journey to use typed endpoints (`/bids/manufacturing`, `/bids/sales`, `/bids/rd`), replace `selectedBidIds` with `selectedResponseIds`, include projection data in submission payloads
- [ ] T046 [P] Update `stryker-config.json` in `src/Innoventity.API/stryker-config.json` — add to mutate list: `SubmitManufacturingResponse.cs`, `SubmitSalesMarketingResponse.cs`, `SubmitResearchDevelopmentResponse.cs`, `SubmitInvestorResponse.cs`, `GetBids.cs`, `SelectPartners.cs`; remove `SubmitBid.cs`, `UpdateBid.cs`
- [ ] T047 Run `dotnet test` from repo root — verify ~166 tests pass (131 pre-existing + ~35 new), 3 pre-existing skips unchanged (SC-006)
- [ ] T048 Run `dotnet stryker` from `src/Innoventity.API/` — verify mutation score ≥ 80% across new feature files (SC-005)
- [ ] T049 Run `dotnet build -c Release --no-restore` from repo root — verify Release build is clean

**Checkpoint**: Feature complete. All quality gates satisfied. Ready for `/speckit-analyze`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Domain)**: No dependencies — start immediately
- **Phase 2 (Persistence)**: Depends on Phase 1 completion — BLOCKS all user stories
- **Phase 3–8 (User Stories)**: All depend on Phase 2 completion; can proceed sequentially in priority order (P1 first: US1 → US2 → US3 → US5 → US6, then US4)
- **Phase 9 (Polish)**: Depends on all user story phases complete

### User Story Dependencies

- **US1 (P1)**: Can start after Phase 2 — no story dependencies
- **US2 (P1)**: Can start after Phase 2 — no story dependencies (different files from US1)
- **US3 (P1)**: Can start after Phase 2 — no story dependencies
- **US4 (P2)**: Can start after Phase 2 — no story dependencies
- **US5 (P1)**: Requires working submission to have responses to retrieve; logically after US1–US4, but can be implemented in parallel
- **US6 (P1)**: Requires Phase 2 foundation; logically after US1–US3 to have test data, but implementation is independent

### Within Each User Story

1. Write tests → verify they FAIL (red phase — Principle 5)
2. Implement handler
3. Register in Program.cs
4. Run tests → verify they PASS (green phase)

---

## Parallel Opportunities

### Foundation Phase 1 (can run simultaneously)

```
T001 GeographicRegion.cs
T002 ResponseStatus.cs
T003 YearlyManufacturingCost.cs
T004 YearlySale.cs
T005 YearlyDevelopmentCost.cs
```
Then T006 (FormalResponse base), then simultaneously:
```
T007 ManufacturingResponse.cs
T008 SalesMarketingResponse.cs
T009 ResearchDevelopmentResponse.cs
T010 InvestorResponse.cs
```

### Foundation Phase 2 (can run simultaneously)

```
T014 AppDbContext.cs TPH config
T015 SeedData.cs typed seeds
```

### US1–US4 Test Writing (can run simultaneously after Phase 2)

```
T020 SubmitManufacturingResponseTests.cs
T024 SubmitSalesMarketingResponseTests.cs
T028 SubmitResearchDevelopmentResponseTests.cs
T032 SubmitInvestorResponseTests.cs
```

### US6 Cleanup (can run simultaneously)

```
T039 Delete SubmitBid.cs + UpdateBid.cs
T040 Delete SubmitBidTests.cs + UpdateBidTests.cs
T041 Update SelectPartnersTests.cs
T042 Update SelectPartners.cs
```

---

## Implementation Strategy

### MVP First (US1 Only — 3 phases)

1. Complete Phase 1: Domain Layer
2. Complete Phase 2: Persistence Layer
3. Complete Phase 3: US1 — Manufacturing submission
4. **STOP and VALIDATE**: `dotnet test --filter SubmitManufacturingResponse` passes; manually call endpoint; verify JSON projection data stored
5. Foundation proven — proceed to US2

### Incremental Delivery Order

1. Foundation (Phase 1 + 2) → builds without errors
2. US1 (Phase 3) → Manufacturing submissions working
3. US2 (Phase 4) → SalesMarketing submissions working
4. US3 (Phase 5) → RD submissions working; all financial types complete
5. US5 (Phase 7) → Owner can compare all responses (3-tier visibility)
6. US6 (Phase 8) → SelectPartners re-verified; old Bid code removed
7. US4 (Phase 6) → Investor submissions working
8. Polish (Phase 9) → Full quality gate

---

## Notes

- `[P]` tasks operate on different files — no merge conflicts when parallelized
- Tests are written FIRST; they must fail before the handler exists (Principle 5)
- T006 (FormalResponse.cs) is a hard prerequisite for T007–T010 — do not parallelize
- T016–T018 (migration) are strictly sequential: generate → review → apply
- T017 is human-reviewed — the auto-generated migration must be augmented with the data movement SQL (Bids → FormalResponses by ActorType discriminator)
- If `OwnsMany().ToJson()` fails in T014, fall back to `HasConversion<string>` with System.Text.Json per plan.md Key Risk section
- Commit after each phase using `feat(bids)!:` for breaking changes with BREAKING CHANGE footer (Principle 8)
