# Data Model: Platform Core (v1.0)

**Feature**: Platform Core  
**Date**: February 8, 2026  
**Source**: Functional specification (specs/001-platform-core/spec.md)  
**Status**: Phase 1 Design

---

## Overview

This data model extracts entities, relationships, value objects, and validation rules from the functional specification. The model supports the 5 core user journeys (registration, innovation submission, bidding, partner selection, virtual incubator collaboration) while maintaining extensibility for v2.0 multi-tenancy and new actor types.

**Key Design Principles**:
- **Fresh Schema**: No legacy constraints (Constitution Constraint 2)
- **v2.0 Extensibility**: Repository abstractions, inheritance for actor types (Constitution Principle 7)
- **Business Rule Enforcement**: Database constraints + application-level validation
- **Audit Trail**: Timestamps on all entities for debugging and compliance

---

## Entity Relationship Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                             ACTOR (Base Class)                          │
│─────────────────────────────────────────────────────────────────────────│
│ Id: Guid (PK)                                                           │
│ Email: string (unique per actor type)                                   │
│ FullName: string                                                        │
│ ContactAddress: string                                                  │
│ ActorType: enum (IdeaGenerator, RD, Manufacturing, Sales, Investor)    │
│ AccountStatus: enum (PendingActivation, Active, Suspended)             │
│ ActivationToken: string? (nullable, null after activation)             │
│ PasswordHash: string                                                    │
│ CreatedAt: DateTimeOffset                                              │
│ UpdatedAt: DateTimeOffset                                              │
└─────────────────────────────────────────────────────────────────────────┘
                    ▲
                    │ (Inheritance - TPH: Table Per Hierarchy)
        ┌───────────┼───────────┬────────────┬─────────────┐
        │           │           │            │             │
┌───────▼──────┐ ┌──▼────────┐ ┌▼─────────┐ ┌▼──────────┐ ┌▼─────────┐
│IdeaGenerator │ │RDOrganiz. │ │Manufactu-│ │SalesMarke-│ │ Investor │
│              │ │           │ │ring      │ │ting       │ │          │
│(no extra     │ │(no extra  │ │(no extra │ │(no extra  │ │(no extra │
│ fields)      │ │ fields)   │ │ fields)  │ │ fields)   │ │ fields)  │
└──────────────┘ └───────────┘ └──────────┘ └───────────┘ └──────────┘
        │                                │
        │ owns                           │ has
        │ 1:N                            │ M:N
        ▼                                ▼
┌─────────────────────────────┐   ┌──────────────────────────┐
│ INNOVATION                  │   │ ACTOR_INDUSTRY           │
│─────────────────────────────│   │──────────────────────────│
│ Id: Guid (PK)               │   │ ActorId: Guid (FK)       │
│ OwnerId: Guid (FK → Actor)  │   │ IndustryId: Guid (FK)    │
│ Title: string               │   └──────────────────────────┘
│ ProductType: string         │              │
│ ResearchBackground: string  │              │ references
│ ResearchCategory: enum      │              ▼
│ HasIPR: bool                │   ┌──────────────────────────┐
│ IPRExplanation: string?     │   │ INDUSTRY                 │
│ RightToUseConfirmed: bool   │   │──────────────────────────│
│ ProductDescription: string  │   │ Id: Guid (PK)            │
│ KeyAdvantages: string       │   │ Name: string             │
│ DevelopmentPhase: string    │   │ ParentIndustryId: Guid?  │
│ DevelopmentProcess: string  │   │ (hierarchical)           │
│ ProductKeywords: string     │   └──────────────────────────┘
│ AdvantageKeywords: string   │
│ TargetMarket: string        │
│ TargetCustomerBase: string  │
│ TargetCustomerType: string  │
│ RequiresRD: bool            │
│ RequiresManufacturing: bool │
│ RequiresSalesMarketing: bool│
│ RequiresInvestment: bool    │
│ Status: enum (see below)    │
│ SubmittedAt: DateTimeOffset?│
│ CreatedAt: DateTimeOffset   │
│ UpdatedAt: DateTimeOffset   │
└─────────────────────────────┘
        │ has
        │ 1:N
        ▼
