# Phase 0.6 Review: API Completion & Subcutaneous Testing

**Review Date**: February 15, 2026  
**Reviewer**: Analysis Agent  
**Status**: Pre-Implementation Review

---

## Executive Summary

**Overall Assessment**: ✅ **Plan is comprehensive and well-structured**

The Phase 0.6 specification, plan, and tasks are thorough and follow constitutional principles. The plan has clear dependencies, acceptance criteria, and verification commands. However, several operational checklists are missing that would improve execution flow and quality assurance.

**Key Strengths**:
- Clear constitutional compliance assessment (6/6 gates passing)
- Detailed task breakdown with acceptance criteria and verification commands
- Well-defined critical path and dependencies
- Comprehensive subcutaneous testing approach
- Test-first discipline evident throughout

**Key Recommendations**:
1. Add pre-task checklists (environment setup, branch verification)
2. Add post-task checklists (commit guidelines, documentation updates)
3. Add weekly milestone checklists
4. Add merge readiness checklist
5. Consider adding interim checkpoints within longer tasks (T007, T013, T014)

---

## Order of Activities (Critical Path)

### Sequential Execution Order

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
│   └── GATE: 67/67 tests passing (100% pass rate)
├── T016: Complete OpenAPI Documentation (2h) [DEPENDS: T015]
└── T017: Update Specification Traceability (2h) [DEPENDS: T016]
    └── GATE: Phase 0.6 Complete, Ready for Merge
