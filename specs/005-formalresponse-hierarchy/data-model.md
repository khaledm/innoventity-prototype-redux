# Data Model: FormalResponse Polymorphic Hierarchy

**Branch**: `005-formalresponse-hierarchy`
**Date**: 2026-06-07
**Reference**: [spec.md](spec.md) | [research.md](research.md)

---

## Entity Relationship Overview

```
Innovation (aggregate root)
├── FormalResponses: ICollection<FormalResponse>  [RENAMED from Bids]
│
FormalResponse (abstract, EntityOfGuid)
├── ManufacturingResponse
│   └── YearlyManufacturingCosts: IList<YearlyManufacturingCost>  [JSON column]
│       └── YearlyManufacturingCost (value object)
│           ├── Year: int
│           ├── ProductionVolume: int
│           ├── ProductionVolumeRationale: string
│           ├── UnitCost: decimal
│           ├── UnitCostRationale: string
│           ├── AverageGlobalDistributionExpense: decimal
│           └── AvgDistributionExpenseRationale: string
│
├── SalesMarketingResponse
│   └── YearlySales: IList<YearlySale>  [JSON column]
│       └── YearlySale (value object)
│           ├── Year: int
│           ├── UnitsSold: int
│           ├── UnitsSoldRationale: string
│           ├── UnitPrice: decimal
│           ├── UnitPriceRationale: string
│           ├── SalesMarketingExpense: decimal
│           └── SalesMarketingExpenseRationale: string
│
├── ResearchDevelopmentResponse
│   ├── ProductDevelopmentDuration: int  [years]
│   └── YearlyDevelopmentCosts: IList<YearlyDevelopmentCost>  [JSON column]
│       └── YearlyDevelopmentCost (value object)
│           ├── Year: int
│           ├── InfrastructureCost: decimal
│           ├── InfrastructureCostRationale: string
│           ├── PeopleCost: decimal
│           └── PeopleCostRationale: string
│
└── InvestorResponse
    └── Feedback: string  [min 50 chars]
```

---

## New / Changed Enums

### GeographicRegion (NEW — replaces Location string on Bid)

```
Asia
Americas
Europe
Africa
Oceania
```

Stored as string in the database (`.HasConversion<string>()`).

### ResponseStatus (RENAMED from BidStatus — same values)

```
Pending   → initial state on submission
Accepted  → set by SelectPartners
Rejected  → set by SelectPartners (remaining Pending responses)
```

Stored as string in the database (`.HasConversion<string>()`).

---

## Abstract Base: FormalResponse

**Table**: `FormalResponses` (TPH — all subtypes in one table)
**Discriminator column**: `ResponseType` (string, NOT NULL)
**Discriminator values**: `ManufacturingResponse`, `SalesMarketingResponse`, `ResearchDevelopmentResponse`, `InvestorResponse`

| Column | Type | Constraints |
|--------|------|-------------|
| `Id` | uniqueidentifier | PK, NOT NULL |
| `ResponseType` | nvarchar(50) | NOT NULL (discriminator) |
| `InnovationId` | uniqueidentifier | FK → Innovations.Id, NOT NULL |
| `ActorId` | uniqueidentifier | FK → Actors.Id, NOT NULL |
| `Location` | nvarchar(20) | NOT NULL (GeographicRegion enum stored as string) |
| `ParticipationType` | nvarchar(100) | NOT NULL |
| `ParticipationProposal` | nvarchar(5000) | NOT NULL, min 100 chars |
| `Status` | nvarchar(20) | NOT NULL, default 'Pending' |
| `SubmittedAt` | datetimeoffset | NOT NULL |
| `UpdatedAt` | datetimeoffset | NULL |
| `AcceptedAt` | datetimeoffset | NULL |

**Unique constraint**: `IX_FormalResponse_ActorId_InnovationId` (one response per actor per innovation)

**Relationships**:
- `HasOne(Innovation).WithMany(i => i.FormalResponses).HasForeignKey(InnovationId).OnDelete(Cascade)`
- `HasOne(Actor).WithMany().HasForeignKey(ActorId).OnDelete(Restrict)`

