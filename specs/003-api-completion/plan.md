# Implementation Plan: API Completion & Subcutaneous Testing

**Branch**: `003-api-completion` | **Date**: 2026-02-15 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-api-completion/spec.md`

## Summary

**Primary Requirement**: Complete the backend API surface area (8 new endpoints) and achieve zero test failures (94/97 tests passing; 3 Phase 1 domain tests intentionally skipped) through comprehensive subcutaneous testing pattern before any frontend or deployment work begins.

**Technical Approach**:
1. Fix database context lifecycle issues in existing integration tests (T001-T004)
2. Implement Innovation CRUD endpoints following Vertical Slice Architecture (T005-T009)
3. Implement Bid Management endpoints (T010-T012)
4. Build comprehensive journey test suites validating complete user flows (T013-T015)
5. Complete OpenAPI documentation and traceability (T016-T017)

**Key Innovation**: Adopts **subcutaneous testing pattern** (Martin Fowler) - integration tests operating "just under the UI" that exercise complete user journeys through API endpoints with real infrastructure, providing faster feedback than UI tests (~18s vs minutes) while validating backend completeness without frontend dependency.

## Technical Context

**Language/Version**: C# 12 / .NET 8.0
**Primary Dependencies**:
- ASP.NET Core 8.0 (Minimal APIs)
- Entity Framework Core 8.0 (In-Memory provider for tests, SQL Server for production)
- xUnit 2.6+ (test framework)
- BCrypt.Net-Next 4.0+ (password hashing)

**Storage**:
- Development: SQL Server LocalDB / SQLite
- Testing: EF Core In-Memory Database Provider
- Production: Azure SQL Database / PostgreSQL (TBD)

**Testing**:
- xUnit with WebApplicationFactory<Program> for integration tests
- In-memory database for test isolation
- Subcutaneous tests orchestrate 5-10 API calls per journey

**Target Platform**:
- Server: Linux/Windows Server (containerized via Docker)
- Runtime: .NET 8.0 Runtime
- Cloud: Azure App Service (deployment target)

**Project Type**: Web API (backend-only, RESTful API endpoints)

**Performance Goals**:
- API response time: <200ms p95 for CRUD operations
- Test suite execution: <30 seconds for 97 tests (actual: 21.8s)
- Subcutaneous test execution: <5 seconds per journey test

**Constraints**:
- Zero build warnings (enforced)
- 100% test pass rate required (no failing tests)
- Manual validation only (no FluentValidation library dependency)
- Vertical Slice Architecture (feature folders, not layered)
- Minimal APIs pattern (no controllers)

**Scale/Scope**:
- Phase 0.6: 14 total endpoints (6 existing + 8 new)
- Phase 0.6: 97 tests total — 8 subcutaneous journey + 34 integration + 55 unit/infra (94 passing, 3 Phase 1 skipped)
- Journey 1 & 2 coverage: 100% (Innovation Submission, Discovery & Bidding)
- Estimated effort: 40-50 hours over 3-4 weeks

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Gate 1: Principle 2 - Quality is Non-Negotiable ✅
**Status**: PASS
**Assessment**: Feature requires zero failures (94/97 tests passing, 3 Phase 1 domain tests skipped) with zero build warnings. Subcutaneous testing pattern validates production-readiness before deployment. Test-first approach ensures quality at every step.

### Gate 2: Principle 3 - Simplicity Over Cleverness ✅
**Status**: PASS
**Assessment**: Uses standard patterns throughout:
- Minimal APIs (ASP.NET Core recommended pattern)
- Vertical Slice Architecture (simple feature folders)
- EF Core In-Memory provider (standard testing approach)
- Manual validation (no additional library complexity)
- No custom abstractions or clever patterns introduced

### Gate 3: Principle 4 - Specification Drives Implementation ✅
**Status**: PASS
**Assessment**: Complete specification exists (spec.md) with 7 user stories, detailed task breakdown (T001-T017 in tasks.md), and acceptance criteria. This plan follows spec-kit workflow: SPECIFY → PLAN → TASKS → IMPLEMENT.

### Gate 4: Principle 5 - Tests Must Prove They Work ✅
**Status**: PASS
**Assessment**: Feature fixes 4 failing tests first (T001-T004) proving test-first discipline. Each new endpoint requires integration test before implementation. Journey tests validate complete flows. Tests observed failing before fixes applied (existing infrastructure has failing tests documented).

### Gate 5: Principle 6 - Architecture Must Support Evolution ✅
**Status**: PASS
**Assessment**: Phase 0.6 completes Journey 1 & 2 API surface without building Journey 3+ features prematurely. Vertical Slice Architecture allows adding new features (Journeys 3-5) without refactoring. Subcutaneous testing pattern extensible to future journeys.

### Gate 6: Principle 7 - Incremental & Sustainable ✅
**Status**: PASS
**Assessment**: **Backend-first validation** - completes entire backend API layer before frontend work (constitution Principle 6 alignment). 4-week timeline broken into weekly milestones. Each week delivers incremental value (test fixes → Innovation CRUD → Bid management → journey validation).

**Overall Constitutional Compliance**: ✅ 6/6 gates passing - **99/100 rating maintained**

## Critical Path Analysis

### Sequential Execution Order

The implementation follows a strict dependency chain with clear blocking relationships:

```
WEEK 1: Infrastructure Fixes (CRITICAL - BLOCKS ALL)
├── T001: Diagnose Database Context Lifecycle Issues (3h)
│   └── Deliverable: Root cause diagnostic report
├── T002: Implement Database Context Lifecycle Fix (4h) [DEPENDS: T001]
│   └── Deliverable: Fixed test infrastructure
├── T003: Fix JWT Token Attachment Issues (2h) [DEPENDS: T002]
│   └── Deliverable: JWT helper methods
└── T004: Validate Subcutaneous Test Infrastructure Complete (1h) [DEPENDS: T001-T003]
    └── Deliverable: 57/60 tests passing (95% pass rate)
    └── GATE: Must pass before Week 2 begins

