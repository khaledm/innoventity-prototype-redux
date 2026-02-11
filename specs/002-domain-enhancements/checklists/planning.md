# Planning Quality Checklist: Phase 0.5 Domain Model Refactoring

**Purpose**: Validate plan.md completeness, accuracy, and implementability before coding begins
**Created**: February 10, 2026
**Feature**: [plan.md](../plan.md)
**Scope**: Phase 0.5 only (R8.4.1, R1.4, R1.5, R9.1)

---

## Checklist Purpose

This checklist validates the **QUALITY OF TECHNICAL PLANNING**, not implementation correctness. Each item tests whether plan.md provides sufficient, accurate, and actionable guidance for developers to implement Phase 0.5 successfully.

**What This Checklist Tests**:
- ✅ Is plan.md complete (all spec.md requirements addressed)?
- ✅ Is plan.md accurate (no conflicts with existing architecture)?
- ✅ Is plan.md actionable (specific steps, not vague guidance)?
- ✅ Are risks identified with mitigation strategies?

**What This Checklist Does NOT Test**:
- ❌ Whether code works correctly (that's test execution)
- ❌ Whether implementation matches plan (that's code review)
- ❌ Whether tests pass (that's CI/CD validation)

---

## Requirements Coverage

- [ ] CHK001: Is R8.4.1 (Password Salt Storage) implementation approach documented? [Completeness, Plan §Phase B]
  - Actor.PasswordSalt property defined with type/length
  - Salt generation strategy specified (RandomNumberGenerator, 32 bytes, Base64 encoding)
  - BCrypt compatibility validated (salt storage separate from embedded salt in hash)
  - Database column specifications (nvarchar(44), NOT NULL, DEFAULT '')

- [ ] CHK002: Is R1.4 (Actor Name Decomposition) implementation approach documented? [Completeness, Plan §Phase B]
  - Actor.FirstName and Actor.LastName properties defined
  - FullName removal documented as breaking change
  - Migration data transformation logic specified (split on last space)
  - Edge cases handled (single-word names, multiple spaces, empty values)
  - API contract changes documented (request/response DTOs)

- [ ] CHK003: Is R1.5 (Address Value Object) implementation approach documented? [Completeness, Plan §Phase B]
  - Address class structure defined (5 properties: Address1, Address2, City, PostCode, CountryCode)
  - EF Core owned entity configuration specified (OwnsOne, column naming prefix)
  - All-or-nothing validation strategy documented (if any address field, all required fields must be present)
  - Database schema specified (inline columns in Actors table, nullable)

- [ ] CHK004: Is R9.1 (Entity Base Class) implementation approach documented? [Completeness, Plan §Phase A]
  - EntityBase<TId> structure defined (Id property, equality methods, IsTransient())
  - Specializations defined (EntityOfGuid, EntityOfInt32)
  - Entity inheritance documented (Actor/Innovation/Industry inherit from appropriate base)
  - Equality semantics specified (identity-based, transient vs persisted)
  - GetHashCode() strategy documented (RuntimeHelpers for transient, Id hash for persisted)

- [ ] CHK005: Are all acceptance criteria from spec.md mapped to implementation steps in plan.md? [Traceability]
  - R8.4.1 AC-1 (PasswordSalt column exists) → Plan Phase B2 migration
  - R8.4.1 AC-2 (Salt generated on registration) → Plan Phase C1 Register.cs
  - R8.4.1 AC-3 (BCrypt validation uses stored salt) → Plan Phase C2 Login.cs
  - R1.4 AC-1 (FirstName/LastName properties exist) → Plan Phase B2 Actor.cs
  - R1.4 AC-2 (FullName dropped) → Plan Phase B4 migration
  - R1.4 AC-3 (Name split logic) → Plan Phase B4 migration SQL
  - R1.5 AC-1-5 (Address properties, EF config, validation) → Plan Phase B1-B3
  - R9.1 AC-1-4 (EntityBase methods, inheritance) → Plan Phase A1-A5

- [ ] CHK006: Are Phase 1+ features explicitly excluded from Phase 0.5 plan? [Scope Boundary]
  - Innovation composition (IdeaSummary, Product, Market) deferred to Phase 1
  - FormalResponse hierarchy deferred to Phase 1
  - BusinessPlan aggregate deferred to Phase 2
  - Valuation Service deferred to Phase 2
  - No mentions of create/edit/delete Innovation operations

- [ ] CHK007: Are "Domain Intent" rationales from spec.md preserved in technical approach? [Domain Knowledge Preservation]
  - R8.4.1 Domain Intent (OWASP compliance, audit trail) reflected in implementation notes
  - R1.4 Domain Intent (internationalization, proper sorting) reflected in DisplayName/SortableName computed properties
  - R1.5 Domain Intent (structured data, querying) reflected in EF Core owned entity justification
  - R9.1 Domain Intent (DDD semantics, collection safety) reflected in equality implementation

---

## Architecture Consistency

- [ ] CHK010: Does EntityBase<TId> design align with existing architecture? [Consistency, Plan §Phase A]
  - No conflicts with ASP.NET Core 8.0 conventions
  - No conflicts with EF Core 8.0 entity tracking
  - IEquatable<T> implementation standard for C#
  - RuntimeHelpers.GetHashCode() documented as stable for object lifetime

- [ ] CHK011: Does Address owned entity configuration follow EF Core best practices? [Technical Accuracy, Plan §Phase B]
  - OwnsOne() syntax correct for EF Core 8.0
  - Column naming convention documented (ContactAddress_ prefix)
  - Nullable owned entity support verified (Address? ContactAddress)
  - No separate Address table created (inline columns confirmed)

- [ ] CHK012: Are database migration patterns consistent with existing migrations? [Consistency]
  - Migration file naming follows EF Core convention ([Timestamp]_[Name])
  - Up() method adds columns, transforms data, drops old columns (correct order)
  - Down() method reverses changes and preserves data
  - Migration extends EF Core Migration base class

- [ ] CHK013: Are API contract changes consistent with existing endpoint patterns? [Consistency, Plan §Phase C]
  - Request/response DTOs follow record pattern (existing convention)
  - Validation error format uses ValidationProblemDetails (existing pattern)
  - JSON property naming camelCase (existing convention)
  - Nested object structure (Address) follows existing patterns

- [ ] CHK014: Is technology stack unchanged from Phase 0-5? [Stability]
  - ASP.NET Core 8.0 (no upgrade)
  - EF Core 8.0 (no upgrade)
  - BCrypt.Net (existing library)
  - xUnit + FluentAssertions (existing libraries)
  - No new dependencies introduced

---

## Migration Strategy Quality

- [ ] CHK020: Is FullName split algorithm specified with sufficient detail? [Completeness, Plan §Database Migration Strategy]
  - Split logic documented (CHARINDEX on REVERSE to find last space)
  - FirstName extraction logic (LEFT, LEN - CHARINDEX)
  - LastName extraction logic (RIGHT, CHARINDEX - 1)
  - Default behavior for no-space names documented (FirstName=full string, LastName='')

- [ ] CHK021: Are FullName split edge cases handled? [Edge Cases, Plan §Risk Assessment]
  - Single-word names (e.g., "Madonna") → FirstName="Madonna", LastName=""
  - Multiple spaces (e.g., "Mary Jane Watson") → splits on LAST space
  - Leading/trailing whitespace → should be trimmed (not documented - GAP?)
  - Empty/null FullName → should fail migration with error (not documented - GAP?)
  - Unicode/international characters → SQL compatibility (not documented - GAP?)

- [ ] CHK022: Is PasswordSalt backfill strategy decided and justified? [Decision Documentation, Plan §Database Migration Strategy]
  - Strategy selected: Generate new random salt (existing hashes remain valid)
  - Alternative considered: Extract embedded salt from BCrypt hash (complex parsing)
  - Justification documented: BCrypt embeds salt in hash, explicit salt for audit trail
  - SQL implementation: HASHBYTES('SHA2_256', NEWID()) or RandomNumberGenerator in C#

- [ ] CHK023: Is Address migration strategy complete? [Completeness, Plan §Database Migration Strategy]
  - Current ContactAddress is string (no structured data to parse)
  - New Address columns created as nullable (existing actors have NULL address)
  - Future registrations require structured Address or leave entire object NULL
  - No data loss (old ContactAddress dropped after migration, data was already unstructured)

- [ ] CHK024: Is Down() migration data-preserving? [Rollback Safety, Plan §Database Migration Strategy]
  - FullName reconstruction documented (FirstName + ' ' + LastName)
  - PasswordSalt drop documented (acceptable - hash still valid)
  - Address columns drop documented (acceptable - data was NULL for existing actors)
  - Rollback tested on local database (documented in Pre-Implementation checklist)

- [ ] CHK025: Are migration warnings/logging specified? [Observability, Plan §Risk Assessment]
  - Single-word names flagged for manual review
  - Empty FirstName or LastName flagged as error
  - Post-migration validation query documented (SELECT where LastName = '')

---

## Breaking Changes Management

- [ ] CHK030: Are all breaking API changes identified? [Completeness, Plan §API Contract Changes]
  - Registration request: fullName → firstName + lastName
  - Registration request: contactAddress string → Address object
  - Login response: fullName → firstName, lastName, displayName
  - RefreshToken response: fullName → firstName, lastName, displayName
  - GetInnovation response: Owner.fullName → firstName, lastName, displayName

- [ ] CHK031: Are before/after examples provided for each breaking change? [Clarity, Plan §API Contract Changes]
  - Registration endpoint: ✅ Complete JSON examples (before/after)
  - Login response: ✅ Complete JSON examples (before/after)
  - GetInnovation response: ✅ Complete JSON examples (before/after)

- [ ] CHK032: Are new validation rules documented? [Completeness, Plan §Phase C1]
  - firstName: Required, min 2 chars, max 50 chars
  - lastName: Required, min 2 chars, max 50 chars
  - contactAddress: Optional (entire object nullable)
  - contactAddress: All-or-nothing validation (if any field provided, Address1/City/PostCode/CountryCode required)
  - contactAddress.CountryCode: Exactly 2 uppercase letters (ISO 3166-1 alpha-2)
  - phone: Optional, max 20 chars

- [ ] CHK033: Is deployment strategy for breaking changes addressed? [Deployment Planning, Plan §Risk Assessment]
  - Deployment approach documented: Big-bang (frontend + backend together)
  - API versioning strategy: NOT implemented in Phase 0.5 (breaking change accepted)
  - Grace period: NOT implemented (no support for both old/new schemas)
  - Rollback strategy: Revert migration + redeploy previous version

- [ ] CHK034: Is stakeholder notification documented? [Communication, Plan §Risk Assessment]
  - Frontend team notification: Mentioned ("Communication: Notify client developers 2 weeks before")
  - Migration guide status: NOT created (Plan documents changes but no separate migration guide)
  - API documentation update: Mentioned (OpenAPI spec update in Definition of Done)

---

## Test Strategy Quality

- [ ] CHK040: Are all affected test files identified? [Completeness, Plan §Test Coverage Matrix]
  - ActorTests.cs (4 unit tests to update)
  - RegisterActorTests.cs (6 integration tests to update)
  - ActivateAccountTests.cs (3 integration tests to update)
  - LoginTests.cs (5 integration tests to update)
  - RefreshTokenTests.cs (2 integration tests to update)
  - GetInnovationTests.cs (4 integration tests to update)
  - Phase0JourneyTests.cs (2 E2E tests to update)
  - TOTAL: 26 tests to update (not 32? - VERIFY)

- [ ] CHK041: Are new test requirements specified? [Completeness, Plan §Test Coverage Matrix]
  - EntityBaseTests.cs: 7 new tests (equality, hash, transient, operators)
  - AddressTests.cs: 3 new tests (creation, CountryCode validation, required fields)
  - Registration with structured address: 2 new integration tests
  - E2E journey with name decomposition: 1 new integration test
  - TOTAL: 13 new tests (plan says 10? - VERIFY)

- [ ] CHK042: Is test execution plan per phase specified? [Actionability, Plan §Testing Strategy]
  - Phase A completion: 7 EntityBase tests passing
  - Phase B completion: 9 tests passing (6 Actor + 3 Address)
  - Phase C completion: 42 tests passing (ALL tests)
  - Incremental verification: Run tests after each phase

- [ ] CHK043: Are test update patterns documented? [Guidance, Plan §Phase C5-C6]
  - Unit test updates: Replace FullName with FirstName/LastName in test setup
  - Integration test updates: Update JSON payloads (fullName → firstName, lastName)
  - Response assertion updates: Update expected schema (FullName → firstName, lastName, displayName)
  - Seed data updates: Update all Actor creations (3 seed actors documented)

- [ ] CHK044: Is TDD discipline documented? [Methodology, Plan §Constraints]
  - "TDD Discipline: Tests FIRST, implementation SECOND" explicitly stated
  - Test execution after each file change documented
  - Red-Green-Refactor cycle implied by test-first approach

---

## Risk & Rollback Quality

- [ ] CHK050: Are all HIGH and MEDIUM risks identified? [Risk Coverage, Plan §Risk Assessment]
  - HIGH: FullName split algorithm incorrect (data corruption)
  - HIGH: Breaking changes break production (API downtime)
  - MEDIUM: Test updates incomplete (compilation errors)
  - MEDIUM: EF Core owned entity configuration error (runtime error)
  - LOW: Performance degradation (minimal impact)

- [ ] CHK051: Does each risk have a mitigation strategy? [Risk Management]
  - FullName split: Migration logs warnings, manual review process, post-migration validation
  - Breaking changes: Deploy to staging first, notify clients 2 weeks prior, rollback procedure
  - Test updates: Global search, compiler catches references, full suite run after each phase
  - EF Core config: Explicit column names, review migration SQL, test on local database

- [ ] CHK052: Is rollback procedure complete and testable? [Rollback Safety, Plan §Rollback Strategy]
  - Git rollback: `git checkout 001-platform-core` (branch revert)
  - Database rollback: `dotnet ef database update [PreviousMigration]`
  - Data restoration: Down() migration reconstructs FullName from FirstName + LastName
  - Verification: Run original 32 tests, all should pass

- [ ] CHK053: Are rollback complexity and risks documented? [Transparency, Plan §Rollback Strategy]
  - Complexity level: MEDIUM (data transformation, not just schema)
  - Data loss risks: FullName must be preserved in Down() migration
  - Mitigation: Test rollback on staging, backup database before production migration

- [ ] CHK054: Is pre-deployment testing documented? [Safety, Plan §Risk Assessment]
  - Test migration on staging environment before production
  - Backup database before migration
  - Validate rollback procedure works on staging

---

## Success Criteria Quality

- [ ] CHK060: Are success criteria measurable? [Measurability, Plan §Success Criteria]
  - Functional: "Actor.PasswordSalt column exists" (binary: yes/no)
  - Functional: "FullName dropped" (binary: yes/no)
  - Technical: "42+ tests passing" (quantitative: count tests)
  - Technical: "Zero compilation warnings" (quantitative: warning count = 0)
  - Quality: "Constitutional score 99/100" (quantitative: score)

- [ ] CHK061: Are success criteria aligned with spec.md acceptance criteria? [Traceability]
  - Spec R8.4.1 AC-1 → Plan Success Criteria "R8.4.1 (Password Salt): Actor.PasswordSalt column exists"
  - Spec R1.4 AC-1 → Plan Success Criteria "R1.4 (Name Decomposition): Actor.FirstName and Actor.LastName columns exist"
  - Spec R1.5 AC-1 → Plan Success Criteria "R1.5 (Address Value Object): Actor.ContactAddress is Address owned entity"
  - Spec R9.1 AC-1 → Plan Success Criteria "R9.1 (EntityBase): Actor, Innovation, Industry inherit from EntityBase<TId>"

- [ ] CHK062: Is "Definition of Done" comprehensive? [Completeness, Plan §Definition of Done]
  - Phase A Done: 5 criteria (EntityBase created, tests passing, inheritance added, code committed)
  - Phase B Done: 6 criteria (Address created, Actor refactored, migration created, seed data updated, tests passing, committed)
  - Phase C Done: 7 criteria (endpoints updated, tests updated, full suite passing, build clean, committed)
  - Overall Done: 9 criteria (all phases complete, 42+ tests, migration applied, docs updated, constitutional score, PR created)

---

## Effort Estimation Quality

- [ ] CHK070: Is effort estimate (7.5 hours) justified with breakdown? [Justification, Plan §Timeline]
  - Phase A: 2 hours (6 tasks × 15-30 min each = 105 min ≈ 2 hours) ✅ Justified
  - Phase B: 3 hours (6 tasks × 15-45 min each = 180 min = 3 hours) ✅ Justified
  - Phase C: 2.5 hours (7 tasks × 15-40 min each = 180 min ≈ 2.5 hours) ✅ Justified
  - Total: 7.5 hours ✅ Matches sum of phases

- [ ] CHK071: Is effort estimate realistic compared to Phase 0-5? [Reasonableness]
  - Phase 0-5: 58 tasks completed (actual hours not documented)
  - Phase 0.5: 19 tasks estimated at 7.5 hours (≈24 min/task average)
  - Complexity: Phase 0.5 has breaking changes + data migration (higher risk than Phase 0-5 greenfield)
  - Estimate reasonableness: Appears conservative (good for risky work)

- [ ] CHK072: Does timeline account for buffer? [Risk Management, Plan §Timeline]
  - 2-day sprint: Day 1 (4 hours), Day 2 (3.5 hours) = 7.5 hours planned
  - Buffer: 0.5 hours documented for "unexpected issues"
  - Buffer reasonableness: 6.7% buffer (industry standard 15-25% for risky work) - POTENTIALLY INSUFFICIENT

- [ ] CHK073: Are implementation phases sequentially dependent? [Sequencing, Plan §Implementation Phases]
  - Phase A → Phase B dependency: YES (Actor must inherit EntityOfGuid before refactoring)
  - Phase B → Phase C dependency: YES (API endpoints need new Actor schema before updates)
  - Phases cannot be parallelized: CORRECT (sequential implementation required)

---

## Documentation Quality

- [ ] CHK080: Are all new files documented with purpose? [Documentation, Plan §Appendix A]
  - EntityBase.cs: "Abstract base with equality" ✅ Purpose documented
  - EntityOfGuid.cs: "Guid specialization" ✅ Purpose documented
  - Address.cs: "Value object (owned entity)" ✅ Purpose documented
  - Test files: Purpose implied by naming (EntityBaseTests, AddressTests)

- [ ] CHK081: Are file modifications documented with change rationale? [Documentation, Plan §Appendix A]
  - Actor.cs: "[REFACTOR] Inherit EntityOfGuid" ✅ Change type documented
  - Innovation.cs: "[UPDATE] Inherit EntityOfGuid" ✅ Change type documented
  - Register.cs: Detailed changes documented in Phase C1 (40-minute task)
  - All 15 modified files have corresponding implementation sections in plan

- [ ] CHK082: Are external dependencies/references documented? [Traceability, Plan §Appendix C]
  - Expert DDD Architectural Analysis (source of patterns)
  - Legacy Domain Analysis (source of recommendations)
  - Feature Specification spec.md (source of requirements)
  - Eric Evans DDD book Chapter 5 (Entity pattern reference)
  - Martin Fowler Refactoring (Value Objects pattern reference)
  - EF Core Owned Entity Types documentation (technical reference)

- [ ] CHK083: Is plan.md version controlled? [Maintenance, Plan §Footer]
  - Plan Version: 1.0
  - Last Updated: February 10, 2026
  - Next Review: After Phase 0.5 completion

---

## Actionability Assessment

- [ ] CHK090: Can a developer implement Phase A without additional research? [Actionability]
  - EntityBase.cs: Full implementation provided in plan (copy-paste ready)
  - EntityOfGuid.cs: Full implementation provided (simple inheritance)
  - Actor inheritance: Clear before/after code samples
  - Test requirements: 7 test cases specified with descriptions

- [ ] CHK091: Can a developer implement Phase B without additional research? [Actionability]
  - Address.cs: Properties and attributes specified
  - Actor refactoring: Properties, types, and validations specified
  - AppDbContext: OwnsOne configuration syntax provided
  - Migration: SQL logic provided with edge case handling

- [ ] CHK092: Can a developer implement Phase C without additional research? [Actionability]
  - Registration endpoint: Request DTO structure provided, validation logic provided
  - Login endpoint: Response DTO structure provided, mapping code provided
  - Test updates: Before/after examples provided, update patterns documented
  - 7 test files: Specific update instructions for each (Phase C5-C6)

- [ ] CHK093: Are ambiguous terms defined within plan.md? [Clarity]
  - "Owned entity": Defined as "no separate Id, embedded in Actor table"
  - "Transient entity": Defined as "Id == default(TId), not yet persisted"
  - "Identity-based equality": Defined as "two entities equal if same Id"
  - "Constitutional score": NOT defined in plan (assumes reader knows context) - POTENTIAL GAP

---

## Identified Gaps & Recommendations

### Critical Gaps (Block Implementation)

**NONE IDENTIFIED** - Plan.md is comprehensive and implementation-ready.

### Minor Gaps (Address Before Implementation)

1. **CHK021**: FullName split edge cases incomplete
   - **Missing**: Whitespace trimming logic not documented
   - **Missing**: Empty/null FullName handling not documented
   - **Missing**: Unicode character compatibility not validated
   - **Recommendation**: Add to migration SQL with explicit TRIM() and NULL check

2. **CHK040**: Test count discrepancy
   - **Issue**: Plan says "32 tests to update" but only 26 identified in file list (ActorTests 4 + RegisterActorTests 6 + ActivateAccountTests 3 + LoginTests 5 + RefreshTokenTests 2 + GetInnovationTests 4 + Phase0JourneyTests 2 = 26)
   - **Issue**: Plan says "10 new tests" but calculation shows 13 (EntityBase 7 + Address 3 + Registration 2 + E2E 1 = 13)
   - **Recommendation**: Verify actual test count, update plan if discrepancy confirmed

3. **CHK072**: Buffer potentially insufficient
   - **Issue**: 6.7% buffer for high-risk work (industry standard 15-25%)
   - **Recommendation**: Consider extending timeline to 8-9 hours (add 1-1.5 hour buffer)

4. **CHK093**: "Constitutional score" not defined
   - **Issue**: Term used without explanation (readers unfamiliar with project may not understand)
   - **Recommendation**: Add footnote: "Constitutional score: Project health metric (0-100) measuring architecture quality, test coverage, and technical debt"

### Strengths

- ✅ **Exceptionally detailed**: Full code samples for EntityBase, Address, migration logic
- ✅ **Well-structured**: Clear phases with time estimates per task
- ✅ **Risk-aware**: Comprehensive risk assessment with mitigation strategies
- ✅ **Traceable**: All spec.md requirements mapped to implementation steps
- ✅ **Rollback-ready**: Complete rollback procedure documented and testable

---

## Overall Assessment

**Status**: ✅ **PASSED** (with 4 minor recommendations)

**Readiness**: 95% - Plan.md is exceptionally detailed and implementation-ready. The 4 identified gaps are minor and can be addressed quickly (< 30 minutes total).

**Quality**: EXCELLENT - This plan demonstrates expert-level technical planning with:
- Comprehensive requirement coverage
- Detailed implementation guidance (copy-paste ready code)
- Thorough risk assessment
- Clear success criteria
- Realistic effort estimation

**Recommendation**: Address 4 minor gaps (whitespace handling, test count verification, buffer extension, constitutional score definition), then proceed to implementation.

**Estimated Time to Address Gaps**: 20-30 minutes

---

## Sign-Off

**Reviewer**: [Name]
**Date**: [Date]
**Status**: PASSED (with minor recommendations)
**Next Step**: Address 4 minor gaps, then begin Phase A implementation