---

## ManufacturingResponse

**Discriminator**: `ManufacturingResponse`

| Column | Type | Constraints |
|--------|------|-------------|
| `YearlyManufacturingCosts` | nvarchar(max) | NULL (JSON, owned collection) |

**JSON schema** (`YearlyManufacturingCosts`):
```json
[
  {
    "year": 1,
    "productionVolume": 10000,
    "productionVolumeRationale": "Based on Q1 supplier capacity quotes...",
    "unitCost": 5.50,
    "unitCostRationale": "Materials $3.50, labor $1.50, overhead $0.50...",
    "averageGlobalDistributionExpense": 1.20,
    "avgDistributionExpenseRationale": "Weighted by target market logistics..."
  }
]
```

**Validation rules**:
- List must be non-empty
- Year values must be contiguous starting from 1 (no gaps)
- Year values must be in range 1–10
- All Rationale fields: minimum 20 characters, maximum 500 characters
- `ProductionVolume`: integer ≥ 0
- `UnitCost`, `AverageGlobalDistributionExpense`: decimal ≥ 0

---

## SalesMarketingResponse

**Discriminator**: `SalesMarketingResponse`

| Column | Type | Constraints |
|--------|------|-------------|
| `YearlySales` | nvarchar(max) | NULL (JSON, owned collection) |

**JSON schema** (`YearlySales`):
```json
[
  {
    "year": 1,
    "unitsSold": 5000,
    "unitsSoldRationale": "Conservative estimate based on...",
    "unitPrice": 49.99,
    "unitPriceRationale": "Market pricing analysis shows...",
    "salesMarketingExpense": 25000.00,
    "salesMarketingExpenseRationale": "Channel costs plus digital marketing..."
  }
]
```

**Validation rules**: same structure as ManufacturingResponse (contiguous years 1–10, all Rationale fields ≥ 20 chars).

---

## ResearchDevelopmentResponse

**Discriminator**: `ResearchDevelopmentResponse`

| Column | Type | Constraints |
|--------|------|-------------|
| `ProductDevelopmentDuration` | int | NULL (years, 1–10) |
| `YearlyDevelopmentCosts` | nvarchar(max) | NULL (JSON, owned collection) |

**JSON schema** (`YearlyDevelopmentCosts`):
```json
[
  {
    "year": 1,
    "infrastructureCost": 50000.00,
    "infrastructureCostRationale": "Cloud hosting, tooling, licenses...",
    "peopleCost": 200000.00,
    "peopleCostRationale": "3 engineers at 60k + 1 PM at 70k..."
  }
]
```

**Validation rules**: same structure plus `ProductDevelopmentDuration` must be integer 1–10.

---

## InvestorResponse

**Discriminator**: `InvestorResponse`

| Column | Type | Constraints |
|--------|------|-------------|
| `Feedback` | nvarchar(2000) | NULL (min 50 chars, max 2000 chars) |

---

## Innovation Entity Changes

| Change | From | To |
|--------|------|----|
| Navigation property name | `Bids: ICollection<Bid>` | `FormalResponses: ICollection<FormalResponse>` |
| EF relationship config | `WithMany(i => i.Bids)` | `WithMany(i => i.FormalResponses)` |

SelectPartners.cs is updated to navigate `innovation.FormalResponses` and cast to specific types where needed.

---

## Removed Entities

| Entity | Action |
|--------|--------|
| `Bid` | Removed — replaced by `FormalResponse` hierarchy |
| `BidStatus` | Removed — replaced by `ResponseStatus` |

---

## EF Core Configuration Summary