WEEK 2: Innovation CRUD (Journey 1 API Surface)
├── T005: Implement POST /innovations (3h) [DEPENDS: T004]
├── T006: Implement PUT /innovations/{id} (3h) [DEPENDS: T005]
├── T007: Implement PATCH /innovations/{id}/submit (4h) [DEPENDS: T006]
├── T008: Implement GET /innovations (3h) [DEPENDS: T007]
└── T009: Implement GET /industries (2h) [DEPENDS: T008]
    └── GATE: 5 endpoints + 16 integration tests passing

WEEK 3: Bid Management (Journey 2 API Surface)
├── T010: Implement POST /innovations/{innovationId}/bids (4h) [DEPENDS: T009]
├── T011: Implement GET /innovations/{innovationId}/bids (3h) [DEPENDS: T010]
└── T012: Implement PUT /bids/{bidId} (3h) [DEPENDS: T011]
    └── GATE: 3 endpoints + 10 integration tests passing

WEEK 4: Journey Tests + Documentation
├── T013: Build Journey 1 Complete Test Suite (5h) [DEPENDS: T005-T009]
├── T014: Build Journey 2 Complete Test Suite (4h) [DEPENDS: T010-T012]
├── T015: Validate Complete Test Suite (1h) [DEPENDS: T013-T014]
    └── GATE: 94/97 tests passing (0 failures; 3 Phase 1 domain tests deferred)
├── T016: Complete OpenAPI Documentation (2h) [DEPENDS: T015]
└── T017: Update Specification Traceability (2h) [DEPENDS: T016]
    └── GATE: Phase 0.6 Complete, Ready for Merge