┌─────────────────────────────────────────────────────────────────────┐
│ INNOVATION_TARGET_INDUSTRY                                           │
│─────────────────────────────────────────────────────────────────────│
│ InnovationId: Guid (FK)                                             │
│ IndustryId: Guid (FK)                                               │
└─────────────────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────┐
│ BID (Base Class)                                                     │
│─────────────────────────────────────────────────────────────────────│
│ Id: Guid (PK)                                                       │
│ InnovationId: Guid (FK → Innovation)                                │
│ ActorId: Guid (FK → Actor)                                          │
│ BidType: enum (RD, Manufacturing, SalesMarketing, Investment)       │
│ Location: string                                                    │
│ ParticipationType: string                                           │
│ ParticipationProposal: string (min 200 chars)                       │
│ IsAccepted: bool (false = pending, true = selected)                 │
│ PostedAt: DateTimeOffset                                            │
│ AcceptedAt: DateTimeOffset?                                         │
│ CreatedAt: DateTimeOffset                                           │
│ UpdatedAt: DateTimeOffset                                           │
└─────────────────────────────────────────────────────────────────────┘
        ▲
        │ (Inheritance - TPH)
        ├─────────┬──────────────┬────────────────┬────────────┐
┌───────▼──────┐ ┌▼───────────┐ ┌▼──────────────┐ ┌▼─────────┐
│ RDBid        │ │Manufactur- │ │SalesMarketing-│ │Investment│
│              │ │ingBid      │ │Bid            │ │Bid       │
│(no extra     │ │(no extra   │ │(no extra      │ │(inv-     │
│ fields)      │ │ fields)    │ │ fields)       │ │specific  │
│              │ │            │ │               │ │ fields)  │
└──────────────┘ └────────────┘ └───────────────┘ └──────────┘


┌──────────────────────────────────────────────────────────────────────┐
│ BUSINESS_PLAN                                                        │
│──────────────────────────────────────────────────────────────────────│
│ Id: Guid (PK)                                                        │
│ InnovationId: Guid (FK → Innovation, unique)                        │
│ FinancialModel: string (JSON or separate table)                     │
│ OwnershipStructure: string                                          │
│ MarketStrategy: string                                               │
│ TechnicalRoadmap: string                                             │
│ ProductionPlan: string                                               │
│ CompetitiveAnalysis: string                                          │
│ ProjectValuation: decimal?                                           │
│ Status: enum (Draft, InProgress, Complete)                           │
│ CompletedAt: DateTimeOffset?                                         │
│ CreatedAt: DateTimeOffset                                            │
│ UpdatedAt: DateTimeOffset                                            │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Core Entities

### 1. Actor (Base Entity)

**Purpose**: Represents all user types in the system (5 actor types).

**Fields**:
| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | Guid | PK, required | Unique identifier |
| `Email` | string | Unique (per actor type), required, max 256 | Login username |
| `FullName` | string | Required, max 200 | Display name |
| `ContactAddress` | string | Required, max 500 | Physical/mailing address |
| `ActorType` | enum | Required | IdeaGenerator, RD, Manufacturing, SalesMarketing, Investor |
| `AccountStatus` | enum | Required, default PendingActivation | PendingActivation, Active, Suspended |
| `ActivationToken` | string? | Nullable, max 64 | GUID token for email activation, null after activation |
| `PasswordHash` | string | Required, max 256 | BCrypt hashed password |
| `CreatedAt` | DateTimeOffset | Required, default now | Account creation timestamp |
| `UpdatedAt` | DateTimeOffset | Required, default now | Last update timestamp |

**Business Rules**:- **R1.1**: New actors start with `AccountStatus = PendingActivation`
- **R1.1**: `ActivationToken` generated on creation, nulled on activation
- **R1.1**: Only `Active` users can submit innovations or bids
- **R1.2**: `ActorType` immutable after creation
- **R1.3**: `Email` unique within same `ActorType` (can have innovator@example.com as both IdeaGenerator and Investor)
- **R8.3**: `PasswordHash` never exposed in API responses
- **R8.4**: Password must meet complexity requirements before hashing

**Inheritance** (Table Per Hierarchy - TPH):
- **IdeaGenerator**: No additional fields
- **RDOrganization**: No additional fields (renamed from `DomainExpert` in legacy)
- **ManufacturingCompany**: No additional fields
- **SalesMarketingCompany**: No additional fields
- **Investor**: No additional fields (could add `InvestmentRange`, `IndustryFocus` in v1.1)

**Rationale for TPH**: All actor types currently share same fields. TPH avoids join complexity while enabling future discriminator-based queries. If actor types diverge significantly in v2.0, migra to TPT (Table Per Type).

---

### 2. Industry

**Purpose**: Hierarchical industry classification for actor affiliations and innovation targeting.

