# Feature Specification: Legacy Domain Model Enhancements

**Feature Branch**: `002-domain-enhancements`
**Created**: February 10, 2026
**Status**: Draft
**Input**: Incorporate legacy domain model enhancements from the expert DDD architectural analysis (.specify/analysis/ddd-architectural-analysis.md). Add new requirements preserving domain knowledge while modernizing infrastructure.

**Reference**: [Expert DDD Architectural Analysis](../../.specify/analysis/ddd-architectural-analysis.md)

---

## Context & Background

The legacy Innoventity system (MVC Core) demonstrates expert DDD implementation with:
- **Proper aggregate boundaries** (ProductIdea root, BusinessPlan separate aggregate)
- **Rich domain behavior** (15+ validation methods protecting invariants)
- **Intentional design patterns** (Strategy for FormalResponse, Factory Methods for BusinessPlan)
- **Auditability pattern** (Rationale fields for compliance/governance)

This specification incorporates proven patterns from the legacy system while modernizing infrastructure approach. The goal is **preservation of domain knowledge**, not simplification for ease of implementation.

**Constitutional Impact**: 98/100 (current) → 99/100 (Phase 0.5) → 100/100 (Phase 2)

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Secure Password Management (Priority: P1)

As a **system operator**, I need passwords stored with proper cryptographic salt so that the system resists rainbow table attacks and meets security compliance standards.

**Why this priority**: CRITICAL security vulnerability. Current implementation missing explicit salt storage violates OWASP authentication guidelines.

**Independent Test**: Can be fully tested by registering new actors, inspecting database for PasswordSalt column, attempting login with correct/incorrect passwords, and verifying BCrypt validation uses stored salt.

**Acceptance Scenarios**:

1. **Given** a new actor registration, **When** password is hashed, **Then** system generates unique cryptographic salt and stores separately in PasswordSalt column
2. **Given** an existing actor with stored salt, **When** actor attempts login, **Then** system retrieves salt and validates password using BCrypt with stored salt
3. **Given** database migration executed, **When** existing actors verified, **Then** all actors have non-null PasswordSalt values backfilled

---

### User Story 2 - Structured Actor Names (Priority: P1)

As a **developer implementing user interfaces**, I need actor names split into FirstName and LastName so that I can properly sort, address, and display names according to international conventions.

**Why this priority**: HIGH - Data quality issue blocking proper name handling, internationalization, and professional communication templates.

**Independent Test**: Can be fully tested by registering actors with first/last names, querying API responses for proper structure, sorting actors alphabetically by last name, and generating formatted display names.

**Acceptance Scenarios**:

1. **Given** registration form input, **When** actor submits first name "Jane" and last name "Doe", **Then** system stores as separate FirstName="Jane" and LastName="Doe" properties
2. **Given** actor list query, **When** sorted by last name, **Then** actors appear in correct alphabetical order (not broken by mixed full name formats)
3. **Given** database migration executed, **When** existing FullName="John Smith" values processed, **Then** system splits into FirstName="John" and LastName="Smith"

---

### User Story 3 - Structured Contact Addresses (Priority: P1)

As a **system implementing location-based features**, I need actor addresses structured as value objects so that I can validate addresses, geocode locations, and enable geographic filtering/matching.

**Why this priority**: HIGH - Current string-based ContactAddress prevents validation, geocoding for partner matching, and structured queries for location-based features.

**Independent Test**: Can be fully tested by registering actors with structured addresses, validating required address components, querying actors by city/country, and ensuring proper EF Core owned entity configuration.

**Acceptance Scenarios**:

1. **Given** actor registration with address, **When** address provided with City="London", PostCode="SW1A 1AA", CountryCode="GB", **Then** system stores as Address owned entity (NOT separate table with Id)
2. **Given** actor profile update, **When** address missing required City field, **Then** system rejects update with validation error
3. **Given** partner matching query, **When** filtered by CountryCode="US", **Then** system returns only actors with US addresses using structured query

---

### User Story 4 - Entity Equality Semantics (Priority: P1)

As a **developer working with domain entities**, I need all entities to inherit from EntityBase<TId> so that entity equality works correctly in collections, comparisons, and transient entity detection.

**Why this priority**: HIGH - Foundational DDD pattern enabling correct entity semantics (identity-based equality vs structural equality). Required before Phase 1 composition patterns.

**Independent Test**: Can be fully tested by creating transient entities (no Id), adding entities to HashSet collections, comparing entities by Id, and verifying GetHashCode stability for collection membership.

**Acceptance Scenarios**:

1. **Given** two Actor entities with same Id, **When** compared using Equals(), **Then** returns true (identity-based equality, NOT property comparison)
2. **Given** new Innovation entity without Id assigned, **When** IsTransient() called, **Then** returns true
3. **Given** Actor entities added to HashSet<Actor>, **When** duplicate Id added, **Then** collection maintains uniqueness by Id

---

### User Story 5 - Innovation Composition Pattern (Priority: P2 - Phase 1+)

As a **developer implementing innovation workflows**, I need Innovation aggregate composed of IdeaSummary, Product, Market, and CollaborationRequirement entities so that domain model reflects logical boundaries and validation gates for submission workflow.

**Why this priority**: MEDIUM - Enables rich validation behavior and logical separation. Required for Phase 1 refactoring, builds on Phase 0.5 EntityBase foundation.

