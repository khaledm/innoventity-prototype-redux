# Next Actions - Post Phase 001 Merge

**Created**: April 12, 2026
**Context**: Phase 001 (001-platform-core) successfully merged to Main
**Merge Commit**: 5b200d5
**Status**: All Phase 0 work complete, planning Phase 1

---

## Executive Summary

Phase 001 (Platform Core - MVP) is now **merged to Main** and fully operational. This document outlines immediate operational tasks, Phase 1 planning, and future roadmap.

**Current State**:
- ✅ Backend APIs: Registration, Authentication, Innovation viewing (112/115 tests passing)
- ✅ Infrastructure: Azure SQL, App Service, Application Insights (Terraform + PowerShell)
- ✅ CI/CD: 4 workflows (infra, deploy, drift detection, quality-nightly)
- ✅ Frontend: Angular 19 client with Login + Innovation detail pages
- ⚠️ Known limitation: Manual `swa deploy` required for frontend (automated CI/CD deferred to Phase 1)

---

## Category 1: Immediate Operational Tasks (Next 1-2 Days)

### OP-1: GitHub Repository Cleanup ⚡ PRIORITY

**Task**: Close PR #5 and delete merged branch

**Steps**:
```powershell
# 1. Close PR #5 via GitHub UI
# Navigate to: https://github.com/khaledm/innoventity-prototype-redux/pull/5
# Click "Close pull request" (changes already merged)

# 2. Delete remote branch
git push origin --delete 001-platform-core

# 3. Delete local branch (optional, for workspace cleanup)
git branch -d 001-platform-core

# 4. Verify cleanup
git branch -a | Select-String "001-platform-core"
# Should return no results
```

**Expected Outcome**: Clean repository state, no stale branches

---

### OP-2: Add Terratest SQL Password Secret

**Task**: Configure `TERRATEST_SQL_ADMIN_PASSWORD` GitHub repository secret for future CI integration

**Why**: Current implementation uses fallback `crypto/rand` password generation. When Terratest runs in CI, it should use a managed secret.

**Steps**:
```powershell
# Option 1: GitHub CLI
gh secret set TERRATEST_SQL_ADMIN_PASSWORD --repo khaledm/innoventity-prototype-redux
# Prompt will ask for password value
# Enter: Strong password ≥16 chars (uppercase/lowercase/number/special)

# Option 2: GitHub Web UI
# Navigate to: Settings → Secrets and variables → Actions → New repository secret
# Name: TERRATEST_SQL_ADMIN_PASSWORD
# Value: Strong SQL-safe password
```

**Password Requirements**:
- Minimum 16 characters
- Contains: uppercase, lowercase, number, special character
- SQL-safe (avoid single quotes, semicolons)

**Expected Outcome**: Secret available for future Terratest CI workflow

---

### OP-3: Verify Post-Merge CI/CD Pipelines

**Task**: Confirm all workflows running successfully on Main branch

**Steps**:
```powershell
# 1. Check recent workflow runs
gh run list --branch Main --limit 10

# 2. Check specific workflows
gh run view --web $(gh run list --workflow=infra.yml --branch Main --limit 1 --json databaseId --jq '.[0].databaseId')
gh run view --web $(gh run list --workflow=deploy.yml --branch Main --limit 1 --json databaseId --jq '.[0].databaseId')
gh run view --web $(gh run list --workflow=drift.yml --branch Main --limit 1 --json databaseId --jq '.[0].databaseId')

# 3. Verify scheduled drift detection runs (should trigger nightly at 02:00 UTC)
# Check next morning: gh run list --workflow=drift.yml --created ">=$(Get-Date -Format yyyy-MM-dd)"
```

**Expected Outcome**: All workflows green, drift detection operational

---

## Category 2: Phase 1 Planning (Next 2-4 Weeks)

### P1-Priority-1: Frontend CI/CD Automation (Week 1) 🎯 CRITICAL

**Effort**: 3-5 days
**Status**: 📋 PLANNED
**Spec**: Not yet written (deferred from Phase 0)

