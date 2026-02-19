# LEGACY SYSTEM ARCHITECTURAL INVENTORY
## Innoventity: Global Open, Hybrid, and Closed Innovation Platform

**Document Version**: 1.3  
**Analysis Date**: February 8, 2026  
**System Era**: ASP.NET MVC (circa 2010-2012, .NET 4.0)  
**System Status**: ⚠️ **DORMANT - NO OPERATIONAL DATA**  
**Purpose**: Reference Architecture for Greenfield Reimplementation  
**Status**: Living Document - Aligned with v1.0 Scope Decisions

---

## ⚠️ CRITICAL FINDINGS SUMMARY

### 🔴 CRITICAL DISCOVERY: Legacy System Status
**The legacy MVC application is DORMANT with NO operational data.** This fundamentally changes the project from a "data migration" to a "reference-based reimplementation":
- ✅ No complex data migration patterns needed
- ✅ No dual-write synchronization required
- ✅ Fresh modern database schema (no legacy constraints)
- ✅ Legacy codebase serves as domain knowledge reference only
- ✅ Freedom to optimize naming and structure from day one

**Project Type**: Greenfield reimplementation using legacy as specification source.

### Strong Alignment Areas ✅
The implementation **strongly aligns** with the Innoventity platform vision for:
- **Virtual Incubator Functions**: Partnership formation, business planning, management team building, project valuation
- **Innovation Actor Ecosystem**: All five primary actor types implemented (Idea Generators, R&D Organizations, Manufacturing Companies, Sales & Marketing Companies, Investors)
- **Formal Collaboration Workflow**: Structured bid submission and partner selection process
- **Research-Based Innovation Submission**: IPR protection, research background documentation
- **Industry-Based Matching**: Actor affiliation enables innovation-to-actor matching

### Legacy-to-v1.0 Feature Mapping ✅

**✅ Implemented in v1.0** (Legacy feature carried forward):
- **Open Innovation Mode**: Fully implemented in legacy, will be reimplemented in v1.0
- **Five Actor Types**: IdeaGenerator, RDOrganization, Manufacturing, Sales, Investor
- **Formal Collaboration Workflow**: Bid-based partner selection
- **Virtual Incubator Functions**: Business planning, team formation, valuation
- **Research Categories**: ✅ **RESOLVED** - Simple enum (Management/Engineering/NaturalScience) for metadata/filtering only

**📋 Deferred to v2.0** (Intentional v1.0 scope exclusions):
- **Platform Modes (Closed/Hybrid)**: Not evident in legacy code, deferred to v2.0 with multi-tenancy
- **Government/Technology Parks**: 6th actor type, deferred to v2.0
- **Multi-Tenancy**: Required for Closed/Hybrid modes, deferred to v2.0
- **Organization Entities**: Corporate structure support, deferred to v2.0

### v1.0 Scope Decisions ✅ **RESOLVED**
**Strategic decision made by project owner**: v1.0 will focus exclusively on **Open Innovation** mode:
- ✅ **v1.0 Scope**: Open Innovation only (10 months / 40 weeks)
- 📋 **v2.0 Deferred**: Closed/Hybrid modes, multi-tenancy, organizations
- ✅ **Actor Types**: 5 types in v1.0 (IdeaGenerator, RDOrganization, Manufacturing, Sales, Investor)
- 📋 **Deferred Actors**: Government/Technology Parks (v2.0)
- ✅ **Research Categories**: Simple enum in v1.0 (Management/Engineering/NaturalScience) - no workflow differences
- ✅ **Database Strategy**: Modern schema with naming fixes (IdeaAuthor → IdeaGenerator)

**Rationale**: Simplified scope enables focused learning and portfolio demonstration. Complex multi-tenancy deferred to v2.0 after core platform proven.

---

## DOCUMENT QUICK REFERENCE

### What This Document Contains
This comprehensive architectural inventory captures the design, implementation, and operational characteristics of the **Innoventity Global Open, Hybrid, and Closed Innovation Platform** - a virtual incubator linking research-based innovations with commercialization actors. This analysis supports planning a greenfield re-architecture using ASP.NET Core + Angular + Vertical Slice Architecture.

### Authoritative System Context
**Innoventity** is a Global Open, Hybrid, and Closed Innovation Platform that:
- Functions as a **virtual incubator** linking research-based innovation ideas with innovation actors
- Supports **three innovation modes**: Open, Closed, and Hybrid
- Categorizes innovations by **research type**: Management, Engineering, Natural Science
- Connects **innovation ecosystem actors**: Idea Generators, R&D Organizations, Manufacturing Companies, Sales & Marketing Companies, Investors, and Government/Technology Parks

### Implementation Analysis Summary
✅ **Strongly Aligned with Context**:
- Innovation actor types (Idea Generators, R&D, Manufacturing, Sales/Marketing, Investors)
- Virtual incubator functions (partnership formation, business planning, team building)
- Research-based innovation submission with IPR protection
- Formal bid-based collaboration workflows
- Industry affiliation for actor-innovation matching

⚠️ **Partial Alignment / Requires Investigation**:
- **Platform Modes**: Codebase primarily implements Open Innovation model
  - Closed innovation (organization-scoped): Not evident in reviewed code
  - Hybrid innovation (selective disclosure): Partial patterns via `LimitedView`
- **Research Categories**: Field exists but classification system unclear
- **Government/Technology Parks**: Not implemented in reviewed code

❓ **Requires Stakeholder Validation**:
- Are Closed and Hybrid modes implemented elsewhere or planned?
- How are research categories (Management/Engineering/Natural Science) used?
- What is the role of organization entities in closed/hybrid models?

### Sections Overview
1. **System Overview** - Platform vision, innovation modes, actor ecosystem
2. **Core User Journeys** - Innovation submission, actor matching, partnership formation
3. **Domain Model** - Innovations, actors, platform modes, lifecycle states
4. **Application Responsibilities** - Layered architecture, CQRS, virtual incubator functions
5. **Data & Persistence** - NHibernate ORM, repository pattern, transaction boundaries
6. **Integrations & Infrastructure** - Azure Service Bus, MailGun, external services
7. **Security & Access Control** - Authentication, actor-based authorization, IP protection
8. **Technical Constraints** - Dependencies, platform mode assumptions, coupling points
9. **Risk & Ambiguity Register** - Platform mode gaps, research category validation, hidden coupling
10. **Controller & Repository Analysis** - Complete code inventory and behavior documentation

### Key Statistics
- **Platform Type**: Global Open, Hybrid, and Closed Innovation Platform (Virtual Incubator)
- **Innovation Actors**: 5 primary types (Idea Generators, R&D Orgs, Manufacturers, Sales/Marketing, Investors)
- **Research Categories**: 3 types (Management, Engineering, Natural Science) - implementation unclear
- **Platform Modes**: 3 modes (Open, Closed, Hybrid) - Open mode primary implementation
- **Controllers**: 5 (50+ action methods)
- **Domain Entities**: 15+ core entities
- **Repositories**: 6 implementations
- **Command Handlers**: 5 CQRS handlers
- **Routes**: 30+ mapped endpoints
- **External Integrations**: 2 (Azure Service Bus, MailGun)
- **Innovation Lifecycle States**: 10 implicit states
- **Collaboration Workflow**: Formal bid-based with 4 response types
- **Documentation Coverage**: ~70% of system analyzed

### Analysis Confidence
- ✅ **High Confidence**: Innovation actors, collaboration workflows, virtual incubator functions, domain model, routes, authorization
- ⚠️ **Medium Confidence**: Platform modes (Open primary, Closed/Hybrid unclear), research categorization, performance patterns
- ❓ **Requires Investigation**: Multi-tenancy implementation, organization entities, research category usage, Government/Technology Park actors
- 🔍 **Gap Analysis**: Client-side JavaScript, database schema, NHibernate mappings, closed/hybrid mode implementation

### Alignment with Authoritative Context
| Requirement | Implementation Status | Evidence |
|-------------|----------------------|----------|
| Virtual Incubator | ✅ Implemented | Business planning, management team, valuation, partnership workflows |
| Idea Generators | ✅ Implemented | `IdeaAuthor`, `CorporateIdeaAuthor` entities |
| R&D Organizations | ✅ Implemented | `DomainExpert` entity with R&D proposals |
| Manufacturing Companies | ✅ Implemented | `Manufacturer` entity with cost/volume modeling |
| Sales & Marketing Companies | ✅ Implemented | `SalesMarketing` entity with market strategy |
| Investors | ✅ Implemented | `Investor` entity with investment proposals |
| Government/Tech Parks | ❓ Not Found | Not evident in reviewed code |
| Open Innovation | ✅ Implemented | Primary platform mode |
| Closed Innovation | ❓ Unclear | No multi-tenancy or organization scoping visible |
| Hybrid Innovation | ⚠️ Partial | `LimitedView` pattern supports IP protection |
| Management Research | ❓ Unclear | Field exists but categorization unclear |
| Engineering Research | ❓ Unclear | Field exists but categorization unclear |
| Natural Science Research | ❓ Unclear | Field exists but categorization unclear |

---

## SECTION 1 — SYSTEM OVERVIEW

### Primary Business Purpose
**Innoventity** is a **Global Open, Hybrid, and Closed Innovation Platform** that functions as a virtual incubator linking research-based innovation ideas with innovation actors. The platform enables innovation lifecycle management through idea submission, collaboration facilitation, business planning, and commercialization pathways.

### Platform Vision Realization
The system realizes the Innoventity vision by:
- Providing a marketplace for research-based innovations to connect with commercialization partners
- Supporting multiple innovation models (Open, Closed, Hybrid) within a single platform
- Facilitating structured collaboration between idea generators and domain-specific actors
- Enabling business plan development and financial modeling for innovation commercialization
- Creating a virtual incubator environment with role-based participation workflows

### Innovation Actor Types
The platform supports distinct innovation actors as defined in the authoritative system context:

**Primary Actors**:
- **Idea Generators** (mapped to `IdeaAuthor` entity): Individuals or organizations submitting research-based innovation ideas
- **Sales and Marketing Companies** (mapped to `SalesMarketing` entity): Organizations providing market strategy and commercialization expertise
- **R&D Organizations** (mapped to `DomainExpert` entity): Research and development partners providing technical expertise
- **Manufacturing Companies** (mapped to `Manufacturer` entity): Production and distribution partners
- **Investors** (mapped to `Investor` entity): Financial backers and investment reviewers
- **Governments and Technology Parks** (implicit in closed/hybrid models): Policy enablers and incubator hosts

**Secondary Actors**:
- **System Administrators**: Platform management and configuration (implicit)
- **Organization Administrators**: Closed/hybrid innovation workspace managers (implicit / requires confirmation)

### Core Platform Responsibilities
- **Innovation Submission & Lifecycle Management**: Research-based idea creation, categorization, and state tracking
- **Actor Matching & Discovery**: Connecting innovation ideas with appropriate commercialization partners
- **Collaboration Orchestration**: Formal bid-based partnership selection and collaboration workflows
- **Business Plan Development**: Financial modeling, ownership structure, and commercialization planning
- **Multi-Modal Support**: Platform configuration for Open, Closed, and Hybrid innovation modes (implicit / requires confirmation)
- **Research Categorization**: Classification of innovations by research type (Management, Engineering, Natural Science) (implicit / requires confirmation)
- **Intellectual Property Protection**: Controlled disclosure and access based on innovation mode
- **Virtual Incubator Functions**: Management team formation, competitive analysis, project valuation

### External Systems & Dependencies

**Data Storage:**
- SQL Server (primary data store)
- NHibernate 3.2 ORM

