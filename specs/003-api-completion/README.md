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
| **[plan.md](plan.md)** | Implementation plan with technical approach, constitution check, timeline | ✅ Complete |
| **[tasks.md](tasks.md)** | Detailed task breakdown (T001-T017) with acceptance criteria | ✅ Complete |
| **[subcutaneous-test-requirements.md](subcutaneous-test-requirements.md)** | Subcutaneous testing patterns, infrastructure fixes, code examples | ✅ Complete |
| **[implementation-lessons.md](implementation-lessons.md)** | **NEW** Lessons learned from T001-T009 (specification gaps, patterns discovered) | ✅ Complete |
| **[traceability.md](traceability.md)** | (To be created) Implementation traceability matrix | ⏳ Phase 5 deliverable |

---

## 🎯 Quick Reference

**Success Criteria**: Phase 0.6 is complete when 67/67 tests pass (100%), 8 new endpoints implemented, Journey 1-2 validated. See [spec.md](spec.md) for detailed success criteria.

**Timeline**: 47 hours over 4 weeks (Week 1: Infrastructure fixes, Week 2: Innovation CRUD, Week 3: Bid management, Week 4: Journey tests). See [plan.md](plan.md#timeline--effort) for details.

**API Endpoints**: 14 total endpoints (6 existing from Phase 0, 8 new in Phase 0.6). See [spec.md](spec.md#api-contracts) for complete endpoint specifications.

**Test Coverage**: 67 tests total (29 unit, 38 integration, 7 journey tests). See [PHASE-06-COMPLETION-CHECKLIST.md](PHASE-06-COMPLETION-CHECKLIST.md) for current progress.

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
- **xUnit Documentation**: https://xunit.net/

---

## 🎓 Constitutional Alignment

**Constitutional Compliance**: Phase 0.6 maintains 99/100 rating through 100% test pass rate requirement, test-first discipline, and complete backend validation before frontend work. See [plan.md](plan.md#constitution-check) for detailed gate assessment.

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