```

**Blocking Chain**: T001 → T002 → T003 → T004 → BLOCKS ALL

**Explanation**: Week 1 infrastructure fixes are the critical path. Until database context and JWT issues are resolved, no new endpoints can be reliably tested. This is correctly identified as P0-CRITICAL in tasks.

**Parallel Opportunities**:
- T005-T009 can be parallelized if multiple developers available
- T010-T012 can be parallelized if multiple developers available
- T016 and T017 could potentially run in parallel (minor time savings)

**Risk Hotspots**:
- **T001-T002** (7 hours): If database context fix takes longer than estimated, entire timeline shifts
- **T007** (4 hours): Most complex validation logic (13 completeness rules)
- **T013** (5 hours): First comprehensive journey test - template for T014

## Project Structure

### Documentation (this feature)

```text
specs/003-api-completion/
├── README.md                              # Navigation hub (created)
├── spec.md                                # Complete specification (created)
├── tasks.md                               # Task breakdown T001-T017 (created)
├── subcutaneous-test-requirements.md      # Testing patterns (created)
├── plan.md                                # This file
└── contracts/                             # Phase 1 output (to be generated)
    ├── innovations.openapi.yaml
    └── bids.openapi.yaml
```

### Source Code (repository root)

```text
src/
└── Innoventity.API/
    ├── Domain/                            # Entities (existing)
    │   ├── Actor.cs
    │   ├── Innovation.cs
    │   ├── Industry.cs
    │   ├── Bid.cs (to be created)
    │   └── EntityBase.cs
    ├── Features/                          # Vertical slices (existing + new)
    │   ├── Authentication/                # Phase 0 (existing)
    │   │   ├── Register.cs
    │   │   ├── Login.cs
    │   │   ├── RefreshToken.cs
    │   │   └── Activate.cs
    │   ├── Innovations/                   # Phase 0.6 (to be created)
    │   │   ├── CreateInnovation.cs         # T005
    │   │   ├── UpdateInnovation.cs         # T006
    │   │   ├── SubmitInnovation.cs         # T007
    │   │   ├── ListInnovations.cs          # T008
    │   │   └── GetInnovation.cs            # Phase 0.5 (exists)
    │   ├── Industries/                    # Phase 0.6 (to be created)
    │   │   └── GetIndustries.cs            # T009
    │   ├── Bids/                          # Phase 0.6 (to be created)
    │   │   ├── SubmitBid.cs                # T010
    │   │   ├── GetBids.cs                  # T011
    │   │   └── UpdateBid.cs                # T012
    │   └── Health/                        # Phase 0 (existing)
    │       └── HealthCheck.cs
    ├── Infrastructure/                    # Cross-cutting (existing)
    │   ├── AppDbContext.cs
    │   ├── JwtTokenService.cs
    │   └── PasswordHasher.cs
    ├── Program.cs                         # App entry point (existing)
    └── appsettings.json                   # Configuration (existing)

tests/
└── Innoventity.API.Tests/
    ├── Integration/                       # Endpoint tests (existing + new)
    │   └── Features/                      # NOTE: actual sub-path is Features/
    │       ├── Authentication/
    │       │   ├── RegisterActorTests.cs
    │       │   ├── LoginTests.cs
    │       │   └── ActivateAccountTests.cs
    │       ├── Innovations/                   # Phase 0.6 (created)
    │       │   ├── CreateInnovationTests.cs    # T005 ✅
    │       │   ├── UpdateInnovationTests.cs    # T006 ✅
    │       │   ├── SubmitInnovationTests.cs    # T007 ✅
    │       │   ├── ListInnovationsTests.cs     # T008 ✅
    │       │   └── GetInnovationTests.cs       # Phase 0.5 (fixed) ✅
    │       ├── Industries/                    # Phase 0.6 (created)
    │       │   └── GetIndustriesTests.cs       # T009 ✅
    │       └── Bids/                          # Phase 0.6 (created)
    │           ├── SubmitBidTests.cs           # T010 ✅
    │           ├── GetBidsTests.cs             # T011 ✅ (6 tests, not 3)
    │           └── UpdateBidTests.cs           # T012 ✅
    ├── E2E/Journeys/                      # Journey tests (NOTE: not Subcutaneous/)
    │   ├── Phase0JourneyTests.cs           # Phase 0 (fixed) ✅
    │   ├── Journey1_InnovationSubmissionTests.cs  # T013 ✅
    │   └── Journey2_BiddingTests.cs        # T014 ✅
    ├── Unit/                              # Domain logic tests (existing)
    │   ├── Domain/
    │   │   ├── ActorTests.cs
    │   │   ├── InnovationTests.cs          # 2 passing, 3 skipped (Phase 1)
    │   │   └── EntityBaseTests.cs
    │   └── Infrastructure/
    │       ├── JwtTokenServiceTests.cs
    │       └── PasswordHasherTests.cs
    └── TestFixtures/                      # Test helpers
        └── HttpClientExtensions.cs         # T003 (JWT helper); TestFixture.cs NOT created — db context lifecycle handled inline per test class