**Fields**:
| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | Guid | PK, required | Unique identifier |
| `Name` | string | Required, unique, max 200 | Industry name (e.g., "Healthcare", "Medical Devices") |
| `ParentIndustryId` | Guid? | Nullable, FK → Industry.Id | Parent industry for hierarchy (null = top-level sector) |
| `CreatedAt` | DateTimeOffset | Required | Timestamp |

**Business Rules**:
- **R3.2**: Actors affiliated with industry see innovations targeting that industry
- Hierarchy depth: 2 levels (Sector → Subsector) for v1.0
- Example: "Healthcare" (parent=null) → "Medical Devices" (parent=Healthcare.Id)

**Seed Data** (Phase 1 migration):
- Major sectors: Healthcare, Technology, Manufacturing, Energy, Finance, etc.
- Example subsectors: Medical Devices, Pharmaceuticals, Software, Hardware, etc.

---

### 3. ActorIndustry (Join Table)

**Purpose**: Many-to-many relationship between actors and industries (actor expertise/focus areas).

**Fields**:
| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `ActorId` | Guid | PK, FK → Actor.Id | Actor |
| `IndustryId` | Guid | PK, FK → Industry.Id | Industry |

**Business Rules**:
- Actors can have multiple industry affiliations
- Used for discovery filtering (R3.2)
- Can be updated over time (R Journey 5)

---

### 4. Innovation

**Purpose**: Core entity representing innovations submitted by idea generators.

**Fields**:
| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | Guid | PK, required | Unique identifier (IdeaToken in legacy) |
| `OwnerId` | Guid | FK → Actor.Id, required | IdeaGenerator who owns this innovation |
| **Idea Summary** |  |  |
| `Title` | string | Required, max 200, not default | Innovation title |
| `ProductType` | string | Required, max 200 | Type of product/innovation |
| `ResearchBackground` | string | Required, max 2000 | Research context |
| `ResearchCategory` | enum | Required | Management, Engineering, NaturalScience |
| `HasIPR` | bool | Required | Has intellectual property rights? |
| `IPRExplanation` | string? | Nullable, max 1000 | Explanation if no IPR, or details if has IPR |
| `RightToUseConfirmed` | bool | Required, must be true to submit | Checkbox confirmation |
| **Product Details** |  |  |
| `ProductDescription` | string | Required, max 2000 | Proposed product description |
| `KeyAdvantages` | string | Required, max 1000 | Product advantages |
| `DevelopmentPhase` | string | Required, max 200 | Current development stage |
| `DevelopmentProcess` | string | Required, max 2000 | Idea development process |
| `ProductKeywords` | string | Required, max 500 | Comma-separated keywords |
| `AdvantageKeywords` | string | Required, max 500 | Comma-separated keywords |
| **Market Details** |  |  |
| `TargetMarket` | string | Required, max 2000 | Target market description |
| `TargetCustomerBase` | string | Required, max 1000 | Target customer definition |
| `TargetCustomerType` | string | Required, max 200 | Customer type |
| **Collaboration Requirements** |  |  |
| `RequiresRD` | bool | Required, default false | Needs R&D partner? |
| `RequiresManufacturing` | bool | Required, default false | Needs manufacturing partner? |
| `RequiresSalesMarketing` | bool | Required, default false | Needs sales/marketing partner? |
| `RequiresInvestment` | bool | Required, default false | Needs investor? |
| **Lifecycle** |  |  |
| `Status` | enum | Required, default Draft | See InnovationStatus below |
| `SubmittedAt` | DateTimeOffset? | Nullable | Publication timestamp |
| `CreatedAt` | DateTimeOffset | Required | Draft creation timestamp |
| `UpdatedAt` | DateTimeOffset | Required | Last update timestamp |

**InnovationStatus Enum**:
```csharp
public enum InnovationStatus
{
    Draft,                    // Not yet submitted
    AwaitingBids,             // Submitted, accepting bids
    ReceivingBids,            // Same as AwaitingBids (legacy state)
    SufficientBids,           // Has ≥1 bid per required type
    PartnersSelected,         // Partner selection finalized (irreversible)
    BusinessPlanInProgress,   // Virtual incubator active
    BusinessPlanComplete      // Business plan finalized
}
```