**Independent Test**: Can be fully tested by creating innovation with incomplete idea summary, calling IsIdeaSummaryComplete() validation, and verifying submission workflow gates enforce completion checks.

**Acceptance Scenarios**:

1. **Given** Innovation with IdeaSummary.Title="[enter new product idea....]" (default placeholder), **When** IsIdeaSummaryComplete() called, **Then** returns false (business rule: placeholder not valid)
2. **Given** Innovation with complete idea summary, incomplete product details, **When** IsReadyForSubmission() called, **Then** returns false (all sections required)
3. **Given** Innovation aggregate update, **When** IdeaSummary modified, **Then** changes persist as owned entity (NOT separate table with Id)

---

### User Story 6 - FormalResponse Strategy Pattern (Priority: P3 - Phase 1+)

As a **developer implementing partnership workflows**, I need polymorphic FormalResponse hierarchy so that different actor types (Manufacturing, Sales/Marketing, R&D) provide type-safe, role-specific financial projection data structures.

**Why this priority**: MEDIUM - Enables financial modeling and NPV calculation service. Required for Phase 1+ collaboration features.

**Independent Test**: Can be fully tested by creating ManufacturingResponse with yearly manufacturing costs, querying accepted responses by type, and accessing role-specific properties (YearlyManufacturingCosts, YearlySales, YearlyDevelopmentCosts).

**Acceptance Scenarios**:

1. **Given** Manufacturing actor submitting response, **When** YearlyManufacturingCosts provided for years 1-3, **Then** each year's ManufacturingInformation includes ProductionVolume + ProductionVolumeRationale
2. **Given** Innovation with multiple FormalResponse types, **When** querying accepted ManufacturingResponse, **Then** system returns type-safe access to ManufacturingResponse-specific properties
3. **Given** Financial projection data, **When** Rationale field empty or under 20 characters, **Then** validation rejects submission (auditability requirement)

---

### User Story 7 - BusinessPlan Aggregate Protection (Priority: P3 - Phase 2+)

As a **developer implementing financial planning**, I need BusinessPlan aggregate with factory methods so that child entities cannot be orphaned and ownership percentage invariants are protected.

**Why this priority**: LOW - Advanced aggregate pattern for Phase 2 financial features. Demonstrates proper DDD aggregate boundary protection.

**Independent Test**: Can be fully tested by attempting to create ManagementTeamMember without BusinessPlan parent, using AddToManagementTeam() factory method, and verifying bidirectional consistency.

**Acceptance Scenarios**:

1. **Given** BusinessPlan aggregate, **When** AddToManagementTeam("CTO", "Jane", "Doe", 150000) called, **Then** new ManagementTeamMember created with bidirectional reference to BusinessPlan
2. **Given** BusinessPlan creation attempt, **When** trying to directly instantiate ManagementTeamMember without parent, **Then** protected constructor prevents orphaned entity
3. **Given** BusinessPlan.ManagementTeam exposed as IEnumerable, **When** client code attempts .Add(), **Then** compilation error (read-only interface)

---

### User Story 8 - Project Valuation Domain Service (Priority: P3 - Phase 2+)

As a **innovation evaluator**, I need NPV calculation across multiple partnership responses so that I can compare financial viability of different collaboration scenarios.

**Why this priority**: LOW - Advanced financial feature for Phase 2. Demonstrates proper domain service usage for cross-aggregate operations.

**Independent Test**: Can be fully tested by providing ProductIdea with 3 accepted FormalResponse types, calling IProjectValuationService.Calculate(), and receiving ProjectValuationResult with NetPresentValue and PerYearValuations.

**Acceptance Scenarios**:

1. **Given** Innovation with accepted Manufacturing, Sales, and R&D responses, **When** Calculate() called with capital cost and tax rate, **Then** returns ProjectValuationResult with calculated NPV
2. **Given** Multiple response scenarios, **When** Calculate() called for all scenarios, **Then** returns IList<ProjectValuationResult> with BestValuation flag on highest NPV
3. **Given** ProjectValuationResult returned, **When** inspecting PerYearValuations, **Then** includes yearly cash flow breakdown

---

### Edge Cases

- **What happens when existing FullName cannot be split?** (e.g., single word name): Default to FirstName=FullName, LastName="" (empty string), log warning for manual review
- **What happens when Address optional but City provided without Country?**: Validation rejects partial address (if any address field provided, City+PostCode+CountryCode required)
- **What happens when entity added to HashSet before Id assigned?**: EntityBase.GetHashCode() uses RuntimeHelpers.GetHashCode(this) for transient entities (stable hash)
- **What happens when FormalResponse missing Rationale for financial field?**: Validation rejects response (min 20 characters required for compliance audit trail)
- **What happens when BusinessPlan ownership percentages don't sum to 100%?**: Factory method validates total upon adding each Ownership, raises domain error if exceeds 100%

---

## Requirements *(mandatory)*

### Phase 0.5 - Critical Security & Data Quality (IMMEDIATE)

#### R8.4.1: Password Salt Storage

**Requirement**: Actor entity MUST store password salt separately from hash for BCrypt authentication.

**Rationale**: Current implementation violates security best practices by missing explicit salt storage. Legacy system demonstrates proper hash+salt separation. BCrypt internally handles salt, but explicit storage enables salt rotation and audit compliance.