```
AppDbContext:
  - DbSet<Bid> Bids         → REMOVED
  - DbSet<FormalResponse> FormalResponses  → ADDED

FormalResponse (TPH):
  - HasDiscriminator<string>("ResponseType")
  - ManufacturingResponse → "ManufacturingResponse"
  - SalesMarketingResponse → "SalesMarketingResponse"
  - ResearchDevelopmentResponse → "ResearchDevelopmentResponse"
  - InvestorResponse → "InvestorResponse"

ManufacturingResponse.YearlyManufacturingCosts:
  - OwnsMany(x => x.YearlyManufacturingCosts).ToJson()
  
SalesMarketingResponse.YearlySales:
  - OwnsMany(x => x.YearlySales).ToJson()
  
ResearchDevelopmentResponse.YearlyDevelopmentCosts:
  - OwnsMany(x => x.YearlyDevelopmentCosts).ToJson()
```

---

## Database Migration Plan

**Migration name**: `AddFormalResponseHierarchy`

Steps executed in a single migration:
1. Create `FormalResponses` table (all columns from the base + nullable columns for each subtype)
2. `INSERT INTO FormalResponses SELECT ... FROM Bids` (mapping ActorType to Discriminator, empty JSON for projection columns)
3. Drop unique index `IX_Bid_ActorId_InnovationId`
4. Drop `Bids` table
5. Add unique index `IX_FormalResponse_ActorId_InnovationId` on `FormalResponses`

**Down migration** reverses steps 5→1 (creates Bids, moves data back, drops FormalResponses).

---

## File Change Map

| File | Change Type | Summary |
|------|-------------|---------|
| `Domain/Entities/Bid.cs` | DELETE | Replaced by FormalResponse hierarchy |
| `Domain/Entities/BidStatus.cs` | DELETE | Replaced by ResponseStatus |
| `Domain/Entities/GeographicRegion.cs` | NEW | Enum: Asia, Americas, Europe, Africa, Oceania |
| `Domain/Entities/ResponseStatus.cs` | NEW | Enum: Pending, Accepted, Rejected |
| `Domain/Entities/FormalResponse.cs` | NEW | Abstract base EntityOfGuid |
| `Domain/Entities/ManufacturingResponse.cs` | NEW | + YearlyManufacturingCosts |
| `Domain/Entities/SalesMarketingResponse.cs` | NEW | + YearlySales |
| `Domain/Entities/ResearchDevelopmentResponse.cs` | NEW | + ProductDevelopmentDuration + YearlyDevelopmentCosts |
| `Domain/Entities/InvestorResponse.cs` | NEW | + Feedback |
| `Domain/Entities/YearlyManufacturingCost.cs` | NEW | Value object for JSON |
| `Domain/Entities/YearlySale.cs` | NEW | Value object for JSON |
| `Domain/Entities/YearlyDevelopmentCost.cs` | NEW | Value object for JSON |
| `Domain/Entities/Innovation.cs` | MODIFY | Bids → FormalResponses nav property |
| `Infrastructure/Persistence/AppDbContext.cs` | MODIFY | Remove Bids, add FormalResponses + TPH config |
| `Infrastructure/Persistence/SeedData.cs` | MODIFY | Replace Bid seeds with typed FormalResponse seeds |
| `Infrastructure/Persistence/Migrations/*` | NEW | AddFormalResponseHierarchy migration |
| `Features/Bids/SubmitBid.cs` | DELETE | Replaced by 4 typed submit handlers |
| `Features/Bids/UpdateBid.cs` | DELETE | Out of scope for this feature (removed with Bid entity) |
| `Features/Bids/GetBids.cs` | MODIFY | Return typed FormalResponse DTOs |
| `Features/Bids/SelectPartners.cs` | MODIFY | Update to FormalResponse, rename SelectedBidIds → SelectedResponseIds |
| `Features/Bids/SubmitManufacturingResponse.cs` | NEW | POST /innovations/{id}/bids/manufacturing |
| `Features/Bids/SubmitSalesMarketingResponse.cs` | NEW | POST /innovations/{id}/bids/sales |
| `Features/Bids/SubmitResearchDevelopmentResponse.cs` | NEW | POST /innovations/{id}/bids/rd |
| `Features/Bids/SubmitInvestorResponse.cs` | NEW | POST /innovations/{id}/bids/investor |
| `Program.cs` | MODIFY | Unregister old endpoints, register 4 new submit endpoints |