**Business Rules**:
- **R2.1**: All "Idea Summary", "Product Details", "Market Details" required to transition from Draft → AwaitingBids
- **R2.2**: Only `OwnerId` can edit innovation
- **R2.3**: Innovation gets unique `Id` (Guid) on creation
- **R3.1**: Draft innovations visible only to owner, ≥AwaitingBids visible to industry-matched actors
- **R5.3**: Once `Status = PartnersSelected`, transition is irreversible
- **R5.5**: `Status = SufficientBids` when innovation has ≥1 Manufacturing bid, ≥1 SalesMarketing bid, ≥1 RD bid

**Validation**:
- `Title` cannot be default/placeholder (e.g., "New Innovation")
- `RightToUseConfirmed` must be `true` to submit
- At least one target industry required (via InnovationTargetIndustry join table)

---

### 5. InnovationTargetIndustry (Join Table)

**Purpose**: Many-to-many relationship between innovations and target industries.

**Fields**:
| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `InnovationId` | Guid | PK, FK → Innovation.Id | Innovation |
| `IndustryId` | Guid | PK, FK → Industry.Id | Target industry |

**Business Rules**:
- **R2.1**: At least 1 target industry required to submit innovation
- **R3.2**: Used for industry-based matching (actors see innovations in their affiliated industries)

---

### 6. Bid (Base Entity)

**Purpose**: Formal partnership proposals from actors (R&D, Manufacturing, Sales/Marketing, Investor).

**Fields**:
| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | Guid | PK, required | Unique identifier |
| `InnovationId` | Guid | FK → Innovation.Id, required | Innovation being bid on |
| `ActorId` | Guid | FK → Actor.Id, required | Actor submitting bid |
| `BidType` | enum | Required | RD, Manufacturing, SalesMarketing, Investment |
| `Location` | string | Required, max 200 | Geographic presence |
| `ParticipationType` | string | Required, max 200 | e.g., "Equity Partner", "Service Provider" |
| `ParticipationProposal` | string | Required, min 200, max 5000 | Detailed proposal |
| `IsAccepted` | bool | Required, default false | Selected by innovation owner? |
| `PostedAt` | DateTimeOffset | Required | Bid submission timestamp |
| `AcceptedAt` | DateTimeOffset? | Nullable | Partner selection timestamp (if accepted) |
| `CreatedAt` | DateTimeOffset | Required | Creation timestamp |
| `UpdatedAt` | DateTimeOffset | Required | Last update timestamp |

**BidType Enum**:
```csharp
public enum BidType
{
    RD,                      // Research & Development
    Manufacturing,           // Production & Distribution
    SalesMarketing,          // Commercialization
    Investment               // Funding
}
```

**Business Rules**:
- **R4.1**: Only non-IdeaGenerator actors can submit bids
- **R4.1**: Actor cannot bid on own innovation (if actor also has IdeaGenerator account with same email)
- **R4.1**: Innovation must be in AwaitingBids/ReceivingBids/SufficientBids status
- **R4.1**: Actor cannot submit multiple bids for same innovation (unique constraint: ActorId + InnovationId)
- **R4.2**: `ParticipationProposal` min 200 characters
- **R4.3**: Once `IsAccepted = true`, bid cannot be edited or withdrawn
- **R4.4**: System tracks bid counts per BidType (query: `COUNT(*) GROUP BY BidType`)

**Inheritance** (TPH):
- **RDBid**: No additional fields
- **ManufacturingBid**: No additional fields
- **SalesMarketingBid**: No additional fields
- **InvestmentBid**: Could add `InvestmentAmount`, `ProposedTerms` fields (future v1.1)

**Validation**:
- `BidType` must match `ActorType` (e.g., ManufacturingCompany can only submit Manufacturing bids)
- Unique constraint: `(ActorId, InnovationId)` to prevent duplicate bids

---

### 7. BusinessPlan

**Purpose**: Collaborative business plan developed in virtual incubator workspace.

**Fields**:
| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | Guid | PK, required | Unique identifier |
| `InnovationId` | Guid | FK → Innovation.Id, unique, required | Innovation (1:1 relationship) |
| `FinancialModel` | string | Nullable, max 10000 | JSON or text (revenue projections, cost structure, break-even) |
| `OwnershipStructure` | string | Nullable, max 5000 | Equity splits, IP licensing, revenue sharing |
| `MarketStrategy` | string | Nullable, max 10000 | Customer acquisition, pricing, distribution |
| `TechnicalRoadmap` | string | Nullable, max 10000 | Development phases, resource requirements |
| `ProductionPlan` | string | Nullable, max 10000 | Manufacturing approach, volume scaling |
| `CompetitiveAnalysis` | string | Nullable, max 5000 | Market landscape, competitors, differentiation |
| `ProjectValuation` | decimal? | Nullable, precision 18,2 | Calculated project value |
| `Status` | enum | Required, default Draft | Draft, InProgress, Complete |
| `CompletedAt` | DateTimeOffset? | Nullable | Completion timestamp |
| `CreatedAt` | DateTimeOffset | Required | Creation timestamp (when virtual incubator formed) |
| `UpdatedAt` | DateTimeOffset | Required | Last update timestamp |

