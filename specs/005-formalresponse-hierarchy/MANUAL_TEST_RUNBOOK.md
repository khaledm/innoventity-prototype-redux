# Manual Test Runbook: 005-FormalResponse Polymorphic Hierarchy

**Feature**: `005-formalresponse-hierarchy` | **Date**: 2026-07-19 | **Environment**: Azure Dev

**Tool**: Scalar API Explorer (`/scalar` endpoint on your deployed service)

**Estimated Duration**: 45–60 minutes

---

## Prerequisites

### 1. Deployment Verification
- [ ] Azure Dev environment is online
- [ ] Scalar is accessible at `https://<your-app>.azurewebsites.net/scalar`
- [ ] Database migrations applied (`AddFormalResponseHierarchy` migration must be in place)
- [ ] Seed data populated (actors and at least one innovation)

### 2. Test Accounts Needed
You'll need JWT tokens for 5 different actors:

| Role | ActorType | Used In | Token Variable |
|------|-----------|---------|-----------------|
| **Idea Owner** | IdeaGenerator | Setup & verification | `$OWNER_TOKEN` |
| **Manufacturing Co** | Manufacturing | US1 tests | `$MFG_TOKEN` |
| **Sales/Marketing Co** | SalesMarketing | US2 tests | `$SALES_TOKEN` |
| **R&D Org** | RD | US3 tests | `$RD_TOKEN` |
| **Investment Firm** | Investor | US4 tests | `$INVESTOR_TOKEN` |

**How to get tokens**:
1. Open Scalar at your deployed endpoint
2. Navigate to `POST /auth/login`
3. For each actor, log in with their credentials (or use your registration flow if needed)
4. Copy the JWT from the response and note it in the table below
5. Keep these tokens available for all test steps

### 3. Test Innovation Setup
Before starting, you need a Published innovation:

1. **Create Innovation** (using `$OWNER_TOKEN`):
   - Navigate to `POST /innovations` in Scalar
   - Submit a new innovation with status "Published"
   - **Copy the `innovationId`** — you'll use it for all tests below
   - Note: `$INNOVATION_ID = <your-innovation-id>`

---

## Phase 1: Happy Path Tests — User Story 1 (Manufacturing Response)

**Goal**: Verify a Manufacturing actor can submit a 3-year cost projection response.

### Test 1.1: Submit Manufacturing Response (Happy Path)

**Endpoint**: `POST /innovations/{innovationId}/bids/manufacturing`  
**Auth**: `$MFG_TOKEN`  
**Innovation**: `$INNOVATION_ID` (Published, not owned by Manufacturing actor)

1. Open Scalar → search for `POST .../bids/manufacturing`
2. Click **Try it out**
3. Replace `{innovationId}` with `$INNOVATION_ID`
4. Paste the following in the request body:

```json
{
  "location": "Europe",
  "participationType": "Manufacturing Partner",
  "participationProposal": "We have 25 years of automotive component manufacturing experience across EU and can scale to your volume requirements within 6 months of final design approval. Our facilities are ISO 9001 certified with redundant production lines.",
  "yearlyManufacturingCosts": [
    {
      "year": 1,
      "productionVolume": 50000,
      "productionVolumeRationale": "Conservative first-year estimate based on market penetration analysis and supply chain validation. Assumes ramp-up from month 6.",
      "unitCost": 12.50,
      "unitCostRationale": "Current injection molding cost plus material markup. Negotiable at volumes >100K units/year.",
      "averageGlobalDistributionExpense": 2.30,
      "avgDistributionExpenseRationale": "Logistics to major EU distribution hubs. Includes palletization and cold-chain compliance for sensitive components."
    },
    {
      "year": 2,
      "productionVolume": 120000,
      "productionVolumeRationale": "Market growth projection + supply agreements with 3 major retailers already in discussions.",
      "unitCost": 11.80,
      "unitCostRationale": "2% cost reduction from economies of scale and material supplier negotiations planned for Q1 Year 2.",
      "averageGlobalDistributionExpense": 2.15,
      "avgDistributionExpenseRationale": "Lower per-unit logistics cost due to consolidated shipments and increased retailer pickup frequency."
    },
    {
      "year": 3,
      "productionVolume": 250000,
      "productionVolumeRationale": "Full market maturity with global distribution. Based on signed LOIs from Asian and North American distributors.",
      "unitCost": 11.20,
      "unitCostRationale": "Additional 3% reduction from assembly line automation investment planned for year 2 ROI.",
      "averageGlobalDistributionExpense": 1.95,
      "avgDistributionExpenseRationale": "Direct-to-retailer logistics network established; third-party logistics provider agreement reduces overhead."
    }
  ]
}
```