```

### Critical Path Analysis

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

---

## Detailed Activity Sequence

### Phase 1: Week 1 - Infrastructure Fixes (10 hours)

**Day 1-2: Diagnostic & Fix (7 hours)**

1. **T001: Diagnose Database Context** (3 hours)
   - Run failing tests with detailed logging
   - Analyze EF Core In-Memory provider behavior
   - Document root cause in `.specify/analysis/test-context-diagnostic.md`
   - **Deliverable**: Diagnostic report with proposed solution

2. **T002: Implement Database Context Fix** (4 hours)
   - Implement unique database naming strategy
   - Refactor test fixture with consistent DbContext lifecycle
   - Update Phase0JourneyTests and GetInnovationTests
   - **Deliverable**: 4 subcutaneous tests passing
   - **Verification**: `dotnet test --filter "FullyQualifiedName~Phase0JourneyTests|FullyQualifiedName~GetInnovationTests"`

**Day 3: JWT & Validation (3 hours)**

3. **T003: Fix JWT Token Attachment** (2 hours)
   - Create JWT helper methods in test fixture
   - Update all authenticated test requests
   - **Deliverable**: JWT token helper methods
   - **Verification**: `dotnet test --filter "FullyQualifiedName~GetInnovationTests"`

4. **T004: Validate Infrastructure Complete** (1 hour)
   - Run full test suite: `dotnet test --verbosity normal`
   - Document pass rate: 57/60 (95%)
   - **Deliverable**: Test execution report
   - **GATE CHECKPOINT**: Must achieve 95% pass rate before proceeding to Week 2

---

### Phase 2: Week 2 - Innovation CRUD (15 hours)

**Day 1-2: Draft Management (6 hours)**

5. **T005: POST /innovations** (3 hours)
   - Create CreateInnovation.cs endpoint
   - Implement request validation (Title, ProductType, ResearchCategory required)
   - Implement authorization (JWT + ActorType = IdeaGenerator)
   - Write 4 integration tests
   - **Deliverable**: CreateInnovationTests (4 tests passing)
   - **Verification**: `dotnet test --filter "FullyQualifiedName~CreateInnovationTests"`

6. **T006: PUT /innovations/{id}** (3 hours)
   - Create UpdateInnovation.cs endpoint
   - Implement ownership validation
   - Implement status validation (Draft only)
   - Write 4 integration tests
   - **Deliverable**: UpdateInnovationTests (4 tests passing)
   - **Verification**: `dotnet test --filter "FullyQualifiedName~UpdateInnovationTests"`

**Day 3: Publication & Discovery (7 hours)**

7. **T007: PATCH /innovations/{id}/submit** (4 hours)
   - Create SubmitInnovation.cs endpoint
   - Implement 13-rule completeness validation (R2.1)
   - Implement status transition (Draft → Published)
   - Write 4 integration tests
   - **Deliverable**: SubmitInnovationTests (4 tests passing)
   - **Verification**: `dotnet test --filter "FullyQualifiedName~SubmitInnovationTests"`
   - **⚠️ COMPLEXITY WARNING**: Most complex validation logic

8. **T008: GET /innovations** (3 hours)
   - Create ListInnovations.cs endpoint
   - Implement filtering (industryId, researchCategory, status)
   - Implement pagination (page, pageSize)
   - Write 4 integration tests
   - **Deliverable**: ListInnovationsTests (4 tests passing)
   - **Verification**: `dotnet test --filter "FullyQualifiedName~ListInnovationsTests"`

**Day 4: Reference Data (2 hours)**

9. **T009: GET /industries** (2 hours)
   - Create GetIndustries.cs endpoint
   - Seed industry master data in database migrations or startup
   - Write 1 integration test
   - **Deliverable**: GetIndustriesTests (1 test passing)
   - **Verification**: `dotnet test --filter "FullyQualifiedName~GetIndustriesTests"`
   - **GATE CHECKPOINT**: 5 endpoints + 16 integration tests passing

---

### Phase 3: Week 3 - Bid Management (10 hours)

**Day 1-2: Bid Submission (4 hours)**

10. **T010: POST /innovations/{innovationId}/bids** (4 hours)
    - Create Bid.cs entity in Domain folder
    - Create SubmitBid.cs endpoint
    - Implement eligibility rules (R4.1: actor type, duplicate check)
    - Implement proposal validation (min 200 chars per R4.2)
    - Write 4 integration tests
    - **Deliverable**: SubmitBidTests (4 tests passing)
    - **Verification**: `dotnet test --filter "FullyQualifiedName~SubmitBidTests"`

**Day 3: Bid Viewing & Updates (6 hours)**

11. **T011: GET /innovations/{innovationId}/bids** (3 hours)
    - Create GetBids.cs endpoint
    - Implement owner-only authorization
    - Return full bid details including proposals
    - Write 3 integration tests
    - **Deliverable**: GetBidsTests (3 tests passing)
    - **Verification**: `dotnet test --filter "FullyQualifiedName~GetBidsTests"`

12. **T012: PUT /bids/{bidId}** (3 hours)
    - Create UpdateBid.cs endpoint
    - Implement author authorization (R8.2)
    - Implement status validation (Pending only per R4.3)
    - Write 3 integration tests
    - **Deliverable**: UpdateBidTests (3 tests passing)
    - **Verification**: `dotnet test --filter "FullyQualifiedName~UpdateBidTests"`
    - **GATE CHECKPOINT**: 3 endpoints + 10 integration tests passing

---

### Phase 4: Week 4 - Journey Tests & Documentation (12 hours)

**Day 1-2: Journey Test Suites (9 hours)**

13. **T013: Journey 1 - Innovation Submission** (5 hours)
    - Create Journey1_InnovationSubmissionTests.cs
    - Implement test fixture with 7 helper methods
    - Write primary journey test (8-step orchestration)
    - Write 3 error path tests
    - **Deliverable**: Journey1Tests (4 tests passing)
    - **Verification**: `dotnet test --filter "FullyQualifiedName~Journey1_InnovationSubmissionTests"`
    - **⚠️ COMPLEXITY WARNING**: First comprehensive journey test (template for T014)

14. **T014: Journey 2 - Discovery & Bidding** (4 hours)
    - Create Journey2_BiddingTests.cs
    - Extend test fixture with 3 bid helper methods
    - Write primary journey test (8-step orchestration)
    - Write 2 error path tests
    - **Deliverable**: Journey2Tests (3 tests passing)
    - **Verification**: `dotnet test --filter "FullyQualifiedName~Journey2_BiddingTests"`

**Day 3: Validation & Documentation (3 hours)**

15. **T015: Validate Complete Test Suite** (1 hour)
    - Run full test suite: `dotnet test --verbosity normal`
    - Verify 67/67 tests passing (100% pass rate)
    - Verify execution time <30 seconds
    - **Deliverable**: Test execution report showing 100% pass rate
    - **GATE CHECKPOINT**: 100% test pass rate required to proceed

16. **T016: Complete OpenAPI Documentation** (2 hours)
    - Review all 8 new endpoint files for XML documentation
    - Add missing `<summary>`, `<remarks>`, `<param>`, `<response>` tags
    - Build and verify zero XML documentation warnings
    - Launch Swagger UI and validate all endpoints visible
    - **Deliverable**: Zero build warnings, Swagger UI shows 14 endpoints
    - **Verification**: `dotnet build 2>&1 | grep -i "warning.*xml"` (expect no output)

**Day 4: Traceability & Sign-off (2 hours)**

17. **T017: Update Specification Traceability** (2 hours)
    - Create traceability matrix: `specs/003-api-completion/traceability.md`
    - Map 7 user stories → endpoints → tests
    - Update spec.md with implementation annotations
    - Update README.md with Phase 0.6 section
    - **Deliverable**: Complete traceability documentation
    - **GATE CHECKPOINT**: Phase 0.6 Complete, Ready for Merge

---

## Missing Checklists

### 1. Pre-Task Setup Checklist (CRITICAL MISSING)

**Recommendation**: Add at beginning of tasks.md

```markdown
## Pre-Task Setup Checklist

