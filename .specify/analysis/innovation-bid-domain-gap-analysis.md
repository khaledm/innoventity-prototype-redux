# Innovation & Bid Domain Model Gap Analysis
## Legacy MVC vs. Current Implementation Comparison

**Analysis Date**: February 17, 2026
**Branch**: 003-api-completion (Phase 0.6)
**Analyst**: GitHub Copilot (Senior DDD Architect)
**Analysis Type**: Non-Destructive Domain Model Comparison

---

## Executive Summary

### Critical Finding

The current `Innovation` and `Bid` implementation represents a **significant simplification** of the legacy `ProductIdea` and `FormalResponse` domain model. While this simplification aligns with **Phase 0 MVP goals**, it has **lost critical business logic, domain relationships, and workflow state management** present in the mature legacy system.

### Severity Assessment

| Aspect | Legacy Maturity | Current State | Impact | Phase |
|--------|----------------|---------------|---------|-------|
| **Domain Composition** | ✅ Rich aggregate (IdeaSummary, Product, Market, CollaborationRequirement) | ❌ Flat anemic entity | **CRITICAL** | Phase 1 |
| **Business Logic Methods** | ✅ 8+ domain methods (IsComplete, HasPartners, etc.) | ❌ Zero methods | **CRITICAL** | Phase 1 |
| **FormalResponse Polymorphism** | ✅ 4 specialized types with financial projections | ❌ Single generic Bid entity | **CRITICAL** | Phase 1 |
| **Partnership Workflow** | ✅ Selection process with validation rules | ❌ Simple status flag | **HIGH** | Phase 1 |
| **Communication Model** | ✅ IdeaCommunication with feedback tracking | ❌ Not implemented | **MEDIUM** | Phase 2 |
| **Interest Registration** | ✅ RegisteredInterest with engagement levels | ❌ Not implemented | **MEDIUM** | Phase 2 |
| **Industry Hierarchy** | ✅ 4-level taxonomy (Industry→Subsector) | ❌ Flat string | **MEDIUM** | Phase 1+ |
| **Comments/Discussion** | ✅ Comments collection on ProductIdea | ❌ Not implemented | **LOW** | Phase 2 |

---

## Part 1: Innovation (ProductIdea) Domain Model Analysis

### 1.1 Legacy ProductIdea - Rich Aggregate Structure

#### Aggregate Boundary (DDD Analysis)
```
ProductIdea (Root - EntityOfGuid)
  ├── IdeaSummary (Entity of Int32)
  │     ├── Title
  │     ├── ProductType
  │     ├── ResearchBackground
  │     ├── HasIpr / NoIprExplanation
  │     └── IdeaResearchType (ResearchCategory equivalent)
  │
  ├── Product (Entity of Int32)
  │     ├── ProposedProductDescription
  │     ├── KeyProductAdvantages
  │     ├── CurrentDevelopmentPhase
  │     ├── IdeaDevelopmentProcess
  │     ├── ProductKeyWords
  │     └── ProductAdvantageKeyWords
  │
  ├── Market (Entity of Int32)
  │     ├── TargetIndustry: IList<Subsector> (4-level hierarchy)
  │     ├── TargetMarketDescription
  │     ├── TargetCustomerbase
  │     ├── TargetCustomerType
  │     └── RemoveFromIndustryList(Subsector) method
  │
  ├── CollaborationRequirement (Entity of Int32)
  │     ├── DomainExpertCollaborationRequired
  │     ├── SalesMarketingCollaborationRequired
  │     ├── ManufacturingCollaborationRequired
  │     └── InvestorCollaborationRequired
  │
  ├── FormalIdeaResponses: IList<FormalResponse> (Polymorphic - 4 types)
  ├── IdeaCommunications: IList<IdeaCommunication> (Actor messaging)
  ├── Comments: IList<Comment> (Public discussion)
  ├── BusinessPlan (Separate aggregate reference)
  │
  └── [Rich Domain Methods - See 1.2 below]
```

**Key Design Patterns**:
- **Composite Aggregate Pattern**: ProductIdea is root, child entities (IdeaSummary, Product, Market) have no meaning outside context
- **Template Method Pattern**: Validation methods compose smaller checks (IsIdeaSummaryComplete, IsProductDetailsComplete, etc.)
- **Strategy Pattern**: FormalResponse hierarchy enables polymorphic bid handling
- **Factory Method Pattern**: CollaborationRequirement encapsulates partner type selection logic

**DDD Verdict** (from ddd-architectural-analysis.md):
> "The aggregate enforces **workflow progression gates**. You cannot submit an incomplete idea. You cannot select partners without receiving minimum responses. These are **business rules**, not UI validation."

---

### 1.2 Legacy ProductIdea - Rich Domain Behavior

#### Business Logic Methods (8 Methods)

```csharp
// Workflow State Validation
public virtual bool IsIdeaSummaryComplete()
// Checks: Title (not placeholder), ProductType, IdeaResearchType,
// ResearchBackground, HasRightToUseThisIdea

public virtual bool IsProductDetailsComplete()
// Checks: ProposedProductDescription, KeyProductAdvantages,
// CurrentDevelopmentPhase, IdeaDevelopmentProcess,
// ProductKeyWords, ProductAdvantageKeyWords

public virtual bool IsMarketDetailsSectionComplete()
// Checks: TargetCustomerType, TargetCustomerbase,
// TargetMarketDescription, TargetIndustry.Count > 0

// Partnership Workflow State
public virtual bool HasCollaborationPartnerSelectionCompleted()
// Checks: Manufacturing, SalesMarketing, ResearchDevelopment all have
// Accepted=true responses

public virtual bool HasReceivedEnoughOfFormalResponses()
// Checks: At least one bid from Manufacturing, SalesMarketing,
// ResearchDevelopment (minimum threshold for evaluation)

public virtual bool HasBusinessPlan()
// Checks: BusinessPlan != null (indicator of advanced workflow stage)

// Partner Query Methods
public virtual Member GetSelectedPartner<T>() where T : FormalResponse
// Returns accepted partner for given bid type (Manufacturing, Sales, R&D)

public virtual T FindFormalResponseBy<T>(Member member) where T : FormalResponse
// Finds specific bid by member and bid type

public virtual bool HasIdeaOwnerGeneratedFormalResponseOfType<T>()
    where T: FormalResponse
// Checks if idea owner created placeholder bid (legacy workflow)

// Collection Manipulation
public virtual void RemoveFromFormalResponses(Type responseType, Guid postedBy)
// Encapsulated removal (maintains invariants)
```

**Business Rule Validation** (from SelectCollaborationPartnerCommandHandler):
```csharp
// Rule 1: Must not have already completed partner selection
public class MustNotHaveCollabSelectionProcessCompleted : ValidationRule
// Enforces: HasCollaborationPartnerSelectionCompleted() == false
// Reason: Partner selection is IRREVERSIBLE (constitution principle)

// Rule 2: Must select exactly one partner from each required type
public class MustSelectBidsFromAllCollabTypes : ValidationRule
// Enforces: One unaccepted bid from Manufacturing, SalesMarketing, R&D
// Reason: All three roles required for commercialization
```

---

### 1.3 Current Innovation - Flat Anemic Entity