**Messaging & Communication:**
- Azure Service Bus (`Infrastructure.AzureMessaging` project)
- MailGun API for email delivery
- Email templating system (PHPMailer-based templates in `doc/Email Templates/`)

**Client-Side:**
- jQuery 1.6.1
- Handlebars.js runtime for client-side templating
- CKEditor for WYSIWYG editing

**Development Infrastructure:**
- MvcContrib framework
- AutoMapper for DTO transformations
- Common.Logging 1.2.0
- Code Contracts (enabled in multiple projects)

---

## SECTION 2 — CORE USER JOURNEYS

### Journey 1: Research-Based Innovation Submission
**Primary Actor**: Idea Generator (IdeaAuthor)  
**Description**: Researcher or innovator submits a research-based innovation idea to the platform, categorized by research type (Management/Engineering/Natural Science - implicit)  
**Platform Mode**: Open, Closed, or Hybrid (implicit / requires confirmation)  
**Entry Point**: `/ProductIdea/Create` (inferred)  
**Key Actions**:
- Enter innovation summary and research background
- Specify research type and intellectual property status
- Describe proposed product/solution
- Define target market and industry
- Specify required collaboration partners
**Outcome**: `ProductIdea` entity created in Draft state

**Code Evidence**: `IdeaSummary` entity captures research background, IPR status, and idea research type

### Journey 2: Business Plan Development
**Primary Actor**: Idea Author  
**Description**: Author develops comprehensive business plan for their idea  
**Entry Point**: Controllers handling `SaveBusinessPlan` command  
**Key Actions** (from `BusinessPlan` entity):
- Define product development lead company
- Specify estimated capital need
- Detail financial investments
- Define ownership structure
- Add competitor analysis
- Compose management team
- Set revenue projections
**Outcome**: `BusinessPlan` aggregate persisted, linked to `ProductIdea`

**Technical Flow** (from `CreateBusinessPlanCommandHandler`):
```
User Input → SaveBusinessPlan Command → Command Handler → 
Validation → Repository.Save() → ReturnValue Response
```

### Journey 3: Innovation Discovery & Actor Matching
**Primary Actor**: Innovation Actors (R&D Orgs, Manufacturers, Sales/Marketing, Investors)  
**Description**: Innovation actors discover research-based ideas matching their expertise and industry focus to participate in commercialization  
**Platform Mode**: Open (public discovery), Closed (organization-scoped), Hybrid (mixed)  
**Entry Point**: Idea listing/search controllers (implicit)  
**Key Actions**:
- Browse innovations by research category (implicit)
- Filter by industry affiliation
- View limited idea information (IP protection)
- Review collaboration requirements
- Assess commercial potential
**Outcome**: Actor identifies relevant innovation opportunities

**Code Evidence**: `IIndustryAffiliation` interface enables actor-to-idea matching by industry. `LimitedView` action restricts IP exposure.

### Journey 4: Formal Collaboration Bid Submission
**Primary Actor**: Innovation Actor (R&D Org, Manufacturer, Sales/Marketing, or Investor)  
**Description**: Actor submits formal proposal (bid) to participate in innovation commercialization, aligned with their specialized role  
**Platform Mode**: Open, Closed, or Hybrid  
**Entry Point**: `/productidea/{id}/post-formalresponse`  
**Key Actions**:
- Review innovation details in limited view
- Prepare role-specific proposal:
  - **Manufacturing**: Production costs, volumes, distribution plans
  - **Sales/Marketing**: Market strategy, channel plans, revenue projections
  - **R&D**: Technical development approach, timelines
  - **Investor**: Investment terms, funding amounts
- Submit formal response with participation proposal
- Specify geographic location and participation type
**Outcome**: `FormalResponse` entity created and linked to `ProductIdea`

**Code Evidence**: Specialized response types (`ManufacturingResponse`, `SalesMarketingResponse`, `ResearchDevelopmentResponse`, `InvestorResponse`) map to actor types

### Journey 5: Collaboration Partner Selection
**Primary Actor**: Idea Generator (IdeaAuthor)  
**Description**: Innovation originator reviews formal bids and selects commercialization partners from each required actor category  
**Platform Mode**: All modes (Open, Closed, Hybrid)  
**Entry Point**: `/productidea/{id}/SelectCollabortionPartners`  
**Key Actions**:
- Review all submitted formal responses/bids
- Evaluate proposals from each actor type
- Select one partner from each required category:
  - Manufacturing company
  - Sales & marketing company
  - R&D organization
- Confirm selection (irreversible process)
**Outcome**: Selected bids marked as `Accepted`, virtual incubator team formed

**Code Evidence**: `SelectCollaborationPartnerCommandHandler` validates selection of one bid from each type. `MustSelectBidsFromAllCollabTypes` validation rule enforces completeness.

### Journey 6: Virtual Incubator Operations
**Primary Actor**: Idea Generator (with selected partners)  
**Description**: Post-selection collaboration within virtual incubator environment (implicit / requires confirmation)  
**Platform Mode**: All modes  
**Key Actions** (inferred):
- Build management team from selected partners
- Develop comprehensive business plan
- Create financial projections and ownership structure
- Conduct competitive analysis
- Calculate project valuation
**Outcome**: Innovation ready for commercialization or investment

**Code Evidence**: `CreateManagementTeam`, `CreateBusinessPlan`, `ViewProjectValuation` actions support incubator functions

---

## SECTION 3 — DOMAIN MODEL (CONCEPTUAL)

### Core Domain Concepts

#### Innovation (ProductIdea Entity)
Represents a research-based innovation idea submitted to the platform. The core aggregate root around which all collaboration occurs.

**Research-Based Categorization** (implicit / requires confirmation):
- Management research-based innovations
- Engineering research-based innovations  
- Natural science research-based innovations

**Platform Mode Context** (implicit / requires confirmation):
- Open: Publicly discoverable innovations
- Closed: Organization-scoped innovations
- Hybrid: Selectively disclosed innovations

**Innovation Lifecycle**: See detailed state machine below

#### Innovation Actor (Member Hierarchy)
Participants in the innovation ecosystem with distinct roles and capabilities.

**Actor Types**:
1. **Idea Generator** (`IdeaAuthor`, `CorporateIdeaAuthor`)
   - Submits research-based innovations
   - Owns innovation intellectual property
   - Selects commercialization partners
   - Manages virtual incubator team

2. **Sales and Marketing Company** (`SalesMarketing`)
   - Provides market strategy expertise
   - Submits formal sales/marketing proposals
   - Industry-affiliated via `IIndustryAffiliation`

3. **R&D Organization** (`DomainExpert`)
   - Provides technical development expertise
   - Submits research & development proposals
   - Industry-affiliated specialization

4. **Manufacturing Company** (`Manufacturer`)
   - Provides production and distribution capabilities
   - Submits manufacturing cost and volume proposals
   - Industry-affiliated manufacturing sectors

5. **Investor** (`Investor`)
   - Provides financial backing
   - Reviews business plans and valuations
   - Industry-affiliated investment focus

6. **Government/Technology Park** (implicit / requires confirmation)
   - Supports closed/hybrid innovation models
   - Provides policy and infrastructure support
   - Not directly evident in current codebase

**Actor Characteristics**:
- Industry affiliation for matching
- Geographic location specification
- Participation history tracking
- Role-specific onboarding workflows

### Platform Modes (Implicit / Requires Confirmation)

**Open Innovation Mode**:
- Public innovation submission and discovery
- Any registered actor can participate
- Visible to all platform users
- Current codebase appears primarily designed for this mode

**Closed Innovation Mode**:
- Organization-internal innovation workflows
- Restricted actor participation within organization
- IP protection within organizational boundaries
- **Code Evidence**: Not directly visible in reviewed code. May exist in unreviewed areas or may be planned functionality.

**Hybrid Innovation Mode**:
- Mixed open and closed participation
- Selective disclosure of innovation details
- Organization-controlled external collaboration
- **Code Evidence**: `LimitedView` action suggests restricted information disclosure patterns that could support hybrid models.

**Observation**: The reviewed codebase focuses primarily on open innovation workflows. Multi-tenancy or organization-scoped logic for closed/hybrid modes requires further investigation.

#### BusinessPlan (Aggregate or Part of ProductIdea Aggregate)
**Defined Properties** (from `BusinessPlan.cs`):
- `ProductDevelopmentLeadCompany` (string)
- `EstimatedCapitalNeed` (decimal)
- `ExpectedAnnualRevenue` (decimal)
- `Ownerships` (collection of `Ownership`)
- `FinancialInvestments` (collection of `FinancialInvestment`)
- `CompetitorAnalyses` (collection of `CompetitorAnalysis`)
- `ManagementTeam` (collection, type unspecified)

**Responsibilities**:
- Financial planning and capital requirements
- Ownership structure management
- Competitive intelligence
- Team composition

#### Supporting Entities

**FinancialInvestment**
- Tracks investment sources and amounts
- Part of BusinessPlan aggregate

**Ownership**
- Defines equity/ownership distribution
- Part of BusinessPlan aggregate

**CompetitorAnalysis**
- Captures competitive landscape information
- Part of BusinessPlan aggregate

**ManagementTeam** (inferred)
- Team member tracking
- Role/expertise mapping

### Value Objects
**Implicit / Requires Confirmation**:
- Company information
- Financial amounts (using decimal primitives, not value objects)
- Contact information
- Expertise/skill descriptors

### Lifecycle States

**Innovation Lifecycle** (implicit state machine based on data completeness):

1. **Draft** → Innovation created, sections being completed
   - Trigger: `CreatedOn` timestamp set
   - Validation: None

2. **Summary Complete** → Research background and IPR status documented
   - Validation: `IsIdeaSummaryComplete() == true`
   - Required: Title, product type, research background, IPR rights confirmation

3. **Product Details Complete** → Technical description and advantages defined
   - Validation: `IsProductDetailsComplete() == true`
   - Required: Description, advantages, development phase, keywords

4. **Market Details Complete** → Target market and industries identified
   - Validation: `IsMarketDetailsSectionComplete() == true`
   - Required: Customer type, market description, target industries

5. **Submitted/Published** → Available for actor discovery
   - Trigger: `SubmittedOn` timestamp set
   - Visibility: Platform mode-dependent (Open/Closed/Hybrid)

6. **Receiving Proposals** → Innovation actors submitting formal bids
   - Collection: `FormalIdeaResponses` populated
   - Actors: Manufacturing, Sales/Marketing, R&D, Investors

7. **Sufficient Proposals** → Minimum bids received for partner selection
   - Validation: `HasReceivedEnoughOfFormalResponses() == true`
   - Required: At least one bid from Manufacturing, Sales/Marketing, and R&D

8. **Partners Selected** → Virtual incubator team formed
   - Validation: `HasCollaborationPartnerSelectionCompleted() == true`
   - Action: One accepted bid from each required actor type
   - Note: Irreversible process

9. **Business Plan Developed** → Commercialization plan complete
   - Validation: `HasBusinessPlan() == true`
   - Components: Financial model, ownership structure, competitive analysis

10. **Commercialization Ready** (implicit / requires confirmation)
    - Innovation ready for market entry or investment
    - Management team formed
    - Project valuation calculated

**State Transition Authorization**:
- Draft → Published: Idea Generator only
- Published → Receiving Proposals: Automatic (actor-driven)
- Proposals → Partners Selected: Idea Generator only
- Partners Selected → Business Plan: Collaborative (incubator team)

**Reversibility**: Unknown - requires confirmation if innovations can be withdrawn or archived

### Business Invariants