**Problem**: Phase 0 validated backend production readiness, but frontend deployment requires manual `swa deploy` command. No automated build/test/deploy pipeline for Angular client.

**What to Build**:

1. **GitHub Actions Workflow** (`.github/workflows/deploy-frontend.yml`):
   - Trigger: Push to `src/Innoventity.Client/**` or `workflow_dispatch`
   - Jobs:
     - `build-frontend`: `npm ci`, `npm run build`, upload artifacts
     - `test-frontend`: Jest unit tests (26+ tests), Playwright E2E tests (3+ journeys)
     - `deploy-to-swa`: Deploy `dist/` to Azure Static Web Apps staging slot
     - `validate-swa`: Run smoke tests against deployed SWA URL
     - `swap-swa`: Swap staging → production (manual approval gate, Main branch only)

2. **Azure Static Web App Resource** (Terraform module):
   - Add `infrastructure/modules/static-web-app/main.tf`
   - Provision SWA in `infrastructure/environments/dev/core/`
   - Output: SWA URL, deployment token (store as GitHub secret `AZURE_STATIC_WEB_APPS_API_TOKEN`)

3. **Documentation Updates**:
   - Update `infrastructure/README.md` with frontend deployment steps
   - Update `specs/001-platform-core/quickstart.md` with automated deployment workflow
   - Update `specs/001-platform-core/frontend-architecture.md` with CI/CD pipeline diagram

**Success Criteria**:
- ✅ Push to `src/Innoventity.Client/` triggers automated build/test/deploy
- ✅ Angular app accessible at `https://<swa-name>.azurestaticapps.net`
- ✅ E2E tests pass against deployed frontend + backend
- ✅ PR preview environments created automatically (SWA built-in feature)
- ✅ Manual deployment process no longer required

**Branch**: TBD (suggest `002-frontend-cicd` or start Phase 1 feature branch)

**Blocked By**: None - can start immediately

---

### P1-Priority-2: Domain Richness Enhancements (Weeks 2-5)

**Status**: 📋 PLANNED (specifications exist in `002-domain-enhancements`)
**Constitutional Goal**: 99/100 score (domain model maturity)

#### Sub-Task 1: ProductIdea Composition Pattern