```csharp
public class Innovation : EntityOfGuid
{
    public Guid IdeaToken { get; set; }
    public Guid OwnerId { get; set; }

    // FLATTENED: IdeaSummary fields
    public string Title { get; set; }
    public string ProductType { get; set; }
    public string ResearchBackground { get; set; }
    public ResearchCategory ResearchCategory { get; set; }
    public string IprStatus { get; set; }

    // FLATTENED: Product fields
    public string ProductDescription { get; set; }
    public string TechnologyDescription { get; set; }
    public string ProductAdvantages { get; set; }
    public string DevelopmentPhase { get; set; }
    public string DevelopmentProcess { get; set; }

    // FLATTENED: Market fields
    public string TargetMarket { get; set; }
    public string TargetCustomerBase { get; set; }
    public string TargetBeneficiaries { get; set; }
    public string TargetCustomerType { get; set; }
    public decimal? RelevantMarketSize { get; set; }
    public decimal? PotentialMarketSize { get; set; }

    // SIMPLIFIED: Single industry string instead of hierarchy
    public string IndustryId { get; set; }

    // SIMPLIFIED: Single status enum (Draft/Published/Collaboration/Closed)
    public InnovationStatus Status { get; set; }

    // MISSING: CollaborationRequirement (which partners needed)
    // MISSING: FormalIdeaResponses collection
    // MISSING: IdeaCommunications collection
    // MISSING: Comments collection
    // MISSING: BusinessPlan reference

    // ❌ ZERO BUSINESS LOGIC METHODS
}
```

**Phase 0 MVP Rationale** (from spec.md §6):
- Read-only innovation viewing for Phase 0
- Innovation submission workflow deferred to Phase 1
- Simplification acceptable for **testing authentication/authorization patterns**
- Focus on API infrastructure, not domain richness

**Constitutional Validation** (Principle 3: Simplicity Over Cleverness):
✅ **Phase 0**: Flat model acceptable for **reading test data**
⚠️ **Phase 1**: MUST refactor to composed model when implementing **innovation submission workflow**

---

### 1.4 Critical Missing Domain Concepts

#### Missing Concept 1: CollaborationRequirement

**Legacy Purpose**: Declares which partner types are needed for this specific innovation.

```csharp
public class CollaborationRequirement : EntityOfInt32
{
    public virtual bool DomainExpertCollaborationRequired { get; set; }      // R&D
    public virtual bool SalesMarketingCollaborationRequired { get; set; }    // Sales
    public virtual bool ManufacturingCollaborationRequired { get; set; }
    public virtual bool InvestorCollaborationRequired { get; set; }
}
```

**Business Impact**:
- ✅ **Legacy**: Idea owner explicitly declares partner needs → bidders know if relevant
- ✅ **Legacy**: System validates partner selection against declared requirements
- ✅ **Legacy**: Business logic: `HasReceivedEnoughOfFormalResponses()` checks against requirements
- ❌ **Current**: ALL innovations implicitly require ALL partner types (no flexibility)

**User Story Impact**:
- **US Bid-1** (Partner bids on innovation): How does bidder know if their type is needed?
- **US Partner-1** (Select collaboration partners): How does system validate selection completeness?

**Constitutional Violation**:
- **Principle 1 (User Experience First)**: Users cannot declare nuanced collaboration needs
- **Principle 2 (Quality is Non-Negotiable)**: Business rule validation incomplete

**Recommendation**:
- **Phase 0**: SKIP (all innovations assumed to need all partner types)
- **Phase 1**: IMPLEMENT as part of innovation submission workflow
- **Implementation**: Add `CollaborationRequirement` entity as owned entity (EF Core) or separate table with ProductIdea FK

---

#### Missing Concept 2: Submission Workflow State Machine

**Legacy Implementation**:
```csharp
// ProductIdea has CreatedOn + SubmittedOn dates
public virtual DateTime CreatedOn { get; set; }
public virtual DateTime? SubmittedOn { get; set; }  // Nullable = Draft state

// Business logic determines readiness
public bool IsReadyForSubmission()
{
    return IsIdeaSummaryComplete() &&
           IsProductDetailsComplete() &&
           IsMarketDetailsSectionComplete();
}
```

**Current Implementation**:
```csharp
public InnovationStatus Status { get; set; }  // Draft/Published
```

**Missing Capabilities**:
1. **Validation Gates**: No enforcement of completeness before publication
2. **Audit Trail**: No separate CreatedOn vs. SubmittedOn timestamps
3. **Progressive Disclosure**: No ability to save incomplete drafts
4. **Business Rules**: No domain method to validate submission readiness

**Business Impact**:
- ✅ **Legacy**: Multi-step form can save incomplete state → resume later
- ✅ **Legacy**: Submission button disabled until `IsReadyForSubmission()` returns true
- ✅ **Legacy**: Audit trail shows idea creation date vs. publication date
- ❌ **Current**: Status field provides no validation logic

**Recommendation**:
- **Phase 0**: SKIP (test data is always complete/published)
- **Phase 1**: Add domain methods: `IsReadyForSubmission()`, `Submit()` (state transition with validation)
- **Technical**: Add `SubmittedOn` timestamp separate from `CreatedOn`

---

#### Missing Concept 3: Industry Hierarchy (Subsector Targeting)

**Legacy Implementation**:
```
Industry (Top Level - e.g., "Healthcare")
  └── Supersector (e.g., "Medical Devices")
      └── Sector (e.g., "Diagnostic Equipment")
          └── Subsector (Leaf - e.g., "Imaging Systems")

Market entity stores: IList<Subsector> TargetIndustry
```

**Current Implementation**:
```csharp
public string IndustryId { get; set; }  // Flat reference to single Industry
```

**Business Impact**:
- ✅ **Legacy**: Fine-grained matching (innovation targets "Imaging Systems" → bidders filter by subsector)
- ✅ **Legacy**: Multiple industry targeting (innovation can target 3 different subsectors)
- ✅ **Legacy**: Industry-affiliated actors (Manufacturer stores IList of Subsector affiliations)
- ❌ **Current**: Coarse matching only (innovation = "Healthcare" → too broad for discovery)

**DDD Analysis Recommendation** (from ddd-architectural-analysis.md):
> "⚠️ DEFERRED TO PHASE 1+
> Current flat model sufficient for Phase 0 (MVP)
> Implement hierarchy when:
> - Advanced filtering/matching required
> - Partner discovery by detailed classification"

**Recommendation**:
- **Phase 0**: ACCEPTABLE (proof-of-concept only needs coarse industry)
- **Phase 1**: IMPLEMENT hierarchy when implementing:
  - Discovery filtering (search by subsector)
  - Partner matching algorithms
  - Industry-affiliated actor profiles

---

#### Missing Concept 4: Partner Selection State Machine

**Legacy State Transitions**:
```
1. Innovation Created (Draft)
   ↓
2. Innovation Submitted (Published - SubmittedOn != null)
   ↓
3. Bids Received (FormalIdeaResponses.Count > 0)
   ↓
4. Minimum Bids Met (HasReceivedEnoughOfFormalResponses() == true)
   ↓ (Idea owner can now select partners)
5. Partners Selected (HasCollaborationPartnerSelectionCompleted() == true)
   ↓ (IRREVERSIBLE - see validation rule)
6. Business Plan Created (BusinessPlan != null)
   ↓
7. ... (Virtual Incubator workflow)
```

**Current State Model**:
```csharp
public enum InnovationStatus
{
    Draft = 1,
    Published = 2,
    InCollaboration = 3,  // ⚠️ No distinction between "accepting bids" and "partners selected"
    Closed = 4
}
```