**Acceptance Criteria**:
- Actor entity has `PasswordSalt` property (string, NOT NULL)
- Registration endpoint generates 16-byte cryptographic salt (Base64 encoded)
- PasswordHash stores BCrypt output (includes embedded salt, but PasswordSalt field stores original for auditability)
- Login endpoint validates using BCrypt.Verify(password, PasswordHash)
- Migration script backfills PasswordSalt for existing actors (extract from BCrypt hash or generate new)
- All 32+ existing tests updated for new Actor schema

**Domain Intent**: Security hardening against rainbow table attacks, compliance audit trail for password policy enforcement

---

#### R1.4: Actor Name Decomposition

**Requirement**: Actor entity MUST store FirstName and LastName separately (NOT combined FullName).

**Rationale**: Current flat `FullName` prevents proper sorting (last name first convention), addressing (Dear Ms. LastName), and internationalization (Eastern vs Western name order). Legacy system correctly decomposes as `FirstName` + `LastName`.

**Acceptance Criteria**:
- Actor entity has `FirstName` property (string, NOT NULL, max 50 chars)
- Actor entity has `LastName` property (string, NOT NULL, max 50 chars)
- Registration validates both names (min 2 chars each, letters/spaces/hyphens only)
- API responses can format as display name: `$"{FirstName} {LastName}"` for UI, `$"{LastName}, {FirstName}"` for sorted lists
- Migration script splits existing FullName values (split on last space, handle edge cases)
- Update all actor creation/retrieval code to use FirstName/LastName

**Domain Intent**: Support proper name handling for global users, enable professional communication templates, improve sorting/searching

---

#### R1.5: Address Value Object

**Requirement**: Actor contact information MUST be modeled as structured Address value object (NOT flat string).

**Rationale**: Current `ContactAddress: string` prevents validation (is "123 Main" complete?), geocoding (cannot extract city/country), and structured queries (find partners in London). Legacy system models Address as composite with City, PostCode, Country.

**Acceptance Criteria**:
- Address class (NOT entity, NO Id property) with properties:
  - Address1 (string, required if address provided, max 100 chars)
  - Address2 (string, optional, max 100 chars)
  - City (string, required if address provided, max 50 chars)
  - PostCode (string, required if address provided, max 20 chars)
  - CountryCode (string, required if address provided, ISO 3166-1 alpha-2, exactly 2 chars)
- Actor entity has `ContactAddress: Address?` (nullable, entire address optional)
- Actor entity has `Phone: string?` (nullable, separate from address, max 20 chars)
- EF Core configured as owned entity: `modelBuilder.Entity<Actor>().OwnsOne(a => a.ContactAddress)`
- Validation rule: If ANY address field provided, MUST provide Address1, City, PostCode, CountryCode (all-or-nothing)
- Update registration/profile endpoints to accept/return structured address

**Domain Intent**: Enable structured location data for geographic partner matching, address validation, future geocoding integration

---

#### R9.1: Entity Base Class for Equality Semantics

**Requirement**: All entities MUST inherit from EntityBase<TId> providing identity-based equality per DDD principles.

**Rationale**: Current entities lack proper equality semantics for collections (HashSet, Dictionary key usage) and comparisons. Legacy system provides EntityBase with GetHashCode/Equals overrides and transient entity detection. DDD principle: Entity equality is IDENTITY-based (same Id = same entity), NOT structural (same properties).

**Acceptance Criteria**:

**EntityBase<TId> abstract class**:
```plaintext
public abstract class EntityBase<TId>
{
    public TId Id { get; protected set; }

    public bool IsTransient()
      // Returns true if Id equals default(TId)

    public override bool Equals(object obj)
      // Identity-based: Compare Id values only
      // Transient entities: Compare by reference (ReferenceEquals)

    public override int GetHashCode()
      // Stable for collections: Use Id.GetHashCode() if not transient
      // Transient: Use RuntimeHelpers.GetHashCode(this)

    public static bool operator ==(EntityBase<TId> left, EntityBase<TId> right)
    public static bool operator !=(EntityBase<TId> left, EntityBase<TId> right)
}
```

**Specialization classes**:
```plaintext
public abstract class EntityOfGuid : EntityBase<Guid> { }
public abstract class EntityOfInt32 : EntityBase<int> { }
```

**Entity inheritance updates**:
- Actor inherits EntityOfGuid (change from: `public class Actor : EntityOfGuid`)
- Innovation inherits EntityOfGuid
- Industry inherits EntityOfInt32 (if needs integer PK) OR EntityBase<string> (if keeping string IndustryId)
- Future entities (Comment, FormalResponse, etc.) inherit from appropriate base

**Test coverage**:
- Unit tests for EntityBase equality semantics (same Id = equal, different Id = not equal)
- Unit tests for transient entity handling (IsTransient() = true before Id assigned)
- Unit tests for GetHashCode stability (add to HashSet, membership should work)
- Integration tests verify EF Core tracking works with EntityBase

**Domain Intent**: Correct entity semantics per DDD (identity-based equality, not structural), enable safe usage in collections, provide transient entity detection for validation

---

### Phase 1 - Domain Composition & Rich Behavior (Future)

#### R3.5: ProductIdea Composition Pattern

**Requirement**: Innovation aggregate MUST be composed of IdeaSummary, Product, Market, and CollaborationRequirement owned entities (NOT flat 24-property entity).