**Business Rules**:
- **R7.1**: Business plan is optional (innovation can proceed without formal plan in system)
- **R7.2**: Each section owned by designated lead (tracked outside DB, in application layer)
- **R7.3**: `ProjectValuation` calculated from `FinancialModel` inputs (application logic, not DB trigger)
- **R6.1**: Only innovation owner and accepted partners can access business plan

**Future Considerations** (v1.1+):
- Separate tables for each section with version history
- Section approval workflow (ready/approved states)
- Comment/discussion threads on sections
- Management team composition (separate ManagementTeam entity)

---

## Value Objects & Enums

### ResearchCategory

```csharp
public enum ResearchCategory
{
    Management,
    Engineering,
    NaturalScience
}
```

### ActorType

```csharp
public enum ActorType
{
    IdeaGenerator,
    RD,                    // R&D Organization
    Manufacturing,         // Manufacturing Company
    SalesMarketing,        // Sales & Marketing Company
    Investor
}
```

### AccountStatus

```csharp
public enum AccountStatus
{
    PendingActivation,     // Awaiting email activation
    Active,                // Can use system
    Suspended              // Admin-disabled (v1.0: out of scope for user-facing UI)
}
```

---

## Indexes & Performance

**Queries to Optimize**:

1. **Innovation Discovery** (most frequent):
   ```sql
   -- Find innovations by industry affiliation
   WHERE InnovationId IN (
       SELECT InnovationId FROM InnovationTargetIndustry 
       WHERE IndustryId IN @ActorIndustryIds
   )
   AND Status IN ('AwaitingBids', 'ReceivingBids', 'SufficientBids')
   ```
   **Index**: `IX_InnovationTargetIndustry_IndustryId_InnovationId`

2. **Bid Counts by Type**:
   ```sql
   SELECT BidType, COUNT(*) 
   FROM Bid 
   WHERE InnovationId = @Id 
   GROUP BY BidType
   ```
   **Index**: `IX_Bid_InnovationId_BidType`

3. **Actor Authentication**:
   ```sql
   WHERE Email = @Email AND ActorType = @Type
   ```
   **Index**: `IX_Actor_Email_ActorType` (unique composite)

4. **Innovation Ownership**:
   ```sql
   WHERE OwnerId = @ActorId AND Status = 'Draft'
   ```
   **Index**: `IX_Innovation_OwnerId_Status`

**Proposed Indexes**:
```sql
-- Actor
CREATE UNIQUE INDEX IX_Actor_Email_ActorType ON Actor(Email, ActorType);
CREATE INDEX IX_Actor_AccountStatus ON Actor(AccountStatus) WHERE AccountStatus = 'Active';

-- Innovation
CREATE INDEX IX_Innovation_OwnerId_Status ON Innovation(OwnerId, Status);
CREATE INDEX IX_Innovation_Status ON Innovation(Status) WHERE Status IN ('AwaitingBids', 'ReceivingBids', 'SufficientBids');

-- InnovationTargetIndustry
CREATE INDEX IX_InnovationTargetIndustry_IndustryId_InnovationId ON InnovationTargetIndustry(IndustryId, InnovationId);

-- Bid
CREATE UNIQUE INDEX IX_Bid_ActorId_InnovationId ON Bid(ActorId, InnovationId); -- Prevent duplicate bids
CREATE INDEX IX_Bid_InnovationId_BidType ON Bid(InnovationId, BidType);
CREATE INDEX IX_Bid_InnovationId_IsAccepted ON Bid(InnovationId, IsAccepted);

-- ActorIndustry
CREATE INDEX IX_ActorIndustry_ActorId ON ActorIndustry(ActorId);
CREATE INDEX IX_ActorIndustry_IndustryId ON ActorIndustry(IndustryId);
```

---

## Database Constraints