**Missing State Distinctions**:
- **"Published - Accepting Bids"**: Innovation visible, no partners selected yet
- **"Evaluating Bids"**: Minimum bids received, owner reviewing
- **"Partners Selected"**: Collaboration team formed (IRREVERSIBLE)
- **"Incubator Active"**: Business plan created, team working

**Business Impact**:
- ✅ **Legacy**: Clear workflow gates → users know what stage they're in
- ✅ **Legacy**: Validation rules prevent invalid transitions (e.g., select partners before minimum bids)
- ❌ **Current**: `InCollaboration` status too ambiguous

**Recommendation**:
- **Phase 0.6**: ACCEPTABLE (current tasks only need Draft/Published distinction for bid eligibility)
- **Phase 1**: REFINE status enum or add separate boolean flags:
  ```csharp
  public bool PartnersSelected { get; set; }
  public DateTimeOffset? PartnerSelectionCompletedOn { get; set; }
  ```

---

## Part 2: Bid (FormalResponse) Domain Model Analysis

### 2.1 Legacy FormalResponse - Polymorphic Hierarchy

#### Inheritance Structure
```
FormalResponse (Abstract Base - EntityOfGuid)
  ├── PostedBy: Member (FK)
  ├── ForIdea: ProductIdea (FK)
  ├── Location: string (geographic region)
  ├── ParticipationType: string (role description)
  ├── ParticipationProposal: string (detailed proposal)
  ├── PostedOn: DateTime
  ├── Accepted: bool (selected by idea owner)
  ├── AcceptedOn: DateTime? (selection timestamp)
  └── CompareTo(FormalResponse) method (sorting)

  ┌─────────────────────────┬─────────────────────────┬─────────────────────────┬─────────────────────────┐
  │                         │                         │                         │                         │
ManufacturingResponse   SalesMarketingResponse   ResearchDevelopmentResponse   InvestorResponse
  │                         │                         │                         │
  ├── YearlyManufacturing   ├── YearlySales:         ├── ProductDevelopment     └── Response: string
  │   Costs: Dictionary     │   Dictionary           │   Duration: int            (simple text response)
  │   <int, MfgInfo>        │   <int, SalesInfo>     └── YearlyDevelopment
  │                         │                             Costs: Dictionary
  └── ManufacturingInfo:    └── SalesMarketingInfo:       <int, DevInfo>
      - ProductionVolume        - UnitsSold
      - UnitCost                - UnitPrice            └── ProductDevelopmentInfo:
      - DistributionExpense     - SalesMarketingExpense     - InfrastructureCost
      - Rationales (3)          - Rationales (3)             - PeopleCost
                                                             - Rationales (2)
```

#### Financial Projection Data Structures

**ManufacturingResponse - Production & Distribution Costs**:
```csharp
public class ManufacturingInformation
{
    public int ProductionVolume { get; set; }
    public string ProductionVolumeRationale { get; set; }  // Audit trail

    public decimal UnitCost { get; set; }
    public string UnitCostRationale { get; set; }

    public decimal AverageGlobalDistributionExpense { get; set; }
    public string DistributionExpenseRationale { get; set; }
}

// Dictionary Key = Year (1, 2, 3, 4, 5)
// YearlyManufacturingCosts[1] = Year 1 projections
```

**SalesMarketingResponse - Revenue & Sales Costs**:
```csharp
public class SalesMarketingInformation
{
    public int UnitsSold { get; set; }
    public string UnitsSoldRationale { get; set; }

    public decimal UnitPrice { get; set; }
    public string UnitPriceRationale { get; set; }

    public decimal SalesMarketingExpense { get; set; }
    public string ExpenseRationale { get; set; }
}

// YearlySales[3] = Year 3 revenue + expense projections
```

**ResearchDevelopmentResponse - Development Timeline & Costs**:
```csharp
public class ProductDevelopmentInformation
{
    public decimal InfrastructureCost { get; set; }
    public string InfrastructureCostRationale { get; set; }

    public decimal PeopleCost { get; set; }
    public string PeopleCostRationale { get; set; }
}

// ProductDevelopmentDuration = Total years (e.g., 2)
// YearlyDevelopmentCosts[1], [2] = Year-by-year breakdown
```

**InvestorResponse - Unstructured Feedback**:
```csharp
public class InvestorResponse : FormalResponse
{
    public virtual string Response { get; set; }  // Free-form text
}
```

---

### 2.2 Legacy Business Valuation Integration

**IProjectValuationService** (from IProjectValuationService.cs):
```csharp
public interface IProjectValuationService
{
    ProjectValuationResult Calculate(
        ManufacturingResponse manufacturingBid,
        SalesMarketingResponse salesBid,
        ResearchDevelopmentResponse rdBid);
}
```

**Business Logic** (from ddd-architectural-analysis.md):
> "The calculation needs data from 3 different FormalResponse instances. Putting this on ProductIdea would violate aggregate boundaries (would need to navigate to FormalResponses, load their yearly data, etc.)."

**Return Value**:
```csharp
public class ProjectValuationResult  // Value Object (no identity)
{
    public IList<YearlyValuation> PerYearValuations { get; private set; }
    public decimal NetPresentValue { get; private set; }
    public string PaybackPeriod { get; private set; }
}
```

**Business Impact**:
- ✅ **Legacy**: Valuation service combines 3 accepted bids → NPV calculation
- ✅ **Legacy**: Financial projections guide partner selection (compare competing bids)
- ✅ **Legacy**: Business plan includes auto-generated financial model
- ✅ **Legacy**: "Rationale" fields create audit trail for investors

**DDD Analysis Verdict** (Domain Service Pattern):
> "✅ **KEEP** - Textbook domain service use case."

---

### 2.3 Current Bid - Generic Flat Entity

```csharp
public class Bid : EntityOfGuid
{
    public Guid InnovationId { get; set; }
    public Innovation? Innovation { get; set; }  // Navigation

    public Guid ActorId { get; set; }
    public Actor? Actor { get; set; }  // Navigation

    // Generic bid fields (no actor-type specialization)
    public string Location { get; set; }                  // ✅ Same as legacy
    public string ParticipationType { get; set; }         // ✅ Same as legacy
    public string ParticipationProposal { get; set; }     // ✅ Same as legacy

    public BidStatus Status { get; set; }  // Pending/Accepted/Rejected

    public DateTimeOffset SubmittedAt { get; set; }       // ✅ Same as legacy PostedOn
    public DateTimeOffset? UpdatedAt { get; set; }        // ✅ Enhancement
    public DateTimeOffset? AcceptedAt { get; set; }       // ✅ Same as legacy AcceptedOn

    // ❌ MISSING: Yearly financial projections (Manufacturing/Sales/R&D specific)
    // ❌ MISSING: Polymorphic type discrimination
    // ❌ MISSING: CompareTo implementation for sorting
}

public enum BidStatus  // ✅ Enhancement over legacy boolean "Accepted"
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3  // NEW: Explicitly track rejection
}
```

**Phase 0.6 MVP Rationale** (from tasks.md T010):
- **R4.1 Eligibility Rules**: Who can bid (actor types, ownership check, duplicate prevention)
- **R4.2 Content Rules**: Basic proposal requirements (location, type, 200+ char proposal)
- **Focus**: Bid submission and viewing, NOT financial modeling
- **Simplification Rationale**: Financial projections deferred to Phase 1 (virtual incubator)

---

### 2.4 Critical Gap - Financial Projection Structure Loss

#### What Was Lost