Before starting T001, ensure the following:

### Environment Verification
- [ ] On correct branch: `003-api-completion`
- [ ] Branch is up-to-date with parent: `git fetch origin && git merge origin/001-platform-core`
- [ ] No uncommitted changes: `git status` shows clean working directory
- [ ] Build succeeds: `dotnet build` (zero errors)
- [ ] Test suite runs: `dotnet test` (captures baseline pass rate)
- [ ] IDE/Editor configured: XML documentation warnings enabled

### Development Tools
- [ ] .NET 8.0 SDK installed: `dotnet --version` (8.0.x)
- [ ] EF Core tools installed: `dotnet tool list -g` (includes dotnet-ef)
- [ ] Git configured: `git config user.name` and `git config user.email` set
- [ ] Text editor/IDE: VS Code, Visual Studio, or Rider

### Documentation Access
- [ ] Read spec.md (989 lines) - understand user stories
- [ ] Read plan.md (564 lines) - understand technical approach
- [ ] Read tasks.md (this file) - understand acceptance criteria
- [ ] Read subcutaneous-test-requirements.md - understand testing patterns
- [ ] Review constitution.md (principles 2, 3, 4, 5, 6)

### Knowledge Prerequisites
- [ ] Understand Vertical Slice Architecture (feature folders)
- [ ] Understand ASP.NET Core Minimal APIs pattern
- [ ] Understand xUnit + WebApplicationFactory testing
- [ ] Understand EF Core In-Memory provider behavior
- [ ] Understand JWT authentication flow

### Risk Awareness
- [ ] T001-T002 are HIGH RISK (database context) - allocate 3 full days if blocked
- [ ] T007 is COMPLEX (13 validation rules) - may need 5-6 hours instead of 4
- [ ] T013 is COMPLEX (first journey test) - template for all future journey tests
```

**Rationale**: Prevents common setup issues, establishes baseline expectations, ensures developer has read critical documentation before coding.

---

### 2. Post-Task Commit Checklist (CRITICAL MISSING)

**Recommendation**: Add after each task section

```markdown
## Post-Task Commit Guidelines

After completing each task, follow this checklist:

### Before Committing
- [ ] All acceptance criteria met (review task section)
- [ ] Verification command executed successfully
- [ ] Build succeeds with zero warnings: `dotnet build`
- [ ] Tests pass: `dotnet test` (task-specific tests + full suite)
- [ ] Code formatted: Follow existing code style conventions
- [ ] No commented-out code or debug statements
- [ ] No TODO/FIXME comments without GitHub issue reference

### Commit Message Format
```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types**: feat, fix, test, docs, refactor, chore  
**Scope**: endpoint name or component (e.g., CreateInnovation, TestFixture)  
**Subject**: Imperative, lowercase, no period, max 72 chars  
**Body**: What and why (not how), wrap at 80 chars  
**Footer**: Closes #issue or BREAKING CHANGE (if applicable)

### Examples
```bash
# T001 commit
git add .specify/analysis/test-context-diagnostic.md
git commit -m "docs(test-infrastructure): diagnose database context lifecycle issue

Root cause identified: EF Core In-Memory provider creates separate database 
instances when DbContext instances differ between test constructor and 
WebApplicationFactory. Proposed solution: unique database naming strategy 
with shared configuration.

Related to Phase 0.6 T001."

# T005 commit
git add src/Innoventity.API/Features/Innovations/CreateInnovation.cs
git add tests/Innoventity.API.Tests/Integration/Innovations/CreateInnovationTests.cs
git commit -m "feat(CreateInnovation): implement POST /innovations endpoint

- Add CreateInnovation.cs with request validation
- Implement authorization (JWT + ActorType = IdeaGenerator)
- Add CreateInnovationTests.cs with 4 integration tests
- All acceptance criteria met (T005)

Tests: CreateInnovationTests (4/4 passing)"
```

### After Committing
- [ ] Commit message follows format above
- [ ] Commit is atomic (one task, one commit)
- [ ] Commit hash recorded in task tracking (optional)
- [ ] Push to remote branch: `git push origin 003-api-completion`
```

**Rationale**: Ensures consistent commit history, traceability between commits and tasks, atomic commits for easier code review.

---

### 3. Weekly Milestone Checklist (IMPORTANT MISSING)

**Recommendation**: Add at end of each week section

