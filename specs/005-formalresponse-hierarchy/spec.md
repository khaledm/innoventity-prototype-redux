# Feature Specification: FormalResponse Polymorphic Hierarchy

**Feature Branch**: `005-formalresponse-hierarchy`

**Created**: 2026-06-07

**Status**: Implemented (2026-07-18) — all 49 tasks complete, 195 tests passing, mutation score 74.95% (aggregate, clears constitution >70% floor)

**Input**: Replace the flat `Bid` entity with a `FormalResponse` abstract base plus `ManufacturingResponse`, `SalesMarketingResponse`, `ResearchDevelopmentResponse`, and `InvestorResponse` subtypes. EF Core TPH discriminator. New type-specific bid endpoints. Yearly financial projection dictionaries with mandatory rationale fields.

**Reference**: [Legacy Domain Spec §R3.6](../002-domain-enhancements/spec.md#r36-formalresponse-strategy-pattern), [Gap Analysis §2.1](../../.specify/analysis/innovation-bid-domain-gap-analysis.md#21-legacy-formalresponse---polymorphic-hierarchy)

---

## Context & Background

The current platform uses a single generic `Bid` entity with free-text fields (`Location`, `ParticipationType`, `ParticipationProposal`). This flat structure blocks three critical platform capabilities:

1. **Financial modeling** — cannot calculate NPV without structured yearly projection data (production volume, unit cost, sales figures, R&D costs)
2. **Objective bid comparison** — innovation owners cannot compare manufacturing costs or revenue projections across multiple bidders
3. **Business plan generation** — the auto-generated business plan requires typed financial data from all three partner types

The legacy system solved this with a polymorphic `FormalResponse` hierarchy. This feature ports that design to the current platform, replacing the generic bid entity with role-specific response types that enforce structured financial data and mandatory audit rationale fields.

**Breaking changes**: All bid submission and retrieval endpoints change. Existing seed data and tests referencing the flat `Bid` entity must be updated.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Manufacturing Actor Submits Typed Bid with Cost Projections (Priority: P1)

As a **Manufacturing actor**, I want to submit a partnership response that includes yearly production volumes and unit costs so that the innovation owner can objectively compare my manufacturing capabilities against other manufacturers.

**Why this priority**: P1 — core differentiator of the platform. Without structured financial data, bid comparison is impossible and the business plan cannot be auto-generated. Manufacturing bids are the most data-rich type and drive the majority of NPV calculation.

**Independent Test**: Can be fully tested by submitting a ManufacturingResponse with a 3-year cost projection, retrieving it, and verifying each year's ProductionVolume, UnitCost, and all three Rationale fields are stored and returned correctly.

**Acceptance Scenarios**:

1. **Given** a Manufacturing actor viewing a Published innovation, **When** the actor submits a response including yearly manufacturing costs for years 1–3, **Then** the system stores the structured projection data and returns the response ID
2. **Given** a Manufacturing actor submitting a response, **When** any Rationale field is shorter than 20 characters, **Then** the system rejects the submission with a validation error identifying the specific field
3. **Given** a Manufacturing actor submitting a response, **When** the same actor has already submitted a response for that innovation, **Then** the system rejects with a duplicate-bid error (one response per actor per innovation)
4. **Given** a Manufacturing actor submitting a response, **When** the actor does not own a Manufacturing actor type, **Then** the system rejects with a forbidden error

---

### User Story 2 — Sales/Marketing Actor Submits Typed Bid with Revenue Projections (Priority: P1)

As a **Sales/Marketing actor**, I want to submit a partnership response that includes yearly sales volume and pricing projections so that the innovation owner can evaluate market potential through my channel.

**Why this priority**: P1 — required alongside Manufacturing for a complete three-partner collaboration team. Revenue projections from the Sales actor are the primary income side of the NPV calculation.

**Independent Test**: Can be fully tested by submitting a SalesMarketingResponse with yearly units-sold and price projections, then verifying all SalesMarketingInformation fields including rationale strings are stored.

**Acceptance Scenarios**:

1. **Given** a Sales/Marketing actor viewing a Published innovation, **When** the actor submits a response with yearly revenue projections (units sold, price per unit, sales expense), **Then** the system stores all projection data and returns the response ID
2. **Given** a Sales/Marketing actor submitting a response, **When** UnitsSoldRationale is absent or under 20 characters, **Then** the system rejects with a field-specific validation error
3. **Given** a Sales/Marketing actor submitting a response with a 5-year projection, **When** year 3 data is missing, **Then** the system rejects (years must be a contiguous range starting at 1)

---

### User Story 3 — R&D Actor Submits Typed Bid with Development Cost Projections (Priority: P1)

As an **R&D actor**, I want to submit a partnership response that includes development duration and yearly infrastructure and people costs so that the innovation owner understands the investment required for the technical development phase.

**Why this priority**: P1 — completes the three required partner types. Development costs from the R&D actor feed the expense side of the NPV calculation.

**Independent Test**: Can be fully tested by submitting a ResearchDevelopmentResponse with a 2-year development plan, then verifying ProductDevelopmentDuration and all YearlyDevelopmentCosts fields are stored and accessible.

**Acceptance Scenarios**:

1. **Given** an R&D actor viewing a Published innovation, **When** the actor submits a response with ProductDevelopmentDuration and yearly development costs, **Then** the system stores all projection data and returns the response ID
2. **Given** an R&D actor submitting a response, **When** InfrastructureCostRationale is under 20 characters, **Then** the system rejects with a field-specific validation error
3. **Given** an R&D actor submitting a response with ProductDevelopmentDuration=3, **When** the yearly development costs dictionary contains data for year 4, **Then** the system accepts it (projections may extend beyond development duration)

---

### User Story 4 — Investor Actor Submits Feedback Response (Priority: P2)

As an **Investor actor**, I want to submit a lightweight response expressing my interest and feedback on an innovation so that the innovation owner knows there is investor attention, even before formal investment terms are defined.

**Why this priority**: P2 — investor responses are structurally simpler (free text only) and do not participate in NPV calculation, but are still part of the polymorphic hierarchy for consistency.

**Independent Test**: Can be fully tested by submitting an InvestorResponse with a feedback string, retrieving it, and verifying it is stored as a distinct type from the financial response types.

**Acceptance Scenarios**:

1. **Given** an Investor actor viewing a Published innovation, **When** the actor submits a response with feedback text, **Then** the system stores it and returns the response ID
2. **Given** any actor type submitting a response, **When** querying responses for an innovation, **Then** the system returns all response types in a unified list distinguishable by type

---

### User Story 5 — Innovation Owner Views and Compares Typed Responses (Priority: P1)

As an **Innovation owner**, I want to view all formal responses for my innovation, grouped by type and showing the structured financial projections, so that I can make an informed partner selection decision based on objective data.

**Why this priority**: P1 — the entire purpose of the financial projection data is to enable comparison. If the owner cannot see and compare projections, the typing adds no user value.

**Independent Test**: Can be fully tested by seeding three typed responses (Manufacturing, SalesMarketing, RD) for an innovation, calling the view endpoint, and verifying each response type returns its specific projection fields.

**Acceptance Scenarios**:

1. **Given** an innovation with one ManufacturingResponse, one SalesMarketingResponse, and one ResearchDevelopmentResponse, **When** the owner retrieves responses, **Then** each response includes its type-specific projection data (not just generic fields)
2. **Given** an innovation owner viewing responses, **When** multiple Manufacturing actors have responded, **Then** the owner can see all ManufacturingResponses with full cost breakdowns for side-by-side comparison
3. **Given** an innovation owner, **When** a non-owner actor requests the full response list with financial details, **Then** the system returns only public fields — `responseType`, `actorId`, `location`, `participationType`, `status`, `submittedAt` — with `participationProposal`, financial projections, and rationale text all excluded

---

### User Story 6 — Partner Selection Continues Working with New Response Types (Priority: P1)

As an **Innovation owner**, I want the existing partner selection workflow to select partners from the new typed responses so that my investment in the `POST /select-partners` endpoint is preserved.

**Why this priority**: P1 — the SelectPartners endpoint (Phase 1c) must continue working after this breaking change. A regression here would break a fully tested, production-ready feature.

**Independent Test**: Can be fully tested end-to-end: submit three typed responses, then call the existing SelectPartners endpoint with the three response IDs and verify it transitions the innovation to PartnersSelected.

**Acceptance Scenarios**:

1. **Given** an innovation with one accepted ManufacturingResponse, one SalesMarketingResponse, and one ResearchDevelopmentResponse, **When** SelectPartners is called with all three IDs, **Then** the innovation transitions to PartnersSelected and all three responses are marked Accepted
2. **Given** an innovation where partner selection is already complete, **When** SelectPartners is called again, **Then** the system returns 403 (immutability preserved from Phase 1c)

---

### Edge Cases

- **What happens when an actor submits a response for an innovation they own?** System rejects with 403 (cannot bid on your own innovation — existing rule preserved from Bid entity)
- **What happens when yearly projection data contains zero production volume?** Zero is a valid value; validation only enforces Rationale minimum length, not numeric ranges
- **What happens when years in the projection dictionary are non-contiguous?** System rejects — years must be contiguous starting from year 1 (e.g., years 1, 2, 3 valid; years 1, 3, 5 invalid)
- **What happens to existing Bid records during migration?** Existing bids are converted to the closest matching FormalResponse subtype based on ActorType (Manufacturing → ManufacturingResponse, etc.) with empty projection dictionaries; migration script logs all conversions for review
- **What happens when an actor type does not match the response type?** System rejects (Manufacturing actor cannot submit a SalesMarketingResponse)
- **What happens when SelectPartners is called with a mix of old Bid IDs and new FormalResponse IDs?** All IDs are unified under FormalResponse post-migration; SelectPartners operates on FormalResponse IDs only
- **What happens when a Rationale field is exactly 20 characters?** Accepted (minimum is inclusive: ≥ 20 characters)

---

## Requirements *(mandatory)*

### Functional Requirements

**Bid Submission (Type-Specific)**

- **FR-001**: A Manufacturing actor MUST be able to submit a ManufacturingResponse for a Published innovation, including yearly manufacturing cost projections (production volume, unit cost, distribution expense) for years 1 through N (minimum 1 year, maximum 10 years)
- **FR-002**: A SalesMarketing actor MUST be able to submit a SalesMarketingResponse for a Published innovation, including yearly revenue projections (units sold, unit price, sales expense) for years 1 through N (minimum 1 year, maximum 10 years)
- **FR-003**: An R&D actor MUST be able to submit a ResearchDevelopmentResponse for a Published innovation, including product development duration in years (valid range: 1–10 inclusive) and yearly development cost projections (infrastructure cost, people cost) for years 1 through N (minimum 1 year, maximum 10 years)
- **FR-004**: An Investor actor MUST be able to submit an InvestorResponse for a Published innovation, containing a free-text feedback field (minimum 50 characters, maximum 2000 characters)
- **FR-005**: Each actor type MUST only be permitted to submit the response type that corresponds to their role (Manufacturing actor → ManufacturingResponse only, etc.)
- **FR-006**: Each actor MUST be limited to one response per innovation (duplicate prevention)

**Rationale Enforcement (Auditability)**

- **FR-007**: Every financial metric field in ManufacturingResponse, SalesMarketingResponse, and ResearchDevelopmentResponse MUST be accompanied by a paired Rationale text field
- **FR-008**: Every Rationale field MUST contain a minimum of 20 characters and a maximum of 500 characters; submissions with missing or insufficient rationale MUST be rejected with a field-specific validation error
- **FR-009**: Yearly projection entries with missing rationale fields MUST be rejected; partial projection entries (some years complete, others missing rationale) MUST be rejected as a unit

**Yearly Projection Rules**

- **FR-010**: Yearly projection keys MUST be contiguous positive integers starting from 1 (years 1, 2, 3... — gaps not permitted)
- **FR-011**: The maximum projection window is 10 years; submissions with year keys beyond 10 MUST be rejected

**Response Retrieval**

- **FR-012**: The innovation owner MUST be able to retrieve all FormalResponses for their innovation, with each response returning its type-specific fields (not just common base fields)
- **FR-013**: The `GET /innovations/{id}/bids` endpoint MUST apply three-tier visibility:
  1. **Innovation owner**: receives full response data — base fields, `participationProposal`, type-specific financial projections, all rationale fields, and `feedback` (InvestorResponse)
  2. **The submitting actor** (the actor whose own response is being returned): receives their full own response — base fields, `participationProposal`, `feedback` (if InvestorResponse), type-specific financial projections, and all rationale fields; for all other actors' responses in the list, only public summary fields are returned
  3. **All other authenticated actors**: receive only public summary fields — `responseType`, `actorId`, `location`, `participationType`, `status`, `submittedAt` — with `participationProposal`, `feedback`, financial projections, and rationale text omitted
- **FR-014**: The system MUST support filtering responses by type via a `?type=` query parameter; accepted values are `manufacturing`, `sales`, `rd`, `investor` (case-insensitive; e.g., `?type=manufacturing` returns only ManufacturingResponses)

**Partner Selection Compatibility**

- **FR-015**: The existing `POST /innovations/{id}/select-partners` endpoint MUST continue to function after migration, accepting FormalResponse IDs in place of Bid IDs
- **FR-016**: Accepted FormalResponse records MUST include an `AcceptedAt` timestamp when partner selection completes (existing behavior preserved)

**Migration**

- **FR-017**: Existing Bid records MUST be migrated to the FormalResponse table with their ActorType used to determine the discriminator value; bids from actors without type-specific projection data MUST be migrated with empty projection dictionaries and flagged for manual review
- **FR-018**: The migration MUST be reversible (down migration restores the Bid table)

### Key Entities

- **FormalResponse** (abstract, aggregate root): Common base for all response types. Properties: InnovationId (FK), ActorId (FK), Location (geographic region), ParticipationType (string), ParticipationProposal (string, min 100 chars, max 5000 chars), Status (Pending / Accepted / Rejected), SubmittedAt, UpdatedAt, AcceptedAt. Inherits EntityOfGuid.

- **ManufacturingResponse** (concrete subtype): Extends FormalResponse. Adds: `YearlyManufacturingCosts` — a keyed collection of yearly cost projections, each containing ProductionVolume + Rationale, UnitCost + Rationale, AverageGlobalDistributionExpense + Rationale.

- **SalesMarketingResponse** (concrete subtype): Extends FormalResponse. Adds: `YearlySales` — a keyed collection of yearly revenue projections, each containing UnitsSold + Rationale, UnitPrice + Rationale, SalesMarketingExpense + Rationale.

- **ResearchDevelopmentResponse** (concrete subtype): Extends FormalResponse. Adds: `ProductDevelopmentDuration` (years, integer) and `YearlyDevelopmentCosts` — a keyed collection containing InfrastructureCost + Rationale, PeopleCost + Rationale.

- **InvestorResponse** (concrete subtype): Extends FormalResponse. Adds: `Feedback` (free text, min 50 chars, max 2000 chars). No financial projection data.

- **YearlyManufacturingCost** (value object, no identity): Represents one year's manufacturing data. Fields: ProductionVolume, ProductionVolumeRationale, UnitCost, UnitCostRationale, AverageGlobalDistributionExpense, AvgDistributionExpenseRationale.

- **YearlySale** (value object, no identity): Represents one year's sales data. Fields: UnitsSold, UnitsSoldRationale, UnitPrice, UnitPriceRationale, SalesMarketingExpense, SalesMarketingExpenseRationale.

- **YearlyDevelopmentCost** (value object, no identity): Represents one year's R&D data. Fields: InfrastructureCost, InfrastructureCostRationale, PeopleCost, PeopleCostRationale.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A Manufacturing actor can successfully submit a response with a 3-year projection in a single API call, and the owner can retrieve all three years' data in full within one round trip
- **SC-002**: All existing SelectPartners integration tests (14 tests from Phase 1c) pass after two mechanical updates: (a) seed data replaced with typed FormalResponse objects, and (b) request body field renamed from `selectedBidIds` to `selectedResponseIds`; the core assertion logic (PartnersSelected status, AcceptedAt timestamps, 403 on re-selection) remains unchanged
- **SC-003**: Submission of a ManufacturingResponse with any Rationale field under 20 characters is rejected 100% of the time with a response that identifies the failing field by name
- **SC-004**: The three-tier visibility rule is enforced: (a) the innovation owner receives full data including financial projections, rationale, and `feedback`; (b) the submitting actor receives their full own response — including their own projections, rationale, `participationProposal`, and `feedback` — while all other responses in the list show only public summary fields; (c) all other authenticated actors receive only public summary fields (`responseType`, `actorId`, `location`, `participationType`, `status`, `submittedAt`) for every response
- **SC-005**: The mutation testing score for the new FormalResponse feature code meets or exceeds the Stryker high threshold of ≥80% for new feature files
- **SC-006**: All 131 existing tests (pre-feature) continue to pass after migration — zero regressions in authentication, innovation, and actor features
- **SC-007**: A response with year keys 1, 2, 4 (non-contiguous) is rejected at submission time with an error that identifies the gap
- **SC-008**: Retrieving responses for an innovation returns each response's discriminator type, allowing callers to identify ManufacturingResponse vs SalesMarketingResponse vs ResearchDevelopmentResponse vs InvestorResponse

---

## Assumptions

1. **EF Core TPH selected over TPT**: Table-Per-Hierarchy (single `FormalResponses` table with Discriminator column) is preferred over Table-Per-Type (separate tables per subtype) to keep query performance predictable and avoid complex joins. JSON columns store the yearly projection dictionaries.

2. **Bid entity is fully replaced**: The `Bid` entity, `BidStatus` enum, and existing bid endpoints are removed and replaced by FormalResponse equivalents. No parallel operation period.

3. **ActorType-to-ResponseType mapping**: Manufacturing → ManufacturingResponse, SalesMarketing → SalesMarketingResponse, RD → ResearchDevelopmentResponse, Investor → InvestorResponse. IdeaGenerator actors cannot submit any response type (they own innovations, not bid on them).

4. **SelectPartners endpoint ID field rename**: The `SelectedBidIds` field in the SelectPartners request body becomes `SelectedResponseIds` (or a compatible alias is kept during transition). Phase 1c tests are updated accordingly.

5. **Yearly projection stored as JSON**: EF Core 7+ JSON column support is used for the yearly projection dictionaries (Dictionary<int, ManufacturingInformation>, etc.). The application already targets .NET 8 / EF Core 8.

6. **Rationale minimum length is business-non-negotiable**: The 20-character minimum comes from the legacy system's auditability pattern and is not a configurable threshold.

7. **Migration of existing Bid records**: Existing bids from Phase 0.6 seed data carry no financial projection data. They will be migrated with empty projection dictionaries. Test seed data referencing `Bid` entities will be updated to use appropriate FormalResponse subtypes.

8. **InvestorResponse does not participate in SelectPartners**: The existing SelectPartners endpoint requires exactly one Manufacturing, one SalesMarketing, and one RD response. Investor responses are informational only and not part of the partner selection payload.

9. **Legacy spec authority**: The domain model in `specs/002-domain-enhancements/spec.md §R3.6` is the authoritative source for entity structure. This spec adds endpoint-level requirements and migration requirements not covered there.

10. **FR-017 "flagged for manual review" scoped out**: No logging/flag mechanism was implemented for migrated Bid→FormalResponse rows. Per Constraint 2 (No Data Migration — the platform is in early development with no production Bid data), the discriminator/location mapping the migration performs is sufficient; a manual-review audit trail was judged unnecessary overhead for seed/test data. Revisit only if this migration is ever run against a database with real Bid rows.

---

## Clarifications

### Session 2026-06-07

- Q: Should `participationProposal` be visible to non-owner actors in `GET /bids`? → A: Option C — visible only to the submitting actor (for their own response) and the innovation owner; all other actors receive only public summary fields
- Q: Should `InvestorResponse.Feedback` follow the same 3-tier visibility rule as `participationProposal`? → A: Option A — yes, same rule: visible to the submitting investor and the innovation owner only
- Q: When a submitting actor calls `GET /bids`, should they see their own financial projections and rationale (not just `participationProposal`)? → A: Option A — yes, submitting actors see their full own response including projections and rationale; only other actors' responses are redacted to public summary fields
- Q: Is `participationProposal` (≥100 chars) required for InvestorResponse even though it already has a dedicated `feedback` field? → A: Option A — yes, required for all four response types; the proposal and feedback serve distinct purposes
- Q: What is the maximum character limit for `InvestorResponse.Feedback`? → A: 2000 characters (matches DB column allocation; appropriate for written investment analysis)