**Legacy Capability** (from SelectCollaborationPartnerCommandHandler):
```csharp
// Idea owner views 3 Manufacturing bids side-by-side:
// Bid A: Year 1 = 10,000 units @ $5.50/unit, $1.2M distribution
// Bid B: Year 1 = 15,000 units @ $4.75/unit, $800K distribution
// Bid C: Year 1 = 8,000 units @ $6.00/unit, $1.5M distribution

// System can calculate which bid yields best NPV
var valuation = _valuationService.Calculate(
    mfgBid: selectedManufacturingResponse,
    salesBid: selectedSalesMarketingResponse,
    rdBid: selectedResearchDevelopmentResponse
);

// Result: "Bid B has NPV of $4.2M over 5 years with 2.3 year payback"
```

**Current Capability**:
```csharp
// Idea owner views 3 bids:
// Bid A: "We can manufacture 10,000-15,000 units annually..."
// Bid B: "Our production capacity is 15,000 units with $4.75 cost..."
// Bid C: "We produce 8,000 units at $6 per unit..."

// ❌ NO STRUCTURED DATA → Cannot calculate NPV
// ❌ NO STANDARDIZED FORMAT → Manual comparison required
// ❌ NO RATIONALE FIELDS → No audit trail for investors
```

**Business Impact Severity**: **CRITICAL**

**User Stories Impacted**:
- **US Partner-1** (Select collaboration partners): How does idea owner compare bids objectively?
- **US BusinessPlan-1** (Create business plan): How are financial projections generated?
- **US Valuation-1** (Calculate project NPV): Impossible without structured data

**Constitutional Violations**:
- **Principle 1 (User Experience First)**: Manual comparison = poor UX
- **Principle 2 (Quality is Non-Negotiable)**: Unstructured data prevents validation
- **Principle 4 (Specification Drives Implementation)**: Legacy spec shows clear intent

---

### 2.5 Polymorphism Implementation Comparison

#### Legacy - Table-Per-Hierarchy (TPH) Strategy

**Database Schema**:
```sql
CREATE TABLE FormalResponses
(
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Discriminator VARCHAR(50) NOT NULL,  -- 'ManufacturingResponse', etc.
    PostedById UNIQUEIDENTIFIER NOT NULL,
    ForIdeaId UNIQUEIDENTIFIER NOT NULL,
    Location NVARCHAR(200),
    ParticipationType NVARCHAR(200),
    ParticipationProposal NVARCHAR(MAX),
    PostedOn DATETIME NOT NULL,
    Accepted BIT NOT NULL DEFAULT 0,
    AcceptedOn DATETIME NULL,

    -- ManufacturingResponse specific (nullable for other types)
    YearlyManufacturingCosts_JSON NVARCHAR(MAX),  -- Serialized dictionary

    -- SalesMarketingResponse specific
    YearlySales_JSON NVARCHAR(MAX),

    -- ResearchDevelopmentResponse specific
    ProductDevelopmentDuration INT,
    YearlyDevelopmentCosts_JSON NVARCHAR(MAX),

    -- InvestorResponse specific
    Response NVARCHAR(MAX)
);
```

**ORM Mapping** (NHibernate legacy):
```xml
<class name="FormalResponse" table="FormalResponses" abstract="true">
    <id name="Id" column="Id" type="Guid" />
    <discriminator column="Discriminator" type="String" />

    <subclass name="ManufacturingResponse" discriminator-value="ManufacturingResponse">
        <property name="YearlyManufacturingCosts" type="serializable" />
    </subclass>

    <!-- Other subclasses... -->
</class>
```

**Domain Usage**:
```csharp
// Type-safe polymorphic queries
var mfgBids = idea.FormalIdeaResponses
    .OfType<ManufacturingResponse>()
    .Where(b => !b.Accepted)
    .ToList();

// Access subclass-specific properties
var year1Cost = mfgBids[0].YearlyManufacturingCosts[1].UnitCost;
```

---

#### Current - Single Generic Table

**Database Schema**:
```sql
CREATE TABLE Bids
(
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    InnovationId UNIQUEIDENTIFIER NOT NULL,
    ActorId UNIQUEIDENTIFIER NOT NULL,
    Location NVARCHAR(200) NOT NULL,
    ParticipationType NVARCHAR(100) NOT NULL,
    ParticipationProposal NVARCHAR(5000) NOT NULL,
    Status INT NOT NULL DEFAULT 1,  -- 1=Pending, 2=Accepted, 3=Rejected
    SubmittedAt DATETIMEOFFSET NOT NULL,
    UpdatedAt DATETIMEOFFSET NULL,
    AcceptedAt DATETIMEOFFSET NULL,

    CONSTRAINT IX_Bid_ActorId_InnovationId UNIQUE (ActorId, InnovationId)
);
```

**Domain Usage**:
```csharp
// All bids treated identically
var bids = await context.Bids
    .Where(b => b.InnovationId == innovationId && b.Status == BidStatus.Pending)
    .ToListAsync();

// ❌ NO TYPE DISCRIMINATION
// ❌ NO FINANCIAL DATA ACCESS
```

**Consequences**:
1. **Lost Type Safety**: Cannot distinguish Manufacturing vs. Sales bids at compile time
2. **Lost Financial Structure**: Free-text proposal instead of structured projections
3. **Lost Valuation Capability**: NPV calculation impossible

---

### 2.6 Partnership Selection Workflow Comparison

#### Legacy - Multi-Step Selection Process

**Step 1**: Check Minimum Bids Received
```csharp
public virtual bool HasReceivedEnoughOfFormalResponses()
{
    return FormalIdeaResponses.OfType<ManufacturingResponse>().Any() &&
           FormalIdeaResponses.OfType<SalesMarketingResponse>().Any() &&
           FormalIdeaResponses.OfType<ResearchDevelopmentResponse>().Any();
}
```

**Step 2**: Idea Owner Selects Partners (Command)
```csharp
var command = new SaveCollaborationPartnerSelection
{
    Id = innovationId,
    SelectedManufacturingBidId = guidA,
    SelectedSalesMarketingBidId = guidB,
    SelectedResearchDevelopmentBidId = guidC
};
```

**Step 3**: Validation Rules Execute
```csharp
// Rule 1: Selection not already completed
MustNotHaveCollabSelectionProcessCompleted.IsValid(command)

// Rule 2: Must select exactly one from each type
MustSelectBidsFromAllCollabTypes.IsValid(command)

// Rule 3: Selected bids must not already be accepted (implied)
```

**Step 4**: Domain State Transition
```csharp
idea.FormalIdeaResponses
    .Where(bid => bid.Id == selectedMfgId ||
                  bid.Id == selectedSalesId ||
                  bid.Id == selectedRdId)
    .Each(bid =>
    {
        bid.Accepted = true;
        bid.AcceptedOn = DateTime.UtcNow;
    });

_repository.Save(idea);  // Single transaction
```

**Step 5**: Check Completion
```csharp
if (idea.HasCollaborationPartnerSelectionCompleted())
{
    // Trigger: CreateBusinessPlan workflow
    // Trigger: Email notifications to selected partners
    // Trigger: Update innovation status to "InCollaboration"
}
```

---

#### Current - No Selection Workflow (Phase 0.6)

**Available Operations**:
- ✅ T010: POST /innovations/{id}/bids (submit bid)
- ✅ T011: GET /innovations/{id}/bids (view bids for innovation)
- ✅ T012: PUT /bids/{id} (update bid content before acceptance)

**Missing Operations**:
- ❌ **PUT /bids/{id}/accept**: Idea owner accepts specific bid
- ❌ **POST /innovations/{id}/select-partners**: Batch selection with validation
- ❌ **GET /innovations/{id}/selected-partners**: View collaboration team