5. Click **Send Request**

**Expected Response**: `201 Created`

```json
{
  "responseId": "<guid>",
  "responseType": "ManufacturingResponse",
  "innovationId": "$INNOVATION_ID",
  "actorId": "<mfg-actor-id>",
  "status": "Pending",
  "submittedAt": "2026-07-19T..."
}
```

**Verification**:
- [ ] HTTP status is `201 Created`
- [ ] `responseType` is exactly `"ManufacturingResponse"`
- [ ] `status` is `"Pending"`
- [ ] **Copy `responseId`** → note as `$MFG_RESPONSE_ID`

---

## Phase 2: Happy Path Tests — User Story 2 (Sales/Marketing Response)

**Goal**: Verify SalesMarketing actor can submit revenue projections.

### Test 2.1: Submit SalesMarketing Response

**Endpoint**: `POST /innovations/{innovationId}/bids/sales`  
**Auth**: `$SALES_TOKEN`  
**Innovation**: `$INNOVATION_ID`

1. In Scalar, search for `POST .../bids/sales`
2. Click **Try it out**
3. Replace `{innovationId}` with `$INNOVATION_ID`
4. Paste:

```json
{
  "location": "Americas",
  "participationType": "Sales & Marketing Partner",
  "participationProposal": "Our 15-person B2B sales team covers 200+ retail accounts across North America (Walmart, Best Buy, Target, Amazon). Average order cycle is 60 days from POC to signed PO. We've successfully launched 12 consumer electronics in the past 3 years with average shelf penetration of 45% within 9 months.",
  "yearlySales": [
    {
      "year": 1,
      "unitsSold": 25000,
      "unitsSoldRationale": "Initial launch with 50 retail locations and 3 online channels. Conservative forecast based on similar product category performance in pilot markets.",
      "unitPrice": 79.99,
      "unitPriceRationale": "Premium tier positioning vs $49.99 competitors. Market research shows 35% willingness to pay for superior features at this price point.",
      "salesMarketingExpense": 180000.00,
      "salesMarketingExpenseRationale": "Includes launch event, trade show presence (2 major venues), digital ad spend (Google/Facebook), and retail training materials for 200 store associates."
    },
    {
      "year": 2,
      "unitsSold": 85000,
      "unitsSoldRationale": "Expansion to 150 retail locations plus e-commerce growth. Word-of-mouth and reviews drive organic traffic after successful Year 1 launch.",
      "unitPrice": 74.99,
      "unitPriceRationale": "Strategic $5 price reduction to capture mid-market segment and compete with new market entrants expected in Year 2.",
      "salesMarketingExpense": 220000.00,
      "salesMarketingExpenseRationale": "Sustained marketing cadence, expanded digital channels, and seasonal promotions (Black Friday, holiday campaigns) to maintain shelf presence."
    },
    {
      "year": 3,
      "unitsSold": 150000,
      "unitsSoldRationale": "Market maturity across North America. Estimated market share 8% in addressable category based on total addressable market analysis.",
      "unitPrice": 69.99,
      "unitPriceRationale": "Competitive pricing as category matures and production costs decline. Volume discounting begins for bulk orders from enterprise customers.",
      "salesMarketingExpense": 250000.00,
      "salesMarketingExpenseRationale": "Brand maintenance spend; focus shifts from acquisition to retention and upselling to existing customer base via loyalty programs."
    }
  ]
}
```

5. Click **Send Request**

**Expected Response**: `201 Created`

```json
{
  "responseId": "<guid>",
  "responseType": "SalesMarketingResponse",
  "innovationId": "$INNOVATION_ID",
  "actorId": "<sales-actor-id>",
  "status": "Pending",
  "submittedAt": "2026-07-19T..."
}
```

**Verification**:
- [ ] HTTP status is `201 Created`
- [ ] `responseType` is exactly `"SalesMarketingResponse"`
- [ ] **Copy `responseId`** → note as `$SALES_RESPONSE_ID`

---

## Phase 3: Happy Path Tests — User Story 3 (R&D Response)

**Goal**: Verify R&D actor can submit development duration and costs.

### Test 3.1: Submit R&D Response

**Endpoint**: `POST /innovations/{innovationId}/bids/rd`  
**Auth**: `$RD_TOKEN`  
**Innovation**: `$INNOVATION_ID`

1. In Scalar, search for `POST .../bids/rd`
2. Click **Try it out**
3. Replace `{innovationId}` with `$INNOVATION_ID`
4. Paste:

```json
{
  "location": "Asia",
  "participationType": "R&D Partner",
  "participationProposal": "Our 80-person R&D team specializes in materials science and embedded systems. We've delivered 22 products from concept to production in the past 5 years, with average time-to-market of 18 months. Our facilities include a 5000 sq-m prototyping lab with injection molding, PCB assembly, and environmental testing chambers.",
  "productDevelopmentDuration": 2,
  "yearlyDevelopmentCosts": [
    {
      "year": 1,
      "infrastructureCost": 500000.00,
      "infrastructureCostRationale": "Lab equipment maintenance, utilities (electricity, climate control for sensitive equipment), software licenses (CAD, simulation tools), and facility lease for dedicated development space.",
      "peopleCost": 1200000.00,
      "peopleCostRationale": "12-person core team @ avg $100K salary + benefits. Includes 2 senior materials engineers ($140K), 3 firmware engineers ($130K), and support staff. Assumes 80% allocation to this project."
    },
    {
      "year": 2,
      "infrastructureCost": 350000.00,
      "infrastructureCostRationale": "Reduced facility load post-design-freeze. Transition to production validation and manufacturing support. Includes reliability testing and regulatory compliance certification support.",
      "peopleCost": 800000.00,
      "peopleCostRationale": "8-person team for production support and optimization. Full project closeout by mid-Year 2; remaining budget allocated to manufacturing liaison and post-launch technical support."
    }
  ]
}
```

5. Click **Send Request**

**Expected Response**: `201 Created`

```json
{
  "responseId": "<guid>",
  "responseType": "ResearchDevelopmentResponse",
  "innovationId": "$INNOVATION_ID",
  "actorId": "<rd-actor-id>",
  "status": "Pending",
  "submittedAt": "2026-07-19T..."
}
```

**Verification**:
- [ ] HTTP status is `201 Created`
- [ ] `responseType` is exactly `"ResearchDevelopmentResponse"`
- [ ] **Copy `responseId`** → note as `$RD_RESPONSE_ID`

---

## Phase 4: Happy Path Tests — User Story 4 (Investor Response)

**Goal**: Verify Investor actor can submit lightweight feedback response.

### Test 4.1: Submit Investor Response

**Endpoint**: `POST /innovations/{innovationId}/bids/investor`  
**Auth**: `$INVESTOR_TOKEN`  
**Innovation**: `$INNOVATION_ID`

1. In Scalar, search for `POST .../bids/investor`
2. Click **Try it out**
3. Replace `{innovationId}` with `$INNOVATION_ID`
4. Paste:

```json
{
  "location": "Europe",
  "participationType": "Investment Partner",
  "participationProposal": "We are a venture capital firm with €150M under management, specializing in deep-tech and consumer electronics. Our portfolio includes 8 exits to strategic acquirers (avg 4.5x return). We provide not just capital but operational expertise and executive network to help portfolio companies scale internationally.",
  "feedback": "Strong technical team and clear market need. Manufacturing costs appear conservative; opportunity to improve gross margins 5–8 points through supply chain optimization. Recommend Series A round of €5–7M to reach profitability threshold. Key risk: regulatory approval timeline in EU and Asia may slip; build 6-month contingency buffer into timeline. Otherwise solid investment thesis."
}
```

5. Click **Send Request**

**Expected Response**: `201 Created`

```json
{
  "responseId": "<guid>",
  "responseType": "InvestorResponse",
  "innovationId": "$INNOVATION_ID",
  "actorId": "<investor-actor-id>",
  "status": "Pending",
  "submittedAt": "2026-07-19T..."
}
```

**Verification**:
- [ ] HTTP status is `201 Created`
- [ ] `responseType` is exactly `"InvestorResponse"`
- [ ] **Copy `responseId`** → note as `$INVESTOR_RESPONSE_ID`

---

## Phase 5: Happy Path Tests — User Story 5 (View & Compare Responses)

**Goal**: Verify innovation owner can see all responses with full financial data.

### Test 5.1: Retrieve All Responses (Owner View)

**Endpoint**: `GET /innovations/{innovationId}/bids`  
**Auth**: `$OWNER_TOKEN` (the innovation owner)  
**Innovation**: `$INNOVATION_ID`

1. In Scalar, search for `GET /innovations/{innovationId}/bids`
2. Click **Try it out**
3. Replace `{innovationId}` with `$INNOVATION_ID`
4. Leave query parameters empty for now (no filter)
5. Click **Send Request**

**Expected Response**: `200 OK`