**Rationale**: Current flat Innovation entity violates Single Responsibility and Separation of Concerns. User mental model divides innovation submission into logical stages: (1) Describe idea summary, (2) Detail the product, (3) Analyze the market, (4) Define collaboration needs. Legacy ProductIdea demonstrates proper composition with clear boundaries.

**Acceptance Criteria**:

**IdeaSummary owned entity** (NO separate Id):
- Title (string, required)
- ProductType (string, required)
- ResearchType (enum: Management, Engineering, NaturalScience)
- ResearchBackground (string, required, min 50 chars)
- HasRightToUse (bool, required)

**Product owned entity** (NO separate Id):
- Description (string, required)
- TechnologyDescription (string, required)
- BriefSketch (string, optional)
- TargetBeneficiaries (string, required)

**Market owned entity** (NO separate Id):
- RelevantMarketSize (decimal, required)
- PotentialMarketSize (decimal, required)
- TargetIndustries (collection reference to Industry entities)

**CollaborationRequirement owned entity** (NO separate Id):
- PartnersNeeded (flags enum: Manufacturing, SalesMarketing, Engineering)
- CollaborationType (string)
- DesiredTimeline (string)

**Validation methods on Innovation aggregate**:
- `bool IsIdeaSummaryComplete()` - checks Title not placeholder, ResearchType != default, ResearchBackground >= 50 chars, HasRightToUse == true
- `bool IsProductDetailsComplete()` - checks Description, TechnologyDescription, TargetBeneficiaries all present
- `bool IsMarketDetailsSectionComplete()` - checks MarketSize > 0, TargetIndustries.Count > 0
- `bool IsReadyForSubmission()` - returns IsIdeaSummaryComplete() && IsProductDetailsComplete() && IsMarketDetailsSectionComplete()

**EF Core configuration**:
```plaintext
modelBuilder.Entity<Innovation>()
    .OwnsOne(i => i.Summary);
modelBuilder.Entity<Innovation>()
    .OwnsOne(i => i.Product);
modelBuilder.Entity<Innovation>()
    .OwnsOne(i => i.Market);
modelBuilder.Entity<Innovation>()
    .OwnsOne(i => i.CollaborationRequirement);
```

**Domain Intent**: Logical separation matching user mental model (idea → product → market analysis), enable workflow progression gates, improve maintainability by reducing god object

**Phasing**: Phase 1 refactoring (breaking changes to Innovation schema, requires migration)

---

#### R3.6: FormalResponse Strategy Pattern

**Requirement**: Partnership proposals MUST use polymorphic FormalResponse hierarchy with actor-specific data structures.

**Rationale**: Different partnership types (Manufacturing, Sales/Marketing, R&D) require fundamentally different financial projection structures. Manufacturing partners project production volumes and unit costs. Sales partners project units sold and pricing. R&D partners project development duration and infrastructure costs. Legacy system demonstrates Strategy pattern with ManufacturingResponse, SalesMarketingResponse, ResearchDevelopmentResponse. Polymorphism enables type-safe access to role-specific projections.

**Acceptance Criteria**:

**Abstract FormalResponse base** (inherits EntityOfGuid):
- PostedBy (Actor reference, required)
- ForIdea (Innovation reference, required)
- Location (enum: Asia, Americas, Europe, Africa)
- ParticipationType (string)
- ParticipationProposal (string, required, min 100 chars)
- Accepted (bool, default false)
- AcceptedOn (DateTime?, nullable)
- Implements IComparable<FormalResponse> for sorting by date posted

**ManufacturingResponse specialization**:
- YearlyManufacturingCosts (Dictionary<int, ManufacturingInformation>) - key = year (1-10)
- ManufacturingInformation value object:
  - ProductionVolume (int, required)
  - ProductionVolumeRationale (string, required, min 20 chars)
  - UnitCost (decimal, required)
  - UnitCostRationale (string, required, min 20 chars)
  - AverageGlobalDistributionExpense (decimal, required)
  - AvgDistributionExpenseRationale (string, required, min 20 chars)

**SalesMarketingResponse specialization**:
- YearlySales (Dictionary<int, SalesMarketingInformation>) - key = year (1-10)
- SalesMarketingInformation value object:
  - UnitsSold (int, required)
  - UnitsSoldRationale (string, required, min 20 chars)
  - UnitPrice (decimal, required)
  - UnitPriceRationale (string, required, min 20 chars)
  - SalesMarketingExpense (decimal, required)
  - SalesMarketingExpenseRationale (string, required, min 20 chars)

**ResearchDevelopmentResponse specialization**:
- ProductDevelopmentDuration (int, required, in years)
- YearlyDevelopmentCosts (Dictionary<int, ProductDevelopmentInformation>) - key = year (1-10)
- ProductDevelopmentInformation value object:
  - InfrastructureCost (decimal, required)
  - InfrastructureCostRationale (string, required, min 20 chars)
  - PeopleCost (decimal, required)
  - PeopleCostRationale (string, required, min 20 chars)

**Type-safe querying support**:
```plaintext
public Member GetSelectedPartner<T>() where T : FormalResponse
{
    return FormalIdeaResponses.OfType<T>()
        .First(item => item.Accepted)
        .PostedBy;
}
```

**EF Core Table-Per-Hierarchy configuration**:
- Single FormalResponses table with Discriminator column (ManufacturingResponse, SalesMarketingResponse, ResearchDevelopmentResponse)
- Yearly dictionaries stored as JSON columns (EF Core 7+)