**Innovation Submission Rules**:
- Innovation must have research background documented (`IdeaSummary.ResearchBackground`)
- Idea generator must confirm right to use the innovation (`IdeaSummary.HasRightToUseThisIdea`)
- IPR status must be declared (`IdeaSummary.HasIpr`)

**Actor Participation Rules**:
- Each actor can submit only one formal response per innovation per actor type
- Actors are matched to innovations via industry affiliation
- Geographic location must be specified for formal responses

**Collaboration Selection Rules** (from `SelectCollaborationPartnerCommandHandler`):
- Must select exactly one partner from each required actor type:
  - One Manufacturing company
  - One Sales & Marketing company
  - One R&D organization
- Selection process cannot be repeated once completed
- Only unaccepted bids can be selected

**Virtual Incubator Rules**:
- BusinessPlan is optional but recommended for commercialization
- Multiple financial investments can be tracked
- Ownership structure supports multiple stakeholders
- Management team composition required for business plan

**Platform Mode Rules** (implicit / requires confirmation):
- Open: Public innovation visibility and participation
- Closed: Organization-scoped innovation and actor restrictions
- Hybrid: Controlled external participation with IP protection

### Domain Vocabulary Summary
| Term | Platform Concept | Technical Implementation |
|------|------------------|-------------------------|
| Innovation / ProductIdea | Research-based innovation idea | `ProductIdea` aggregate root |
| Idea Generator | Research-based innovation submitter | `IdeaAuthor`, `CorporateIdeaAuthor` |
| Innovation Actor | Ecosystem participant | `Member` hierarchy |
| R&D Organization | Technical development partner | `DomainExpert` entity |
| Manufacturing Company | Production partner | `Manufacturer` entity |
| Sales & Marketing Company | Commercialization partner | `SalesMarketing` entity |
| Investor | Financial backer | `Investor` entity |
| Formal Response / Bid | Partnership proposal | `FormalResponse` hierarchy |
| Virtual Incubator | Collaboration workspace | Implicit in workflows |
| Research Category | Innovation classification | Implicit property (Management/Engineering/Natural Science) |
| Platform Mode | Open/Closed/Hybrid | Implicit / requires confirmation |
| Business Plan | Commercialization plan | `BusinessPlan` aggregate |
| Collaboration Requirement | Needed partner types | `CollaborationRequirement` entity |
| Actor Affiliation | Industry expertise match | `IIndustryAffiliation` interface |
| Management Team | Incubator leadership | `ManagementTeamMember` collection |

---

## SECTION 4 — APPLICATION RESPONSIBILITIES

### Major Subsystems / Bounded Contexts

#### 1. Innovation Lifecycle Management (`src/Core` project)
**Responsibilities**:
- Research-based innovation submission and validation
- Innovation state progression (Draft → Published → Commercialization)
- IPR declaration and protection
- Research category classification (implicit)
- Innovation completeness validation

**Domain Layer Structure**:
- `Domain/Bases/` - Core innovation and actor entities
- `Service/CommandHandlers/` - CQRS command processing
- Repository abstractions for data access

#### 2. Actor Ecosystem & Matching
**Responsibilities**:
- Innovation actor registration and onboarding
- Role-specific actor types (Idea Generators, R&D Orgs, Manufacturers, Sales/Marketing, Investors)
- Industry affiliation management
- Actor-to-innovation matching via industry sectors
- Geographic location tracking

**Pattern**: Industry affiliation enables discovery and matching between innovations and appropriate actors

#### 3. Collaboration & Partnership Formation
**Responsibilities**:
- Formal bid submission by innovation actors
- Role-specific proposal collection (Manufacturing, Sales/Marketing, R&D, Investment)
- Partner selection and virtual incubator team formation
- Irreversible partner commitment workflow

**CQRS Commands**:
- Submit formal responses (bids)
- Select collaboration partners
- Form management team

#### 4. Virtual Incubator Operations
**Responsibilities**:
- Business plan development for commercialization
- Financial modeling and ownership structure
- Management team composition
- Competitive analysis
- Project valuation calculation

**Supporting Services**:
- `IProjectValuationService` - Financial valuation
- Business plan command handlers
- Management team formation

#### 5. Platform Mode Management (Implicit / Requires Confirmation)
**Responsibilities**:
- Open innovation: Public discovery and participation
- Closed innovation: Organization-scoped workflows
- Hybrid innovation: Controlled external collaboration
- IP protection and limited information disclosure

**Code Evidence**: 
- `LimitedView` actions suggest IP protection patterns
- Multi-tenancy or organization scoping not evident in reviewed code
- May exist in configuration or unreviewed modules

#### 2. Infrastructure Layer

**2a. Data Access (`src/Infrastructure.NHibernate`)**
**Responsibilities**:
- NHibernate session management
- Concrete repository implementations
- Database mapping configurations
- Transaction coordination

**2b. Messaging (`src/Infrastructure.AzureMessaging`)**
**Responsibilities**:
- Azure Service Bus integration
- Asynchronous message publishing
- Event distribution

**2c. Email (`src/Infrastructure.Emailer`)**
**Responsibilities**:
- Email composition
- MailGun API integration
- Template rendering

#### 3. Presentation Layer (`src/UI`)
**Responsibilities**:
- HTTP request handling (MVC Controllers)
- View rendering (Razor)
- Client-side interaction (jQuery)
- DTO mapping (AutoMapper profiles)
- Client-side templating (Handlebars)

**Key Components**:
- `AutoMapper/Profiles/Bases/IdeaMapperProfile.cs` - Domain to ViewModel transformations
- Controllers (implicit, not reviewed)
- Razor views (implicit)
- JavaScript modules (CKEditor, Handlebars)

#### 4. Database Management (`src/Database`, `src/SchemaBuilder`)
**Responsibilities**:
- Schema versioning
- Database initialization
- Migration scripts (implicit)

### CQRS Application Pattern

**Command Side** (from `CreateBusinessPlanCommandHandler`):

**Command Message**: `SaveBusinessPlan`
- Encapsulates intent to create/update business plan
- Contains all necessary data for operation

**Command Handler**: Implements processing logic
```csharp
public class CreateBusinessPlanCommandHandler : 
    ICommandHandler<SaveBusinessPlan, ReturnValue>
{
    IProductIdeaRepository _repository;
    
    public ReturnValue Handle(SaveBusinessPlan command)
    {
        // 1. Validation
        // 2. Domain operation
        // 3. Repository.Save()
        // 4. Return success/failure
    }
}
```

**Pattern Characteristics**:
- Single responsibility per handler
- Repository abstraction for persistence
- ReturnValue pattern for operation result
- Dependency injection (constructor-based)

**Query Side**:
**Implicit / Requires Confirmation**: 
- Likely uses direct repository queries
- May use separate read models
- AutoMapper suggests ViewModel projections for queries

### Cross-Cutting Concerns

#### Validation
**Location**: Command handler level  
**Pattern**: Validation before persistence  
**Evidence**: Handler structure suggests validation in `Handle()` method

#### Error Handling
**Mechanism**: `ReturnValue` pattern from MvcContrib  
**Characteristics**:
- Encapsulates success/failure state
- Likely includes error messages
- Avoids exception-based flow control for business errors

#### Logging
**Framework**: Common.Logging 1.2.0  
**Integration**: Implicit throughout infrastructure  
**Detail Level**: Requires confirmation

#### Authentication & Authorization
**Implicit / Requires Confirmation**: Not visible in reviewed files  
**Likely Approach**: ASP.NET Forms Authentication or custom

#### Transaction Management
**Implicit / Requires Confirmation**: Likely managed at:
- NHibernate session level
- Repository boundary
- Per-request scope

---

## SECTION 5 — DATA & PERSISTENCE

### ORM Usage: NHibernate 3.2

#### Session Management
**Pattern**: Session-per-request (typical for NHibernate with MVC)  
**Location**: `Infrastructure.NHibernate` project  
**Lifecycle**: Implicit HTTP module or filter-based

#### Mapping Approach
**Configuration Style**: 
- Likely FluentNHibernate or XML mappings
- Located in Infrastructure.NHibernate project
- Not visible in reviewed code samples

#### Entity Relationships

**From `BusinessPlan`**:
```
BusinessPlan (1) -------- (1..1) ProductIdea
BusinessPlan (1) -------- (0..*) FinancialInvestment
BusinessPlan (1) -------- (0..*) Ownership
BusinessPlan (1) -------- (0..*) CompetitorAnalysis
BusinessPlan (1) -------- (0..*) ManagementTeam
```

**Cascade Behavior**: Implicit / Requires Confirmation
- Likely cascade saves from BusinessPlan to child collections
- Delete behavior not evident

#### Repository Pattern

**Interface**: `IProductIdeaRepository` (from `CreateBusinessPlanCommandHandler`)

**Implied Methods**:
- `Save(ProductIdea idea)` or `SaveBusinessPlan(BusinessPlan plan)`
- `Get(id)` / `GetById(id)`
- Query methods (implicit)

**Responsibilities**:
- Abstract data access
- Encapsulate NHibernate session usage
- Manage aggregate persistence

### Transaction Boundaries

**Inferred Behavior**:
- Command handler scope = transaction scope
- Repository methods participate in ambient transaction
- Likely using NHibernate's ISession transaction management

**Pattern**:
```
Controller → Command → Handler → Repository → Session → Transaction
```

### Read vs Write Models

**Evidence from `IdeaMapperProfile`**:

**Write Model**: Domain entities (`ProductIdea`, `BusinessPlan`)  
**Read Model**: ViewModels (target of AutoMapper)

**Separation Characteristics**:
- UI layer uses ViewModels (DTOs)
- Domain entities not directly exposed to views
- AutoMapper provides transformation layer
- Read models flatten aggregate structure

**Example Transformation**:
```csharp
.ForMember(dest => dest.ProductDevelopmentLeadCompany,
           src => src.MapFrom(m => m.BusinessPlan.ProductDevelopmentLeadCompany))
```
*Flattens nested BusinessPlan property to ViewModel root*

### Data Access Patterns

**Command Operations**:
- Use domain entities directly
- Modify through entity methods
- Persist via repository

**Query Operations** (implicit):
- Retrieve entities via repository
- Transform to ViewModels via AutoMapper
- Optimized projections (requires confirmation)

---

## SECTION 6 — INTEGRATIONS & INFRASTRUCTURE

### External Service Integrations

#### 1. Azure Service Bus (`Infrastructure.AzureMessaging`)
**Purpose**: Asynchronous messaging and event distribution  
**Use Cases** (inferred):
- Collaboration request notifications
- System event propagation
- Background job triggering
- Cross-component communication

**Pattern**: Publish-Subscribe or Queue-based messaging

#### 2. MailGun API (`Infrastructure.Emailer`, `lib/MailGunApi`)
**Purpose**: Transactional email delivery  
**Use Cases**:
- User notifications
- Collaboration invitations
- System alerts
- Welcome emails

**Template System** (from `doc/Email Templates/`):
- PHPMailer-based templates
- HTML email layouts
- Templating engine (likely server-side rendering)

**Template Inventory**:
- `Account Activation.txt` - User account activation flow
- `Welcome Email upon Activation - IdeaAuthor.txt` - Onboarding for idea generators
- `Welcome Email upon Activation - Investor.txt` - Onboarding for investors
- `Welcome Email upon Activation - Mfg.txt` - Onboarding for manufacturing companies
- `Welcome Email upon Activation - R&D.txt` - Onboarding for R&D organizations
- `Welcome Email upon Activation - Sales&Mktg.txt` - Onboarding for sales/marketing companies

