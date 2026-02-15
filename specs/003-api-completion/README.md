# Phase 0.6: API Completion & Subcutaneous Testing

**Feature Branch**: `003-api-completion`
**Parent Branch**: `001-platform-core` (includes Phase 0 + Phase 0.5)
**Status**: 📋 Specification Complete - Ready for Implementation
**Estimated Effort**: 40-50 hours (3-4 weeks)

---

## Overview

Phase 0.6 completes the backend API surface area and establishes comprehensive subcutaneous testing coverage for Journey 1 (Innovation Submission) and Journey 2 (Innovation Discovery & Bidding) **before** any frontend or deployment work begins.

### Problem Statement

After merging Phase 0.5, we have:
- ✅ 6 API endpoints (Authentication + GetInnovation + Health)
- ⚠️ **53/60 tests passing** (88.3%) - 4 failing subcutaneous tests due to infrastructure issues
- ❌ **API surface 35% complete** - Missing 11+ endpoints required for Journey 1-2 validation
- ❌ **Cannot validate core journeys end-to-end** without UI (subcutaneous testing blocked)

### Solution

Complete the backend API with **subcutaneous testing** (Martin Fowler pattern) to validate all critical user journeys through API endpoints with real infrastructure, avoiding UI complexity while proving backend completeness.

**Phase 0.6 delivers**:
1. Fix 4 failing subcutaneous tests (database context lifecycle, JWT token issues)
2. Implement 8 new API endpoints (Innovation CRUD, Bid management, Industries list)
3. Build comprehensive journey test suites (Journey1Tests, Journey2Tests)
4. Achieve **100% test pass rate** (67/67 tests passing)
5. **Backend validated as complete** before frontend work begins

---

## 📄 Specification Documents

| Document | Purpose | Status |
|----------|---------|--------|
| **[spec.md](spec.md)** | Complete feature specification with user stories, system requirements, success metrics | ✅ Complete |
| **[tasks.md](tasks.md)** | Detailed task breakdown (T001-T017) with acceptance criteria | ✅ Complete |
| **[subcutaneous-test-requirements.md](subcutaneous-test-requirements.md)** | Subcutaneous testing patterns, infrastructure fixes, code examples | ✅ Complete |
| **[traceability.md](traceability.md)** | (To be created) Implementation traceability matrix | ⏳ Phase 5 deliverable |

---

## 🎯 Success Criteria

**Phase 0.6 is COMPLETE when**:
- ✅ **67/67 tests passing** (100% pass rate, excluding 3 Phase 1 domain tests)
- ✅ **8 new API endpoints** fully implemented and documented
- ✅ **Journey 1 validated** via subcutaneous tests (Innovation Submission & Publication)
- ✅ **Journey 2 validated** via subcutaneous tests (Innovation Discovery & Bid Submission)
- ✅ **Zero build warnings**
- ✅ **Test suite executes in <30 seconds**
- ✅ **Backend API surface complete** for frontend integration

---

## 📅 Timeline & Effort

**Total Estimated Effort**: 47 hours (3-4 weeks at 12-15 hours/week)

### Week 1: Fix Failing Subcutaneous Tests (10 hours)
- **T001-T004**: Diagnose and fix database context lifecycle issues
- **Goal**: 95% pass rate (57/60 tests passing)

### Week 2: Innovation CRUD API (15 hours)
- **T005-T009**: Implement 5 Innovation endpoints (POST, PUT, PATCH, GET, GET /industries)
- **Goal**: Journey 1 API surface complete

### Week 3: Bid Submission API (10 hours)
- **T010-T012**: Implement 3 Bid endpoints (POST, GET, PUT)
- **Goal**: Journey 2 API surface complete

### Week 4: Journey Tests & Documentation (12 hours)
- **T013-T017**: Build Journey1Tests, Journey2Tests, complete documentation
- **Goal**: 100% pass rate (67/67), comprehensive journey validation

---

## 🚀 API Endpoints (Phase 0.6 Deliverables)

### Existing Endpoints (Phase 0 + Phase 0.5)
- ✅ POST /auth/register
- ✅ POST /auth/login
- ✅ POST /auth/refresh
- ✅ POST /auth/activate
- ✅ GET /innovations/{id}
- ✅ GET /health

