# Phase 1 Data Model: ProductIdea Composition Pattern

## Entity: Innovation (aggregate root)

Unchanged root-level fields (FR-010): `Id`, `IdeaToken`, `OwnerId`/`Owner`, `Status`, `CreatedAt`, `SubmittedAt`, `FormalResponses`, `TargetIndustries`, `PartnerSelectionCompletedOn`, `SelectedByActorId`.

New composed navigation properties, each required (non-null) and mapped via `OwnsOne`:

| Property | Type | Notes |
|---|---|---|
| `IdeaSummary` | `IdeaSummary` | required, `= null!` default (set via object initializer at construction) |
| `Product` | `Product` | required, `= null!` default |
| `Market` | `Market` | required, `= null!` default |
| `CollaborationRequirement` | `CollaborationRequirement` | required, `= null!` default |

New behavior (FR-003):

```csharp
bool IsIdeaSummaryComplete() => IdeaSummary.IsComplete();
bool IsProductDetailsComplete() => Product.IsComplete();
bool IsMarketDetailsSectionComplete() => Market.IsComplete() && TargetIndustries.Count > 0;
bool IsReadyForSubmission() =>
    IsIdeaSummaryComplete() && IsProductDetailsComplete() && IsMarketDetailsSectionComplete()
    && !string.IsNullOrWhiteSpace(CollaborationRequirement.PartnersNeeded);
```

Note: `IsMarketDetailsSectionComplete()` reads `TargetIndustries` off the **root**, not `Market` — the many-to-many association physically stays on `Innovation` (FR-010) even though it logically belongs to market completeness (rule 12).

## Entity: IdeaSummary (owned, no identity)

| Field | Type | Column (unchanged name) | Max length | Required |
|---|---|---|---|---|
| `Title` | `string` | `Title` | 200 | Yes |
| `ProductType` | `string` | `ProductType` | 200 | Yes |
| `ResearchCategory` | `ResearchCategory` (enum) | `ResearchCategory` | stored as `string` via `HasConversion<string>()` | Yes |
| `IprStatus` | `string` | `IprStatus` | 200 | Yes |
| `ResearchBackground` | `string` | `ResearchBackground` | 2000 | Yes |

**Validation** (`Validate()`): throws `ArgumentNullException`/`ArgumentOutOfRangeException` on null/empty required fields or an undefined enum value. Used at construction boundaries, not by the publish gate.

**Completeness** (`IsComplete()`, rules 1-5 per spec's rule-mapping table):
- `Title` non-empty AND does not contain "Untitled" (case-insensitive) AND does not contain "TODO" (case-insensitive)
- `ProductType` non-empty
- `ResearchCategory` is a defined enum value
- `IprStatus` non-empty
- `ResearchBackground` non-empty AND length ≥ 50

## Entity: Product (owned, no identity)

| Field | Type | Column | Max length | Required |
|---|---|---|---|---|
| `ProductDescription` | `string` | `ProductDescription` | 2000 | Yes |
| `TechnologyDescription` | `string` | `TechnologyDescription` | 5000 (`nvarchar(max)`) | Yes |
| `ProductAdvantages` | `string` | `ProductAdvantages` | 2000 | Yes |
| `DevelopmentPhase` | `string` | `DevelopmentPhase` | 200 | Yes |
| `DevelopmentProcess` | `string` | `DevelopmentProcess` | 2000 | Yes |
| `TargetBeneficiaries` | `string` | `TargetBeneficiaries` | 2000 | Yes |
| `ProductKeywords` | `string` | `ProductKeywords` | 500 | Yes (trimmed via private backing field) |
| `AdvantageKeywords` | `string` | `AdvantageKeywords` | 500 | Yes |

**Completeness** (`IsComplete()`, rules 7-9): `ProductDescription`, `TechnologyDescription`, `TargetBeneficiaries` all non-empty. (`ProductAdvantages`, `DevelopmentPhase`, `DevelopmentProcess`, keywords are stored/required at construction via `Validate()` but are not publish-gate criteria — no spec rule references them.)

## Entity: Market (owned, no identity)

| Field | Type | Column | Max length / precision | Required |
|---|---|---|---|---|
| `TargetMarket` | `string` | `TargetMarket` | 2000 | Yes |
| `TargetCustomerBase` | `string` | `TargetCustomerBase` | 2000 | Yes |
| `TargetCustomerType` | `string` | `TargetCustomerType` | 100 | Yes |
| `RelevantMarketSize` | `decimal?` | `RelevantMarketSize` | precision(28,2) | No (optional on drafts) |
| `PotentialMarketSize` | `decimal?` | `PotentialMarketSize` | precision(28,2) | No (optional on drafts) |

**Completeness** (`IsComplete()`, rules 10-11): `RelevantMarketSize > 0` AND `PotentialMarketSize > 0`. Rule 12 (`TargetIndustries ≥ 1`) is evaluated at the `Innovation.IsMarketDetailsSectionComplete()` level against the root's `TargetIndustries` collection, not inside `Market.IsComplete()`.

## Entity: CollaborationRequirement (owned, no identity)

| Field | Type | Column | Max length | Required |
|---|---|---|---|---|
| `PartnersNeeded` | `string?` | `PartnersNeeded` | 500 | No — optional on drafts; comma-separated actor-type list (unchanged format, per spec Assumptions) |

No `IsComplete()` method — rule 13 (`PartnersNeeded` non-empty after splitting on comma) is evaluated directly inside `Innovation.IsReadyForSubmission()`.

## Migration

`20260823142911_ComposeInnovationSections`: `Up()`/`Down()` are both empty method bodies. Verified via `dotnet ef migrations add` producing no data-loss warning once `AppDbContext.cs` OwnsOne configuration exactly reproduced every field's original column name, type, length, precision, and nullability from the last-committed `AppDbContextModelSnapshot.cs`. This confirms FR-009: existing rows require no data movement, only a C#-level object-graph reshape.