**Actor-Specific Onboarding**: Email templates confirm the platform's multi-actor architecture aligns with the innovation ecosystem model.

#### 3. Database: SQL Server
**Access Pattern**: NHibernate ORM  
**Schema Management**: `src/Database` project  
**Initialization**: `src/SchemaBuilder` utility

### Configuration-Driven Behavior

**Environment-Specific Configurations** (from project files):

**Identified Profiles**:
- `MahmudPC-debug` - Personal development environment
- `WajahatPC-Debug` - Personal development environment
- `apphb.release` - AppHarbor deployment (cloud hosting platform)

**Configuration Sources** (implicit):
- Web.config transformations (standard ASP.NET pattern)
- Connection strings per environment
- Service endpoint URLs
- API keys (Azure, MailGun)

**Risk**: Hard-coded personal environment configurations suggest:
- Lack of standardized local development setup
- Potential configuration drift
- Manual environment management

### API Usage Patterns

**Inbound**: 
- HTTP/MVC (web UI)
- Implicit API endpoints (requires confirmation)

**Outbound**:
- MailGun REST API
- Azure Service Bus SDK
- Potential third-party integrations (not visible)

### Background Jobs & Async Processing

**Evidence**: Azure Service Bus integration suggests:
- Message-driven background processing
- Asynchronous workflows
- Event-driven architecture components

**Job Types** (inferred):
- Email delivery (queued)
- Notification distribution
- Data synchronization (potential)

**Technology**: Implicit / Requires Confirmation
- Could be Azure WebJobs
- Could be in-process background threads
- Could be separate worker role

---

## SECTION 7 — SECURITY & ACCESS CONTROL

### Authentication Mechanisms
**Status**: ASP.NET Forms Authentication  
**Implementation**: `IFormsAuthenticationService` wrapper

**Evidence**: Confirmed in`AccountController` analysis  
**Configuration**: Web.config based

**User Identification**: Email address as username/principal identity

### Authorization Model

**Actor-Based Authorization**:
- **Idea Generators** (`IdeaAuthor`): Full control over own innovations, partner selection, business plan
- **R&D Organizations** (`DomainExpert`): Submit R&D proposals, view limited innovation details
- **Manufacturing Companies** (`Manufacturer`): Submit manufacturing proposals, cost modeling
- **Sales & Marketing Companies** (`SalesMarketing`): Submit market strategy proposals
- **Investors** (`Investor`): Submit investment proposals, review business plans
- **Administrators**: Platform management (implicit)

**Custom Authorization Filters**:
- `[IdeaAuthorsOnly]` - Restricts to idea generators
- `[ManufacturerOnly]`, `[SalesMarketingOnly]`, `[DomainExpertOnly]` - Actor-specific access
- Manual ownership checks in controller actions

### Platform Mode Access Control (Implicit / Requires Confirmation)

**Open Innovation Mode**:
- Public innovation discovery
- Any registered actor can view and participate
- `LimitedView` protects IP while allowing evaluation
- Full details visible only to idea generator

**Closed Innovation Mode** (requires confirmation):
- Organization-scoped innovation visibility
- Internal actor participation only
- Organization-level access control
- Code evidence: Not directly visible in reviewed code

**Hybrid Innovation Mode** (requires confirmation):
- Selective external actor participation
- Controlled information disclosure
- Organization approves external collaboration
- Code evidence: `LimitedView` pattern supports this model

### Intellectual Property Protection

**Sensitive Information**:
- Full innovation details (technical specifications)
- Research background and methodology
- Competitive analysis
- Financial projections and ownership structure
- Business plan details

**Access Patterns**:
- **Full View** (`Details` action): Idea generator only
- **Limited View** (`LimitedView` action): Prospective actors for evaluation
- **Dashboard View** (`Dashboard` action): Idea generator's management interface
- **Business Plan**: Restricted visibility (requires confirmation of access rules)

### Authorization Rules

**Innovation Ownership**:
- Idea generators own their innovations
- Only owners can modify innovation details
- Only owners can select collaboration partners
- Owner controls business plan development

**Actor Participation Rules**:
- Actors submit proposals based on their role type
- One proposal per actor per innovation
- Cannot bid on own innovations (implicit)
- Bid eligibility checked via `IsLoggedInUserAllowedToPlaceBid()`

**Collaboration Access**:
- Selected partners gain access to virtual incubator workspace (implicit / requires confirmation)
- Rejected bidders maintain limited view only
- Partner selection is irreversible

**Platform Mode Authorization** (implicit / requires confirmation):
- Open: Authorization based on actor role
- Closed: Additional organization membership requirement
- Hybrid: Idea generator controls external actor access

**Code Pattern**:
```csharp
// Declarative authorization
[Authorize]
[IdeaAuthorsOnly]
public ActionResult Dashboard(Guid id) { }

// Imperative ownership check
if (productIdea.IdeaAuthor.EmailAddress != HttpContext.User.Identity.Name)
    return new RedirectResult("/error/accessdenied", true);
```

### Innovation Actor Onboarding

**From Email Templates**:
The system implements actor-specific onboarding aligned with the platform's innovation ecosystem:
- **IdeaAuthor** → Idea Generators: Research-based innovation submitters
- **Investor** → Financial backers and commercialization funders
- **Mfg (Manufacturing)** → Manufacturing Companies: Production and distribution partners
- **R&D** → R&D Organizations: Technical development and research partners
- **Sales&Mktg** → Sales and Marketing Companies: Market strategy and commercialization experts

**Platform Mode Implications**:
- Open Innovation: All actors can discover and participate in public ideas
- Closed Innovation: Organization-restricted actor participation (implicit / requires confirmation)
- Hybrid Innovation: Mixed public and restricted participation (implicit / requires confirmation)

### Sensitive Operations

**From Domain Model**:
1. **BusinessPlan Creation/Modification**
   - Financial data exposure
   - Competitive intelligence protection
   - Ownership information sensitivity

2. **Collaboration Requests**
   - Access control to idea details
   - Contact information protection

3. **User Communication**
   - Message privacy
   - Email address protection

### Data Protection

**Financial Information**:
- `EstimatedCapitalNeed`
- `ExpectedAnnualRevenue`
- `FinancialInvestments`
- `Ownerships`

**Business-Critical Information**:
- `CompetitorAnalyses`
- `ManagementTeam` composition
- Product development strategies

**Access Control Requirements** (implicit):
- Authors access their own ideas
- Contributors see published ideas only
- Business plans visible based on disclosure settings

### Security Patterns

**Implicit / Requires Confirmation**:
- CSRF protection (ASP.NET MVC default)
- SQL injection protection (NHibernate parameterization)
- XSS protection (Razor encoding)
- Authentication cookies (Forms Auth)

**Gaps / Risks**:
- jQuery 1.6.1 has known vulnerabilities
- No evidence of API authentication for external calls
- Personal configuration profiles suggest inconsistent security practices

---

## SECTION 8 — TECHNICAL CONSTRAINTS & ASSUMPTIONS

### Hard Dependencies

#### Runtime Requirements
- .NET Framework 4.0
- IIS or compatible web server
- SQL Server (version unspecified)
- Azure subscription (for Service Bus)

#### Library Dependencies
- **NHibernate 3.2**: Deep integration throughout data layer
  - Constraint: Migration to another ORM requires significant refactoring
  - All repository implementations tied to NHibernate session management

- **MvcContrib**: Command processing framework
  - Constraint: Framework appears abandoned (circa 2011)
  - Command pattern implementation depends on this library

- **jQuery 1.6.1**: Client-side interaction
  - Constraint: Security vulnerabilities, incompatible with modern plugins
  - Handlebars.js integration tied to this version

- **AutoMapper**: DTO transformation layer
  - Constraint: Version-specific configuration in UI project
  - All ViewModels depend on configured mappings

### Architectural Assumptions

#### 1. Single-Database Design
**Assumption**: All platform data stored in single SQL Server database  
**Impact**: 
- No data partitioning by organization (closed/hybrid mode implications)
- Vertical scaling only
- No polyglot persistence
- Cross-platform mode data sharing

#### 2. Open Innovation as Primary Mode
**Assumption**: Platform primarily designed for open innovation model  
**Evidence**: 
- Public discovery workflows predominant
- No multi-tenancy patterns visible
- Organization scoping not evident in reviewed code
**Impact**:
- Closed/hybrid mode support may require significant architectural changes
- Organization-level access control needs investigation
- Multi-tenant data isolation unclear

#### 3. Synchronous UI Layer
**Assumption**: Controllers execute synchronously  
**Evidence**: No async/await patterns visible  
**Impact**:
- Thread pool blocking on long operations
- Limited scalability under high actor concurrency
- Virtual incubator collaboration may face performance issues

#### 4. Session-Per-Request Pattern
**Assumption**: NHibernate session lifecycle matches HTTP request  
**Impact**:
- Lazy loading confined to request boundary
- Session management complexity
- Potential N+1 query issues in actor-innovation matching

#### 5. Rich Domain Model
**Assumption**: Business logic resides in domain entities  
**Evidence**: `ProductIdea` entity with completeness checks, lifecycle methods  
**Impact**:
- Entities must be loaded for business operations
- Cannot use anemic DTOs for commands
- Change tracking overhead for innovation updates

#### 6. Message-Based Integration
**Assumption**: Azure Service Bus for asynchronous operations  
**Use Cases**: Actor notifications, collaboration events
**Impact**:
- Eventual consistency model
- Requires message handlers outside web app
- Dependency on Azure-specific SDK

#### 7. Research Category as Metadata (Implicit)
**Assumption**: Innovation research type (Management/Engineering/Natural Science) stored but not prominently used  
**Evidence**: Not visible in reviewed entity properties or validation
**Impact**: Research-based categorization may be planned feature or stored differently

### Coupling Points

#### 1. UI → Domain Coupling
**Mechanism**: AutoMapper profiles in UI project  
**Issue**: UI project references Core project  
**Impact**: Changes to domain entities require UI ViewModel updates

#### 2. Infrastructure → Domain Coupling
**Mechanism**: NHibernate mappings for domain entities  
**Issue**: ORM concerns leak into domain design  
**Impact**: Entity structure constrained by ORM capabilities

#### 3. External Service Coupling
**MailGun SDK**: Concrete implementation in Infrastructure.Emailer  
**Azure SDK**: Concrete implementation in Infrastructure.AzureMessaging  
**Impact**: Vendor lock-in, difficult to swap providers

#### 4. Configuration Coupling
**Personal Profiles**: Hard-coded developer machine configurations  
**Impact**: 
- Non-portable development environment
- Manual setup required for new developers
- Configuration drift between environments

### Technology Constraints

#### .NET Framework 4.0 Limitations
- No async/await (added in 4.5)
- Limited LINQ performance
- No HttpClient (added in 4.5)
- No Roslyn compiler features

#### NHibernate 3.2 Limitations
- No async query support
- Second-level cache complexity
- Verbose configuration
- Limited LINQ provider

#### ASP.NET MVC (pre-Core) Constraints
- System.Web dependency (IIS-only hosting)
- Global.asax lifecycle
- No built-in dependency injection
- Limited middleware extensibility

### Known Technical Debt

#### 1. Outdated Dependencies
**Risk Level**: High  
**Items**:
- jQuery 1.6.1 (2011) - 15 years of security patches missed
- NHibernate 3.2 (2011) - performance and feature gaps
- .NET Framework 4.0 (2010) - unsupported by Microsoft

#### 2. Personal Configuration Profiles
**Risk Level**: Medium  
**Items**:
- `MahmudPC-debug`, `WajahatPC-Debug` configurations
- Hardcoded to individual developer machines
- No standardized local development story