### New Endpoints (Phase 0.6)
- ➕ **POST /innovations** - Create innovation draft (T005)
- ➕ **PUT /innovations/{id}** - Update draft innovation (T006)
- ➕ **PATCH /innovations/{id}/submit** - Publish innovation (T007)
- ➕ **GET /innovations** - List innovations with filters (T008)
- ➕ **GET /industries** - Industry master list (T009)
- ➕ **POST /innovations/{innovationId}/bids** - Submit bid (T010)
- ➕ **GET /innovations/{innovationId}/bids** - List bids for innovation (T011)
- ➕ **PUT /bids/{bidId}** - Update unaccepted bid (T012)

**Total Endpoints After Phase 0.6**: 14 endpoints (6 existing + 8 new)

---

## 🧪 Test Coverage (Phase 0.6 Deliverables)

### Existing Tests (Phase 0 + Phase 0.5)
- ✅ **Unit Tests**: 29 tests (authentication, domain models)
- ⚠️ **Integration Tests**: 24 tests (4 failing due to infrastructure)
- ⚠️ **Phase 1 Domain Tests**: 3 tests (expected failures, deferred to Phase 1)

### New Tests (Phase 0.6)
- ➕ **Infrastructure Fixes**: 4 tests fixed (Phase0JourneyTests, GetInnovationTests)
- ➕ **Innovation CRUD Tests**: 16 tests (CreateInnovation, UpdateInnovation, SubmitInnovation, ListInnovations)
- ➕ **Bid Management Tests**: 10 tests (SubmitBid, GetBids, UpdateBid)
- ➕ **Journey 1 Tests**: 4 tests (1 complete journey + 3 error paths)
- ➕ **Journey 2 Tests**: 3 tests (1 complete journey + 2 error paths)

**Total Tests After Phase 0.6**: 67 tests passing (100% pass rate)

---

## 📊 API Completeness Progression

| Journey | Steps | Endpoints Required | Before Phase 0.6 | After Phase 0.6 | Coverage |
|---------|-------|-------------------|------------------|-----------------|----------|
| **Journey 1** (Innovation Submission) | 7 steps | 6 endpoints | 1 endpoint (17%) | 6 endpoints (100%) | ✅ Complete |
| **Journey 2** (Discovery & Bidding) | 6 steps | 3 endpoints | 0 endpoints (0%) | 3 endpoints (100%) | ✅ Complete |
| **Journey 3** (Partner Selection) | 5 steps | 1+ endpoint | 0 endpoints | 0 endpoints | ⏳ Phase 0.7+ |
| **Overall** | 18 steps | 14 endpoints | 6 endpoints (43%) | 14 endpoints (100%) | ✅ Complete |

---

## 🛠️ Technical Architecture

### Subcutaneous Testing Pattern

**Definition**: Integration tests that operate "just under the skin" of the UI, exercising complete user journeys through API endpoints with real infrastructure (database, authentication) while avoiding UI complexity.

**Benefits**:
- 🚀 **Faster than UI tests**: 18-30s vs. 2-5 minutes
- 🎯 **More stable than UI tests**: No DOM selectors, no async rendering issues
- 🏗️ **Backend-first validation**: Proves API completeness before expensive frontend work
- 🔓 **Unblocks frontend team**: Complete, validated API available immediately
- 🐛 **Shifts bugs left**: API bugs caught in integration tests, not during UI development