**Domain Intent**: Type-safe access to role-specific financial projections, enables NPV calculation service requiring all 3 response types, auditability pattern via Rationale fields

**Phasing**: Phase 1+ collaboration features (after Innovation composition pattern)

---

#### R3.7: BusinessPlan Aggregate with Factory Methods

**Requirement**: Financial planning MUST be modeled as separate BusinessPlan aggregate with protected collections and factory methods enforcing aggregate boundary.

**Rationale**: Legacy system demonstrates textbook DDD aggregate boundary protection via factory methods (AddToManagementTeam, AddToCompetitorAnalyses). Pattern prevents orphaned entities (ManagementTeamMember cannot exist without BusinessPlan parent) and maintains invariants (ownership percentages must sum to 100%). Current flat model would allow child entities created without parent reference, violating consistency boundaries.

**Acceptance Criteria**:

**BusinessPlan aggregate root** (inherits EntityOfGuid):
- BelongToProductIdea (Innovation reference, required, bidirectional)
- ProductDevelopmentLeadCompany (string)
- SalesDevelopmentLeadCharge (string)
- ManufacturingLeadCompany (string)
- CurrentInvestmentRequirement (decimal)

**Protected collections with factory methods**:
```plaintext
private IList<ManagementTeamMember> _managementTeam = new List<ManagementTeamMember>();
public virtual IEnumerable<ManagementTeamMember> ManagementTeam
{
    get { return _managementTeam.ToArray(); }  // Defensive copy
}

public virtual ManagementTeamMember AddToManagementTeam(
    string jobTitle,
    string firstName,
    string lastName,
    decimal salary,
    string bio = null)
{
    var newMember = new ManagementTeamMember(this, jobTitle, firstName, lastName, salary, bio);
    _managementTeam.Add(newMember);
    return newMember;
}

public virtual void RemoveFromManagementTeam(ManagementTeamMember member)
{
    _managementTeam.Remove(member);
}
```

**Similar patterns for**:
- CompetitorAnalyses collection + AddToCompetitorAnalyses() factory
- Ownerships collection + AddToOwnerships() factory (validates total <= 100%)
- Investments collection + AddToInvestmentsMade() factory
- Advisors collection + AddToAdvisors() factory

**Child entity pattern** (example: ManagementTeamMember):
```plaintext
public class ManagementTeamMember : EntityOfInt32
{
    protected ManagementTeamMember() { }  // ORM constructor

    public ManagementTeamMember(BusinessPlan parent, string jobTitle, string firstName, string lastName, decimal salary, string bio = null)
    {
        BelongToProductBusinessPlan = parent ?? throw new ArgumentNullException();
        JobTitle = jobTitle;
        FirstName = firstName;
        LastName = lastName;
        Salary = salary;
        Bio = bio;
    }

    public virtual BusinessPlan BelongToProductBusinessPlan { get; protected set; }  // Bidirectional
    public virtual string JobTitle { get; set; }
    public virtual string FirstName { get; set; }
    public virtual string LastName { get; set; }
    public virtual decimal Salary { get; set; }
    public virtual string Bio { get; set; }
}
```

**Aggregate invariants enforced**:
- Cannot create child entities without parent (protected constructor requires BusinessPlan reference)
- Cannot directly manipulate collections (exposed as IEnumerable, not IList)
- Ownership percentage validation (sum must not exceed 100%)

**Domain Intent**: Enforce consistency boundary, protect ownership percentage invariants (must sum to 100%), prevent orphaned entities, maintain bidirectional consistency

**Phasing**: Phase 2 financial modeling features (advanced DDD pattern)

---

#### R3.8: Auditability Pattern for Financial Inputs

**Requirement**: All financial projections MUST include Rationale text explaining business justification.

**Rationale**: Legacy system includes Rationale property for every financial metric (ProductionVolumeRationale, UnitCostRationale, etc.). This is compliance/governance pattern enabling stakeholder review. Financial projections without justification are arbitrary numbers - rationale enables audit trail, due diligence review, and investor confidence. Prevents "made-up" numbers without business reasoning.

**Acceptance Criteria**:

**Every financial input paired with Rationale field**:
- ManufacturingInformation: ProductionVolume + ProductionVolumeRationale, UnitCost + UnitCostRationale, etc.
- SalesMarketingInformation: UnitsSold + UnitsSoldRationale, UnitPrice + UnitPriceRationale, etc.
- ProductDevelopmentInformation: InfrastructureCost + InfrastructureCostRationale, PeopleCost + PeopleCostRationale

**Rationale field requirements**:
- String type, NOT NULL
- Minimum 20 characters (forces meaningful explanation)
- Maximum 500 characters (keeps concise)
- Validation rejects submission if any Rationale field empty or under minimum length

**Example value object**:
```plaintext
public class ManufacturingInformation  // Value object, NO Id
{
    public int ProductionVolume { get; set; }
    public string ProductionVolumeRationale { get; set; }  // Required, min 20 chars

    public decimal UnitCost { get; set; }
    public string UnitCostRationale { get; set; }  // Required, min 20 chars

    public decimal AverageGlobalDistributionExpense { get; set; }
    public string AvgDistributionExpenseRationale { get; set; }  // Required, min 20 chars
}
```

**UI implications**:
- Forms must provide text area for each Rationale field
- Placeholder text: "Explain the business reasoning for this projection..."
- Validation errors must highlight missing/insufficient rationales