```json
{
  "innovationId": "$INNOVATION_ID",
  "responses": [
    {
      "responseId": "$MFG_RESPONSE_ID",
      "responseType": "ManufacturingResponse",
      "actorId": "<mfg-actor-id>",
      "location": "Europe",
      "participationType": "Manufacturing Partner",
      "participationProposal": "...",
      "status": "Pending",
      "submittedAt": "2026-07-19T...",
      "yearlyManufacturingCosts": [
        {
          "year": 1,
          "productionVolume": 50000,
          "productionVolumeRationale": "...",
          "unitCost": 12.50,
          "unitCostRationale": "...",
          "averageGlobalDistributionExpense": 2.30,
          "avgDistributionExpenseRationale": "..."
        },
        /* years 2 and 3 present with full data */
      ]
    },
    {
      "responseId": "$SALES_RESPONSE_ID",
      "responseType": "SalesMarketingResponse",
      "actorId": "<sales-actor-id>",
      "location": "Americas",
      "participationType": "Sales & Marketing Partner",
      "participationProposal": "...",
      "status": "Pending",
      "submittedAt": "2026-07-19T...",
      "yearlySales": [
        /* full data for all 3 years */
      ]
    },
    {
      "responseId": "$RD_RESPONSE_ID",
      "responseType": "ResearchDevelopmentResponse",
      "actorId": "<rd-actor-id>",
      "location": "Asia",
      "participationType": "R&D Partner",
      "participationProposal": "...",
      "status": "Pending",
      "submittedAt": "2026-07-19T...",
      "yearlyDevelopmentCosts": [
        /* full data for both years */
      ]
    },
    {
      "responseId": "$INVESTOR_RESPONSE_ID",
      "responseType": "InvestorResponse",
      "actorId": "<investor-actor-id>",
      "location": "Europe",
      "participationType": "Investment Partner",
      "participationProposal": "...",
      "status": "Pending",
      "submittedAt": "2026-07-19T...",
      "feedback": "..."
    }
  ]
}
```

**Verification**:
- [ ] HTTP status is `200 OK`
- [ ] Response contains all 4 responses (manufacturing, sales, R&D, investor)
- [ ] Each response includes `responseType` discriminator
- [ ] **Owner sees all financial data**: 
  - [ ] Manufacturing response includes full `yearlyManufacturingCosts` with all rationale fields
  - [ ] Sales response includes full `yearlySales` with all rationale fields
  - [ ] R&D response includes `productDevelopmentDuration` and full `yearlyDevelopmentCosts` with rationale
  - [ ] Investor response includes `feedback` field
- [ ] All `participationProposal` fields are present (100+ characters)

### Test 5.2: Filter Responses by Type

**Endpoint**: `GET /innovations/{innovationId}/bids?type=manufacturing`  
**Auth**: `$OWNER_TOKEN`