**References**:
- Martin Fowler: [Testing Strategies in a Microservice Architecture](https://martinfowler.com/articles/microservice-testing/) (2014)
- Pattern: Subcutaneous Testing
- See [subcutaneous-test-requirements.md](subcutaneous-test-requirements.md) for comprehensive implementation guide

---

## 🚧 Prerequisites & Dependencies

### Prerequisites (MUST be complete before starting Phase 0.6)
- ✅ Phase 0 (Authentication) complete and merged to 001-platform-core
- ✅ Phase 0.5 (Domain Model Refactoring) complete and merged to 001-platform-core
- ✅ EntityBase infrastructure exists
- ✅ Address value object implemented
- ✅ Test infrastructure established (WebApplicationFactory, in-memory database)

### Blocking Dependencies
- **T001-T004 (Infrastructure Fixes)** block all other work
  - Database context lifecycle must be fixed before implementing new endpoints
  - JWT token attachment must work before authenticated endpoint testing

### Non-Blocking Dependencies
- T005-T009 (Innovation CRUD) can proceed independently of T010-T012 (Bid management)
- T013 (Journey1Tests) depends on T005-T009 complete
- T014 (Journey2Tests) depends on T010-T012 complete
- T015-T017 (Documentation) require all implementation complete

---

## 📚 Related Documentation

### Implementation Resources
- **DDD Analysis**: [.specify/analysis/ddd-architectural-analysis.md](../../.specify/analysis/ddd-architectural-analysis.md)
- **Constitutional Principles**: [.specify/memory/constitution.md](../../.specify/memory/constitution.md)
- **Phase 0 Spec**: [001-platform-core/spec.md](../001-platform-core/spec.md)
- **Phase 0.5 Spec**: [002-domain-enhancements/spec.md](../002-domain-enhancements/spec.md)

### Testing Resources
- **EF Core In-Memory Testing**: https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy
- **WebApplicationFactory**: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests
- **FluentAssertions**: https://fluentassertions.com/introduction
- **xUnit Documentation**: https://xunit.net/

---

## 🎓 Constitutional Alignment

Phase 0.6 aligns with these constitutional principles:

### Principle 2: Quality is Non-Negotiable
- **100% test pass rate required** (no failing tests allowed)
- Zero build warnings enforced
- Comprehensive validation before merge

### Principle 4: Specification Drives Implementation
- Complete specification created before implementation (this document)
- Tasks breakdown with acceptance criteria (tasks.md)
- Traceability from user stories to endpoints to tests

### Principle 5: Test-First Discipline
- **Subcutaneous tests as first-class deliverables** (not afterthought)
- Test infrastructure fixes prioritized (T001-T004)
- Journey tests validate complete flows, not just individual endpoints

### Principle 6: Incremental & Sustainable
- **Complete each layer fully before advancing** to next layer
- Backend API surface 100% complete before frontend work
- Subcutaneous tests prove backend completeness without UI dependency

**Constitutional Impact**: Maintains 99/100 rating (same as Phase 0.5)

---

## 🔄 Next Steps After Phase 0.6

### Option A: Frontend Development (Phase 0.7)
- Implement React + Next.js frontend
- Consume validated API endpoints
- No API rework required (backend proven complete)

### Option B: Deployment Preparation (Phase 0.8)
- Complete remaining 55 checklist items (migration guides, staging tests)
- Deploy to Azure App Service
- Configure production database (Azure SQL/PostgreSQL)

### Option C: Domain Evolution (Phase 1)
- Implement Innovation Composition Pattern (IdeaSummary, Product, Market owned entities)
- Add Journey 3 (Partner Selection) endpoints
- Expand subcutaneous test coverage

**Recommendation**: **Option A (Frontend)** after Phase 0.6 completion. Backend API surface is complete and validated, unblocking frontend team for parallel development.

---

## 📞 Questions & Support

**If you have questions about**:
- Specification details → See [spec.md](spec.md)
- Task breakdown → See [tasks.md](tasks.md)
- Subcutaneous testing patterns → See [subcutaneous-test-requirements.md](subcutaneous-test-requirements.md)
- Implementation approach → Review `.specify/analysis/` documents
- Constitutional compliance → See [.specify/memory/constitution.md](../../.specify/memory/constitution.md)

**For implementation guidance**:
1. Start with [tasks.md](tasks.md) - read T001-T004 first (infrastructure fixes)
2. Review [subcutaneous-test-requirements.md](subcutaneous-test-requirements.md) for test patterns
3. Follow task order strictly (T001 → T002 → T003 → T004 → T005...)
4. Mark tasks complete as you finish them

**Ready to implement?**
```bash
# Create feature branch
git checkout 001-platform-core
git pull origin 001-platform-core
git checkout -b 003-api-completion

# Start with T001 (diagnostic)
# See tasks.md for detailed instructions
```

---

**Status**: ✅ Specification Phase Complete - Ready for `/speckit.plan` to generate detailed implementation plan
