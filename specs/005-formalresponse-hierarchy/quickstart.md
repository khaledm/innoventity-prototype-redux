# Quickstart Validation Guide: FormalResponse Polymorphic Hierarchy

**Branch**: `005-formalresponse-hierarchy`
**Date**: 2026-06-07
**Reference**: [contracts/api-contracts.md](contracts/api-contracts.md) | [data-model.md](data-model.md)

---

## Prerequisites

- .NET 8 SDK installed
- SQL Server (LocalDB or dev instance) — configured in `appsettings.Development.json`
- EF Core migration applied: `dotnet ef database update` from `src/Innoventity.API/`

---

## Build & Test

```powershell
# From repo root
dotnet build
dotnet test
```

All 131 pre-existing tests should continue to pass (zero regression baseline). New tests added in this feature add to the count.

---

## Scenario 1 — Manufacturing Actor Submits Typed Response

**Goal**: Verify FR-001, FR-007, FR-008 (structured submission + rationale enforcement)

### Step 1: Register and authenticate a Manufacturing actor
Use existing `/register` and `/login` endpoints (seed data available in test fixtures).

### Step 2: Submit a ManufacturingResponse
```
POST /innovations/{publishedInnovationId}/bids/manufacturing
Authorization: Bearer {token}

{
  "location": "Europe",
  "participationType": "Manufacturing Partner",
  "participationProposal": "Our facility in Stuttgart has 15 years experience in precision manufacturing for medical devices. We offer ISO 9001 certified production lines with capacity for 50,000 units annually.",
  "yearlyManufacturingCosts": [
    {
      "year": 1,
      "productionVolume": 10000,
      "productionVolumeRationale": "Conservative ramp-up based on supplier capacity assessment and market launch plan.",
      "unitCost": 5.50,
      "unitCostRationale": "Materials $3.50 per unit (aluminum extrusion), labor $1.50 (3 hrs at 50/hr), overhead $0.50.",
      "averageGlobalDistributionExpense": 1.20,
      "avgDistributionExpenseRationale": "Weighted average: Europe $0.80, Americas $1.40, Asia $1.20 per unit shipped."
    },
    {
      "year": 2,
      "productionVolume": 25000,
      "productionVolumeRationale": "Scaled based on Year 1 performance and contracted distribution expansion.",
      "unitCost": 4.80,
      "unitCostRationale": "Volume discounts on materials achieved at 25k units. Labor efficiency improves.",
      "averageGlobalDistributionExpense": 1.10,
      "avgDistributionExpenseRationale": "Consolidated shipping routes reduce per-unit cost as volume increases."
    }
  ]
}
```

**Expected**: `201 Created` with `responseId`, `responseType: "ManufacturingResponse"`, `status: "Pending"`

### Step 3: Verify rationale enforcement
Submit the same payload but set `productionVolumeRationale` to `"Too short"` (< 20 chars).

**Expected**: `422 Unprocessable Entity` with error referencing `YearlyManufacturingCosts[0].ProductionVolumeRationale`

---

## Scenario 2 — Year Contiguity Validation

**Goal**: Verify FR-010 (contiguous year keys)

Submit a ManufacturingResponse with `yearlyManufacturingCosts` containing years `[1, 3]` (skipping year 2).

**Expected**: `422 Unprocessable Entity` with error indicating year gap.

---

## Scenario 3 — Actor Type Enforcement

**Goal**: Verify FR-005 (actor type must match response type)

Authenticate as a SalesMarketing actor (or RD actor), then attempt `POST /innovations/{id}/bids/manufacturing`.

**Expected**: `403 Forbidden` with detail indicating actor type mismatch.

---

## Scenario 4 — Full Three-Partner Response and SelectPartners

**Goal**: Verify FR-015, FR-016 (SelectPartners compatibility with FormalResponse IDs)

1. Submit ManufacturingResponse → capture `responseId` (mfgId)
2. Submit SalesMarketingResponse → capture `responseId` (salesId)
3. Submit ResearchDevelopmentResponse → capture `responseId` (rdId)
4. Call SelectPartners:

```
POST /innovations/{innovationId}/select-partners
Authorization: Bearer {ownerToken}

{
  "selectedResponseIds": ["{mfgId}", "{salesId}", "{rdId}"]
}
```

**Expected**: `200 OK` with `status: "PartnersSelected"` and three `AcceptedBids` entries in the response.

---

## Scenario 5 — Owner vs Non-Owner Response Visibility

**Goal**: Verify FR-013 (financial data redacted for non-owners)

1. Submit three typed responses as the respective actors
2. Call `GET /innovations/{innovationId}/bids` as the **innovation owner** → verify full projection data returned
3. Call `GET /innovations/{innovationId}/bids` as a **different authenticated actor** → verify `yearlyManufacturingCosts`, `yearlySales`, `yearlyDevelopmentCosts`, and all rationale strings are absent from the response

---

## Scenario 6 — InvestorResponse Submission

**Goal**: Verify FR-004 (investor feedback response)

Authenticate as an Investor actor and submit:
```
POST /innovations/{id}/bids/investor
Authorization: Bearer {investorToken}

{
  "location": "Americas",
  "participationType": "Investment Partner",
  "participationProposal": "We are a Series B fund with a track record in clean-tech hardware. Our portfolio includes 12 manufacturing scale-ups in the EU and North America.",
  "feedback": "Strong IP position and defensible market. Would want to understand manufacturing partner selection before committing term sheet."
}
```

**Expected**: `201 Created` with `responseType: "InvestorResponse"`, `status: "Pending"`

---

## Scenario 7 — Migration Verification

**Goal**: Verify FR-017 (existing Bid records migrated correctly)

After running the migration on a database that contained Bid records:

1. Query `FormalResponses` table — migrated records should appear with correct `ResponseType` discriminator
2. Verify each migrated record has empty JSON projection columns (`YearlyManufacturingCosts = null` etc.)
3. Verify the `Bids` table no longer exists

---

## Running the Integration Test Suite

```powershell
# Run all tests
dotnet test

# Run only FormalResponse feature tests
dotnet test --filter "FullyQualifiedName~FormalResponse"

# Run mutation tests (selective — takes 10+ minutes)
cd src/Innoventity.API
dotnet stryker
```

**Expected test count post-implementation**: 131 (existing) + ~30 new tests for typed submit endpoints and updated SelectPartners = ~161 total.

---

## Quick Regression Check

After implementation, run this before committing:

```powershell
dotnet build --no-incremental 2>&1 | Select-String "error"
dotnet test 2>&1 | Select-String "Failed|Error"
```

Both should return no output.