#### 3. Code Analysis Suppressions
**Risk Level**: Medium  
**Evidence**: Multiple projects have Code Analysis enabled but with suppressions  
**Impact**: Unknown code quality issues masked

#### 4. Lack of Async Patterns
**Risk Level**: Medium  
**Impact**: Scalability limitations, thread pool exhaustion risk

---

## SECTION 9 — RISK & AMBIGUITY REGISTER

### Platform Mode Implementation Gaps ⚠️ CRITICAL

**Authoritative Requirement**: Innoventity supports Open, Closed, and Hybrid innovation models.

**Current Code Evidence**: 
- Primary implementation appears focused on **Open Innovation** workflows
- No multi-tenancy patterns visible in reviewed code
- No organization-scoped data access visible
- No organization entity or organizational hierarchy

**Specific Gaps**:

#### Closed Innovation Mode
**Required**: Organization-internal innovation workflows with restricted actor participation  
**Not Found**:
- Organization entity or organizational hierarchy
- Multi-tenant data isolation
- Organization-scoped actor registration
- Internal-only innovation visibility flags
- Organization administrator roles

**Partial Evidence**:
- `CorporateIdeaAuthor` entity suggests corporate innovation support
- May exist in unreviewed code or configuration

#### Hybrid Innovation Mode
**Required**: Mixed open and closed participation with selective disclosure  
**Partial Evidence**:
- `LimitedView` action provides IP protection pattern
- Could support hybrid model's controlled information disclosure

**Not Found**:
- Explicit hybrid mode flag or configuration
- Organization approval workflows for external actors
- Selective actor whitelisting

#### Government/Technology Park Support
**Required**: Government and technology park actors for closed/hybrid models  
**Not Found**:
- Government actor entity or role
- Technology park entity
- Policy support or incentive mechanisms

**Recommendation**: Requires stakeholder validation to determine if closed/hybrid modes are implemented, planned, or out of scope for current implementation.

### Research Category Classification ⚠️ REQUIRES VALIDATION

**Authoritative Requirement**: Innovation ideas fall into three categories:
- Management research-based innovations
- Engineering research-based innovations
- Natural science research-based innovations

**Current Code Evidence**:
- `IdeaSummary.IdeaResearchType` field exists (string type)
- No enumeration or validation of research categories
- No filtering or discovery by research type visible
- Research background field captured but category classification unclear

**Not Found**:
- Research category enumeration (Management/Engineering/Natural Science)
- Research category validation rules
- Search/filter by research type
- Research-specific workflows or requirements

**Questions**:
- Is research type free-text or controlled vocabulary?
- Are there different workflows per research category?
- Do certain actors specialize in specific research types?
- Is this a planned feature or existing but not in reviewed code?

**Recommendation**: Validate whether research categorization is implemented, how it's used, and its importance for actor matching.

### Areas Lacking Clarity

#### 1. Authentication & Authorization Implementation ✅ CLARIFIED
**Current Understanding**: ASP.NET Forms Authentication with custom authorization filters
**Confirmed Details**:
- User registration flow: `AccountController.Register(actor)` with actor-specific types
- Password security: Salted hashing via `ICryptographer` interface
- Session management: `LoggedInUser` object stored in Session
- Authorization: Custom attributes (`[IdeaAuthorsOnly]`, `[ManufacturerOnly]`, etc.)
- Email-based authentication (email = username)

**Member Status Flow**:
1. Registration → `MemberStatusLevel.PendingActivation`
2. Email activation → `MemberStatusLevel.Active`
3. Suspension possible → `MemberStatusLevel.Suspended`

**Remaining Unknowns**:
- Password complexity requirements
- Session timeout configuration
- Password reset mechanism implementation
- Account lockout policies

#### 2. ProductIdea Lifecycle State Machine ✅ PARTIALLY CLARIFIED
**Current Understanding**: Implicit state machine based on completeness checks  
**Identified State Transitions**:
1. **Draft** → Idea created (`CreatedOn` set)
2. **Summary Complete** → `IsIdeaSummaryComplete() == true`
3. **Product Details Complete** → `IsProductDetailsComplete() == true`
4. **Market Details Complete** → `IsMarketDetailsSectionComplete() == true`
5. **Submitted/Published** → `SubmittedOn` field populated
6. **Receiving Bids** → `FormalIdeaResponses` collection populated
7. **Minimum Bids Met** → `HasReceivedEnoughOfFormalResponses() == true`
8. **Partners Selected** → `HasCollaborationPartnerSelectionCompleted() == true`
9. **Business Plan Added** → `HasBusinessPlan() == true`

**Remaining Unknowns**:
- Explicit state enumeration (if exists)
- State transition authorization rules
- Reversibility of states
- Archival/closure states

#### 3. Collaboration Mechanism Details ✅ CLARIFIED
**Current Understanding**: Formal bid-based collaboration system with partner selection  
**Workflow Documented**:
1. **Idea Author** publishes idea with collaboration requirements
2. **Contributors** view limited idea information
3. **Contributors** submit `FormalResponse` (bids) specific to their role:
   - `ManufacturingResponse` - Production costs, volumes, distribution
   - `SalesMarketingResponse` - Market strategy, channels
   - `ResearchDevelopmentResponse` - Technical development approach
   - `InvestorResponse` - Investment terms
4. **Idea Author** reviews bids via `ViewBids` action
5. **Idea Author** selects partners via `SelectCollaborationPartnerCommandHandler`
6. Selected bids marked `Accepted = true`, `AcceptedOn` timestamp set
7. **Notification** sent via Azure Service Bus (implicit)

**Collaboration Types** (from `CollaborationRequirement` entity):
- Domain Expert (R&D) collaboration
- Sales & Marketing collaboration
- Manufacturing collaboration
- Investor collaboration

**Access Control**:
- Non-authors see `LimitedView` only
- Contributors can only bid once per idea
- Bid viewing restricted based on ownership
- Partner selection cannot be repeated once completed

**Remaining Unknowns**:
- Communication channel after partner selection
- Collaboration workspace features
- Equity/compensation negotiation process
- Collaboration termination workflow

#### 4. Query Side Architecture ✅ CLARIFIED
**Current Understanding**: Domain entities retrieved via repositories, transformed to ViewModels  
**Pattern Confirmed**:
- Controllers query via repository interfaces
- Repositories return domain entities
- AutoMapper transforms to ViewModels
- ViewModels flatten aggregate structures for display

**Example Flow**:
```
Controller → Repository.GetById(id) → ProductIdea entity
         → AutoMapper.Map<ProductIdea, IdeaPreviewModel>() → ViewModel
         → View(viewModel)
```

**No Separate Read Models**: 
- Single database, single entity model
- No CQRS read-side persistence
- No denormalized query tables
- All queries through NHibernate session

**Optimization Patterns**:
- `[UnitOfWork]` attribute manages session lifecycle
- Lazy loading within request boundary
- View counting via separate `RankingRepository`

**Remaining Unknowns**:
- Eager loading strategies (if any)
- Query result caching implementation
- Search/filtering implementation details
- Performance under load

### Implicit Business Rules

#### 1. BusinessPlan Optionality
**Observation**: `HasBusinessPlan()` check in AutoMapper configuration  
**Implicit Rule**: ProductIdea can exist without BusinessPlan  
**Risk**: Business logic may assume BusinessPlan presence  
**Impact**: Null reference exceptions if not handled consistently

#### 2. Financial Data Validation
**Observation**: Decimal types for financial amounts  
**Implicit Rules**:
- Estimated capital need > 0?
- Expected revenue > 0?
- Ownership percentages sum to 100%?
- Investment amounts match ownership stakes?

**Risk**: Invalid financial data if not validated

#### 3. ManagementTeam Composition
**Observation**: Collection type, member structure unknown  
**Implicit Rules**:
- Minimum team size?
- Required roles (CEO, CTO, etc.)?
- Team member validation rules?

**Risk**: Incomplete or invalid team definitions

#### 4. Competitor Analysis Requirements
**Observation**: Collection of CompetitorAnalysis  
**Implicit Rules**:
- Minimum number of competitors?
- Analysis completeness criteria?
- Competitive advantage definition required?

**Risk**: Shallow competitive analysis accepted

### Hidden Coupling

#### 1. JavaScript → Server-Side Logic
**Issue**: Handlebars.js runtime suggests client-side rendering  
**Risk**: Business logic or validation duplicated in JavaScript  
**Discovery Required**:
- Review JavaScript files in `src/UI/Scripts`
- Identify AJAX endpoints
- Document client-side state management

#### 2. Email Templates → Domain Events
**Issue**: PHPMailer templates in `doc/Email Templates/`  
**Risk**: Email content tightly coupled to domain event structure  
**Discovery Required**:
- Map templates to trigger events
- Identify template data binding
- Document email composition logic

#### 3. UI → Database Schema
**Issue**: AutoMapper configurations may encode schema structure  
**Risk**: Database changes require UI mapper updates  
**Discovery Required**:
- Review all AutoMapper profiles
- Identify direct property mappings
- Document schema dependencies

#### 4. Azure Service Bus → Application Logic
**Issue**: Message contracts not visible  
**Risk**: Message schema changes break handlers  
**Discovery Required**:
- Identify message types
- Document message contracts
- Locate message handlers

### Logic Distribution Across Layers ✅ CLARIFIED

#### 1. Validation Logic
**Confirmed Locations**:
- **Command Handlers**: `ValidationRule<T>` classes (MvcContrib pattern)
  - Example: `EmailAddressMustBeUnique`, `MustAgreeToTermsAndConditions`
  - Executed before command execution
- **Domain Entities**: Business rule methods
  - Example: `IsIdeaSummaryComplete()`, `HasCollaborationPartnerSelectionCompleted()`
- **Controllers**: `ModelState.IsValid` checks for input validation
- **Client-Side**: Likely jQuery validation (requires JavaScript review)

**Validation Pattern**:
```
1. Client-side (jQuery) → Quick feedback
2. Model binding (ASP.NET MVC) → Data type validation
3. ValidationRule<T> (Command Handler) → Business rules
4. Domain entity methods → Invariant enforcement
```

**No Evidence Of**:
- Database constraints validation
- Stored procedure validation
- API-level validation middleware

#### 2. Business Rules Enforcement ✅ DOCUMENTED
**Confirmed Locations**:
- **Domain Entities**: Primary location for business logic
  - `ProductIdea`: Completeness checks, partner selection rules
  - `Member` subclasses: Industry affiliation management
  - `FormalResponse`: Comparison and equality logic
- **Command Handlers**: Workflow orchestration
  - Partner selection validation
  - Registration business rules
- **Controllers**: View-level filtering and authorization
  - Bid eligibility: `IsLoggedInUserAllowedToPlaceBid()`
  - Ownership verification

**No Evidence Of**:
- Stored procedures with business logic
- Database triggers
- Significant client-side business rules (beyond validation)

#### 3. Authorization Checks ✅ DOCUMENTED
**Confirmed Locations**:
- **Controller Attributes**: Declaration-level security
  - `[Authorize]`, `[IdeaAuthorsOnly]`, `[ManufacturerOnly]`
- **Controller Actions**: Manual ownership checks
  - Email address comparison for author verification
  - Custom authorization logic in method bodies
- **Custom Filters**: Role-based authorization attributes

**Pattern**:
```csharp
// Declarative
[Authorize]
[IdeaAuthorsOnly]
public ActionResult Dashboard(Guid id) { }

// Imperative
if (productIdea.IdeaAuthor.EmailAddress != HttpContext.User.Identity.Name)
    return new RedirectResult("/error/accessdenied", true);
```