```

**Structure Decision**: Using existing **Vertical Slice Architecture** with feature-based folders. Each endpoint lives in its feature folder with all related logic (request/response DTOs, validation, handlers). This aligns with Constitution Principle 3 (Simplicity) - no layered architecture complexity.

**New Directories Created**:
- `src/Innoventity.API/Features/Innovations/` - Innovation CRUD endpoints (T005-T008)
- `src/Innoventity.API/Features/Industries/` - Industry reference data (T009)
- `src/Innoventity.API/Features/Bids/` - Bid management endpoints (T010-T012)
- `tests/Innoventity.API.Tests/Integration/Features/Innovations/` - Innovation endpoint tests
- `tests/Innoventity.API.Tests/Integration/Features/Industries/` - Industry endpoint tests
- `tests/Innoventity.API.Tests/Integration/Features/Bids/` - Bid endpoint tests
- `tests/Innoventity.API.Tests/E2E/Journeys/` - Journey tests (T013-T014) *(plan said `Subcutaneous/` — actual path differs)*

**New Entities**:
- `src/Innoventity.API/Domain/Bid.cs` - Bid entity (actor proposal for innovation partnership)

## Complexity Tracking

> **No violations identified - all constitutional gates passing**

Phase 0.6 introduces no additional complexity beyond constitutional principles:
- Uses standard Minimal APIs pattern (no custom framework)
- Vertical Slice Architecture already established in Phase 0
- EF Core In-Memory provider is standard testing approach
- xUnit + WebApplicationFactory standard for ASP.NET Core testing
- Subcutaneous testing is **simplification** (replaces UI testing complexity)

**Simpler alternatives rejected**: None - feature uses simplest possible approaches throughout.

---

## Phase 0: Research & Unknowns Resolution

### Research Context

All technical decisions are **already resolved** from Phase 0 and Phase 0.5:
- ✅ Language/Framework: C# 12 / .NET 8.0 / ASP.NET Core Minimal APIs (Phase 0)
- ✅ Database: EF Core with In-Memory provider for tests (Phase 0)
- ✅ Architecture: Vertical Slice Architecture (Phase 0)
- ✅ Testing: xUnit + WebApplicationFactory (Phase 0)
- ✅ Domain Model: EntityBase, Address, Actor, Innovation entities (Phase 0.5)
- ✅ Authentication: JWT tokens, BCrypt password hashing (Phase 0)

### New Technical Decision Required: Subcutaneous Testing Pattern

**Decision**: Adopt Martin Fowler's **Subcutaneous Testing Pattern**

**Rationale**:
1. **Faster Feedback**: Subcutaneous tests execute in 18-30 seconds vs. 2-5 minutes for UI tests
2. **More Stable**: No DOM selectors, no async rendering issues, no visual regressions
3. **Backend-First Validation**: Proves API completeness before expensive frontend work
4. **Frontend Team Unblocked**: Complete, validated API available immediately
5. **Bug Discovery Shifted Left**: API bugs caught in integration tests, not during UI development (~2-3 week lead time savings)

**Alternatives Considered**:
- **UI Testing First**: Rejected due to slow feedback, fragility, requires frontend implementation
- **Postman/Newman Collections**: Rejected due to lack of version control, weaker assertions, no  CI/CD integration
- **Unit Tests Only**: Rejected as insufficient - doesn't validate end-to-end flows

**References**:
- Martin Fowler: "Testing Strategies in a Microservice Architecture" (2014)
- Pattern: Subcutaneous Testing - tests that operate "just under the skin" of the UI

### Research Findings: Database Context Lifecycle (Root Cause)

**Problem**: Data seeded in test constructor not visible to test method execution

**Root Cause**: EF Core In-Memory provider creates separate database instances when:
1. Database name is same but `DbContext` instances are different
2. WebApplicationFactory creates new `DbContext` instance
3. Test constructor `DbContext` and factory `DbContext` not shared

**Solution** (T001-T002):
```csharp
// Unique database name per test instance
_databaseName = $"TestDb_Phase0Journey_{Guid.NewGuid()}";