```markdown
## Week 1 Milestone Checklist

At end of Week 1 (after T004 completion):

### Quality Gates
- [ ] 57/60 tests passing (95% pass rate)
- [ ] 4 subcutaneous infrastructure tests passing
- [ ] Zero build warnings
- [ ] Database context fix validated
- [ ] JWT token attachment fix validated

### Documentation
- [ ] Diagnostic report exists: `.specify/analysis/test-context-diagnostic.md`
- [ ] Test fixture updated and documented
- [ ] Commit history shows 4 commits (T001-T004)

### Team Communication
- [ ] Stand-up update: "Week 1 complete, infrastructure stable"
- [ ] Blockers identified and escalated (if any)
- [ ] Remaining 3 test failures documented as Phase 1 scope

### Risk Assessment
- [ ] If Week 1 took >12 hours, reassess Week 2-4 estimates
- [ ] If T001-T002 revealed deeper issues, escalate before Week 2

### Decision Point
- [ ] PROCEED to Week 2 (Infrastructure is stable)
- [ ] HOLD (Infrastructure issues persist) - allocate more time

---

## Week 2 Milestone Checklist

At end of Week 2 (after T009 completion):

### Quality Gates
- [ ] 5 new endpoints implemented (POST/PUT/PATCH/GET innovations, GET industries)
- [ ] 16 integration tests passing (4+4+4+4+1)
- [ ] 73/77 tests passing (original 57 + new 16)
- [ ] Zero build warnings
- [ ] All endpoints return correct HTTP status codes

### API Contracts
- [ ] POST /innovations returns 201 Created with InnovationId
- [ ] PUT /innovations/{id} validates ownership (403 if wrong owner)
- [ ] PATCH /innovations/{id}/submit validates completeness (13 rules)
- [ ] GET /innovations filters by industryId and researchCategory
- [ ] GET /industries returns master list

### Documentation
- [ ] All 5 endpoints have XML documentation
- [ ] Swagger UI shows new endpoints correctly
- [ ] Integration tests document expected behavior

### Team Communication
- [ ] Stand-up update: "Week 2 complete, Journey 1 API surface ready"
- [ ] Frontend team notification: "Innovation CRUD endpoints ready for integration"

### Risk Assessment
- [ ] If Week 2 took >18 hours, reassess Week 3-4 estimates
- [ ] If T007 completeness validation was complex, document lessons learned

### Decision Point
- [ ] PROCEED to Week 3 (Journey 1 API validated)
- [ ] HOLD (Endpoint issues persist) - fix before Journey 2

---

## Week 3 Milestone Checklist

At end of Week 3 (after T012 completion):

### Quality Gates
- [ ] 3 new endpoints implemented (POST/GET/PUT bids)
- [ ] 10 integration tests passing (4+3+3)
- [ ] 83/87 tests passing (original 73 + new 10)
- [ ] Zero build warnings
- [ ] Bid entity created and validated

### API Contracts
- [ ] POST /innovations/{innovationId}/bids validates eligibility (R4.1)
- [ ] POST /innovations/{innovationId}/bids enforces proposal min length (200 chars)
- [ ] GET /innovations/{innovationId}/bids restricted to owner
- [ ] PUT /bids/{bidId} prevents editing accepted bids (R4.3)

### Documentation
- [ ] All 3 endpoints have XML documentation
- [ ] Swagger UI shows bid endpoints correctly
- [ ] Integration tests document bid lifecycle

### Team Communication
- [ ] Stand-up update: "Week 3 complete, Journey 2 API surface ready"
- [ ] Frontend team notification: "Bid management endpoints ready"

### Risk Assessment
- [ ] If Week 3 took >12 hours, reassess Week 4 estimates
- [ ] If bid eligibility rules were complex, document edge cases

### Decision Point
- [ ] PROCEED to Week 4 (Journey 2 API validated)
- [ ] HOLD (Bid logic issues) - fix before journey tests

---

## Week 4 Milestone Checklist

At end of Week 4 (after T017 completion):

### Quality Gates
- [ ] 67/67 tests passing (100% pass rate) ✅
- [ ] 7 new journey tests passing (4 Journey1 + 3 Journey2)
- [ ] Test suite executes in <30 seconds ✅
- [ ] Zero build warnings ✅
- [ ] All endpoints documented in Swagger UI ✅

### Journey Validation
- [ ] Journey 1 validated end-to-end (Register → Activate → Login → Create Draft → Update → Submit → Published)
- [ ] Journey 2 validated end-to-end (Register → Activate → Login → Discover → View → Bid → Owner Sees Bid)
- [ ] Error paths tested (incomplete submissions, unauthorized access, duplicate bids)

### Documentation
- [ ] Traceability matrix complete (7 user stories → endpoints → tests)
- [ ] spec.md annotated with implementation status
- [ ] README.md updated with Phase 0.6 section
- [ ] OpenAPI documentation complete (XML comments on all endpoints)

### Team Communication
- [ ] Stand-up update: "Phase 0.6 complete, ready for code review"
- [ ] Frontend team notification: "Backend API validated and documented"
- [ ] Code review requested

### Decision Point
- [ ] PROCEED to Merge (all gates passed)
- [ ] HOLD (documentation incomplete) - complete before merge
```

**Rationale**: Weekly checkpoints prevent drift, enable early course correction, provide clear Go/No-Go decision points.

---

### 4. Merge Readiness Checklist (CRITICAL MISSING)

**Recommendation**: Add at end of tasks.md before Summary Statistics