**Domain Intent**: Business justification for compliance, enables audit trail, prevents arbitrary numbers, supports due diligence review by investors/partners

**Phasing**: Phase 1+ collaboration features (required alongside FormalResponse pattern)

---

### Phase 2 - Domain Services & Advanced Patterns (Future)

#### R10.1: Project Valuation Domain Service

**Requirement**: Financial NPV calculation MUST be implemented as domain service (NOT entity method).

**Rationale**: Valuation calculation spans multiple aggregates (ProductIdea + 3 FormalResponse types). Legacy IProjectValuationService demonstrates proper domain service usage. Calculation requires data from ManufacturingResponse (yearly costs), SalesMarketingResponse (yearly sales), ResearchDevelopmentResponse (development duration and costs). Placing this logic on any single entity would violate aggregate boundaries and require cross-aggregate navigation.

**Acceptance Criteria**:

**IProjectValuationService interface**:
```plaintext
public interface IProjectValuationService
{
    ProjectValuationResult Calculate(
        Innovation idea,
        ManufacturingResponse mfgResponse,
        ResearchDevelopmentResponse rdResponse,
        SalesMarketingResponse salesResponse,
        decimal capitalCost,
        decimal longTermEBITDA,
        decimal taxRate);

    IList<ProjectValuationResult> CalculateScenarios(
        Innovation idea,
        IEnumerable<ManufacturingResponse> mfgResponses,
        IEnumerable<ResearchDevelopmentResponse> rdResponses,
        IEnumerable<SalesMarketingResponse> salesResponses,
        decimal capitalCost,
        decimal longTermEBITDA,
        decimal taxRate);
}
```

**ProjectValuationResult value object** (NO Id):
```plaintext
public class ProjectValuationResult
{
    public IList<YearlyValuation> PerYearValuations { get; private set; }
    public decimal TerminalEBITDA { get; set; }
    public decimal EBITDAMultiple { get; set; }
    public decimal TerminalValue { get; set; }
    public decimal NetPresentValue { get; set; }
    public decimal InvestmentRequired { get; set; }

    public string ManufacturingResponseBy { get; set; }  // Actor name for attribution
    public string ResearchDevelopmentResponseBy { get; set; }
    public string SalesMarketingResponseBy { get; set; }

    public bool BestValuation { get; set; }  // Flag highest NPV in multi-scenario comparison

    public class YearlyValuation  // Nested value object
    {
        public int Year { get; private set; }  // Immutable
        public decimal CashflowEBITDA { get; set; }
        public decimal DiscountedCashflow { get; set; }
    }
}
```

**Calculation logic** (NPV formula):
- Iterate through years 1-N (N = max of development duration + projection years)
- For each year:
  - Revenue = SalesMarketingInformation.UnitsSold * UnitPrice
  - COGS = ManufacturingInformation.ProductionVolume * UnitCost + AvgDistributionExpense
  - Operating Expense = SalesMarketingInformation.SalesMarketingExpense + ProductDevelopmentInformation total costs
  - EBITDA = Revenue - COGS - Operating Expense
  - After-tax cash flow = EBITDA * (1 - taxRate)
  - Discounted CF = After-tax CF / (1 + discountRate)^year
- Terminal Value = TerminalEBITDA * EBITDAMultiple / (1 + discountRate)^N
- NPV = Sum of discounted CFs + Terminal Value - capitalCost

**Multi-scenario comparison**:
- Call Calculate() for each combination of accepted responses
- Set BestValuation=true on result with highest NPV
- Return list sorted by NPV descending

**Usage example**:
```plaintext
var mfgResponse = idea.GetSelectedPartner<ManufacturingResponse>();
var salesResponse = idea.GetSelectedPartner<SalesMarketingResponse>();
var rdResponse = idea.GetSelectedPartner<ResearchDevelopmentResponse>();

var result = projectValuationService.Calculate(
    idea, mfgResponse, rdResponse, salesResponse,
    capitalCost: 500000m,
    longTermEBITDA: 2.5m,
    taxRate: 0.21m);

Console.WriteLine($"NPV: {result.NetPresentValue:C}");
```

**Domain Intent**: Complex calculation requiring data from multiple aggregates, stateless operation, enables financial scenario comparison for decision-making

**Phasing**: Phase 2 financial features (requires FormalResponse hierarchy and BusinessPlan aggregate)

---

### Non-Functional Requirements

#### NF8.1: Remove Generic Repository Anti-Pattern

**Change**: Remove generic `IRepository<T>` interface in favor of aggregate-specific repositories.

**Rationale**: Generic repositories encourage CRUD thinking and hide domain-specific query operations. Aggregate-specific repositories expose domain language in interface methods (e.g., `GetPublishedByIndustryAsync`, `GetByIdeaTokenAsync` instead of generic `GetAll()`, `GetById()`).