**Missing Validation**:
- ❌ Partner selection completeness check
- ❌ Duplicate partner type prevention (cannot accept 2 Manufacturing bids)
- ❌ Irreversibility enforcement (cannot change selection once made)

**Phase 0.6 Rationale**:
- Focus: Bid CRUD operations (submit, view, update)
- Deferred: Selection workflow (requires business plan integration)
- Journey 2 scope: "Bid submission" NOT "Partner formation"

---

## Part 3: Missing Domain Concepts

### 3.1 IdeaCommunication - Actor Messaging System

#### Legacy Implementation
```csharp
public class IdeaCommunication : EntityOfInt32
{
    public virtual ProductIdea IdeaSent { get; set; }        // Which innovation
    public virtual Member CommunicatedTo { get; set; }       // Recipient actor
    public virtual bool Viewed { get; set; }                 // Read receipt
    public virtual DateTime CommunicatedOn { get; set; }     // Timestamp
    public virtual Feedback ReceivedFeedback { get; set; }   // Optional reply
}

public class Feedback
{
    public String Suggestion { get; set; }
    public String InformationRequest { get; set; }
    public DateTime ReceivedOn { get; set; }
}
```

**Business Purpose**:
- Idea owner can **privately message** specific actors (e.g., "Check out my innovation")
- Actor receives **notification** + can view innovation + can respond with feedback
- **Distinct from public comments**: Private 1-to-1 communication
- **Distinct from bids**: Soft inquiry before formal proposal

**Use Cases**:
1. Idea owner sees Manufacturing company profile → Send invitation to view innovation
2. Manufacturing company receives communication → Views innovation → Replies with questions
3. Idea owner responds to questions → Manufacturing company submits formal bid

**Current State**: ❌ **NOT IMPLEMENTED**

**Impact Severity**: **MEDIUM** (Phase 2 feature - enhances discovery but not critical for MVP)

**Recommendation**:
- **Phase 0-1**: SKIP (public comments can serve similar purpose if added)
- **Phase 2**: IMPLEMENT as **Actor-to-Actor messaging feature** for private inquiries

---

### 3.2 RegisteredInterest - Engagement Tracking

#### Legacy Implementation
```csharp
public enum InterestLevel
{
    Interested,      // Wants updates, receives regular emails
    NotInterested,   // Opted out of this innovation
    Involved         // Submitted bid or selected as partner
}

public class RegisteredInterest : EntityOfGuid
{
    public virtual Member InterestedMember { get; set; }
    public virtual ProductIdea Idea { get; set; }
    public virtual InterestLevel InterestLevel { get; set; }
}
```

**Business Purpose**:
- Actor **bookmarks** innovation (Interested) → Receives email updates when changes occur
- Actor **dismisses** innovation (NotInterested) → Stops receiving suggestions about it
- System **tracks engagement** (Involved) → Analytics for recommendation engine

**Use Cases**:
1. Manufacturing company browses innovations → Marks 5 as "Interested" → Receives weekly digest
2. Sales company views innovation → Not relevant → Marks "Not Interested" → Never sees it again
3. R&D organization submits bid → System auto-sets InterestLevel = Involved
4. **Analytics**: "This innovation has 15 interested actors but only 2 bids" → Idea owner sees engagement metrics

**Current State**: ❌ **NOT IMPLEMENTED**

**Impact Severity**: **MEDIUM** (Phase 2 feature - improves UX but not critical for basic workflow)

**Recommendation**:
- **Phase 0-1**: SKIP (not needed for basic bid submission)
- **Phase 2**: IMPLEMENT as **"Watch" or "Bookmark" feature** + **Email notification system**

---

### 3.3 Comment System - Public Discussion

#### Legacy Implementation
```csharp
public class ProductIdea : EntityOfGuid
{
    public virtual IList<Comment> Comments { get; protected set; }
}

// Comment entity (inferred)
public class Comment : EntityOfInt32
{
    public virtual Member PostedBy { get; set; }
    public virtual string Content { get; set; }
    public virtual DateTime PostedOn { get; set; }
}
```

**Business Purpose**:
- Public discussion on innovation details
- Q&A between idea owner and potential bidders
- Community engagement (visible to all)

**Distinction from IdeaCommunication**:
- **Comments**: PUBLIC (everyone can see)
- **IdeaCommunication**: PRIVATE (1-to-1 messaging)

**Current State**: ❌ **NOT IMPLEMENTED**

**Impact Severity**: **LOW** (Phase 2+ feature - nice-to-have for community building)

**Recommendation**:
- **Phase 0-1**: SKIP (not critical for MVP workflow)
- **Phase 2+**: Consider implementing as **public discussion forum** if user feedback indicates need

---

## Part 4: Recommendations & Migration Path

### 4.1 Immediate Actions (Phase 0 Completion)

#### ✅ ACCEPTABLE TO SHIP AS-IS

**Current Phase 0.6 Scope** (T010-T012):
- ✅ Bid submission with eligibility rules (R4.1)
- ✅ Bid content validation (R4.2)
- ✅ View bids for innovation (ownership check)
- ✅ Update bid before acceptance

**Rationale**:
- **Constitution Principle 3** (Simplicity Over Cleverness): Flat model acceptable for MVP
- **Spec-Kit Principle**: Phase 0 is **proof-of-concept** for auth/API patterns, not full domain richness
- **Legacy Analysis Document** (ddd-architectural-analysis.md): "Current approach acceptable for Phase 0 (Minimal APIs with Problem Details)"

**What We Successfully Validated in Phase 0**:
- ✅ Entity identity pattern (EntityOfGuid)
- ✅ EF Core aggregate configuration
- ✅ JWT authentication in Minimal APIs
- ✅ Integration testing with WebApplicationFactory
- ✅ Validation pattern (manual, inline)
- ✅ Navigation properties (Actor ↔ Bid ↔ Innovation)

---

### 4.2 Critical Refactorings (Phase 1 - Before Innovation Submission UI)

#### Priority 1: ProductIdea Composition Pattern (2-3 days)

**From**: Flat Innovation entity (63 properties)
**To**: Composed aggregate

```csharp
public class Innovation : EntityOfGuid
{
    // Aggregate root properties
    public Guid IdeaToken { get; set; }
    public Guid OwnerId { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? SubmittedOn { get; set; }
    public InnovationStatus Status { get; set; }

    // Composed child entities (owned entities in EF Core)
    public IdeaSummary Summary { get; set; }
    public ProductDetails Product { get; set; }
    public MarketDetails Market { get; set; }
    public CollaborationRequirement Requirements { get; set; }

    // Collections
    public ICollection<Bid> Bids { get; protected set; }

    // Business logic methods
    public bool IsReadyForSubmission() =>
        Summary.IsComplete() &&
        Product.IsComplete() &&
        Market.IsComplete();

    public void Submit()
    {
        if (!IsReadyForSubmission())
            throw new InvalidOperationException("Innovation incomplete");

        SubmittedOn = DateTime.UtcNow;
        Status = InnovationStatus.Published;
    }

    public bool HasMinimumBidsForSelection() =>
        Bids.Count(b => b.Status == BidStatus.Pending) >= 3;  // Simplified rule
}
```