**Foreign Keys** (with cascade behavior):
- `Innovation.OwnerId` → `Actor.Id` (ON DELETE RESTRICT - cannot delete actor with innovations)
- `Bid.ActorId` → `Actor.Id` (ON DELETE RESTRICT)
- `Bid.InnovationId` → `Innovation.Id` (ON DELETE CASCADE - delete bids when innovation deleted)
- `BusinessPlan.InnovationId` → `Innovation.Id` (ON DELETE CASCADE)
- `InnovationTargetIndustry.InnovationId` → `Innovation.Id` (ON DELETE CASCADE)
- `InnovationTargetIndustry.IndustryId` → `Industry.Id` (ON DELETE RESTRICT - cannot delete industry in use)
- `ActorIndustry.ActorId` → `Actor.Id` (ON DELETE CASCADE)
- `ActorIndustry.IndustryId` → `Industry.Id` (ON DELETE RESTRICT)

**Check Constraints**:
```sql
-- Innovation: Require at least one collaboration requirement
ALTER TABLE Innovation ADD CONSTRAINT CK_Innovation_RequiresAtLeastOne
CHECK (RequiresRD = 1 OR RequiresManufacturing = 1 OR RequiresSalesMarketing = 1 OR RequiresInvestment = 1);

-- Innovation: RightToUseConfirmed must be true to submit
-- (Application-level validation - easier to express in code than CHECK constraint)

-- Bid: ParticipationProposal min length
ALTER TABLE Bid ADD CONSTRAINT CK_Bid_ProposalMinLength
CHECK (LEN(ParticipationProposal) >= 200);
```

---

## Validation Rules Summary

**Extracted from Specification**:

| Entity | Field | Rule | Source |
|--------|-------|------|--------|
| Actor | Email | Unique per ActorType | R1.3 |
| Actor | ActorType | Immutable | R1.2 |
| Actor | AccountStatus | Must be Active to submit | R1.1 |
| Innovation | Title | Cannot be default/placeholder | R2.1 |
| Innovation | RightToUseConfirmed | Must be true to submit | R2.1 |
| Innovation | TargetIndustries | ≥1 required | R2.1 |
| Innovation | Status | Draft → AwaitingBids requires all sections complete | R2.1 |
| Innovation | OwnerId | Only owner can edit | R2.2 |
| Bid | ActorType | Must match BidType | R4.1 |
| Bid | (Actor, Innovation) | Unique pair (no duplicate bids) | R4.1 |
| Bid | ParticipationProposal | Min 200 characters | R4.2 |
| Bid | IsAccepted | Immutable once true | R4.3 |

**Implemented Via**:
- Database constraints: Foreign keys, unique indexes, check constraints
- Application validation: FluentValidation or Data Annotations in DTOs
- Authorization policies: Resource ownership checks before operations

---

## Migration Strategy

**Phase 0 Initial Migration**:
1. Create `Actor` table with TPH discriminator
2. Create `Industry` table with seed data (major sectors + subsectors)
3. Create `ActorIndustry` join table
4. Create `Innovation` table
5. Create `InnovationTargetIndustry` join table  
6. Create indexes for authentication and innovation lookup

**Phase 1 Full Schema**:
7. Create `Bid` table with TPH discriminator
8. Create `BusinessPlan` table
9. Create remaining indexes for bid counts, discovery queries
10. Add audit triggers (UpdatedAt auto-update on modification)

**Seed Data**:
- Industry sectors (Healthcare, Technology, Manufacturing, Energy, Finance, Education, Agriculture, etc.)
- Industry subsectors (Medical Devices, Software, Hardware, Renewable Energy, etc.)  
- Test actors (for integration/E2E testing)
- Sample innovation (for Phase 0 "view innovation" endpoint)

---

## v2.0 Extensibility Notes

**Multi-Tenancy Preparation**:
- Add `OrganizationId: Guid?` to `Actor`, `Innovation`, `Bid` (nullable in v1.0, required in v2.0)
- Repository methods: `GetByIdAsync(Guid id, Guid? tenantId = null)`
  - v1.0: Ignores tenantId
  - v2.0: Adds `WHERE OrganizationId = @tenantId` filter
- No changes to table structure needed (just add column, update queries)

**New Actor Types**:
- Add `Government`, `TechnologyPark` to `ActorType` enum
- TPH inheritance allows adding new discriminators without schema changes
- If actor types diverge significantly (many unique fields), migrate to TPT (Table Per Type)

**Closed/Hybrid Innovation**:
- Add `InnovationMode` enum (Open, Closed, Hybrid) to `Innovation` table
- Access control: Check `Innovation.OrganizationId` matches `Actor.OrganizationId` in Closed mode

---

**END OF DATA MODEL**