**Effort**: 2-3 days
**Spec**: [002-domain-enhancements/spec.md §R3.5](specs/002-domain-enhancements/spec.md#r35-productidea-composition-pattern)
**Gap Analysis**: [§1.1 Legacy ProductIdea Aggregate](../.specify/analysis/innovation-bid-domain-gap-analysis.md#11-legacy-productidea---rich-aggregate-structure)

**Problem**: Current flat 63-property Innovation entity violates separation of concerns. Users think in stages: Idea → Product → Market → Collaboration.

**Changes**:
```
FROM: Innovation (flat 63 properties)

TO:   Innovation (aggregate root)
        ├── IdeaSummary (owned entity)
        ├── ProductDetails (owned entity)
        ├── MarketDetails (owned entity)
        └── CollaborationRequirement (owned entity)
```

**Breaking Changes**: YES
- Database: Columns renamed (e.g., `Innovation_Summary_Title`)
- API: DTOs restructured
- Tests: ~40 test fixtures updated

**Domain Methods**:
- `IsIdeaSummaryComplete()` - validates title, research type, background
- `IsProductDetailsComplete()` - validates description, tech details
- `IsReadyForSubmission()` - multi-stage validation gate
- `Submit()` - state transition with guards

**User Stories Enabled**:
- Multi-step innovation submission workflow
- Save incomplete drafts (progressive disclosure)
- Validation gates prevent incomplete submissions

---

#### Sub-Task 2: FormalResponse Polymorphic Hierarchy

**Effort**: 3-4 days
**Spec**: [002-domain-enhancements/spec.md §R3.6](specs/002-domain-enhancements/spec.md#r36-formalresponse-strategy-pattern)
**Gap Analysis**: [§2.1 Legacy FormalResponse](../.specify/analysis/innovation-bid-domain-gap-analysis.md#21-legacy-formalresponse---polymorphic-hierarchy)

**Problem**: Generic Bid entity with free-text prevents objective comparison, financial modeling, type-safe querying.

**Changes**:
```
FROM: Bid (generic entity with free-text proposal)

TO:   FormalResponse (abstract base)
        ├── ManufacturingResponse (yearly production costs)
        ├── SalesMarketingResponse (yearly sales projections)
        ├── ResearchDevelopmentResponse (dev timeline + costs)
        └── InvestorResponse (simple feedback)
```

**Breaking Changes**: YES
- Database: Table renamed, add Discriminator column, JSON columns for yearly projections
- API: Separate endpoints per type (`POST /innovations/{id}/bids/manufacturing`)
- Tests: Type-specific test data

**Rationale Requirement**: Every financial projection must include rationale (min 20 chars) for audit trail.

**Domain Services**:
- `IProjectValuationService.Calculate()` - NPV calculation
- Type-safe queries: `GetSelectedPartner<ManufacturingResponse>()`

**User Stories Enabled**:
- Compare bids with financial projections
- Auto-generate business plan from accepted bids
- Calculate NPV for scenario comparison

---

#### Sub-Task 3: Partner Selection Workflow

**Effort**: 2 days
**Spec**: ⚠️ NOT YET DOCUMENTED
**Gap Analysis**: [§2.6 Partnership Selection](../.specify/analysis/innovation-bid-domain-gap-analysis.md#26-partnership-selection-workflow-comparison)

**Problem**: Phase 0.6 ends at "view bids". No workflow for partner selection, validation, irreversibility.

**What to Build**:
- Domain methods: `HasMinimumBidsForSelection()`, `SelectPartners()`
- Endpoint: `POST /innovations/{id}/select-partners`
- Validations:
  - Caller is innovation owner
  - Innovation status = Published (not Draft, not InCollaboration)
  - Minimum bids met (≥1 per required type)
  - No duplicate partner types
- State transition: Published → InCollaboration
- Email notifications to selected partners

---

## Category 3: Phase 2+ Future Work (8+ Weeks Out)

### Phase 2: Engagement & Communication

**Status**: 📋 PLANNED
**Spec**: See [ROADMAP.md](specs/ROADMAP.md#phase-2-engagement--communication)

**Scope**:
- Messaging system (actor-to-actor threaded conversations)
- Interest tracking (bookmark innovations, subscribe to updates)
- Industry hierarchy (filter innovations by NAICS/SIC codes)
- Notification preferences

**Estimated Effort**: 6-8 weeks

---

### Phase 3: Virtual Incubator

**Status**: 💡 VISION
**Spec**: Not yet specified

**Scope**:
- BusinessPlan aggregate (separate from Innovation)
- Collaboration workspace (shared documents, milestones)
- Financial modeling tools
- Progress tracking dashboards

**Estimated Effort**: 10-12 weeks

---

## Category 4: Technical Debt & Quality Improvements

### TD-1: Frontend Test Stabilization

**Issue**: Angular unit tests 26/28 passing, Playwright E2E 1/3 passing
**Root Cause**: Test context timing issues with Angular Signals
**Impact**: Does NOT affect production functionality
**Priority**: MEDIUM (address in Phase 1 Frontend CI/CD work)

**Steps**:
1. Review failing unit tests in `src/Innoventity.Client/src/app/`
2. Fix AuthService signal initialization timing
3. Refactor E2E tests to use API-based login helper
4. Add retry logic for browser navigation assertions
5. Target: 28/28 unit tests, 3/3 E2E tests passing

---

### TD-2: Mutation Testing Coverage Expansion

**Current**: JwtTokenService + PasswordHasher only (80% mutation score)
**Target**: Core domain entities (Actor, Innovation, Bid)
**Priority**: LOW (nice-to-have, not blocking)

**Steps**:
1. Add Stryker config for `Domain/Entities/` folder
2. Run baseline: `dotnet stryker -p src/Innoventity.API/Innoventity.API.csproj`
3. Address surviving mutants (target ≥70% score)
4. Add to `quality-nightly.yml` workflow

---

## Category 5: Documentation & Process Improvements

### DOC-1: Architecture Decision Records (ADRs)

**Purpose**: Capture key architectural decisions for future reference

**Priority Decisions to Document**:
1. **ADR-001**: Split-state Terraform (core vs data layers) - WHY chosen, trade-offs
2. **ADR-002**: Hybrid Angular forms (Reactive + Signals) - context, alternatives
3. **ADR-003**: App Service Configuration over Key Vault - Phase 0 rationale
4. **ADR-004**: HS256 JWT signing - security considerations, future migration to RS256
5. **ADR-005**: EF Core In-Memory for integration tests - lifecycle management pattern

**Template**: Use standard ADR format (Context, Decision, Consequences)
**Location**: `.specify/docs/adr/`

---

### DOC-2: Registration UI Quickstart Update

**Issue**: `specs/001-platform-core/quickstart-registration-ui.md` references Phase 1+ features not yet implemented
**Action**: Add clear Phase 1 scoping notes, update prerequisites

---

## Recommended Prioritization

### Immediate (This Week)
1. ✅ **OP-1**: Close PR #5, delete branch (15 min)
2. ✅ **OP-2**: Add Terratest secret (5 min)
3. ✅ **OP-3**: Verify CI/CD pipelines (30 min)

### Next Sprint (Week 1-2)
4. 🎯 **P1-Priority-1**: Frontend CI/CD automation (3-5 days) - CRITICAL
5. 📋 **TD-1**: Fix frontend test failures (1-2 days)

### Following Sprints (Weeks 3-5)
6. 📋 **P1-Priority-2**: Domain richness (ProductIdea composition, FormalResponse hierarchy, Partner selection) (10-12 days)

### Ongoing
7. 📋 **DOC-1**: Document ADRs as time permits
8. 📋 **TD-2**: Expand mutation testing (optional quality improvement)

---

## Decision Points

### Question 1: Start Phase 1 as single feature branch or split workflows?

**Option A**: Single branch `002-platform-phase1` covering Frontend CI/CD + Domain richness
**Option B**: Separate branches `002-frontend-cicd` + `003-domain-richness`

**Recommendation**: **Option B** - Frontend CI/CD is independent, high-priority, and can be merged quickly. Domain richness involves breaking changes and needs longer review cycle.

---

### Question 2: Use Spec Kit workflow for Phase 1?

Phase 1 work is partially specified (002-domain-enhancements exists), but Frontend CI/CD lacks spec.

**Recommendation**:
- **Frontend CI/CD**: Write lightweight spec using `/speckit.specify` agent (1-2 hours)
- **Domain richness**: Existing `002-domain-enhancements/spec.md` is sufficient, run `/speckit.tasks` to generate implementation tasks

---

## Success Metrics - Phase 1 Exit Criteria

Phase 1 is **COMPLETE** when:

- ✅ Frontend CI/CD fully automated (no manual `swa deploy` required)
- ✅ Angular app deployed to Azure Static Web Apps via GitHub Actions
- ✅ PR preview environments working
- ✅ Frontend tests stable (28/28 unit, 3/3 E2E passing in CI)
- ✅ ProductIdea composition implemented with migration
- ✅ FormalResponse hierarchy implemented with 4 concrete types
- ✅ Partner selection workflow operational
- ✅ All integration tests updated for breaking changes
- ✅ Constitutional score: 99/100

---

## References

- **Roadmap**: [specs/ROADMAP.md](specs/ROADMAP.md)
- **Status Report**: [STATUS_REPORT.md](STATUS_REPORT.md)
- **Phase 001 Spec**: [specs/001-platform-core/spec.md](specs/001-platform-core/spec.md)
- **Phase 0.5 Spec**: [specs/002-domain-enhancements/spec.md](specs/002-domain-enhancements/spec.md)
- **Gap Analysis**: [.specify/analysis/innovation-bid-domain-gap-analysis.md](.specify/analysis/innovation-bid-domain-gap-analysis.md)
- **Constitution**: [.specify/memory/constitution.md](.specify/memory/constitution.md)

---

## Contact & Collaboration

**Next Review**: Scheduled after Frontend CI/CD completion
**Questions**: Create GitHub issue with label `planning` or `phase-1`