**EF Core Mapping**:
```csharp
modelBuilder.Entity<Innovation>()
    .OwnsOne(i => i.Summary, summary =>
    {
        summary.Property(s => s.Title).HasMaxLength(500).IsRequired();
        summary.Property(s => s.ProductType).HasMaxLength(200);
        // ... other IdeaSummary properties
    })
    .OwnsOne(i => i.Product, product => { /* ... */ })
    .OwnsOne(i => i.Market, market => { /* ... */ })
    .OwnsOne(i => i.Requirements, req => { /* ... */ });
```

**Migration Impact**:
- **Breaking Change**: YES (database schema change)
- **Test Impact**: HIGH (all Innovation seed data must use new structure)
- **API Impact**: MEDIUM (request/response DTOs must map to composed structure)

**Benefits**:
- ✅ Aligns with legacy domain model (legacy-domain-analysis.md Priority 2)
- ✅ Supports multi-step submission workflow (progressive disclosure)
- ✅ Enables domain validation logic
- ✅ Reduces Innovation entity size (~60 props → ~10 props + 4 owned entities)

---

#### Priority 2: FormalResponse Polymorphic Hierarchy (3-4 days)

**From**: Single Bid entity with free-text proposal
**To**: Polymorphic hierarchy with structured financial data

**Base Class**:
```csharp
public abstract class FormalResponse : EntityOfGuid
{
    public Guid InnovationId { get; set; }
    public Innovation Innovation { get; set; }

    public Guid PostedById { get; set; }
    public Actor PostedBy { get; set; }

    public string Location { get; set; }
    public string ParticipationType { get; set; }
    public string ParticipationProposal { get; set; }

    public DateTime PostedOn { get; set; }
    public bool Accepted { get; set; }
    public DateTime? AcceptedOn { get; set; }
}
```

**Subclasses**:
```csharp
public class ManufacturingResponse : FormalResponse
{
    // Stored as JSON column (Postgres JSONB or SQL Server JSON)
    public Dictionary<int, ManufacturingInfo> YearlyProjections { get; set; }
}

public class ManufacturingInfo
{
    public int ProductionVolume { get; set; }
    public string ProductionVolumeRationale { get; set; }
    public decimal UnitCost { get; set; }
    public string UnitCostRationale { get; set; }
    // ...
}

// Similar for SalesMarketingResponse, ResearchDevelopmentResponse, InvestorResponse
```

**EF Core TPH Mapping**:
```csharp
modelBuilder.Entity<FormalResponse>()
    .HasDiscriminator<string>("ResponseType")
    .HasValue<ManufacturingResponse>("Manufacturing")
    .HasValue<SalesMarketingResponse>("Sales")
    .HasValue<ResearchDevelopmentResponse>("RD")
    .HasValue<InvestorResponse>("Investor");

modelBuilder.Entity<ManufacturingResponse>()
    .Property(m => m.YearlyProjections)
    .HasColumnType("jsonb");  // Postgres
```

**Migration Strategy**:
```sql
-- Step 1: Add Discriminator column to Bids table
ALTER TABLE Bids ADD ResponseType VARCHAR(50) DEFAULT 'Generic';

-- Step 2: Add JSON columns for subclass-specific data
ALTER TABLE Bids ADD YearlyManufacturingCosts JSONB;
ALTER TABLE Bids ADD YearlySales JSONB;
ALTER TABLE Bids ADD YearlyDevelopmentCosts JSONB;
ALTER TABLE Bids ADD InvestorResponseText NVARCHAR(MAX);

-- Step 3: Migrate existing Bid data to "Generic" type
-- (Phase 0 bids have no structured data, safe to leave as-is)

-- Step 4: Rename table Bids → FormalResponses (breaking change)
```

**API Impact**:
```csharp
// NEW endpoint structure
POST /innovations/{id}/bids/manufacturing
{
    "location": "Munich, Germany",
    "participationType": "Full Production Partner",
    "proposal": "We can manufacture...",
    "yearlyProjections": {
        "1": {
            "productionVolume": 10000,
            "productionVolumeRationale": "Based on current capacity...",
            "unitCost": 5.50,
            "unitCostRationale": "Includes materials, labor..."
        }
        // Years 2-5...
    }
}

// Type-safe response
GET /innovations/{id}/bids/manufacturing
→ Returns ManufacturingResponse[] with structured data
```

**Benefits**:
- ✅ Enables NPV calculation (IProjectValuationService)
- ✅ Standardizes financial projections (essential for business plan)
- ✅ Type-safe bid comparison for idea owners
- ✅ Aligns with legacy domain model (DDD analysis recommendation)

---

#### Priority 3: Partner Selection Workflow (2 days)

**New Endpoints**:
```csharp
// Accept single bid (simple approach)
PUT /bids/{bidId}/accept
Authorization: Bearer {idea-owner-token}

// Batch partner selection (legacy approach)
POST /innovations/{id}/select-partners
{
    "manufacturingBidId": "guid-A",
    "salesBidId": "guid-B",
    "rdBidId": "guid-C"
}
```

**Business Rules** (from legacy CommandHandler):
```csharp
public class SelectPartnersValidator
{
    public ValidationResult Validate(SelectPartnersCommand cmd)
    {
        var innovation = _repo.GetById(cmd.InnovationId);

        // Rule 1: Selection not already completed
        if (innovation.HasCollaborationPartnerSelectionCompleted())
            return Error("Partner selection already completed (irreversible)");

        // Rule 2: Must select one from each required type
        var requirements = innovation.Requirements;
        if (requirements.ManufacturingRequired && cmd.ManufacturingBidId == null)
            return Error("Manufacturing partner required");

        // Rule 3: Selected bid must exist and not already accepted
        var mfgBid = innovation.Bids.OfType<ManufacturingResponse>()
            .FirstOrDefault(b => b.Id == cmd.ManufacturingBidId);
        if (mfgBid == null || mfgBid.Accepted)
            return Error("Invalid manufacturing bid selection");

        // ... similar for Sales and R&D

        return Success();
    }
}
```

**State Transition**:
```csharp
public void SelectPartners(
    Guid mfgBidId,
    Guid salesBidId,
    Guid rdBidId)
{
    // Validation already done by validator

    var selectedBids = Bids.Where(b =>
        b.Id == mfgBidId ||
        b.Id == salesBidId ||
        b.Id == rdBidId);

    foreach (var bid in selectedBids)
    {
        bid.Accepted = true;
        bid.AcceptedOn = DateTime.UtcNow;
    }

    Status = InnovationStatus.InCollaboration;
    PartnerSelectionCompletedOn = DateTime.UtcNow;

    // Domain event: PartnersSelectedEvent
    // → Trigger email notifications
    // → Trigger business plan creation prompt
}
```

**Benefits**:
- ✅ Completes Journey 2 workflow (Discovery → Bid → **SELECT** → Collaborate)
- ✅ Enforces irreversibility (critical business rule)
- ✅ Enables transition to Virtual Incubator phase
- ✅ Aligns with legacy SelectCollaborationPartnerCommandHandler

---

### 4.3 Phase 2+ Enhancements (Lower Priority)

#### Enhancement 1: Industry Hierarchy (Phase 2)
- **Current**: Flat Industry string
- **Target**: 4-level taxonomy (Industry → Supersector → Sector → Subsector)
- **Effort**: 3-4 days (migration + seed data + filtering UI)
- **Benefit**: Advanced discovery matching

#### Enhancement 2: IdeaCommunication (Phase 2)
- **Current**: Not implemented
- **Target**: Private actor-to-actor messaging
- **Effort**: 3 days (entity + endpoints + notifications)
- **Benefit**: Pre-bid inquiries, relationship building