```markdown
## Merge Readiness Checklist

Before merging `003-api-completion` into `001-platform-core`:

### Code Quality Gates
- [ ] 67/67 tests passing (100% pass rate)
- [ ] Test suite executes in <30 seconds
- [ ] Zero build warnings: `dotnet build`
- [ ] Zero code analysis warnings (if enabled)
- [ ] All endpoints return correct HTTP status codes
- [ ] All endpoints have XML documentation

### Test Coverage
- [ ] 11 subcutaneous tests passing (Phase0Journey + GetInnovation + Journey1 + Journey2)
- [ ] 27 integration tests passing (8 endpoints × ~3 tests each)
- [ ] 29 original tests passing (authentication, health, domain)
- [ ] Error paths tested (401, 403, 404, 409 responses validated)

### Documentation Complete
- [ ] spec.md: All 7 user stories marked ✅ IMPLEMENTED
- [ ] tasks.md: All 17 tasks marked ✅ COMPLETE
- [ ] plan.md: Implementation complete, risks resolved
- [ ] traceability.md: All user stories traced to endpoints and tests
- [ ] README.md: Phase 0.6 section added
- [ ] OpenAPI/Swagger: All 14 endpoints documented

### Git Hygiene
- [ ] Branch up-to-date with parent: `git fetch origin && git merge origin/001-platform-core`
- [ ] No merge conflicts: `git status` shows clean merge
- [ ] Commit history is clean (17 atomic commits for T001-T017)
- [ ] No uncommitted changes: `git status` shows clean working directory
- [ ] All commits pushed to remote: `git push origin 003-api-completion`

### Code Review
- [ ] Pull request created on GitHub
- [ ] PR description references spec.md and highlights key changes
- [ ] At least one reviewer assigned
- [ ] All review comments addressed
- [ ] Reviewer approval obtained

### CI/CD Validation (if applicable)
- [ ] GitHub Actions build passes (or equivalent CI)
- [ ] All CI tests pass
- [ ] No deployment blockers identified

### Constitutional Compliance
- [ ] Principle 2 (Quality): 100% test pass rate ✅
- [ ] Principle 3 (Simplicity): Uses standard patterns (Minimal APIs, Vertical Slice) ✅
- [ ] Principle 4 (Specification Drives): Spec-first workflow followed ✅
- [ ] Principle 5 (Tests Prove): Test-first discipline maintained ✅
- [ ] Principle 6 (Architecture Supports Evolution): Backend-first validation ✅
- [ ] Principle 7 (Incremental): 4-week timeline, weekly milestones ✅

### Team Communication
- [ ] Stand-up announcement: "Phase 0.6 ready for merge"
- [ ] Frontend team notification: "Backend API complete and stable"
- [ ] Documentation team notification: "API documentation updated"

### Decision Point
- [ ] ✅ MERGE APPROVED - All gates passed
- [ ] ❌ MERGE BLOCKED - Document blockers and remediation plan
```

**Rationale**: Prevents premature merges, ensures quality gates enforced, provides clear checklist for code reviewers.

---

### 5. Task-Specific Interim Checkpoints (RECOMMENDED)

**T007: PATCH /innovations/{id}/submit** (4 hours, COMPLEX)

**Recommendation**: Add interim checkpoints within task

```markdown
### T007 Interim Checkpoints

**Checkpoint 1** (After 1 hour): Endpoint scaffolding complete
- [ ] SubmitInnovation.cs file created
- [ ] Request/response DTOs defined
- [ ] Endpoint registered in Program.cs
- [ ] Empty validation method created

**Checkpoint 2** (After 2.5 hours): Validation logic implemented
- [ ] All 13 completeness rules implemented (R2.1)
- [ ] Status transition logic (Draft → Published) implemented
- [ ] Ownership validation implemented
- [ ] Unit tests for validation logic passing (if applicable)

**Checkpoint 3** (After 4 hours): Integration tests passing
- [ ] SubmitInnovationTests (4 tests) written
- [ ] All 4 tests passing
- [ ] XML documentation complete
- [ ] Manual API test successful: `curl -X PATCH http://localhost:5000/innovations/{id}/submit -H "Authorization: Bearer {token}"`
```

**T013: Journey 1 Complete Test Suite** (5 hours, COMPLEX)

**Recommendation**: Add interim checkpoints within task

```markdown
### T013 Interim Checkpoints