**Implementation**:
```plaintext
// ❌ REMOVE generic repository
public interface IRepository<T>
{
    T GetById(object id);
    void Save(T entity);
    T[] GetAll();
    void Delete(T entity);
}

// ✅ ADD aggregate-specific repositories
public interface IInnovationRepository
{
    Task<Innovation?> GetByIdAsync(Guid id);
    Task<Innovation?> GetByIdeaTokenAsync(Guid token);
    Task<IEnumerable<Innovation>> GetPublishedByIndustryAsync(string industryId);
    Task<IEnumerable<Innovation>> GetByOwnerAsync(Guid ownerId);
    Task SaveAsync(Innovation innovation);
}

public interface IActorRepository
{
    Task<Actor?> GetByIdAsync(Guid id);
    Task<Actor?> GetByEmailAsync(string email);
    Task<IEnumerable<Actor>> GetByActorTypeAsync(ActorType type);
    Task SaveAsync(Actor actor);
}
```

---

#### NF8.2: Modernize ORM Patterns with EF Core

**Guidance**: Use EF Core owned entities (NOT separate entities with Ids) for value objects.

**Rationale**: Address, IdeaSummary, Product, Market should be owned entities (part of parent aggregate table) rather than separate tables with foreign keys. Prevents orphaned value objects and improves query performance.

**Pattern Application**:
- Address: Owned entity of Actor (columns: Actor_ContactAddress_Address1, Actor_ContactAddress_City, etc.)
- IdeaSummary/Product/Market: Owned entities of Innovation (columns: Innovation_Summary_Title, Innovation_Product_Description, etc.)
- ManufacturingInformation/SalesMarketingInformation/ProductDevelopmentInformation: Stored as JSON in FormalResponse table

**EF Core Configuration**:
```plaintext
modelBuilder.Entity<Actor>()
    .OwnsOne(a => a.ContactAddress, address => {
        address.Property(ad => ad.Address1).HasMaxLength(100);
        address.Property(ad => ad.City).HasMaxLength(50);
        address.Property(ad => ad.PostCode).HasMaxLength(20);
        address.Property(ad => ad.CountryCode).HasMaxLength(2).IsFixedLength();
    });

modelBuilder.Entity<Innovation>()
    .OwnsOne(i => i.Summary);
```

---

### Key Entities

- **EntityBase<TId>**: Abstract base class providing identity-based equality semantics for all domain entities (generic type parameter TId supports Guid, int, string primary keys)

- **EntityOfGuid**: Specialization of EntityBase<Guid> for entities using GUID primary keys (Actor, Innovation, FormalResponse, BusinessPlan)

- **EntityOfInt32**: Specialization of EntityBase<int> for entities using integer primary keys (ManagementTeamMember, CompetitorAnalysis, Industry if using int)

- **Actor** (entity): Platform users, inherits EntityOfGuid, has FirstName/LastName (NOT FullName), ContactAddress (Address value object), PasswordHash, PasswordSalt, ActorType (enum)

- **Address** (value object): Structured contact information, NOT entity (NO Id), owned by Actor, properties: Address1, Address2 (optional), City, PostCode, CountryCode (ISO 3166-1 alpha-2), validated as all-or-nothing

- **Innovation** (entity, aggregate root): Inherits EntityOfGuid, composed of IdeaSummary/Product/Market/CollaborationRequirement owned entities, has validation methods IsIdeaSummaryComplete(), IsProductDetailsComplete(), IsMarketDetailsSectionComplete(), IsReadyForSubmission()

- **IdeaSummary** (value object): Owned entity describing innovation concept, NO separate Id, properties: Title, ProductType, ResearchType, ResearchBackground, HasRightToUse

- **Product** (value object): Owned entity describing product details, NO separate Id, properties: Description, TechnologyDescription, BriefSketch, TargetBeneficiaries

- **Market** (value object): Owned entity describing market analysis, NO separate Id, properties: RelevantMarketSize, PotentialMarketSize, TargetIndustries collection

- **CollaborationRequirement** (value object): Owned entity defining partnership needs, NO separate Id, properties: PartnersNeeded (flags enum), CollaborationType, DesiredTimeline

- **FormalResponse** (abstract entity, aggregate root): Base class for partnership proposals, inherits EntityOfGuid, has PostedBy (Actor), ForIdea (Innovation), Location, ParticipationType, ParticipationProposal, Accepted, AcceptedOn

- **ManufacturingResponse** (entity): Specialization of FormalResponse for manufacturing partners, has YearlyManufacturingCosts (Dictionary<int, ManufacturingInformation>)

- **SalesMarketingResponse** (entity): Specialization of FormalResponse for sales/marketing partners, has YearlySales (Dictionary<int, SalesMarketingInformation>)

- **ResearchDevelopmentResponse** (entity): Specialization of FormalResponse for R&D partners, has ProductDevelopmentDuration, YearlyDevelopmentCosts (Dictionary<int, ProductDevelopmentInformation>)

- **ManufacturingInformation** (value object): Financial projection data for manufacturing, properties: ProductionVolume + ProductionVolumeRationale, UnitCost + UnitCostRationale, AvgDistributionExpense + AvgDistributionExpenseRationale

- **SalesMarketingInformation** (value object): Financial projection data for sales, properties: UnitsSold + UnitsSoldRationale, UnitPrice + UnitPriceRationale, SalesMarketingExpense + SalesMarketingExpenseRationale

- **ProductDevelopmentInformation** (value object): Financial projection data for R&D, properties: InfrastructureCost + InfrastructureCostRationale, PeopleCost + PeopleCostRationale

- **BusinessPlan** (entity, aggregate root): Separate aggregate for financial planning, inherits EntityOfGuid, has protected collections (ManagementTeam, CompetitorAnalyses, Ownerships, Investments, Advisors), exposes as IEnumerable, factory methods enforce aggregate boundary