1. Modify the request from Test 5.1
2. Add query parameter: `type=manufacturing` (in Scalar's query parameter section)
3. Click **Send Request**

**Expected Response**: `200 OK`, only 1 response (ManufacturingResponse)

**Verification**:
- [ ] Only 1 response in the list
- [ ] `responseType` is `"ManufacturingResponse"`

**Repeat for other types**:
- [ ] `?type=sales` → returns only `SalesMarketingResponse` (1 item)
- [ ] `?type=rd` → returns only `ResearchDevelopmentResponse` (1 item)
- [ ] `?type=investor` → returns only `InvestorResponse` (1 item)

---

## Phase 6: Visibility Tests — 3-Tier Permission Model

**Goal**: Verify that response visibility is correctly restricted based on actor role.

### Test 6.1: Manufacturing Actor Views Responses (Their Own + Public-Only Others)

**Endpoint**: `GET /innovations/{innovationId}/bids`  
**Auth**: `$MFG_TOKEN` (the Manufacturing actor who submitted)  
**Innovation**: `$INNOVATION_ID`

1. Switch auth to `$MFG_TOKEN` in Scalar
2. Call the same endpoint
3. Click **Send Request**

**Expected Response**: `200 OK`

```json
{
  "innovationId": "$INNOVATION_ID",
  "responses": [
    {
      "responseId": "$MFG_RESPONSE_ID",
      "responseType": "ManufacturingResponse",
      "actorId": "<mfg-actor-id>",
      "location": "Europe",
      "participationType": "Manufacturing Partner",
      "participationProposal": "We have 25 years...",  /* FULL PROPOSAL */
      "status": "Pending",
      "submittedAt": "2026-07-19T...",
      "yearlyManufacturingCosts": [
        /* FULL PROJECTION DATA WITH ALL RATIONALE */
      ]
    },
    {
      "responseId": "$SALES_RESPONSE_ID",
      "responseType": "SalesMarketingResponse",
      "actorId": "<sales-actor-id>",
      "location": "Americas",
      "participationType": "Sales & Marketing Partner",
      "status": "Pending",
      "submittedAt": "2026-07-19T..."
      /* NO participationProposal, NO yearlySales data */
    },
    {
      "responseId": "$RD_RESPONSE_ID",
      "responseType": "ResearchDevelopmentResponse",
      "actorId": "<rd-actor-id>",
      "location": "Asia",
      "participationType": "R&D Partner",
      "status": "Pending",
      "submittedAt": "2026-07-19T..."
      /* NO participationProposal, NO yearlyDevelopmentCosts data */
    },
    {
      "responseId": "$INVESTOR_RESPONSE_ID",
      "responseType": "InvestorResponse",
      "actorId": "<investor-actor-id>",
      "location": "Europe",
      "participationType": "Investment Partner",
      "status": "Pending",
      "submittedAt": "2026-07-19T..."
      /* NO participationProposal, NO feedback field */
    }
  ]
}
```

**Verification**:
- [ ] **Own response** (MFG): Full `participationProposal` + full `yearlyManufacturingCosts` data visible
- [ ] **Other responses** (Sales, RD, Investor): Only public fields visible — `responseType`, `actorId`, `location`, `participationType`, `status`, `submittedAt`
- [ ] **NO financial data** visible for other actors' responses
- [ ] **NO proposals** visible for other actors' responses

### Test 6.2: Third-Party Actor Views Responses (Public-Only)

**Endpoint**: `GET /innovations/{innovationId}/bids`  
**Auth**: Use a different actor token (e.g., create/log in as a 6th actor not involved in this innovation)  
**Innovation**: `$INNOVATION_ID`

1. Switch auth to a third-party actor token
2. Call the same endpoint
3. Click **Send Request**

**Expected Response**: `200 OK`, all responses with **ONLY public fields**

```json
{
  "innovationId": "$INNOVATION_ID",
  "responses": [
    {
      "responseId": "$MFG_RESPONSE_ID",
      "responseType": "ManufacturingResponse",
      "actorId": "<mfg-actor-id>",
      "location": "Europe",
      "participationType": "Manufacturing Partner",
      "status": "Pending",
      "submittedAt": "2026-07-19T..."
      /* ONLY these 6 fields — no proposal, no costs */
    },
    /* ... 3 more responses, same structure */
  ]
}
```

**Verification**:
- [ ] All 4 responses present
- [ ] Each response contains **only** 6 public fields: `responseType`, `actorId`, `location`, `participationType`, `status`, `submittedAt`
- [ ] **NO `participationProposal`** field in any response
- [ ] **NO financial data** (`yearlyManufacturingCosts`, `yearlySales`, `yearlyDevelopmentCosts`)
- [ ] **NO `feedback`** field for InvestorResponse

---

## Phase 7: Validation & Error Cases

**Goal**: Verify that invalid submissions are rejected with appropriate error codes and messages.

### Test 7.1: Rationale Too Short (Under 20 Characters)

**Endpoint**: `POST /innovations/{innovationId}/bids/manufacturing`  
**Auth**: `$MFG_TOKEN`  
**Innovation**: Create a **new** innovation (don't reuse `$INNOVATION_ID` — the actor already has a response there)

1. Get a new Published innovation ID (or create one)
2. Call the manufacturing endpoint with this payload (note: `productionVolumeRationale` is only 15 chars):

```json
{
  "location": "Europe",
  "participationType": "Manufacturing Partner",
  "participationProposal": "We are a manufacturing partner with decades of experience in precision components.",
  "yearlyManufacturingCosts": [
    {
      "year": 1,
      "productionVolume": 50000,
      "productionVolumeRationale": "Too short!",
      "unitCost": 12.50,
      "unitCostRationale": "This is a valid rationale with sufficient length for testing purposes.",
      "averageGlobalDistributionExpense": 2.30,
      "avgDistributionExpenseRationale": "Distribution costs are calculated based on logistics network analysis."
    }
  ]
}
```

3. Click **Send Request**

**Expected Response**: `422 Unprocessable Entity`

```json
{
  "type": "https://...",
  "title": "Validation Failed",
  "status": 422,
  "detail": "Validation error(s) occurred.",
  "errors": {
    "productionVolumeRationale": [
      "Year 1: productionVolumeRationale must be between 20 and 500 characters."
    ]
  }
}
```

**Verification**:
- [ ] HTTP status is `422`
- [ ] Error message identifies the **specific field** (`productionVolumeRationale`)
- [ ] Error message identifies the **year** (`Year 1`)
- [ ] Error message states the constraint (20–500 chars)

### Test 7.2: Non-Contiguous Years

**Endpoint**: `POST /innovations/{innovationId}/bids/manufacturing`  
**Auth**: `$MFG_TOKEN`  
**Innovation**: A new Published innovation

Payload with years [1, 3] (missing year 2):

```json
{
  "location": "Europe",
  "participationType": "Manufacturing Partner",
  "participationProposal": "We are a manufacturing partner with decades of experience.",
  "yearlyManufacturingCosts": [
    {
      "year": 1,
      "productionVolume": 50000,
      "productionVolumeRationale": "This is year 1 with a valid rationale string that meets character requirements.",
      "unitCost": 12.50,
      "unitCostRationale": "Unit cost rationale for year 1 with sufficient detail and character count.",
      "averageGlobalDistributionExpense": 2.30,
      "avgDistributionExpenseRationale": "Distribution expense rationale for year 1 calculated from logistics data."
    },
    {
      "year": 3,
      "productionVolume": 120000,
      "productionVolumeRationale": "Year 3 production volume rationale with appropriate level of detail and justification.",
      "unitCost": 11.50,
      "unitCostRationale": "Year 3 unit cost rationale reflecting economies of scale and supply agreements.",
      "averageGlobalDistributionExpense": 2.00,
      "avgDistributionExpenseRationale": "Year 3 distribution rationale with optimized logistics network and reduced costs."
    }
  ]
}
```

3. Click **Send Request**

**Expected Response**: `422 Unprocessable Entity`

```json
{
  "type": "https://...",
  "title": "Validation Failed",
  "status": 422,
  "detail": "Validation error(s) occurred.",
  "errors": {
    "yearlyManufacturingCosts": [
      "Years must be contiguous starting from 1. Missing year(s): 2"
    ]
  }
}
```

**Verification**:
- [ ] HTTP status is `422`
- [ ] Error message identifies the **missing year(s)** (specifically "2")

### Test 7.3: Duplicate Response (Actor Already Submitted)

**Endpoint**: `POST /innovations/{innovationId}/bids/manufacturing`  
**Auth**: `$MFG_TOKEN`  
**Innovation**: `$INNOVATION_ID` (the one where Manufacturing already submitted in Test 1.1)

Use the same payload as Test 1.1.

**Expected Response**: `409 Conflict`

```json
{
  "type": "https://...",
  "title": "Conflict",
  "status": 409,
  "detail": "An actor of type Manufacturing has already submitted a response for this innovation."
}
```

**Verification**:
- [ ] HTTP status is `409 Conflict`
- [ ] Error message references the duplicate submission

### Test 7.4: Wrong Actor Type

**Endpoint**: `POST /innovations/{innovationId}/bids/manufacturing`  
**Auth**: `$SALES_TOKEN` (using a Sales actor, not Manufacturing)  
**Innovation**: A new Published innovation

1. Call the manufacturing endpoint with Sales actor token
2. Click **Send Request**

**Expected Response**: `403 Forbidden`

```json
{
  "type": "https://...",
  "title": "Forbidden",
  "status": 403,
  "detail": "Only Manufacturing actors can submit ManufacturingResponse."
}
```

**Verification**:
- [ ] HTTP status is `403`
- [ ] Error clearly states the actor type requirement

### Test 7.5: Innovation Not Published

**Endpoint**: `POST /innovations/{innovationId}/bids/manufacturing`  
**Auth**: `$MFG_TOKEN`  
**Innovation**: Create a **Draft** (not Published) innovation and get its ID

1. Call the manufacturing endpoint with a Draft innovation ID
2. Click **Send Request**

**Expected Response**: `404 Not Found`

```json
{
  "type": "https://...",
  "title": "Not Found",
  "status": 404,
  "detail": "Innovation not found or is not in Published status."
}
```

**Verification**:
- [ ] HTTP status is `404`

### Test 7.6: Proposal Too Short (Under 100 Characters)

**Endpoint**: `POST /innovations/{innovationId}/bids/manufacturing`  
**Auth**: `$MFG_TOKEN`  
**Innovation**: A new Published innovation

Payload with short proposal:

```json
{
  "location": "Europe",
  "participationType": "Manufacturing Partner",
  "participationProposal": "Short proposal",
  "yearlyManufacturingCosts": [
    {
      "year": 1,
      "productionVolume": 50000,
      "productionVolumeRationale": "Production volume rationale that meets the minimum character requirement.",
      "unitCost": 12.50,
      "unitCostRationale": "Unit cost rationale that meets the minimum character requirement for validation.",
      "averageGlobalDistributionExpense": 2.30,
      "avgDistributionExpenseRationale": "Distribution expense rationale that meets the minimum character length requirement."
    }
  ]
}
```

**Expected Response**: `400 Bad Request`

**Verification**:
- [ ] HTTP status is `400`
- [ ] Error references `participationProposal` and the 100-character minimum

### Test 7.7: Year Beyond Max (Year 11)

**Endpoint**: `POST /innovations/{innovationId}/bids/manufacturing`  
**Auth**: `$MFG_TOKEN`  
**Innovation**: A new Published innovation

Payload with year 11:

```json
{
  "location": "Europe",
  "participationType": "Manufacturing Partner",
  "participationProposal": "We are a manufacturing partner with decades of experience in precision components and supply chain management.",
  "yearlyManufacturingCosts": [
    {
      "year": 1,
      "productionVolume": 50000,
      "productionVolumeRationale": "Year 1 production volume rationale with sufficient detail and character count.",
      "unitCost": 12.50,
      "unitCostRationale": "Year 1 unit cost rationale based on current market rates and negotiations.",
      "averageGlobalDistributionExpense": 2.30,
      "avgDistributionExpenseRationale": "Year 1 distribution expense rationale calculated from logistics network."
    },
    {
      "year": 11,
      "productionVolume": 100000,
      "productionVolumeRationale": "Year 11 production volume rationale beyond normal planning horizon.",
      "unitCost": 10.00,
      "unitCostRationale": "Year 11 unit cost rationale assuming long-term optimization.",
      "averageGlobalDistributionExpense": 1.50,
      "avgDistributionExpenseRationale": "Year 11 distribution expense rationale with mature logistics network."
    }
  ]
}
```

**Expected Response**: `422 Unprocessable Entity`

**Verification**:
- [ ] HTTP status is `422`
- [ ] Error message references max year limit (10)

---

## Phase 8: Partner Selection Integration (User Story 6)

**Goal**: Verify that SelectPartners still works with the new typed responses.

### Test 8.1: Select Three Partners

**Prerequisites**:
- You have 3 responses already submitted (manufacturing, sales, R&D) from earlier tests
- Note their `responseId` values: `$MFG_RESPONSE_ID`, `$SALES_RESPONSE_ID`, `$RD_RESPONSE_ID`

**Endpoint**: `POST /innovations/{innovationId}/select-partners`  
**Auth**: `$OWNER_TOKEN` (the innovation owner)  
**Innovation**: `$INNOVATION_ID`

1. In Scalar, search for `POST .../select-partners`
2. Click **Try it out**
3. Replace `{innovationId}` with `$INNOVATION_ID`
4. Paste the following request body (using the 3 response IDs from your tests):

```json
{
  "selectedResponseIds": [
    "$MFG_RESPONSE_ID",
    "$SALES_RESPONSE_ID",
    "$RD_RESPONSE_ID"
  ]
}
```

5. Click **Send Request**

**Expected Response**: `200 OK`

```json
{
  "innovationId": "$INNOVATION_ID",
  "status": "PartnersSelected",
  "selectedResponses": [
    {
      "responseId": "$MFG_RESPONSE_ID",
      "actorId": "<mfg-actor-id>",
      "responseType": "ManufacturingResponse",
      "status": "Accepted",
      "acceptedAt": "2026-07-19T..."
    },
    {
      "responseId": "$SALES_RESPONSE_ID",
      "actorId": "<sales-actor-id>",
      "responseType": "SalesMarketingResponse",
      "status": "Accepted",
      "acceptedAt": "2026-07-19T..."
    },
    {
      "responseId": "$RD_RESPONSE_ID",
      "actorId": "<rd-actor-id>",
      "responseType": "ResearchDevelopmentResponse",
      "status": "Accepted",
      "acceptedAt": "2026-07-19T..."
    }
  ]
}
```

**Verification**:
- [ ] HTTP status is `200 OK`
- [ ] Innovation status changed to `"PartnersSelected"`
- [ ] All 3 responses have status `"Accepted"`
- [ ] All 3 responses have `acceptedAt` timestamp
- [ ] Each response includes its `responseType` discriminator

### Test 8.2: Verify Partner Selection is Immutable

**Endpoint**: `POST /innovations/{innovationId}/select-partners` (same as Test 8.1)  
**Auth**: `$OWNER_TOKEN`  
**Innovation**: `$INNOVATION_ID` (same innovation, now with PartnersSelected status)

1. Repeat the same request from Test 8.1
2. Click **Send Request**

**Expected Response**: `403 Forbidden`

```json
{
  "type": "https://...",
  "title": "Forbidden",
  "status": 403,
  "detail": "Partner selection can only be performed once. This innovation has already been through partner selection."
}
```

**Verification**:
- [ ] HTTP status is `403`
- [ ] Error message indicates immutability (cannot re-select)

---

## Phase 9: End-to-End Verification

**Goal**: Verify the complete workflow from submission to selection.

### Test 9.1: Full Workflow Validation

Use a completely fresh innovation and actors to run through the entire flow:

1. **Create a new Published innovation** with `$OWNER_TOKEN`
   - [ ] Innovation ID obtained: `$NEW_INNOVATION_ID`

2. **Submit all 4 response types** (manufacturing, sales, R&D, investor) using separate tokens
   - [ ] All 4 submissions return `201 Created`
   - [ ] All 4 response IDs captured

3. **Owner retrieves all responses**
   - [ ] GET request returns 4 responses
   - [ ] Owner sees full data (proposals + projections)

4. **Manufacturing actor retrieves responses**
   - [ ] GET request returns 4 responses
   - [ ] Own response is full; others are public-only

5. **Third-party actor retrieves responses**
   - [ ] GET request returns 4 responses
   - [ ] All responses show only public fields

6. **Filter responses by type**
   - [ ] `?type=manufacturing` returns 1
   - [ ] `?type=sales` returns 1
   - [ ] `?type=rd` returns 1
   - [ ] `?type=investor` returns 1

7. **SelectPartners with 3 financial types** (manufacturing, sales, R&D)
   - [ ] POST returns `200 OK`
   - [ ] Innovation status = `PartnersSelected`
   - [ ] 3 responses marked `Accepted` with `acceptedAt` timestamp
   - [ ] Investor response remains `Pending` (not part of selection)

8. **Try SelectPartners again** (immutability test)
   - [ ] POST returns `403 Forbidden`

**All Checks**:
- [ ] Steps 1–8 complete without errors
- [ ] All expected HTTP status codes match
- [ ] All data transformations work correctly

---

## Summary Checklist

### Happy Path (All Should Pass ✅)
- [ ] US1: Manufacturing response submission (3-year projection)
- [ ] US2: Sales/Marketing response submission (3-year projection)
- [ ] US3: R&D response submission (2-year development + duration)
- [ ] US4: Investor response submission (feedback)
- [ ] US5a: Owner views all responses with full data
- [ ] US5b: Type filtering works (?type=manufacturing, sales, rd, investor)
- [ ] US6: SelectPartners works with new response types
- [ ] US6: SelectPartners immutability enforced

### Visibility (3-Tier Model Should Work ✅)
- [ ] Owner sees full data for all responses
- [ ] Submitting actor sees own full data + public-only for others
- [ ] Third-party actor sees only public fields for all responses

### Validation (All Should Reject ✅)
- [ ] Rationale under 20 chars → 422 with field name
- [ ] Non-contiguous years → 422 with missing year identified
- [ ] Duplicate response → 409
- [ ] Wrong actor type → 403
- [ ] Innovation not Published → 404
- [ ] Proposal under 100 chars → 400
- [ ] Year beyond 10 → 422

### Data Integrity (All Should Persist ✅)
- [ ] 3-year manufacturing cost projections stored
- [ ] 3-year sales projections stored
- [ ] 2-year R&D development costs + duration stored
- [ ] Investor feedback stored
- [ ] All rationale fields persisted
- [ ] ResponseType discriminators saved correctly

---

## Troubleshooting

| Issue | Resolution |
|-------|-----------|
| 401 Unauthorized | Verify JWT token is current and includes correct claims. Re-authenticate in Scalar. |
| 404 Innovation not found | Verify innovation ID is correct. Check innovation status is "Published". |
| 409 Duplicate response | Actor already submitted for this innovation. Use a different actor or innovation. |
| 422 Validation failed | Check rationale fields are ≥20 chars, years are 1–10 and contiguous, proposal ≥100 chars. |
| Rationale field not in error message | If error omits field name, that's a bug. Log and report. |
| Visibility not working | Verify you're using correct auth tokens. Refresh tokens if expired. |

---

## Sign-Off

**Date Tested**: _______________  
**Tester Name**: _______________  
**Environment**: Azure Dev  
**All Checks Passed**: ☐ YES ☐ NO  
**Issues Found**: _______________  
**Ready for Production**: ☐ YES ☐ NO

---

**End of Runbook**