**Checkpoint 1** (After 1.5 hours): Test fixture complete
- [ ] Journey1_InnovationSubmissionTests.cs created
- [ ] 7 helper methods implemented (RegisterActor, ActivateAccount, Login, CreateInnovation, UpdateInnovation, SubmitInnovation, GetInnovation)
- [ ] Test database seeding helper created

**Checkpoint 2** (After 3 hours): Primary journey test passing
- [ ] Journey1_IdeaGeneratorSubmitsInnovation_PublishedSuccessfully() written
- [ ] All 8 steps orchestrated successfully
- [ ] Test passes (green)
- [ ] Database assertions validated (Status = Published)

**Checkpoint 3** (After 5 hours): Error path tests passing
- [ ] Journey1_SubmitIncompleteInnovation_Returns400WithValidationErrors() passing
- [ ] Journey1_NonOwnerCannotSubmitInnovation_Returns403() passing
- [ ] Journey1_SubmitAlreadyPublished_Returns409() passing
- [ ] All 4 tests passing
```

**Rationale**: Long/complex tasks benefit from interim checkpoints to track progress and identify blockers early.

---

## Additional Observations

### Strengths of Current Plan

1. **Clear Dependencies**: Critical path well-defined (T001-T004 blocks all)
2. **Acceptance Criteria**: Every task has measurable success criteria
3. **Verification Commands**: Concrete commands to validate task completion
4. **Test-First Discipline**: Integration tests required before endpoint implementation
5. **Constitutional Compliance**: 6/6 gates assessed and passing
6. **Risk Assessment**: T001-T002 identified as HIGH RISK with mitigation strategy

### Potential Concerns

1. **Optimistic Time Estimates**: T007 (4h) and T013 (5h) may need 5-6h given complexity
2. **No Buffer Time**: 47-hour estimate assumes no blockers or rework
3. **Sequential Assumptions**: Plan assumes single developer; parallel execution would change timeline
4. **Phase 1 Domain Tests**: 3 tests marked as "Phase 1 deferred" but not documented in plan.md
5. **Industry Master Data**: T009 mentions "seed industry master data" but doesn't specify data structure or count

### Recommendations

1. **Add 20% Buffer**: Increase estimate from 47h to ~56h (3-4 weeks becomes 4-5 weeks realistically)
2. **Document Industry Schema**: Add Industry entity schema to plan.md data model section
3. **Document Phase 1 Scope**: Clarify what "Phase 1 domain validation" means (appears to be out of scope for 0.6)
4. **Add Rollback Plan**: If T001-T002 fail after 3 days, what's the contingency? (Mentioned in Risk Assessment but not actionable)
5. **Consider Test Parallelization**: If test suite >30s, document how to parallelize (e.g., xUnit collection fixtures)

---

## Conclusion

**Overall Verdict**: ✅ **APPROVE WITH RECOMMENDATIONS**

The Phase 0.6 plan is comprehensive, well-structured, and follows constitutional principles. The critical path is clear, dependencies are well-defined, and acceptance criteria are measurable. 

**Must-Have Before T001 Execution**:
1. Add Pre-Task Setup Checklist
2. Add Post-Task Commit Checklist
3. Add Merge Readiness Checklist

**Should-Have Before T001 Execution**:
4. Add Weekly Milestone Checklists
5. Add interim checkpoints for T007, T013, T014

**Nice-to-Have**:
6. document Industry entity schema
7. Add 20% buffer to timeline estimate
8. Document Phase 1 scope explicitly

**Ready to Begin**: Yes, after adding Pre-Task Setup Checklist (5-10 minutes to create).
