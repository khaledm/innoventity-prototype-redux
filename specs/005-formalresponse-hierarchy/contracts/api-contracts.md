# API Contracts: FormalResponse Polymorphic Hierarchy

**Branch**: `005-formalresponse-hierarchy`
**Date**: 2026-06-07
**Base URL**: `/innovations/{innovationId}/bids`

---

## Common Types

### GeographicRegion
```
Asia | Americas | Europe | Africa | Oceania
```

### ResponseStatus
```
Pending | Accepted | Rejected
```

### YearlyManufacturingCost
```json
{
  "year": 1,
  "productionVolume": 10000,
  "productionVolumeRationale": "string (≥20 chars)",
  "unitCost": 5.50,
  "unitCostRationale": "string (≥20 chars)",
  "averageGlobalDistributionExpense": 1.20,
  "avgDistributionExpenseRationale": "string (≥20 chars)"
}
```

### YearlySale
```json
{
  "year": 1,
  "unitsSold": 5000,
  "unitsSoldRationale": "string (≥20 chars)",
  "unitPrice": 49.99,
  "unitPriceRationale": "string (≥20 chars)",
  "salesMarketingExpense": 25000.00,
  "salesMarketingExpenseRationale": "string (≥20 chars)"
}
```

### YearlyDevelopmentCost
```json
{
  "year": 1,
  "infrastructureCost": 50000.00,
  "infrastructureCostRationale": "string (≥20 chars)",
  "peopleCost": 200000.00,
  "peopleCostRationale": "string (≥20 chars)"
}
```

---

## POST /innovations/{innovationId}/bids/manufacturing

**Auth**: Bearer JWT required  
**Actor constraint**: `ActorType = Manufacturing`

### Request Body
```json
{
  "location": "Europe",
  "participationType": "Manufacturing Partner",
  "participationProposal": "string (≥100 chars)",
  "yearlyManufacturingCosts": [
    { /* YearlyManufacturingCost — see Common Types */ }
  ]
}
```

**Validation rules**:
- `yearlyManufacturingCosts`: non-empty, years contiguous starting from 1, all years ≤ 10
- All Rationale fields: ≥ 20 chars, ≤ 500 chars
- `productionVolume`: integer ≥ 0
- `unitCost`, `averageGlobalDistributionExpense`: decimal ≥ 0

### Response 201 Created
```json
{
  "responseId": "guid",
  "responseType": "ManufacturingResponse",
  "innovationId": "guid",
  "actorId": "guid",
  "status": "Pending",
  "submittedAt": "2026-06-07T12:00:00Z"
}
```

### Error Responses
| Status | When |
|--------|------|
| 400 | Missing required fields, proposal too short |
| 401 | No/invalid JWT |
| 403 | Actor is IdeaGenerator or wrong actor type (non-Manufacturing) |
| 404 | Innovation not found or not Published |
| 409 | Actor already submitted a response for this innovation; or actor owns the innovation |
| 422 | Projection validation failure (year gap, missing rationale, rationale too short) |

---

## POST /innovations/{innovationId}/bids/sales

**Auth**: Bearer JWT required  
**Actor constraint**: `ActorType = SalesMarketing`

### Request Body
```json
{
  "location": "Americas",
  "participationType": "Sales & Marketing Partner",
  "participationProposal": "string (≥100 chars)",
  "yearlySales": [
    { /* YearlySale — see Common Types */ }
  ]
}
```

**Validation rules**: same structure as manufacturing (contiguous years 1–10, all Rationale ≥ 20 chars).

### Response 201 Created
```json
{
  "responseId": "guid",
  "responseType": "SalesMarketingResponse",
  "innovationId": "guid",
  "actorId": "guid",
  "status": "Pending",
  "submittedAt": "2026-06-07T12:00:00Z"
}
```

### Error Responses
Same set as manufacturing endpoint (403 when actor type ≠ SalesMarketing).

---

## POST /innovations/{innovationId}/bids/rd

**Auth**: Bearer JWT required  
**Actor constraint**: `ActorType = RD`

### Request Body
```json
{
  "location": "Asia",
  "participationType": "R&D Partner",
  "participationProposal": "string (≥100 chars)",
  "productDevelopmentDuration": 3,
  "yearlyDevelopmentCosts": [
    { /* YearlyDevelopmentCost — see Common Types */ }
  ]
}
```

**Validation rules**:
- `productDevelopmentDuration`: integer 1–10
- `yearlyDevelopmentCosts`: same structure (contiguous years, all Rationale ≥ 20 chars)

