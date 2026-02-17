# Innoventity Platform Development Roadmap
## Vision to Implementation Tracking

**Last Updated**: February 17, 2026
**Constitutional Alignment**: See [constitution.md](../.specify/memory/constitution.md)
**Gap Analysis Reference**: [innovation-bid-domain-gap-analysis.md](../.specify/analysis/innovation-bid-domain-gap-analysis.md)

---

## Document Purpose

This roadmap ensures **all recommendations from legacy domain analysis are captured** in phase-appropriate specifications and not forgotten. Each item links to:
1. **Specification** (WHAT user needs)
2. **Plan** (HOW to implement technically)
3. **Gap Analysis Section** (WHY it matters from legacy insight)

**Use this document to**:
- ✅ Verify phase scope before starting implementation
- ✅ Ensure gap analysis recommendations aren't lost between phases
- ✅ Track constitutional compliance evolution (98/100 → 100/100)
- ✅ Answer "when do we implement X?" questions

---

## Phase Completion Status

| Phase | Status | Branch | Spec | Tests | Gap Analysis Coverage |
|-------|--------|--------|------|-------|----------------------|
| **Phase 0 (MVP)** | ✅ COMPLETE | `001-platform-core` | [spec.md](001-platform-core/spec.md) | 57/57 ✅ | Foundation only (EntityOfGuid minimal) |
| **Phase 0.5 (Foundation)** | ⏸️ DEFERRED | `002-domain-enhancements` | [spec.md](002-domain-enhancements/spec.md) | 0/32 | EntityBase, Actor name/address (HIGH priority items) |
| **Phase 0.6 (API Completion)** | 🏃 IN PROGRESS (70%) | `003-api-completion` | [spec.md](003-api-completion/spec.md) | 61/67 target | Bid CRUD operations (simplified, Phase 1 will refactor) |
| **Phase 1 (Domain Richness)** | 📋 PLANNED | TBD | [See below](#phase-1-domain-richness--rich-behavior) | TBD | ProductIdea composition, FormalResponse polymorphism, Selection workflow |
| **Phase 2 (Engagement)** | 📋 PLANNED | TBD | [See below](#phase-2-engagement--communication) | TBD | Messaging, interest tracking, industry hierarchy |
| **Phase 3 (Virtual Incubator)** | 💡 VISION | TBD | Not yet specified | TBD | BusinessPlan aggregate, collaboration workspace |

**Legend**: ✅ Complete | 🏃 In Progress | 📋 Planned | 💡 Vision | ⏸️ Deferred

---

## Phase 0.6 (Current) - API Completion & Subcutaneous Testing

**Branch**: `003-api-completion`
**Duration**: 3 weeks (Feb 3-24, 2026)
**Status**: Week 3 - 70% complete (T001-T010 done, T011-T017 remaining)

### Scope Summary
Focus on **bid submission and viewing** (NOT partner selection, NOT financial projections). Intentionally simplified for MVP validation.

### Implemented (Gap Analysis Aware)
| Feature | Implementation | Gap Analysis Notes |
|---------|---------------|-------------------|
| **POST /innovations/{id}/bids** | Generic Bid entity | ✅ Phase 1 will refactor to FormalResponse hierarchy ([§2.1](../.specify/analysis/innovation-bid-domain-gap-analysis.md#21-legacy-formalresponse---polymorphic-hierarchy)) |
| **Bid eligibility rules (R4.1)** | 7 validation rules | ✅ Covers actor type, ownership, duplicate prevention |
| **Bid content rules (R4.2)** | Manual validation (200+ char proposal) | ⚠️ Phase 1 will add structured financial projections ([§2.4](../.specify/analysis/innovation-bid-domain-gap-analysis.md#24-critical-gap---financial-projection-structure-loss)) |
| **Innovation entity** | Flat 63-property entity | ⚠️ Phase 1 will refactor to composition ([§1.1](../.specify/analysis/innovation-bid-domain-gap-analysis.md#11-legacy-productidea---rich-aggregate-structure)) |

### Explicitly Deferred to Phase 1
- ❌ Partner selection workflow (selection validation, irreversibility)
- ❌ Financial projection structures (ManufacturingInfo, SalesInfo, DevInfo)
- ❌ NPV calculation service
- ❌ Innovation composition (IdeaSummary, Product, Market)
- ❌ Rich domain behavior methods (IsReadyForSubmission, HasMinimumBids, etc.)

### Related Specifications
- **Current Phase Spec**: [003-api-completion/spec.md](003-api-completion/spec.md)
- **Current Phase Plan**: [003-api-completion/plan.md](003-api-completion/plan.md)
- **Current Phase Tasks**: [003-api-completion/tasks.md](003-api-completion/tasks.md)

---

## Phase 1 (Next) - Domain Richness & Rich Behavior

**Estimated Duration**: 4 weeks (Mar 2026)
**Status**: 📋 PLANNED (specifications complete in 002-domain-enhancements)
**Constitutional Goal**: 99/100 score (domain model maturity)

### Critical Refactorings (MUST complete before Innovation Submission UI)

#### 1. ProductIdea Composition Pattern
**Effort**: 2-3 days | **Priority**: CRITICAL | **Spec**: [§R3.5](002-domain-enhancements/spec.md#r35-productidea-composition-pattern)

**Gap Analysis Reference**: [§1.1 Legacy ProductIdea Aggregate](../.specify/analysis/innovation-bid-domain-gap-analysis.md#11-legacy-productidea---rich-aggregate-structure)

**Problem Solved**: Current flat 63-property Innovation entity violates separation of concerns. Users think in distinct stages: (1) Idea summary, (2) Product details, (3) Market analysis, (4) Collaboration needs.

**What Changes**:
```
FROM: Innovation (flat entity with 63 properties)

TO:   Innovation (aggregate root)
        ├── IdeaSummary (owned entity)
        ├── ProductDetails (owned entity)
        ├── MarketDetails (owned entity)
        └── CollaborationRequirement (owned entity)
```

**Breaking Changes**: YES
- Database: Columns renamed (Innovation_Summary_Title, Innovation_Product_Description, etc.)
- API: Request/response DTOs restructured
- Tests: All Innovation seed data updated (~40 test fixtures)

**Domain Methods Added**:
- `IsIdeaSummaryComplete()` - validates title, research type, background
- `IsProductDetailsComplete()` - validates description, tech details
- `IsMarketDetailsSectionComplete()` - validates market size, target industries
- `IsReadyForSubmission()` - composes all section completeness checks
- `Submit()` - state transition with validation gate

**User Stories Enabled**:
- US Submit-1: Multi-step innovation submission workflow
- US Submit-2: Save incomplete drafts (progressive disclosure)
- US Submit-3: Validation gates prevent incomplete submissions

---

#### 2. FormalResponse Polymorphic Hierarchy
**Effort**: 3-4 days | **Priority**: CRITICAL | **Spec**: [§R3.6](002-domain-enhancements/spec.md#r36-formalresponse-strategy-pattern)

**Gap Analysis Reference**: [§2.1 Legacy FormalResponse Hierarchy](../.specify/analysis/innovation-bid-domain-gap-analysis.md#21-legacy-formalresponse---polymorphic-hierarchy)

**Problem Solved**: Current generic Bid entity with free-text proposal prevents:
- ❌ Objective bid comparison (cannot calculate NPV without structured data)
- ❌ Financial modeling (business plan requires projection tables)
- ❌ Type-safe querying (cannot distinguish Manufacturing vs Sales bids at compile time)

**What Changes**:
```
FROM: Bid (single generic entity)
        - Location: string
        - ParticipationType: string
        - ParticipationProposal: string (free text)

TO:   FormalResponse (abstract base)
        ├── ManufacturingResponse
        │     └── YearlyManufacturingCosts: Dictionary<int, ManufacturingInfo>
        │           - ProductionVolume + Rationale
        │           - UnitCost + Rationale
        │           - DistributionExpense + Rationale
        │
        ├── SalesMarketingResponse
        │     └── YearlySales: Dictionary<int, SalesInfo>
        │           - UnitsSold + Rationale
        │           - UnitPrice + Rationale
        │           - SalesExpense + Rationale
        │
        ├── ResearchDevelopmentResponse
        │     ├── ProductDevelopmentDuration: int (years)
        │     └── YearlyDevelopmentCosts: Dictionary<int, DevInfo>
        │           - InfrastructureCost + Rationale
        │           - PeopleCost + Rationale
        │
        └── InvestorResponse
              └── Response: string (simple feedback)
```

**Breaking Changes**: YES
- Database: Table renamed (Bids → FormalResponses), add Discriminator column, add JSON columns for yearly projections
- API: Separate endpoints for each bid type (`POST /innovations/{id}/bids/manufacturing`, `POST .../bids/sales`, etc.)
- Tests: Update bid submission tests for type-specific data structures

**Rationale Fields Requirement**:
Every financial projection field MUST have companion rationale field (min 20 characters). Creates audit trail for investors and business plan justification.

**Example Financial Projection**:
```json
ManufacturingResponse for Innovation X:
{
  "yearlyProjections": {
    "1": {
      "productionVolume": 10000,
      "productionVolumeRationale": "Based on quotations from suppliers A, B, C. Initial capacity 15K units, 70% utilization for ramp-up phase.",
      "unitCost": 5.50,
      "unitCostRationale": "Materials $3.50 (aluminum extrusion), labor $1.50 (3 hrs @ $50/hr), overhead $0.50 (15% allocation).",
      "distributionExpense": 1.20,
      "distributionExpenseRationale": "Global logistics average: Europe $0.80/unit, Asia $1.40/unit, Americas $1.30/unit. Weighted by target markets."
    },
    "2": { /* Year 2 projections... */ },
    // Years 3-5...
  }
}
```

**Domain Services Enabled**:
- `IProjectValuationService.Calculate()` - NPV calculation across 3 response types
- Type-safe partner queries: `GetSelectedPartner<ManufacturingResponse>()`

**User Stories Enabled**:
- US SelectPartner-1: Compare bids side-by-side with financial projections
- US BusinessPlan-1: Auto-generate financial model from accepted bids
- US Valuation-1: Calculate NPV for scenario comparison

---

#### 3. Partner Selection Workflow
**Effort**: 2 days | **Priority**: HIGH | **Spec**: ⚠️ NOT YET DOCUMENTED

**Gap Analysis Reference**: [§2.6 Partnership Selection Workflow](../.specify/analysis/innovation-bid-domain-gap-analysis.md#26-partnership-selection-workflow-comparison)

**Problem Solved**: Phase 0.6 ends at "view bids". No workflow for:
- Idea owner selecting partners (one Manufacturing + one Sales + one R&D)
- Validation that minimum bids received before selection
- Irreversibility enforcement (cannot change after BusinessPlan created)
- State transition to "InCollaboration" status

**What Changes**:
```
NEW Domain Methods on Innovation:
- HasMinimumBidsForSelection() → checks ≥1 bid per required type
- HasPartnerSelectionCompleted() → checks 3 accepted bids (one per type)
- SelectPartners(mfgId, salesId, rdId) → validates + transitions state

NEW Endpoints:
- POST /innovations/{id}/select-partners
    Body: { manufacturingBidId, salesBidId, rdBidId }
    Validates:
      1. Caller is innovation owner
      2. Innovation status = Published (not Draft, not already InCollaboration)
      3. Minimum bids met (HasMinimumBidsForSelection)
      4. Selection not already completed
      5. Each bid exists and not already accepted
      6. No duplicate partner types (cannot select 2 Manufacturing bids)

    On success:
      - Set Accepted=true, AcceptedOn=DateTime.UtcNow for 3 selected bids
      - Update Innovation.Status = InCollaboration
      - Set Innovation.PartnerSelectionCompletedOn = DateTime.UtcNow
      - Trigger domain event: PartnersSelectedEvent
      - Send email notifications to selected partners

- GET /innovations/{id}/selected-partners
    Returns: Array of 3 accepted bids with actor details
```

**Validation Rules** (from legacy SelectCollaborationPartnerCommandHandler):
```csharp
Rule 1: MustNotHaveCollabSelectionProcessCompleted
  → Enforces: HasPartnerSelectionCompleted() == false
  → Reason: Partner selection is IRREVERSIBLE (constitution principle)

Rule 2: MustSelectBidsFromAllCollabTypes
  → Enforces: Exactly one bid selected from Manufacturing, Sales, R&D
  → Reason: All three roles required for commercialization

Rule 3: SelectedBidsMustBeValid
  → Enforces: Each selected bid exists, not already accepted, belongs to innovation
  → Reason: Prevent invalid state (orphaned acceptances)
```

**Breaking Changes**: Minimal
- Database: Add `Innovation.PartnerSelectionCompletedOn: DateTime?` column
- No impact on existing Phase 0.6 endpoints

**User Stories Enabled**:
- US SelectPartner-2: Select collaboration team through structured process
- US SelectPartner-3: System enforces one partner per type rule
- US SelectPartner-4: Cannot change selection after commitment (irreversibility)

**⚠️ ACTION REQUIRED**: Create specification document for this feature
- Suggested location: `specs/004-partner-selection/spec.md`
- Include: User stories, business rules, validation logic, state transitions
- Reference: Legacy `SelectCollaborationPartnerCommandHandler.cs`

---

### Specifications Status

| Requirement | Spec Document | Plan Document | Gap Analysis Link | Status |
|-------------|--------------|---------------|-------------------|--------|
| **R3.5: ProductIdea Composition** | ✅ [002/spec.md §R3.5](002-domain-enhancements/spec.md#r35-productidea-composition-pattern) | ✅ [002/plan.md](002-domain-enhancements/plan.md) | [§1.1](../.specify/analysis/innovation-bid-domain-gap-analysis.md#11-legacy-productidea---rich-aggregate-structure) | READY |
| **R3.6: FormalResponse Hierarchy** | ✅ [002/spec.md §R3.6](002-domain-enhancements/spec.md#r36-formalresponse-strategy-pattern) | ✅ [002/plan.md](002-domain-enhancements/plan.md) | [§2.1](../.specify/analysis/innovation-bid-domain-gap-analysis.md#21-legacy-formalresponse---polymorphic-hierarchy) | READY |
| **Partner Selection Workflow** | ❌ NOT DOCUMENTED | ❌ NOT DOCUMENTED | [§2.6](../.specify/analysis/innovation-bid-domain-gap-analysis.md#26-partnership-selection-workflow-comparison) | ⚠️ NEEDS SPEC |
| **Rich Domain Behavior Methods** | ✅ [002/spec.md §R3.5](002-domain-enhancements/spec.md#r35-productidea-composition-pattern) | ✅ Covered in composition | [§1.2](../.specify/analysis/innovation-bid-domain-gap-analysis.md#12-legacy-productidea---rich-domain-behavior) | READY |

---

## Phase 2 (Future) - Engagement & Communication

**Estimated Duration**: 3 weeks (Apr-May 2026)
**Status**: 💡 VISION (specifications NOT yet written)
**Constitutional Goal**: 100/100 score (full legacy feature parity)

### Features to Specify

#### 1. Industry Hierarchy (4-Level Taxonomy)
**Effort**: 3-4 days | **Priority**: MEDIUM | **Spec**: ⚠️ NOT YET DOCUMENTED

**Gap Analysis Reference**: [§1.4.3 Missing Concept: Industry Hierarchy](../.specify/analysis/innovation-bid-domain-gap-analysis.md#missing-concept-3-industry-hierarchy-subsector-targeting)

**Problem Solved**: Current flat `Industry` string prevents:
- Fine-grained discovery (innovation targets "Medical Imaging" but Industry="Healthcare" too broad)
- Multiple industry targeting (innovation relevant to 3 subsectors)
- Industry-affiliated actor profiles (Manufacturing company specializes in "Automotive Electronics" subsector)

**What To Implement**:
```
Industry (Top Level - e.g., "Healthcare")
  └── Supersector (e.g., "Medical Devices")
      └── Sector (e.g., "Diagnostic Equipment")
          └── Subsector (Leaf - e.g., "Imaging Systems")

Changes:
- Innovation.Market.TargetIndustries: IList<Subsector> (allows multiple)
- Actor (Manufacturing/Sales/R&D types): AffiliatedIndustries: IList<Subsector>
- Discovery filtering: Search by subsector (precise matching)
- Recommendation engine: Match innovation subsectors with actor affiliations
```

**Migration Complexity**: MEDIUM
- 4 new tables (Industry, Supersector, Sector, Subsector)
- Seed data required (industry taxonomy from standard classification system)
- Migrate existing IndustryId (map flat strings to appropriate Subsector)

**User Stories to Write**:
- US Discovery-1: Filter innovations by subsector (e.g., "Show all in Automotive Electronics")
- US Profile-1: Manufacturing actor declares 3 subsector specializations
- US Match-1: System recommends innovations matching actor's affiliated subsectors

**⚠️ ACTION REQUIRED**:
- Create `specs/005-industry-taxonomy/spec.md`
- Research standard classification (NAICS, ISIC, or custom?)
- Design seed data generation strategy

---

#### 2. IdeaCommunication (Private Actor Messaging)
**Effort**: 3 days | **Priority**: MEDIUM | **Spec**: ⚠️ NOT YET DOCUMENTED

**Gap Analysis Reference**: [§3.1 IdeaCommunication - Actor Messaging System](../.specify/analysis/innovation-bid-domain-gap-analysis.md#31-ideacommunication---actor-messaging-system)

**Problem Solved**: Before formal bid submission, actors may want to:
- Ask idea owner clarifying questions (private inquiry)
- Idea owner can invite specific actors to view innovation
- Soft engagement before formal commitment

**What To Implement**:
```
IdeaCommunication entity:
- IdeaSent: Innovation reference
- CommunicatedTo: Actor reference (recipient)
- Message: string (initial message)
- Viewed: bool (read receipt)
- CommunicatedOn: DateTime
- Feedback: Feedback value object (optional reply)
    - Suggestion: string
    - InformationRequest: string
    - ReceivedOn: DateTime

Endpoints:
- POST /innovations/{id}/invite-actor → Idea owner sends invitation to specific actor
- GET /my-communications → Actor views received messages
- PUT /communications/{id}/respond → Actor replies with feedback
- GET /innovations/{id}/communications → Idea owner views communication thread
```

**Distinct From**:
- **Bids**: Private inquiry vs. formal proposal
- **Comments** (Phase 3): Private 1-to-1 vs. public discussion

**User Stories to Write**:
- US Engage-1: Idea owner invites Manufacturing actor to view innovation
- US Engage-2: Manufacturing actor asks questions before bidding
- US Engage-3: Idea owner responds to inquiry → Actor submits bid

**⚠️ ACTION REQUIRED**:
- Create `specs/006-private-messaging/spec.md`
- Design notification system (email + in-app)
- Consider anti-spam measures (rate limiting, actor verification)

---

#### 3. RegisteredInterest (Bookmarking & Engagement Tracking)
**Effort**: 2 days | **Priority**: MEDIUM | **Spec**: ⚠️ NOT YET DOCUMENTED

**Gap Analysis Reference**: [§3.2 RegisteredInterest - Engagement Tracking](../.specify/analysis/innovation-bid-domain-gap-analysis.md#32-registeredinterest---engagement-tracking)

**Problem Solved**: Actor workflow for innovation discovery:
1. Browse innovations → Find 5 interesting → **How to track them?**
2. Not ready to bid yet → **How to get updates?**
3. Not relevant → **How to stop seeing it?**

**What To Implement**:
```
RegisteredInterest entity:
- InterestedMember: Actor reference
- Idea: Innovation reference
- InterestLevel: enum
    - Interested (bookmark, receive updates)
    - NotInterested (dismissed, stop suggestions)
    - Involved (submitted bid or selected partner - auto-set)

Endpoints:
- POST /innovations/{id}/register-interest → Actor bookmarks innovation
- DELETE /innovations/{id}/register-interest → Actor dismisses
- GET /my-interests → List bookmarked innovations (with status updates)

Email Notifications:
- Weekly digest: "3 innovations you're watching received new bids"
- Status change: "Innovation X partner selection completed"

Analytics for Idea Owner:
- "15 actors watching, 2 bids submitted" (engagement funnel)
```

**User Stories to Write**:
- US Bookmark-1: Manufacturing actor saves 5 innovations for later review
- US Bookmark-2: Actor receives weekly digest of watched innovations
- US Dismiss-1: Actor marks innovation "Not Interested" → Never suggested again
- US Analytics-1: Idea owner sees engagement metrics (watchers vs. bidders)

**⚠️ ACTION REQUIRED**:
- Create `specs/007-interest-tracking/spec.md`
- Design email notification system (digest frequency, opt-out)
- Plan analytics dashboard for idea owners

---

#### 4. Comment System (Public Discussion)
**Effort**: 2 days | **Priority**: LOW | **Spec**: ⚠️ NOT YET DOCUMENTED

**Gap Analysis Reference**: [§3.3 Comment System - Public Discussion](../.specify/analysis/innovation-bid-domain-gap-analysis.md#33-comment-system---public-discussion)

**Problem Solved**: Public Q&A and community engagement on innovations.

**What To Implement**:
```
Comment entity:
- PostedBy: Actor reference
- ForIdea: Innovation reference
- Content: string
- PostedOn: DateTime
- Replies: IList<Comment> (nested comments)

Endpoints:
- POST /innovations/{id}/comments → Add public comment
- GET /innovations/{id}/comments → List all comments (threaded)
- DELETE /comments/{id} → Comment author or idea owner can delete
```

**Distinct From**:
- **IdeaCommunication**: PUBLIC (everyone sees) vs. PRIVATE (1-to-1)

**User Stories to Write**:
- US Comment-1: Actor posts public question on innovation
- US Comment-2: Idea owner replies to question (visible to all)
- US Comment-3: Threaded discussion for complex topics

**⚠️ ACTION REQUIRED**:
- Create `specs/008-public-comments/spec.md`
- Design moderation system (spam prevention, inappropriate content)
- Consider abuse prevention (rate limiting, comment approval for new users)

---

### Phase 2 Specifications Summary

| Feature | Spec Needed | Plan Needed | Gap Analysis Link | Priority | Effort |
|---------|-------------|-------------|-------------------|----------|--------|
| **Industry Hierarchy** | ⚠️ YES | ⚠️ YES | [§1.4.3](../.specify/analysis/innovation-bid-domain-gap-analysis.md#missing-concept-3-industry-hierarchy-subsector-targeting) | MEDIUM | 3-4 days |
| **IdeaCommunication** | ⚠️ YES | ⚠️ YES | [§3.1](../.specify/analysis/innovation-bid-domain-gap-analysis.md#31-ideacommunication---actor-messaging-system) | MEDIUM | 3 days |
| **RegisteredInterest** | ⚠️ YES | ⚠️ YES | [§3.2](../.specify/analysis/innovation-bid-domain-gap-analysis.md#32-registeredinterest---engagement-tracking) | MEDIUM | 2 days |
| **Comment System** | ⚠️ YES | ⚠️ YES | [§3.3](../.specify/analysis/innovation-bid-domain-gap-analysis.md#33-comment-system---public-discussion) | LOW | 2 days |

**Total Effort Estimate**: 10-13 days

---

## Phase 3 (Vision) - Virtual Incubator & Business Planning

**Estimated Duration**: 4-6 weeks (Jun-Jul 2026)
**Status**: 💡 CONCEPT (Phase 2 must complete first)
**Constitutional Goal**: Full platform vision delivery

### Features to Specify (Later)

#### 1. BusinessPlan Aggregate
**Gap Analysis Reference**: [§2.2 Legacy Business Valuation Integration](../.specify/analysis/innovation-bid-domain-gap-analysis.md#22-legacy-business-valuation-integration)

**Key Sub-Features**:
- ManagementTeam (factory methods prevent orphaned entities)
- Financial Projections (auto-generated from accepted bids)
- Competitive Analysis
- Ownership Structure
- NPV Calculation Service

**Spec Status**: Partially documented in [002-domain-enhancements/spec.md §US7](002-domain-enhancements/spec.md#user-story-7---businessplan-aggregate-protection-priority-p3---phase-2)

---

#### 2. Collaboration Workspace
**Features**:
- Document management (upload, share, version control)
- Task assignment and tracking
- Milestone definition and progress
- Team communications

**Spec Status**: ⚠️ NOT YET DOCUMENTED

---

## Gap Analysis Traceability Matrix

All Phase 1-2 items mapped back to gap analysis sections:

| Gap Analysis Section | Feature Name | Spec Location | Phase | Status |
|---------------------|--------------|---------------|-------|--------|
| [§1.1 ProductIdea Aggregate](../.specify/analysis/innovation-bid-domain-gap-analysis.md#11-legacy-productidea---rich-aggregate-structure) | ProductIdea Composition | [002/spec.md §R3.5](002-domain-enhancements/spec.md#r35-productidea-composition-pattern) | Phase 1 | ✅ SPEC READY |
| [§1.2 Rich Domain Behavior](../.specify/analysis/innovation-bid-domain-gap-analysis.md#12-legacy-productidea---rich-domain-behavior) | Domain Validation Methods | [002/spec.md §R3.5](002-domain-enhancements/spec.md#r35-productidea-composition-pattern) | Phase 1 | ✅ SPEC READY |
| [§1.4.1 CollaborationRequirement](../.specify/analysis/innovation-bid-domain-gap-analysis.md#missing-concept-1-collaborationrequirement) | Declare Partner Needs | [002/spec.md §R3.5](002-domain-enhancements/spec.md#r35-productidea-composition-pattern) | Phase 1 | ✅ SPEC READY |
| [§1.4.2 Submission Workflow](../.specify/analysis/innovation-bid-domain-gap-analysis.md#missing-concept-2-submission-workflow-state-machine) | IsReadyForSubmission() | [002/spec.md §R3.5](002-domain-enhancements/spec.md#r35-productidea-composition-pattern) | Phase 1 | ✅ SPEC READY |
| [§1.4.3 Industry Hierarchy](../.specify/analysis/innovation-bid-domain-gap-analysis.md#missing-concept-3-industry-hierarchy-subsector-targeting) | 4-Level Taxonomy | ⚠️ NEEDS SPEC | Phase 2 | ❌ NOT SPEC'D |
| [§1.4.4 Partner Selection State](../.specify/analysis/innovation-bid-domain-gap-analysis.md#missing-concept-4-partner-selection-state-machine) | Selection Workflow | ⚠️ NEEDS SPEC | Phase 1 | ❌ NOT SPEC'D |
| [§2.1 FormalResponse Hierarchy](../.specify/analysis/innovation-bid-domain-gap-analysis.md#21-legacy-formalresponse---polymorphic-hierarchy) | Polymorphic Bids | [002/spec.md §R3.6](002-domain-enhancements/spec.md#r36-formalresponse-strategy-pattern) | Phase 1 | ✅ SPEC READY |
| [§2.2 Business Valuation](../.specify/analysis/innovation-bid-domain-gap-analysis.md#22-legacy-business-valuation-integration) | IProjectValuationService | [002/spec.md §US8](002-domain-enhancements/spec.md#user-story-8---project-valuation-domain-service-priority-p3---phase-2) | Phase 3 | ✅ SPEC READY |
| [§2.4 Financial Projections](../.specify/analysis/innovation-bid-domain-gap-analysis.md#24-critical-gap---financial-projection-structure-loss) | Yearly Projection Dictionaries | [002/spec.md §R3.6](002-domain-enhancements/spec.md#r36-formalresponse-strategy-pattern) | Phase 1 | ✅ SPEC READY |
| [§2.6 Selection Workflow](../.specify/analysis/innovation-bid-domain-gap-analysis.md#26-partnership-selection-workflow-comparison) | SelectPartners Command | ⚠️ NEEDS SPEC | Phase 1 | ❌ NOT SPEC'D |
| [§3.1 IdeaCommunication](../.specify/analysis/innovation-bid-domain-gap-analysis.md#31-ideacommunication---actor-messaging-system) | Private Messaging | ⚠️ NEEDS SPEC | Phase 2 | ❌ NOT SPEC'D |
| [§3.2 RegisteredInterest](../.specify/analysis/innovation-bid-domain-gap-analysis.md#32-registeredinterest---engagement-tracking) | Bookmarking System | ⚠️ NEEDS SPEC | Phase 2 | ❌ NOT SPEC'D |
| [§3.3 Comment System](../.specify/analysis/innovation-bid-domain-gap-analysis.md#33-comment-system---public-discussion) | Public Discussion | ⚠️ NEEDS SPEC | Phase 2 | ❌ NOT SPEC'D |

**Completion Status**: 8/14 items have specifications (57%)

---

## Immediate Action Items

### Before Continuing Phase 0.6 Implementation

✅ **1. Acknowledge Intentional Simplification**
- Current Bid entity is **intentionally simplified** for Phase 0.6 MVP
- Financial projections **deferred by design** (not oversight)
- This roadmap ensures refactoring happens in Phase 1

✅ **2. Update Phase 0.6 Spec**
Add explicit deferral notices:
```markdown
## Phase 0.6 Scope Boundaries

**IN SCOPE**: Bid submission and viewing (generic proposal text)
**OUT OF SCOPE (Phase 1)**:
- Financial projection structures (YearlyManufacturingCosts, etc.)
- Partner selection workflow
- Innovation composition (IdeaSummary, Product, Market)
- Rich domain behavior (IsReadyForSubmission, etc.)

See [ROADMAP.md](../ROADMAP.md#phase-1-domain-richness--rich-behavior) for Phase 1 refactoring plan.
```

### Before Starting Phase 1

❌ **3. Create Missing Specifications** (CRITICAL)

Create these spec documents:

```
specs/004-partner-selection/
  ├── spec.md             ← Partner selection workflow (US, business rules)
  └── plan.md             ← Technical approach (endpoints, validation)

specs/005-industry-taxonomy/
  ├── spec.md             ← 4-level hierarchy (user needs, discovery scenarios)
  ├── plan.md             ← Implementation (seed data, migration)
  └── data/
      └── industry-seed.json  ← Standard classification data

specs/006-private-messaging/
  ├── spec.md             ← IdeaCommunication (user stories, workflows)
  └── plan.md             ← Technical approach (notifications, anti-spam)

specs/007-interest-tracking/
  ├── spec.md             ← RegisteredInterest (bookmarking, analytics)
  └── plan.md             ← Implementation (email digests, dashboard)

specs/008-public-comments/
  ├── spec.md             ← Comment system (moderation, threading)
  └── plan.md             ← Technical approach (abuse prevention)
```

**Estimated Effort**: 1-2 days to write all missing specs

---

## Review Cadence

**Pre-Phase Planning Review** (before each phase):
1. Review this ROADMAP.md for phase scope
2. Verify all gap analysis recommendations captured
3. Ensure specifications exist for all features
4. Update traceability matrix

**Post-Phase Retrospective**:
1. Mark completed items ✅
2. Update constitutional compliance score
3. Adjust future phase estimates based on learnings
4. Identify new gaps discovered during implementation

---

## Success Criteria

**Phase 0.6 Success**:
- ✅ 67/67 tests passing
- ✅ Bid CRUD operations functional
- ✅ Roadmap captures all deferred work

**Phase 1 Success**:
- ✅ ProductIdea composition refactored
- ✅ FormalResponse hierarchy implemented
- ✅ Partner selection workflow complete
- ✅ Constitutional score: 99/100

**Phase 2 Success**:
- ✅ Industry hierarchy supports fine-grained discovery
- ✅ Private messaging enables pre-bid engagement
- ✅ Interest tracking provides analytics
- ✅ Constitutional score: 100/100

**Phase 3 Success**:
- ✅ Virtual incubator operational
- ✅ BusinessPlan aggregate enforces invariants
- ✅ NPV calculation service functional
- ✅ Full legacy feature parity achieved

---

## References

- **Constitution**: [.specify/memory/constitution.md](../.specify/memory/constitution.md)
- **Gap Analysis**: [.specify/analysis/innovation-bid-domain-gap-analysis.md](../.specify/analysis/innovation-bid-domain-gap-analysis.md)
- **Legacy DDD Analysis**: [.specify/analysis/ddd-architectural-analysis.md](../.specify/analysis/ddd-architectural-analysis.md)
- **Legacy Domain Analysis**: [.specify/analysis/legacy-domain-analysis.md](../.specify/analysis/legacy-domain-analysis.md)

---

**Last Updated**: February 17, 2026
**Next Review**: Before Phase 1 kickoff (Est. March 2026)