#### Enhancement 3: RegisteredInterest (Phase 2)
- **Current**: Not implemented
- **Target**: Bookmark innovations, engagement tracking
- **Effort**: 2 days (entity + endpoints + email digest)
- **Benefit**: Analytics, recommendation engine data

#### Enhancement 4: Comment System (Phase 3)
- **Current**: Not implemented
- **Target**: Public discussion on innovations
- **Effort**: 2 days (entity + endpoints + moderation)
- **Benefit**: Community engagement

---

### 4.4 Constitutional Compliance Review

#### Principle 1: User Experience First
- ✅ **Phase 0**: Generic bid submission works for MVP testing
- ⚠️ **Phase 1**: MUST add structured bids for **"idea owner compares bids objectively"** UX requirement
- ⚠️ **Phase 1**: MUST add domain validation for **"smooth workflow progression"** UX requirement

#### Principle 2: Quality is Non-Negotiable
- ✅ **Phase 0**: Tests validate basic CRUD operations
- ⚠️ **Phase 1**: MUST add business rule tests (partner selection validation, submission readiness)
- ⚠️ **Phase 1**: MUST add financial projection validation (rationale min length, numeric ranges)

#### Principle 3: Simplicity Over Cleverness
- ✅ **Phase 0**: Flat model = simplest possible implementation
- ✅ **Phase 1**: Composed model = **necessary complexity** (not premature abstraction)
- ✅ **Legacy reference**: Proven design patterns, not speculative

#### Principle 4: Specification Drives Implementation
- ⚠️ **Current Gap**: Legacy spec (ProductIdea composition) not reflected in current implementation
- ✅ **Mitigation**: Gap documented in specs/002-domain-enhancements
- ⚠️ **Action Required**: Update spec.md to explicitly defer composition to Phase 1

---

## Part 5: Domain Knowledge Transfer

### 5.1 Key Insights from Legacy System

#### Insight 1: Aggregate Boundaries Matter
**Legacy Design**:
- ProductIdea is aggregate root with 4 child entities (IdeaSummary, Product, Market, CollaborationRequirement)
- FormalResponse is separate aggregate (has its own lifecycle, can exist independently)
- BusinessPlan is separate aggregate (idea references it, but not owned)

**Why This Matters**:
- ✅ **Consistency**: Changes to IdeaSummary/Product/Market happen in single transaction
- ✅ **Performance**: Load ProductIdea → automatically loads child entities (single query)
- ✅ **Validation**: Domain methods validate completeness across all child entities

**Current Implementation**:
- Innovation is single entity (no aggregation)
- Bid is separate aggregate (correct boundary)

**Phase 1 Action**: Refactor Innovation to proper aggregate with owned entities

---

#### Insight 2: Polymorphism Enables Type-Safe Workflows
**Legacy Design**:
```csharp
// Compile-time type safety
var mfgBids = idea.FormalIdeaResponses.OfType<ManufacturingResponse>();
var year1Cost = mfgBids.First().YearlyManufacturingCosts[1].UnitCost;

// Generic method with type constraint
public Member GetSelectedPartner<T>() where T : FormalResponse
{
    return FormalIdeaResponses.OfType<T>()
        .First(r => r.Accepted)
        .PostedBy;
}
```

**Benefits**:
- ✅ **Compile-time safety**: Cannot accidentally access Sales bid as Manufacturing bid
- ✅ **LINQ queries**: Can filter by type (`OfType<T>()`)
- ✅ **Domain logic**: Type-specific validation (e.g., ManufacturingResponse must have ≥1 year projection)

**Current Implementation**:
- ❌ All bids treated identically (no type discrimination)
- ❌ ParticipationType is string (e.g., "Manufacturing") → runtime errors possible

**Phase 1 Action**: Implement TPH inheritance for Bid → ManufacturingResponse, etc.

---

#### Insight 3: Domain Methods Encode Business Rules
**Legacy Example**:
```csharp
public virtual bool HasReceivedEnoughOfFormalResponses()
{
    return FormalIdeaResponses.OfType<ManufacturingResponse>().Any() &&
           FormalIdeaResponses.OfType<SalesMarketingResponse>().Any() &&
           FormalIdeaResponses.OfType<ResearchDevelopmentResponse>().Any();
}
```

**Why This is Better Than**:
```csharp
// ❌ Bad: Business logic in controller/endpoint
var mfgCount = await context.Bids
    .CountAsync(b => b.InnovationId == id && b.ParticipationType == "Manufacturing");
var salesCount = await context.Bids
    .CountAsync(b => b.InnovationId == id && b.ParticipationType == "Sales");
// ... 3 database queries, string matching, unclear intent
```

**Benefits**:
- ✅ **Single source of truth**: Business rule in one place
- ✅ **Testable in isolation**: Unit test domain method without database
- ✅ **Reusable**: Called from UI validation, endpoint validation, command handler validation

**Phase 1 Action**: Add domain methods to Innovation entity

---

#### Insight 4: Rationale Fields Create Audit Trail
**Legacy Design**:
```csharp
public class ManufacturingInformation
{
    public decimal UnitCost { get; set; }
    public string UnitCostRationale { get; set; }  // Min 20 characters required
}
```

**Business Purpose**:
- **Investors** need to understand assumptions behind financial projections
- **Idea owners** evaluate partner credibility based on rationale quality
- **Business plan** includes rationales as footnotes/appendices
- **Compliance**: Audit trail for funding applications

**Example**:
```json
{
    "unitCost": 5.50,
    "unitCostRationale": "Based on quotations from suppliers A, B, C. Materials cost $3.50, labor $1.50, overhead $0.50. Assumes 10,000 unit production volume with 15% economies of scale discount applied."
}
```

**Current Implementation**: ❌ Free-text proposal field (no structured rationales)

**Phase 1 Action**: Add rationale fields to financial projection structures

---

### 5.2 Anti-Patterns Avoided in Legacy (Maintain in New System)

#### Anti-Pattern 1: Entity Confusion (Address as Entity)
**Legacy Mistake**:
```csharp
public class Address : EntityOfInt32  // ❌ Address should be Value Object
{
    public virtual Member Member { get; set; }  // Back-reference breaks VO semantics
}
```

**Why This is Wrong** (from DDD analysis):
- Value Objects have no identity (Two addresses with same fields are equal)
- Value Objects should be immutable (or appear immutable)
- Value Objects have no lifecycle independent of owner

**New System** (already correct):
```csharp
public class Address  // ✅ Value Object (no EntityOfGuid base)
{
    public string Street { get; set; }
    public string City { get; set; }
    // No Id, no back-reference to Actor
}

// Used as owned entity in EF Core
public class Actor
{
    public Address? ContactAddress { get; set; }
}
```

**Lesson**: Keep current Address implementation, do NOT follow legacy mistake

---

#### Anti-Pattern 2: Repository Pattern Over-Abstraction
**Legacy Implementation**:
```csharp
public interface IRepository<T>
{
    T GetById(object id);
    void Save(T entity);
    void Delete(T entity);
}
```

**Why This Can Be Over-Abstraction**:
- EF Core DbContext already implements Unit of Work pattern
- Repository pattern adds extra layer with minimal benefit for most LINQ queries
- Harder to write complex queries (need custom repository methods)

**Current System** (already optimal):
```csharp
// Direct EF Core usage in endpoints
app.MapGet("/innovations/{id}", async (Guid id, AppDbContext context) =>
{
    var innovation = await context.Innovations
        .Include(i => i.Actor)
        .FirstOrDefaultAsync(i => i.Id == id);
    // ...
});
```