### Response 201 Created
```json
{
  "responseId": "guid",
  "responseType": "ResearchDevelopmentResponse",
  "innovationId": "guid",
  "actorId": "guid",
  "status": "Pending",
  "submittedAt": "2026-06-07T12:00:00Z"
}
```

### Error Responses
Same set as manufacturing endpoint (403 when actor type ≠ RD).

---

## POST /innovations/{innovationId}/bids/investor

**Auth**: Bearer JWT required  
**Actor constraint**: `ActorType = Investor`

### Request Body
```json
{
  "location": "Europe",
  "participationType": "Investment Partner",
  "participationProposal": "string (≥100 chars)",
  "feedback": "string (≥50 chars, ≤2000 chars)"
}
```

### Response 201 Created
```json
{
  "responseId": "guid",
  "responseType": "InvestorResponse",
  "innovationId": "guid",
  "actorId": "guid",
  "status": "Pending",
  "submittedAt": "2026-06-07T12:00:00Z"
}
```

### Error Responses
Same set as manufacturing endpoint (403 when actor type ≠ Investor).

---

## GET /innovations/{innovationId}/bids

**Auth**: Bearer JWT required (authenticated actors only)  
**Behavior**: Returns all FormalResponses for the innovation. Three visibility tiers apply:
- **Innovation owner** — full data: base fields + `participationProposal` + financial projections + rationale
- **Submitting actor** — for their own response: base fields + their own `participationProposal`; for other actors' responses: base fields only
- **All other authenticated actors** — base fields only: `responseType`, `actorId`, `location`, `participationType`, `status`, `submittedAt`

### Query Parameters
| Param | Type | Description |
|-------|------|-------------|
| `type` | string (optional) | Filter by: `manufacturing`, `sales`, `rd`, `investor` |

### Response 200 OK (owner view)
```json
{
  "innovationId": "guid",
  "responses": [
    {
      "responseId": "guid",
      "responseType": "ManufacturingResponse",
      "actorId": "guid",
      "location": "Europe",
      "participationType": "Manufacturing Partner",
      "participationProposal": "...",
      "status": "Pending",
      "submittedAt": "2026-06-07T12:00:00Z",
      "yearlyManufacturingCosts": [ /* full projection data */ ]
    },
    {
      "responseId": "guid",
      "responseType": "SalesMarketingResponse",
      "actorId": "guid",
      "location": "Americas",
      "participationType": "Sales & Marketing Partner",
      "participationProposal": "...",
      "status": "Pending",
      "submittedAt": "2026-06-07T12:00:00Z",
      "yearlySales": [ /* full projection data */ ]
    }
  ]
}
```

### Response 200 OK (submitting actor view — own response only)
For the response the calling actor submitted: full response data is returned — `participationProposal`, `feedback` (InvestorResponse), type-specific financial projections, and all rationale fields are included. For all other responses in the list, only public summary fields are returned.

```json
{
  "innovationId": "guid",
  "responses": [
    {
      "responseId": "guid",
      "responseType": "ManufacturingResponse",
      "actorId": "guid",
      "location": "Europe",
      "participationType": "Manufacturing Partner",
      "participationProposal": "...",
      "status": "Pending",
      "submittedAt": "2026-06-07T12:00:00Z"
    },
    {
      "responseId": "guid",
      "responseType": "SalesMarketingResponse",
      "actorId": "guid",
      "location": "Americas",
      "participationType": "Sales & Marketing Partner",
      "status": "Pending",
      "submittedAt": "2026-06-07T12:00:00Z"
    }
  ]
}
```

### Response 200 OK (other authenticated actors view)
Only public summary fields returned. `participationProposal`, financial projections, `feedback`, and all rationale strings are **omitted** for all responses.

### Error Responses
| Status | When |
|--------|------|
| 401 | No/invalid JWT |
| 404 | Innovation not found |

---

## POST /innovations/{innovationId}/select-partners (UPDATED)

**Change from Phase 1c**: `selectedBidIds` field renamed to `selectedResponseIds`.

### Request Body (updated)
```json
{
  "selectedResponseIds": ["guid", "guid", "guid"]
}
```

### Response and error contract: unchanged from Phase 1c.

---

## Removed Endpoints

| Endpoint | Reason |
|----------|--------|
| `POST /innovations/{id}/bids` | Replaced by 4 typed endpoints |
| `PUT /bids/{bidId}` | Out of scope — UpdateBid removed with Bid entity |