- **ManagementTeamMember** (entity): Child of BusinessPlan aggregate, inherits EntityOfInt32, has BelongToProductBusinessPlan bidirectional reference, properties: JobTitle, FirstName, LastName, Salary, Bio, protected constructor requires parent

- **ProjectValuationResult** (value object): NPV calculation result, NOT entity, properties: PerYearValuations, TerminalEBITDA, TerminalValue, NetPresentValue, InvestmentRequired, BestValuation flag, response attribution

- **IProjectValuationService** (domain service): Stateless service for NPV calculation spanning multiple aggregates, NOT entity method, operates on Innovation + 3 FormalResponse types + financial parameters

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

**Phase 0.5 (Immediate) Success Criteria**:

- **SC-001**: All 32+ existing tests pass after Actor schema refactoring (FirstName/LastName, Address, PasswordSalt changes)

- **SC-002**: Constitutional compliance improves from 98/100 to 99/100 after password salt implementation

- **SC-003**: Entity equality semantics work correctly: Two Actor entities with same Id return true for .Equals(), entities can be added to HashSet<Actor> without duplicates

- **SC-004**: Address validation rejects partial addresses (if City provided without Country, system returns validation error)

- **SC-005**: Migration script successfully splits 100% of existing FullName values into FirstName/LastName (manual review for edge cases)

**Phase 1 (Composition) Success Criteria**:

- **SC-006**: Innovation validation methods correctly enforce submission gates: IsIdeaSummaryComplete() returns false for placeholder title "[enter new product idea....]", returns true when all required fields present

- **SC-007**: FormalResponse polymorphism enables type-safe queries: Can retrieve accepted ManufacturingResponse and access YearlyManufacturingCosts property without casting

- **SC-008**: Yearly financial projections include all required rationale fields: Submission validation rejects ManufacturingInformation with ProductionVolumeRationale under 20 characters

**Phase 2 (Advanced Patterns) Success Criteria**:

- **SC-009**: BusinessPlan aggregate prevents orphaned entities: Attempting to instantiate ManagementTeamMember without parent BusinessPlan reference fails at compilation (protected constructor)

- **SC-010**: BusinessPlan encapsulation works: BusinessPlan.ManagementTeam returns IEnumerable (read-only), direct .Add() attempts fail at compilation

- **SC-011**: Project valuation service correctly calculates NPV: Given test Innovation with sample responses (known inputs), Calculate() returns expected NetPresentValue within 1% tolerance

- **SC-012**: Multi-scenario valuation comparison flags best option: CalculateScenarios() with 3 response combinations sets BestValuation=true on result with highest NPV

**Code Quality Success Criteria**:

- **SC-013**: All domain entities inherit from EntityBase<TId> (Actor from EntityOfGuid, Innovation from EntityOfGuid, Industry from appropriate base)

- **SC-014**: No value objects misclassified as entities: Address has NO Id property, IdeaSummary has NO Id property, ManufacturingInformation has NO Id property

- **SC-015**: Aggregate repositories follow domain language: IInnovationRepository has GetByIdeaTokenAsync(), GetPublishedByIndustryAsync() (NOT generic GetAll(), GetById())

**Documentation Success Criteria**:

- **SC-016**: Pattern intent documented: For each pattern (Strategy, Factory Method, Owned Entity), documentation explains WHY it exists and what problem it solves

- **SC-017**: Migration guide complete: Clear steps for Phase 0.5 → Phase 1 → Phase 2 progression with breaking change warnings

---

## Assumptions

1. **BCrypt Salt Handling**: Assuming BCrypt.Net library handles salt internally in PasswordHash, but explicit PasswordSalt field provides audit trail and enables future salt rotation policies

2. **Name Splitting Algorithm**: Assuming FullName split on last space works for 95% of cases (Western name convention), edge cases flagged for manual review

3. **Address Optionality**: Assuming ContactAddress is optional (NOT required at registration) to reduce friction, but if any part provided, all required fields must be present (all-or-nothing validation)

4. **Industry Primary Key**: Assuming Industry.IndustryId remains string-based (e.g., "TECH-001") per current schema, if switching to int PK, use EntityOfInt32 instead of EntityBase<string>

5. **Phasing Dependencies**: Assuming Phase 0.5 completes before Phase 1 begins (EntityBase required foundation for composition patterns), Phase 1 completes before Phase 2 (FormalResponse required for BusinessPlan/valuation service)

6. **EF Core Version**: Assuming EF Core 7+ for JSON column support (yearly financial dictionaries stored as JSON), if EF Core 6, use separate tables for yearly data

7. **Breaking Changes Accepted**: Assuming breaking schema changes acceptable for Actor (FullName → FirstName/LastName, ContactAddress → owned entity) since system in early development phase

8. **Legacy System Reference**: Assuming legacy MVC Core system at `C:\Users\mahmu\source\repos\innoventity-prototype-development\legacy-mvc\src\Core` available for pattern verification during implementation

9. **Constitutional Scoring**: Assuming Phase 0.5 achieves 99/100 (from 98/100) by fixing security gap (Salt) and data quality issues (name decomposition, structured address), Phase 2 achieves 100/100 by completing full DDD patterns

10. **Test Coverage Philosophy**: Assuming TDD discipline (create tests → RED → implement → GREEN) for all Phase 0.5 refactoring to ensure no regression