**Lesson**: Keep current approach, do NOT introduce generic repositories (yet)

**Exception**: Consider domain-specific repositories for complex queries
```csharp
// Phase 2+: When queries become complex
public interface IInnovationDiscoveryService
{
    Task<IEnumerable<Innovation>> FindByIndustryAndResearchCategory(
        Guid subsectorId,
        ResearchCategory category,
        int skip,
        int take);
}
```

---

## Part 6: Migration Checklist

### Phase 0 → Phase 1 Migration Plan

#### Week 1: Innovation Composition Refactoring
- [ ] Create IdeaSummary, ProductDetails, MarketDetails, CollaborationRequirement value object classes
- [ ] Configure as owned entities in EF Core
- [ ] Generate migration (breaking change: columns renamed IdeaSummary_Title, etc.)
- [ ] Update seed data in tests (all Innovation instances)
- [ ] Update Innovation DTOs (request/response)
- [ ] Add domain validation methods (IsReadyForSubmission, etc.)
- [ ] Update endpoints to use composed structure
- [ ] Run full test suite (expect ~20 tests to need updates)

#### Week 2: FormalResponse Polymorphic Hierarchy
- [ ] Create abstract FormalResponse base class (copy current Bid properties)
- [ ] Create ManufacturingResponse, SalesMarketingResponse, ResearchDevelopmentResponse, InvestorResponse subclasses
- [ ] Create financial projection value objects (ManufacturingInfo, SalesInfo, DevInfo)
- [ ] Configure TPH in EF Core (discriminator column)
- [ ] Configure JSON columns for yearly projections (Postgres JSONB / SQL Server JSON)
- [ ] Generate migration (rename table Bids → FormalResponses, add discriminator + JSON columns)
- [ ] Create type-specific bid submission endpoints (/POST .../bids/manufacturing, etc.)
- [ ] Update GET .../bids endpoint to return polymorphic results
- [ ] Update seed data (cast existing bids to appropriate types)
- [ ] Run full test suite (expect ~10 bid-related tests to need updates)

#### Week 3: Partner Selection Workflow
- [ ] Add domain method: Innovation.SelectPartners(mfgId, salesId, rdId)
- [ ] Add validation: Innovation.HasMinimumBidsForSelection()
- [ ] Add validation: Innovation.HasPartnerSelectionCompleted()
- [ ] Create SelectPartnersCommand + CommandHandler
- [ ] Add endpoint: POST /innovations/{id}/select-partners
- [ ] Add endpoint: GET /innovations/{id}/selected-partners
- [ ] Create integration tests (selection success, validation failures)
- [ ] Update Innovation status logic (Published → InCollaboration transition)
- [ ] Update Journey 2 test suite (extend to include partner selection)

#### Week 4: Testing & Documentation
- [ ] Run full test suite (target: 80/80 tests passing)
- [ ] Update OpenAPI documentation (new endpoints + schemas)
- [ ] Update spec.md (document Phase 1 changes)
- [ ] Update data-model.md (document entity composition)
- [ ] Create migration guide for existing Phase 0 deployments
- [ ] Performance testing (ensure JSON column queries performant)

---

## Appendix A: Legacy File Reference

### Analyzed Files
```
Legacy Codebase Root: C:\Users\mahmu\source\repos\innoventity-prototype-development\legacy-mvc\src\

Core Domain Model:
├── Core\Domain\Bases\ProductIdea.cs (178 lines)
│   ├── IdeaSummary (entity)
│   ├── Product (entity)
│   ├── Market (entity)
│   └── CollaborationRequirement (entity)
│
├── Core\Domain\Bases\FormalResponse.cs (base, 100 lines)
├── Core\Domain\Bases\ManufacturingResponse.cs (80 lines)
├── Core\Domain\Bases\SalesMarketingResponse.cs (60 lines)
├── Core\Domain\Bases\ResearchDevelopmentResponse.cs (60 lines)
├── Core\Domain\Bases\InvestorResponse.cs (50 lines)
│
├── Core\Domain\Bases\Member.cs (100 lines)
├── Core\Domain\Model\IdeaCommunication.cs (50 lines)
├── Core\Domain\Bases\RegisteredInterest.cs (80 lines)

Application Services:
├── Core\Service\IProjectValuationService.cs
├── Core\Service\IIdeaRankService.cs
├── Core\Service\CommandMessages\SaveCollaborationPartnerSelection.cs
└── Core\Service\CommandHandlers\SelectCollaborationPartnerCommandHandler.cs (104 lines)

Value Objects & Models:
├── Core\Domain\Model\ProjectValuationResult.cs
├── Core\Domain\Model\Address.cs
└── Core\Domain\Model\Country.cs
```

---

## Appendix B: Terminology Mapping

| Legacy Term | Current Term | Notes |
|-------------|--------------|-------|
| **ProductIdea** | Innovation | Core entity name change (v1.0 branding) |
| **FormalResponse** | Bid | Simplified terminology (same concept) |
| **IdeaAuthor** | Actor (type: IdeaGenerator) | Unified actor model |
| **DomainExpert** | Actor (type: RDOrganization) | Renamed for clarity |
| **Member** | Actor | Clearer term for platform participants |
| **IdeaSummary** | (Flattened in Innovation) | Phase 1 will restore composition |
| **Product** | (Flattened in Innovation) | Phase 1 will restore as ProductDetails |
| **Market** | (Flattened in Innovation) | Phase 1 will restore as MarketDetails |
| **Subsector** | Industry | Simplified to flat string (Phase 0) |
| **Accepted** (bool) | BidStatus enum | Enhanced with Pending/Accepted/Rejected |

---

## Conclusion

### Summary: Where We Are
The current `Innovation` and `Bid` implementation is **intentionally simplified** for Phase 0 MVP goals (authentication, API patterns, basic CRUD). This was the **correct decision** for rapid validation of infrastructure concerns.

### Summary: What We Lost
We lost **significant domain richness** present in the legacy system:
1. **Aggregate composition** (IdeaSummary, Product, Market, Requirements)
2. **Rich business logic** (8 domain methods for workflow validation)
3. **Polymorphic bid types** (Manufacturing/Sales/R&D with financial projections)
4. **Financial modeling support** (structured data enables NPV calculation)
5. **Partner selection workflow** (validation rules, irreversibility enforcement)
6. **Communication systems** (IdeaCommunication, RegisteredInterest, Comments)

### Summary: What To Do Next
**Phase 1 CRITICAL Refactorings** (before innovation submission UI):
1. ✅ **ProductIdea Composition** (2-3 days) - restore IdeaSummary, Product, Market, Requirements
2. ✅ **FormalResponse Polymorphism** (3-4 days) - implement TPH with financial projections
3. ✅ **Partner Selection Workflow** (2 days) - add selection validation and state transitions

**Phase 2+ Enhancements** (optional):
4. Industry hierarchy (advanced matching)
5. IdeaCommunication (private messaging)
6. RegisteredInterest (bookmarking)
7. Comment system (public discussion)

### Final Recommendation
✅ **Ship Phase 0.6 as planned** (T010-T017 completion)
⚠️ **Block Phase 1 innovation submission features** until domain refactoring complete
✅ **Use legacy system as reference architecture** for Phase 1+ design decisions

The legacy system is **NOT legacy spaghetti** - it's a **well-architected DDD system** with proven business patterns. We should **adopt its domain structure**, not reinvent it.

---

**End of Analysis**