**Not Found**:
- Domain entity-level authorization
- Repository-level security filtering

#### 4. State Transitions ✅ DOCUMENTED
**Confirmed Locations**:
- **ProductIdea Entity**: State checking methods
  - Completeness predicates determine state
- **Controller Orchestration**: State progression
  - Editing actions populate required fields
  - Submission action sets `SubmittedOn`
- **Command Handlers**: Collaboration state changes
  - `SelectCollaborationPartnerCommandHandler` marks partners as accepted

**State Management Pattern**:
- Implicit state based on data completeness
- No explicit state field
- State derived from presence/absence of data

**Not Centralized**:
- No state machine class
- No explicit transition methods
- State logic distributed across multiple methods

### Critical Discovery Gaps

#### 1. **User Registration & Profile Management** ✅ PARTIALLY DOCUMENTED
**Documented**:
- Member entity structure: `FirstName`, `LastName`, `EmailAddress`, `ContactAddress`
- Registration flows: Actor-specific routes and handlers
- Status levels: `PendingActivation`, `Active`, `Suspended`
- Industry affiliation: `IIndustryAffiliation` interface implementation
- Activation mechanism: Email token-based (currently disabled)

**Remaining Gaps**:
- Profile editing functionality
- Contributor expertise/skill tracking details
- Profile visibility settings
- Member search/discovery for authors
- Reputation or rating system (if any)

#### 2. **Search & Discovery Mechanism** ⚠️ REQUIRES INVESTIGATION
**Known**:
- `IProductIdeaRepository` has commented methods for filtering:
  - Get by status
  - Get by subsector
  - Get by author
- Industry-based member filtering via `IIndustryAffiliation`

**Unknown**:
- Full-text search implementation
- Idea browsing/listing pages
- Filtering UI and criteria
- Contributor matching algorithm
- Recommendation system (if any)

**Discovery Required**:
- Search controller actions
- JavaScript search implementation
- Database indexes for search
- Subsector/Industry filtering logic

#### 3. **Notification System Architecture** ✅ PARTIALLY DOCUMENTED
**Documented**:
- Email notifications via `IEmailUtility` and MailGun
- Azure Service Bus for asynchronous messaging
- Template-based emails for different user types
- `IdeaCommunication` entity tracks shared ideas with read status

**Email Triggers Identified**:
- Account activation (disabled in code)
- Welcome emails by role
- Idea sharing via email

**Remaining Gaps**:
- In-app notification system (if exists)
- Notification preferences and settings
- Azure Service Bus message contracts
- Message handler implementations
- Real-time notifications (SignalR?)
- Notification persistence and history

#### 4. **Performance Optimization** ⚠️ MINIMAL EVIDENCE
**Observed**:
- NHibernate session-per-request pattern
- `[UnitOfWork]` attribute for transaction management
- View counter writes on every view (potential bottleneck)
- AutoMapper for DTO transformation

**Concerns Identified**:
- No explicit caching strategy
- No eager loading configuration visible
- Lazy loading within request scope (N+1 risk)
- Collection filtering in memory (`FormalIdeaResponses.OfType<T>()`)
- View counters perform DB write per view

**Unknown**:
- Output caching configuration
- Second-level cache (NHibernate)
- Query optimization techniques
- Large dataset pagination
- Database indexing strategy

**Discovery Required**:
- NHibernate mapping files for fetch strategies
- Web.config caching settings
- Database schema and indexes

#### 5. **Error Handling Patterns** ✅ DOCUMENTED
**Global Handler**: `MvcApplication_Error` in Global.asax
**Logging**: log4net via `ILogger` interface
**Filter**: `[HandleError]` attribute on controllers
**Error Pages**: `ErrorController` for user-facing errors

**Error Flow**:
```
Exception → [HandleError] filter → Global.asax Error event
         → ILogger.Error() → log4net → Error page/redirect
```

**Logged Information**:
- Stack trace (via `Server.GetLastError()`)
- Formatted log output (via `ToLogFormattedString()`)

**Remaining Gaps**:
- Error page content and design
- User-friendly error messages
- Retry logic for transient failures
- Circuit breaker patterns
- Alerting/monitoring integration

---

## SECTION 10 — CONTROLLER & REPOSITORY ANALYSIS

**Status**: COMPLETED  
**Last Updated**: February 7, 2026

### Controllers Inventory

#### 1. ProductIdeaController (`src/UI/Controllers/Product/ProductIdeaController.cs`)
**Primary Responsibility**: Comprehensive idea lifecycle management

**Dependencies Injected**:
- `IMemberRepository` - User/member data access
- `IProductIdeaRepository` - Idea persistence
- `IIndustryRepository` - Industry/sector data
- `IRankingRepository` - View/engagement tracking
- `IEmailUtility` - Email notifications
- `IProjectValuationService` - Financial valuation calculations
- `IAzureSubscriptionClientManager` - Azure Service Bus messaging
- `ISubscriptionFactory` - Message subscription factory

**Key Action Methods** (20+ identified):

**Idea Viewing**:
- `Dashboard(Guid id)` - Idea author's dashboard view
- `Details(Guid id)` - Full idea details (author only)
- `LimitedView(Guid id)` - Restricted view for non-authors
- `Print(Guid id)` - Printer-friendly version
- `EmailIdea(Guid id, string emailAddress)` - Email idea to others

**Idea Creation & Editing**:
- `EditIdeaSummary(Guid id)` - Edit idea summary section
- `EditIdeaToProductConversion(Guid id)` - Edit product details
- `EditProductToMarket(Guid id)` - Edit market information
- `EditDomainExpertCollaborationRequirement(Guid id)` - R&D collaboration needs
- `EditSalesMarketingCollaborationRequirement(Guid id)` - Sales/marketing needs
- `EditManufacturingCollaborationRequirement(Guid id)` - Manufacturing needs
- `EditInvestorCollaborationRequirement(Guid id)` - Investment needs

**Formal Responses (Bids)**:
- `ViewBids(Guid id)` - View all bids on an idea
- `ViewBid(Guid id)` - View specific bid details
- `EditFormalManufacturingResponse(Guid id)` - Manufacturing partner bid
- `EditFormalSalesMarketingResponse(Guid id)` - Sales/marketing partner bid
- `EditFormalResearchDevelopmentReponse(Guid id)` - R&D partner bid
- `EditFormalInvestorResponse(Guid id)` - Investor bid

**Collaboration & Business Planning**:
- `SelectCollabortionPartners(Guid id)` - Choose collaboration partners
- `CreateManagementTeam(Guid id)` - Build management team
- `CreateBusinessPlan(Guid id)` - Create business plan
- `ViewBusinessPlan(Guid id)` - View business plan
- `ViewProjectValuation(Guid id)` - Calculate project valuation

**Authorization Patterns**:
- `[Authorize]` - Requires authenticated user
- `[IdeaAuthorsOnly]` - Custom attribute for idea authors
- `[ManufacturerOnly]`, `[SalesMarketingOnly]`, etc. - Role-specific attributes
- `[ValidateAntiForgeryToken]` - CSRF protection on POST actions
- `[UnitOfWork]` - NHibernate session management attribute

**Key Behaviors Identified**:
- View counting via `_rankingRepository.RegisterCounter()`
- Read status tracking via `MarkIdeaCommunicationAsRead()`
- Bid eligibility checking via `IsLoggedInUserAllowedToPlaceBid()`
- AutoMapper for ViewModel transformations
- Azure Service Bus for asynchronous notifications

#### 2. AccountController (`src/UI/Controllers/Account/AccountController.cs`)
**Primary Responsibility**: User authentication and registration

**Dependencies Injected**:
- `IMemberRepository` - Member data access
- `ICountryRepository` - Country data for registration
- `IMembershipService` - Password validation service
- `IFormsAuthenticationService` - Forms authentication wrapper
- `IEmailUtility` - Account activation emails

**Key Action Methods**:
- `LogOn()` - Display login form
- `LogOn(LogOnModel model, string returnUrl)` - Process login
- `Register(string actor)` - Registration form for different user types
- `RegisterInnovator()` - Special registration for idea authors
- Account activation handling

**Member Status Handling**:
- `MemberStatusLevel.PendingActivation` - Awaiting email confirmation
- `MemberStatusLevel.Suspended` - Account suspended
- `MemberStatusLevel.Active` - Fully activated account

**Registration Actor Types** (from command handler):
- "investor" → `Investor` entity
- "salesmarketing" → `SalesMarketing` entity
- "manufacturer" → `Manufacturer` entity
- "productdeveloper" → `DomainExpert` entity

**Session Management**:
- Stores `LoggedInUser` object in session
- Tracks `LastLoggedIn` timestamp

#### 3. HomeController (`src/UI/Controllers/HomeController.cs`)
**Primary Responsibility**: Landing page and dashboard (inferred)

#### 4. ErrorController (`src/UI/Controllers/Error/ErrorController.cs`)
**Primary Responsibility**: Error page rendering

#### 5. CommandController (`src/UI/Controllers/CommandController.cs`)
**Primary Responsibility**: Base controller with CQRS command execution support
- Provides `DbSession` access (NHibernate ISession)
- AutoMapper view helpers
- Base functionality for all controllers

### Domain Entity Hierarchy

#### Member Entity Hierarchy
```
Member (abstract base)
├── IdeaAuthor (abstract)
│   └── CorporateIdeaAuthor (concrete)
└── INonIdeaAuthor (interface)
    ├── DomainExpert (R&D contributor)
    ├── SalesMarketing (sales/marketing contributor)
    ├── Manufacturer (manufacturing partner)
    └── Investor (financial backer)
```

**Member Base Properties**:
- `Id` (Guid)
- `FirstName`, `LastName`, `EmailAddress`
- `PasswordHash`, `Salt` - Security credentials
- `MemberSince` (DateTime)
- `ContactAddress` (Address value object)
- `Status` (MemberStatusLevel enum)
- `ActiveSince` (DateTime?)
- `ActivationToken` (Guid) - Email verification

**IIndustryAffiliation Interface**:
Implemented by all non-author member types
- `AffiliatedTo` (IList<Industry>) - Industries of expertise
- `AddNew(Industry)`, `Exists(Industry)`, `Remove(Industry)` - Industry management

**INonIdeaAuthor Interface**:
- `IdeasReceived` (IList<IdeaCommunication>) - Ideas shared with them

#### ProductIdea Entity (Aggregate Root)

**Core Properties**:
- `Id` (Guid)
- `CreatedOn` (DateTime)
- `SubmittedOn` (DateTime?)
- `IdeaAuthor` (IdeaAuthor reference)
- `IdeaToken` (Guid) - Unique public identifier

**Aggregate Components**:
- `IdeaSummary` - Title, product type, IPR status, research background
- `Product` - Description, advantages, development phase, keywords
- `Market` - Target customers, market description, target industries
- `CollaborationRequirement` - Flags for needed collaborator types
- `BusinessPlan` - Financial planning (optional, 1:0..1 relationship)

**Collections**:
- `Comments` (IList<Comment>) - Public comments
- `FormalIdeaResponses` (IList<FormalResponse>) - Collaboration bids
- `IdeaCommunications` (IList<IdeaCommunication>) - Shared idea notifications

**Key Business Methods**:
- `HasBusinessPlan()` - Checks if business plan exists
- `IsIdeaSummaryComplete()` - Validates idea summary completeness
- `IsProductDetailsComplete()` - Validates product section
- `IsMarketDetailsSectionComplete()` - Validates market section
- `HasCollaborationPartnerSelectionCompleted()` - Checks if all partners selected
- `HasReceivedEnoughOfFormalResponses()` - Checks minimum bid threshold
- `GetSelectedPartner<T>()` - Retrieves accepted partner of specific type
- `FindFormalResponseBy<T>(Member)` - Finds member's bid
- `RemoveFromFormalResponses(Type, Guid)` - Removes a bid