// Configure WebApplicationFactory to use SAME database name
_factory = new WebApplicationFactory<Program>()
    .WithWebHostBuilder(builder =>
    {
        builder.ConfigureServices(services =>
        {
            // Remove default DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Register with SAME database name as test DbContext
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    });
```

### Research Findings: JWT Token Attachment (Root Cause)

**Problem**: Authenticated requests return 401 Unauthorized instead of expected status codes

**Root Cause**: JWT token not properly attached to HTTP requests OR attached to `DefaultRequestHeaders` causing cross-test contamination

**Solution** (T003):
```csharp
// Per-request header attachment (recommended for test isolation)
public static async Task<HttpResponseMessage> GetWithAuthAsync(
    this HttpClient client,
    string requestUri,
    string token)
{
    var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    return await client.SendAsync(request);
}
```

### No Additional Research Required

All other aspects already validated in Phase 0/0.5:
- ✅ Innovation entity completeness validation (13 rules) - known from spec
- ✅ Bid eligibility rules (R4.1) - known from spec
- ✅ OpenAPI/Swagger XML documentation - pattern established in Phase 0.5 (T031)
- ✅ Pagination approach - standard offset/limit pattern
- ✅ Authorization patterns - JWT-based, already implemented

---

## Phase 1: Design & Contracts

### Data Model

**Existing Entities** (Phase 0 + Phase 0.5):
- `Actor` - User account with authentication credentials
- `Innovation` - Innovation submission with all sections
- `Industry` - Master industry list for targeting

**New Entity: Bid**

```csharp
public class Bid : EntityBase<Guid>
{
    // Identity
    public Guid BidId { get; set; } = Guid.NewGuid();

    // Relationships
    public Guid InnovationId { get; set; }
    public Innovation Innovation { get; set; } = null!;

    public Guid ActorId { get; set; }
    public Actor Actor { get; set; } = null!;

    // Bid Content (R4.2)
    public string Location { get; set; } = string.Empty; // Geographic presence
    public string ParticipationType { get; set; } = string.Empty; // Role offering
    public string ParticipationProposal { get; set; } = string.Empty; // Min 200 chars

    // Status
    public BidStatus Status { get; set; } = BidStatus.Pending;

    // Timestamps (from EntityBase)
    // CreatedTimestamp, ModifiedTimestamp inherited

    // Audit
    public DateTime SubmittedTimestamp { get; set; }
}

public enum BidStatus
{
    Pending,    // Submitted, awaiting owner review
    Accepted,   // Owner selected this bid (immutable after this)
    Rejected,   // Owner explicitly rejected
    Withdrawn   // Actor withdrew their bid
}
```

**Validation Rules**:
- `Location`: Required, non-empty
- `ParticipationType`: Required, non-empty
- `ParticipationProposal`: Required, min 200 characters (R4.2)
- `ActorId`: Must not equal `Innovation.OwnerId` (cannot bid on own innovation)
- Duplicate check: Unique constraint on `(InnovationId, ActorId)`

**State Transitions**:
```
Pending → Accepted (owner selection, one-way)
Pending → Rejected (owner decision)
Pending → Withdrawn (actor decision)
Accepted → [IMMUTABLE] (R4.3 - accepted bids cannot be modified)
```

### API Contracts

**See**: `specs/003-api-completion/contracts/` directory (to be generated)

**Endpoints Summary**:

1. **POST /innovations** - Create innovation draft
   - Request: CreateInnovationRequest (all innovation fields)
   - Response: 201 Created with InnovationId, IdeaToken, status=Draft

2. **PUT /innovations/{id}** - Update draft innovation
   - Request: UpdateInnovationRequest (partial fields)
   - Response: 200 OK with updated innovation
   - Errors: 403 (not owner), 409 (already published)

3. **PATCH /innovations/{id}/submit** - Publish innovation
   - Request: Empty body (status transition)
   - Response: 200 OK with status=Published, submittedAt timestamp
   - Errors: 400 (incomplete validation), 409 (already published)

4. **GET /innovations** - List innovations with filters
   - Query: ?industryId, ?researchCategory, ?status, ?page, ?pageSize
   - Response: 200 OK with paginated list
   - Errors: 401 (unauthenticated)

5. **GET /industries** - Industry master list
   - Request: No parameters (public endpoint)
   - Response: 200 OK with all industries

6. **POST /innovations/{innovationId}/bids** - Submit bid
   - Request: SubmitBidRequest (location, participationType, proposal)
   - Response: 201 Created with BidId, submittedAt
   - Errors: 403 (wrong actor type), 409 (duplicate), 400 (short proposal)

7. **GET /innovations/{innovationId}/bids** - List bids (owner only)
   - Request: No body
   - Response: 200 OK with all bids including full proposals
   - Errors: 403 (not owner)

8. **PUT /bids/{bidId}** - Update unaccepted bid
   - Request: UpdateBidRequest (updated fields)
   - Response: 200 OK with updated bid
   - Errors: 403 (not author), 409 (already accepted)

**OpenAPI Schema**: See `contracts/` directory for detailed OpenAPI YAML files (to be generated in separate operation).

---

## Phase 2: Implementation Planning

**See**: `specs/003-api-completion/tasks.md` for complete task breakdown (T001-T017)

### Week 1: Infrastructure Fixes (T001-T004, 10 hours)

**Goal**: Fix 4 failing subcutaneous tests, achieve 95% pass rate (57/60 tests)

**Key Tasks**:
- T001: Diagnose database context lifecycle issue (3 hours)
- T002: Implement database context fix (4 hours)
- T003: Fix JWT token attachment (2 hours)
- T004: Validate 57/60 passing (1 hour)

**Deliverables**: All infrastructure tests passing, test fixture updated for reuse

### Week 2: Innovation CRUD (T005-T009, 15 hours)

**Goal**: Complete Journey 1 API surface (5 endpoints)

**Key Tasks**:
- T005: POST /innovations (3 hours)
- T006: PUT /innovations/{id} (3 hours)
- T007: PATCH /innovations/{id}/submit (4 hours) - complex validation
- T008: GET /innovations (3 hours) - filtering + pagination
- T009: GET /industries (2 hours) - seed master data

**Deliverables**: 5 endpoints + 16 integration tests passing

### Week 3: Bid Management (T010-T012, 10 hours)

**Goal**: Complete Journey 2 API surface (3 endpoints)

**Key Tasks**:
- T010: POST /innovations/{innovationId}/bids (4 hours) - eligibility rules
- T011: GET /innovations/{innovationId}/bids (3 hours) - owner auth
- T012: PUT /bids/{bidId} (3 hours) - status validation

**Deliverables**: 3 endpoints + 10 integration tests passing

### Week 4: Journey Tests + Docs (T013-T017, 12 hours)

**Goal**: Achieve 100% pass rate (67/67 tests), complete documentation

**Key Tasks**:
- T013: Journey1Tests (5 hours) - 4 tests (1 complete + 3 error paths)
- T014: Journey2Tests (4 hours) - 3 tests (1 complete + 2 error paths)
- T015: Validate 67/67 passing (1 hour)
- T016: Complete OpenAPI docs (2 hours) - XML comments on all endpoints
- T017: Traceability matrix (2 hours) - user stories → endpoints → tests

**Deliverables**: 67/67 tests passing, comprehensive documentation

### Critical Path

```
T001-T004 (Infrastructure) → BLOCKS ALL
T005-T009 (Innovation CRUD) → BLOCKS T013 (Journey1Tests)
T010-T012 (Bid Management) → BLOCKS T014 (Journey2Tests)
T013-T014 (Journey Tests) → T015 (Validation)
T015 (100% Pass Rate) → T016-T017 (Documentation)
```

### Success Criteria

**Phase 0.6 Complete When**: See [spec.md](spec.md#success-metrics) for detailed acceptance criteria. In summary: 67/67 tests passing (100%), 8 new endpoints implemented, Journey 1-2 validated, zero warnings, <30s test execution.

**Merge Criteria**: All success criteria met, code review approved, no merge conflicts, CI/CD passing.

---

## Agent Context Update

**Action**: Run agent context update script after Phase 1 design completion

```bash
.\.specify\scripts\powershell\update-agent-context.ps1 -AgentType copilot
```

**Context to Add**:
- Subcutaneous testing pattern (Martin Fowler)
- Database context lifecycle fix for integration tests
- JWT token attachment pattern for authenticated requests
- Bid entity and validation rules
- Journey test structure (Journey1Tests, Journey2Tests)

**Technology Additions**:
- None (all technologies already in Phase 0/0.5)

**Preserve Manual Additions**:
- Between `<!-- MANUAL_CONTEXT_START -->` and `<!-- MANUAL_CONTEXT_END -->` markers
- Do not overwrite existing Phase 0/0.5 context

---

## Risk Assessment

### High Risk: Database Context Lifecycle Fix (T001-T002)

**Impact**: Blocks all subsequent work if not resolved
**Mitigation**: Allocate 3 full days, create minimal reproduction case, consult EF Core docs
**Contingency**: Switch to SQLite in-memory mode if EF Core In-Memory provider insufficient

### Medium Risk: Scope Creep (Journey 3)

**Impact**: Timeline inflation, mission drift
**Mitigation**: Journey 3 explicitly out of scope, defer to Phase 0.7
**Contingency**: If ahead of schedule, reassess after T015

### Low Risk: Test Execution Time >30s

**Impact**: Slow CI/CD pipeline
**Mitigation**: Profile slow tests, optimize database seeding, use test parallelization
**Contingency**: Introduce test categories (Unit, Integration, Journey) if needed

---

## Appendix: Reference Documents

- **Feature Specification**: [spec.md](spec.md)
- **Task Breakdown**: [tasks.md](tasks.md)
- **Subcutaneous Test Patterns**: [subcutaneous-test-requirements.md](subcutaneous-test-requirements.md)
- **Constitution**: [../../.specify/memory/constitution.md](../../.specify/memory/constitution.md)
- **Phase 0 Spec**: [../001-platform-core/spec.md](../001-platform-core/spec.md)
- **Phase 0.5 Spec**: [../002-domain-enhancements/spec.md](../002-domain-enhancements/spec.md)
- **DDD Analysis**: [../../.specify/analysis/ddd-architectural-analysis.md](../../.specify/analysis/ddd-architectural-analysis.md)

---

**Plan Status**: ✅ **COMPLETE** - Ready for implementation (T001-T017 execution)