**State Machine** (implicit):
Based on method names and workflow:
1. **Draft** - Idea created, sections being completed
2. **Summary Complete** - `IsIdeaSummaryComplete() == true`
3. **Product Complete** - `IsProductDetailsComplete() == true`
4. **Market Complete** - `IsMarketDetailsSectionComplete() == true`
5. **Published** - Submitted for collaboration
6. **Receiving Bids** - `FormalIdeaResponses` being collected
7. **Partners Selected** - `HasCollaborationPartnerSelectionCompleted() == true`
8. **Business Plan Created** - `HasBusinessPlan() == true`

#### FormalResponse Entity Hierarchy (Collaboration Bids)
```
FormalResponse (abstract base)
├── ManufacturingResponse
├── SalesMarketingResponse
├── ResearchDevelopmentResponse
└── InvestorResponse
```

**Base Properties**:
- `Id` (Guid)
- `PostedBy` (Member) - Bidder reference
- `ForIdea` (ProductIdea) - Idea being bid on
- `Location` (string) - Geographic region
- `ParticipationType` (string) - Type of participation
- `ParticipationProposal` (string) - Bid details
- `PostedOn` (DateTime)
- `Accepted` (bool) - Whether idea author accepted bid
- `AcceptedOn` (DateTime?)

**Specialized Response Types**:
- `ManufacturingResponse` - Multi-year cost projections, production volumes
- `SalesMarketingResponse` - Market strategy, distribution plans
- `ResearchDevelopmentResponse` - Technical development approach
- `InvestorResponse` - Investment terms and amounts

### Repository Implementations

#### Repository Pattern Structure

**Base Interface**: `IRepository<T>`
- Generic CRUD operations
- Located in `Core` project (abstraction)

**Base Implementation**: `RepositoryBase<T>` 
- Located in `Infrastructure.NHibernate`
- Takes `ISession` (NHibernate) in constructor
- Provides base Save/Delete/Get operations

**Concrete Repositories**:

#### 1. ProductIdeaRepository
**Interface**: `IProductIdeaRepository : IRepository<ProductIdea>`
**Location**: `src/Infrastructure.NHibernate/DataAccess/Bases/ProductIdeaRepository.cs`

**Methods**:
- `GetByToken(Guid token)` - Retrieve idea by public token
- Standard CRUD from base (Save, Delete, GetById)

**Query Patterns** (commented in interface):
- Get by status
- Get by subsector
- Get by author

#### 2. MemberRepository
**Interface**: `IMemberRepository : IRepository<Member>`
**Location**: `src/Infrastructure.NHibernate/DataAccess/Bases/MemberRepository.cs`

**Methods**:
- `GetByEmail(string email)` - Primary lookup method
- `GetByActivationCode(Guid activationToken)` - Email verification
- `IsMemberExistsBy(string email)` - Duplicate check
- `SetPassword(string email, string passwordHash)` - Password reset

#### 3. InterestRepository
**Interface**: `IInterestRepository`
**Location**: `src/Infrastructure.NHibernate/DataAccess/Bases/InterestRepository.cs`
**Purpose**: Tracking user interests and preferences (implicit)

#### 4. IndustryRepository
**Interface**: `IIndustryRepository`
**Location**: `src/Infrastructure.NHibernate/DataAccess/Impl/IndustryRepository.cs`
**Purpose**: Industry/sector master data

#### 5. CountryRepository
**Interface**: `ICountryRepository`
**Location**: `src/Infrastructure.NHibernate/DataAccess/Impl/CountryRepository.cs`
**Purpose**: Country master data for registration

#### 6. RankingRepository
**Interface**: `IRankingRepository`
**Purpose**: View counters and engagement metrics
**Key Method**: `RegisterCounter(ProductIdea, Member, CounterType)` - Track views

**CounterType** enum:
- `View` - Idea view tracking

### CQRS Command Handlers

#### 1. RegisterNewMemberCommandHandler
**Command**: `SaveNewMember`
**Responsibilities**:
- Create member based on actor type
- Hash password with salt
- Set activation token
- Set status to `PendingActivation`
- Send activation email (commented out)

**Validation Rules**:
- `EmailAddressMustBeUnique` - Check via `IsMemberExistsBy()`
- `MustAgreeToTermsAndConditions` - Require acceptance

**Actor Type Mapping**:
- "investor" → `Investor` entity
- "salesmarketing" → `SalesMarketing` entity
- "manufacturer" → `Manufacturer` entity
- "productdeveloper" → `DomainExpert` entity

#### 2. RegisterNewInnovatorCommandHandler
**Command**: `SaveNewInnovator` (implicit)
**Purpose**: Register idea author members

#### 3. CreateBusinessPlanCommandHandler
**Command**: `SaveBusinessPlan`
**Responsibilities**:
- Create/update `BusinessPlan` aggregate
- Validate financial data
- Link to `ProductIdea`
- Persist via `IProductIdeaRepository`

**Return**: `ReturnValue` (success/failure indicator)

#### 4. SelectCollaborationPartnerCommandHandler
**Command**: `SaveCollaborationPartnerSelection`
**Responsibilities**:
- Mark selected bids as `Accepted = true`
- Set `AcceptedOn` date
- Support selecting from multiple bid types simultaneously

**Validation Rules**:
- `MustNotHaveCollabSelectionProcessCompleted` - Prevent re-selection
- `MustSelectBidsFromAllCollabTypes` - Require one of each type:
  - ManufacturingResponse
  - SalesMarketingResponse
  - ResearchDevelopmentResponse

**Selection Process**:
- Iterates through `FormalIdeaResponses` collection
- Marks matching IDs as accepted
- Saves via `IProductIdeaRepository`

#### 5. SaveManagementTeamCommandHandler
**Command**: `SaveManagementTeam` (implicit)
**Purpose**: Define management team composition

### Route Mappings

**Base Route Configuration**: `RouteConfigurator.cs`

**Account Routes**:
```
/account/register/investor          → AccountController.Register(actor="investor")
/account/register/salesmarketing    → AccountController.Register(actor="salesmarketing")
/account/register/manufacturer      → AccountController.Register(actor="manufacturer")
/account/register/productdeveloper  → AccountController.Register(actor="productdeveloper")
/account/register/ideaauthor        → AccountController.RegisterInnovator()
```

**ProductIdea Routes**:
```
# Viewing
/productidea/{id}/dashboard         → ProductIdeaController.Dashboard
/productidea/{id}/view              → ProductIdeaController.Details (author only)
/productidea/{id}/limited-view      → ProductIdeaController.LimitedView
/productidea/{id}/print             → ProductIdeaController.Print
/productidea/{id}/bids              → ProductIdeaController.ViewBids
/productidea/{id}/comments          → ProductIdeaController.ViewComments

# Editing Idea Sections
/productidea/{id}/summary                                    → EditIdeaSummary
/productidea/{id}/idea-to-product-conversion                 → EditIdeaToProductConversion
/productidea/{id}/product-to-market                          → EditProductToMarket
/productidea/{id}/collaboration-requirements-domainexpert    → EditDomainExpertCollaborationRequirement
/productidea/{id}/collaboration-requirements-salesmarketing  → EditSalesMarketingCollaborationRequirement
/productidea/{id}/collaboration-requirements-manufacturing   → EditManufacturingCollaborationRequirement
/productidea/{id}/collaboration-requirements-investor        → EditInvestorCollaborationRequirement
/productidea/{id}/preview                                    → Preview

# Business Planning
/productidea/{id}/businessplan/create  → CreateBusinessPlan
/productidea/{id}/businessplan/view    → ViewBusinessPlan
/productidea/{id}/ViewProjectValuation → ViewProjectValuation
/productidea/{id}/CreateManagementTeam → CreateManagementTeam

# Collaboration
/productidea/{id}/post-formalresponse       → FormalResponse (create bid)
/productidea/{id}/SelectCollabortionPartners → SelectCollabortionPartners
```

**Default Routes**:
```
/                                   → HomeController.Index
/{controller}/{action}/{id}         → Standard MVC pattern
```

### Data Access Patterns Observed

#### Session Management
- `DbSession` property exposed on `CommandController`
- NHibernate `ISession` injected per-request
- `[UnitOfWork]` attribute manages transaction boundaries
- Session cleanup via `ObjectFactory.ReleaseAndDisposeAllHttpScopedObjects()` in `Global.asax.cs`

#### Query Patterns
**Direct Session Queries**:
```csharp
var query = Session.CreateQuery("from ProductIdea m where m.IdeaToken = :token");
query.SetGuid("token", token);
return query.UniqueResult<ProductIdea>();
```

**Repository Pattern**:
```csharp
var productIdea = _productIdeaRepository.GetById(id);
var member = _memberRepository.GetByEmail(email);
```

#### Lazy Loading Evidence
- Collections (`FormalIdeaResponses`, `IdeaCommunications`) accessed within request scope
- `[UnitOfWork]` attribute ensures session remains open during view rendering

#### N+1 Query Risk Areas
- `productIdea.IdeaCommunications.FirstOrDefault()` - Potential lazy load
- `FormalIdeaResponses.OfType<T>()` - Collection filtering
- `Market.TargetIndustry` - Industry collection access

### AJAX/JSON Endpoints

**Identified AJAX Endpoints**:
- `EmailIdea(Guid id, string emailAddress)` - Returns `JsonResult`
  - Sends idea via email
  - Returns: `{status: "done"}`
  - Behavior: `JsonRequestBehavior.AllowGet`

**Client-Side JavaScript**:
- jQuery 1.6.1 for AJAX calls
- Handlebars.js for client-side templating
- CKEditor for rich text editing
- Likely additional AJAX endpoints for:
  - Industry selection
  - Comment posting
  - Real-time validation

### Cross-Cutting Concerns Implementation

#### Authentication
**Mechanism**: ASP.NET Forms Authentication
**Service**: `IFormsAuthenticationService`
**Method**: `SignIn(email, persistCookie)`
**Session Storage**: `LoggedInUser` object in `Session`

#### Authorization
**Attributes Used**:
- `[Authorize]` - Standard ASP.NET MVC
- `[IdeaAuthorsOnly]` - Custom authorization filter
- `[ManufacturerOnly]`, `[SalesMarketingOnly]`, `[DomainExpertOnly]` - Role-specific

**Manual Checks**:
```csharp
if (productIdea.IdeaAuthor.EmailAddress != HttpContext.User.Identity.Name)
    return new RedirectResult("/error/accessdenied", true);
```

#### Validation
**Levels**:
1. **Model Validation** - `ModelState.IsValid` in controllers
2. **Command Validation** - `ValidationRule<T>` classes in command handlers
3. **Domain Validation** - Business rules in entity methods
4. **Anti-Forgery** - `[ValidateAntiForgeryToken]` on POST actions

#### Error Handling
**Global Filter**: `[HandleError]` attribute on controllers
**Error Event**: `MvcApplication_Error` in Global.asax
**Logging**: log4net via `ILogger` interface
**User-Facing**: `ErrorController` for error pages

#### Dependency Injection
**Container**: StructureMap
**Configuration**: `Bootstrapper.Bootstrap()` in Application_Start
**Validation**: `ObjectFactory.AssertConfigurationIsValid()` in DEBUG mode
**Cleanup**: `ObjectFactory.ReleaseAndDisposeAllHttpScopedObjects()` per request

### Integration Points Identified

#### Azure Service Bus
**Purpose**: Asynchronous messaging for collaboration notifications
**Usage Pattern**:
```csharp
_azureSubscriptionClient.Send(message);
_subscriptionFactory.Create(subscription);
```
**Message Types**: `ITopicMessage`, `IDto`
**Subscription Management**: `IAzureSubscriptionClientManager`

#### Email (MailGun)
**Service**: `IEmailUtility`
**Key Operations**:
- Account activation emails (disabled in code)
- Idea sharing via email
- Collaboration notifications

**Template Mapping** (from email templates):
- Account Activation → `Account Activation.txt`
- Welcome Emails → Role-specific templates:
  - `Welcome Email upon Activation - IdeaAuthor.txt`
  - `Welcome Email upon Activation - Investor.txt`
  - `Welcome Email upon Activation - Mfg.txt`
  - `Welcome Email upon Activation - R&D.txt`
  - `Welcome Email upon Activation - Sales&Mktg.txt`

#### AutoMapper
**Configuration**: `AutoMapperConfigurator.Configure()` in Application_Start
**Profiles**: `IdeaMapperProfile` and others in `UI/AutoMapper/Profiles/`
**Validation**: `Mapper.AssertConfigurationIsValid()` in DEBUG mode

**Helper Method**:
```csharp
protected ActionResult AutoMappedView<TViewModel>(object model)
{
    return View(Mapper.Map<TViewModel>(model));
}
```

### Performance & Optimization Observations

#### View Counting
**Implementation**: `_rankingRepository.RegisterCounter(productIdea, member, CounterType.View)`
**Trigger**: On `LimitedView` action (non-author viewing)
**Implication**: DB write on every idea view (potential bottleneck)

#### Read Status Tracking
**Implementation**: `MarkIdeaCommunicationAsRead(productIdea)`
**Behavior**: 
- Checks if already viewed before updating
- Performs DB save if status changes
- Executed in controller (not async)

#### Caching
**Evidence**: No explicit caching observed in reviewed code
**Risk**: Repeated queries for master data (industries, countries)

#### Query Optimization
**Concerns**:
- No explicit eager loading strategies visible
- Relies on lazy loading within request scope
- Collection filtering done in-memory (`FormalIdeaResponses.OfType<T>()`)

### Security Observations

#### Password Security
- Salted password hashing via `ICryptographer`
- Salt stored per user in `Member.Salt`
- Separate `PasswordHash` field

#### CSRF Protection
- `[ValidateAntiForgeryToken]` on POST actions
- `AutomaticAntiForgery` filter provider registered globally
- Token generation implicit in forms

#### Authorization Enforcement
- Controller-level `[Authorize]` attributes
- Custom role-based filters
- Manual ownership checks in controller actions

#### Session Security
- Forms Authentication cookie management
- Session-stored user context
- `HttpContext.User.Identity.Name` for current user

#### Potential Vulnerabilities
- jQuery 1.6.1 has known XSS vulnerabilities
- No evidence of rate limiting on login/registration
- View counters could be abused for tracking
- Email addresses used as usernames (enumeration risk)

---

## NEXT STEPS FOR RE-ARCHITECTURE PLANNING

### Completed Discovery ✅

1. **Controller Inventory** - All 5 controllers documented with action methods
2. **Repository Analysis** - 6 repository implementations mapped
3. **Domain Model** - Complete entity hierarchy documented
4. **CQRS Commands** - 5 command handlers analyzed
5. **Route Mappings** - Comprehensive URL structure documented
6. **Authorization** - Authentication and authorization patterns clarified
7. **Validation** - Multi-layer validation strategy documented
8. **Integration Points** - Azure Service Bus and MailGun confirmed

### Immediate Next Actions (Priority 1)

#### 1. JavaScript & Client-Side Analysis
**Objective**: Document client-side behavior and AJAX interactions

**Tasks**:
- Review `src/UI/Scripts` folder structure
- Identify custom JavaScript modules
- Document AJAX endpoint calls
- Map Handlebars.js template usage
- Identify client-side validation rules
- Document jQuery plugin dependencies

**Deliverable**: Client-side architecture addendum

#### 2. NHibernate Mapping Review
**Objective**: Understand data model and relationships

**Tasks**:
- Locate mapping files (FluentNHibernate or XML)
- Document entity relationships and cascades
- Identify eager/lazy loading strategies
- Review database constraints
- Document collection fetch strategies

**Deliverable**: Complete entity relationship diagram

#### 3. Database Schema Export
**Objective**: Capture database structure

**Tasks**:
- Export schema from `src/Database` project
- Document tables, columns, types
- Identify indexes and constraints
- Review stored procedures (if any)
- Map foreign key relationships

**Deliverable**: Database schema documentation

#### 4. Configuration Analysis
**Objective**: Document environment dependencies

**Tasks**:
- Review Web.config and transformations
- Extract connection strings structure
- Document app settings
- Identify external service endpoints
- Review Azure Service Bus configuration

**Deliverable**: Configuration specification document

### Secondary Actions (Priority 2)

#### 5. Test Coverage Analysis
**Objective**: Assess existing test infrastructure

**Tasks**:
- Review `Innoventity.UnitTests` project
- Identify tested components
- Document test patterns and frameworks
- Calculate coverage metrics
- Identify untested critical paths

**Deliverable**: Test coverage report

#### 6. Email Template Analysis
**Objective**: Document email communication patterns

**Tasks**:
- Parse email template files
- Map templates to trigger events
- Identify template variables
- Document email sending logic
- Review email service configuration

**Deliverable**: Email template mapping document

#### 7. View/Razor Analysis
**Objective**: Understand UI structure and patterns

**Tasks**:
- Review Razor view organization
- Document master pages/layouts
- Identify partial views
- Review HTML helper usage
- Document client-side framework usage

**Deliverable**: UI component inventory

#### 8. Azure Messaging Deep Dive
**Objective**: Clarify asynchronous communication patterns

**Tasks**:
- Review `Infrastructure.AzureMessaging` implementation
- Document message types and contracts
- Identify message handlers
- Map message flows
- Review subscription management

**Deliverable**: Messaging architecture document

### Stakeholder Interview Topics

#### Business Domain Clarification
1. **Product Idea Lifecycle**
   - Confirm state transitions
   - Approval workflows (if any)
   - Idea withdrawal/cancellation process

2. **Collaboration Model**
   - Post-selection collaboration features
   - Communication tools available
   - Equity/compensation negotiation

3. **Monetization**
   - Revenue model (freemium, subscription?)
   - Transaction fees
   - Premium features

4. **User Roles & Permissions**
   - Admin capabilities
   - Moderator roles
   - Content moderation

#### Technical Architecture Validation
1. **Deployment Environment**
   - Current hosting (AppHarbor mentioned)
   - Production vs staging environments
   - Backup and disaster recovery

2. **Performance Requirements**
   - Expected user concurrency
   - Response time requirements
   - Data volume projections

3. **Integration Constraints**
   - Must Azure Service Bus remain?
   - Email provider flexibility
   - Third-party service dependencies

### Documentation Outputs for Re-Architecture

#### 1. Complete Architecture Documentation Package
**Contents**:
- ✅ System overview (COMPLETED)
- ✅ User journeys (COMPLETED)
- ✅ Domain model (COMPLETED)
- ✅ Application responsibilities (COMPLETED)
- ✅ Data & persistence (COMPLETED)
- ✅ Integrations (COMPLETED)
- ✅ Security & access control (COMPLETED)
- ✅ Technical constraints (COMPLETED)
- ✅ Controller & repository analysis (COMPLETED)
- ⏳ Client-side architecture (PENDING)
- ⏳ Database schema (PENDING)
- ⏳ Configuration specification (PENDING)

#### 2. Domain Model Diagram
**Required Artifacts**:
- Complete entity relationship diagram (ERD)
- Aggregate boundary visualization
- State machine diagrams for ProductIdea
- Value object documentation

**Format**: C4 Model Component Diagrams

#### 3. API Contract Specification
**Required Artifacts**:
- RESTful API design from MVC routes (documented ✅)
- Request/response schemas (ViewModels documented ✅)
- AJAX endpoint inventory (partial)
- Authentication/authorization requirements (documented ✅)

**Format**: OpenAPI/Swagger specification

#### 4. Data Migration Strategy
**Contents**:
- Source schema documentation (pending)
- Target schema design
- Data transformation rules
- Migration scripts
- Testing approach
- Rollback strategy

#### 5. Feature Parity Checklist
**Structure**:
- All current features documented ✅
- Priority classification (must-have, should-have, nice-to-have)
- Feature gaps in legacy system
- Enhancement opportunities
- Deprecated feature identification

### Risk Assessment for Re-Architecture

#### High-Risk Areas Identified
1. **Data Migration Complexity**
   - Complex aggregate relationships
   - Multi-year financial data structure
   - Collection relationships with cascades

2. **Business Logic Distribution**
   - Logic spread across entities, handlers, controllers
   - Implicit state management
   - In-memory collection filtering

3. **Integration Dependencies**
   - Azure Service Bus coupling
   - MailGun API integration
   - Potential data loss in async messaging

4. **Missing Test Coverage**
   - Unknown test coverage percentage
   - Potential regression risks
   - Manual testing requirements

#### Mitigation Strategies
1. **Implement Strangler Fig Pattern**
   - Gradual migration of features
   - Run old and new systems in parallel
   - Feature flag-based rollout

2. **Comprehensive Test Suite First**
   - Characterization tests for legacy system
   - API contract tests
   - End-to-end journey tests

3. **Data Migration Dry Runs**
   - Multiple test migrations
   - Data validation scripts
   - Performance testing with production volumes

4. **Integration Abstraction Layer**
   - Wrap external services
   - Enable provider switching
   - Mock-friendly interfaces

---

## REVISION HISTORY

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-02-07 | Architecture Review Team | Initial inventory creation |
| 1.1 | 2026-02-07 | Architecture Review Team | Completed controller and repository analysis |
| | | | - Documented all 5 controllers with 50+ action methods |
| | | | - Mapped 6 repository implementations |
| | | | - Clarified domain entity hierarchy (8 member types) |
| | | | - Documented 5 CQRS command handlers |
| | | | - Mapped 30+ routes |
| | | | - Clarified authentication/authorization patterns |
| | | | - Documented validation layers |
| | | | - Identified integration points |
| | | | - Updated ambiguity register with findings |
| 1.2 | 2026-02-07 | Architecture Review Team | **MAJOR UPDATE: Aligned with authoritative system context** |
| | | | - Reframed as Global Open, Hybrid, and Closed Innovation Platform |
| | | | - Updated innovation actor terminology (Idea Generators, R&D Orgs, etc.) |
| | | | - Documented virtual incubator concept and functions |
| | | | - Added platform mode analysis (Open/Closed/Hybrid) |
| | | | - Documented research category requirements (Management/Engineering/Natural Science) |
| | | | - Identified platform mode implementation gaps |
| | | | - Added research categorization validation needs |
| | | | - Updated all user journeys to reflect innovation ecosystem |
| | | | - Revised domain model with platform terminology |
| | | | - Enhanced security section with platform mode access control |
| | | | - Created alignment matrix with authoritative requirements |

---

**Document Status**: Living Document - Aligned with Authoritative Context  
**Confidence Level**: High for Open Innovation implementation, Medium for Closed/Hybrid modes  
**Coverage**: ~70% of system documented with platform context alignment  
**Next Review**: After stakeholder validation of platform mode implementation and research categorization
