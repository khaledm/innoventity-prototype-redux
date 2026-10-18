# Feature Specification: ProductIdea Composition Pattern

**Feature Branch**: `006-productidea-composition`

**Created**: 2026-07-26

**Status**: Draft

**Input**: User description: "Refactor the flat Innovation entity into a composed aggregate per 002-domain-enhancements/spec.md §R3.5: IdeaSummary, Product, Market, and CollaborationRequirement as EF Core owned entities (no separate IDs). Keep all current Innovation properties, redistributing them into the owned entities (produce an explicit field-mapping table; use current code names like ResearchCategory). Add the four validation methods (IsIdeaSummaryComplete, IsProductDetailsComplete, IsMarketDetailsSectionComplete, IsReadyForSubmission) and wire IsReadyForSubmission into the existing submit path. Scope: backend + minimal Angular client fixes to keep tests green — no multi-step form UI, no draft-save endpoints. Migration must preserve existing rows via column renames (as done for Bids → FormalResponses in 005)."

**Reference**: [Legacy Domain Spec §R3.5](../002-domain-enhancements/spec.md#r35-productidea-composition-pattern), [Gap Analysis §1.1](../../.specify/analysis/innovation-bid-domain-gap-analysis.md#11-legacy-productidea---rich-aggregate-structure), [Clarification decisions recorded 2026-07-26 in session preceding this spec]

---

## Context & Background

The Innovation record is currently a single flat structure of ~28 fields covering four distinct concerns that users think of as separate stages: describing the idea, detailing the product, analyzing the market, and declaring collaboration needs. This flat shape makes the innovation data hard to reason about, spreads submission-completeness rules across a single 13-rule inline check in the publish flow, and blocks the planned multi-step submission experience and business-plan generation (future phases).

This feature reorganizes the Innovation record into a composed structure of four logical sections — **IdeaSummary**, **Product**, **Market**, and **CollaborationRequirement** — while **keeping every existing field and its data**. Completeness rules move from the inline publish check into named validation capabilities on the Innovation itself, preserving the exact same accept/reject behavior.

**Clarified scope decisions** (from pre-spec clarification session, 2026-07-26):

1. **Field reconciliation**: Keep all current Innovation fields, redistributing them into the four sections. Legacy-only §R3.5 fields (`HasRightToUse`, `BriefSketch`, `CollaborationType`, `DesiredTimeline`) are NOT added — `IprStatus` already covers right-to-use declaration; the others have no current user need.
2. **Workflow scope**: Composition + validation methods only. The existing single-call publish flow is retained; no draft-save endpoints, no multi-step workflow, no new state transitions.
3. **Frontend scope**: Backend restructuring plus the minimum client updates needed to keep existing views compiling and existing tests passing. No new UI.
4. **Data migration**: Existing innovation rows must be preserved via column rename/relocation (same approach as the 005 `Bids` → `FormalResponses` rename).

**Breaking changes**: YES — innovation create/update/retrieve payload shapes change to a nested-section structure. The client and all tests referencing the flat shape must be updated. No endpoint URLs change.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Innovation Data Organized into Logical Sections (Priority: P1)

As an **innovation owner**, I want my innovation's information organized into the four sections I actually think in — idea summary, product details, market analysis, and collaboration needs — so that reviewing and editing my innovation matches my mental model instead of one undifferentiated form of ~28 fields.

**Why this priority**: P1 — this is the structural core of the feature. Every other capability (section validation, future multi-step submission, business-plan generation) depends on the composed shape existing.

**Independent Test**: Can be fully tested by creating an innovation, retrieving it, and verifying the response groups every field under its correct section per the field-mapping table, with no field lost or renamed beyond its section placement.

**Acceptance Scenarios**:

1. **Given** an authenticated Idea Generator, **When** they create an innovation supplying data for all four sections, **Then** the system stores the innovation and returns it with fields grouped under `ideaSummary`, `product`, `market`, and `collaborationRequirement` sections
2. **Given** an existing innovation, **When** its details are retrieved, **Then** every field listed in the field-mapping table appears under its assigned section with its stored value
3. **Given** an innovation owner updating their innovation, **When** they modify a field in one section (e.g., product description), **Then** the change persists and fields in other sections are unaffected
4. **Given** the four sections, **When** stored, **Then** no section has independent identity or lifecycle — sections exist only as parts of their owning innovation and are deleted with it

---

### User Story 2 — Submission Gate Behavior Preserved via Section Validation (Priority: P1)

As an **innovation owner**, I want the publish step to keep enforcing the same completeness rules as today, now reported per section, so that I cannot publish an incomplete innovation and I can see which section needs work.

**Why this priority**: P1 — the publish gate is existing, tested, business-critical behavior (constitution: state transitions are a critical path). The refactor must not weaken or alter it.

**Independent Test**: Can be fully tested by attempting to publish innovations in various incomplete states and verifying the accept/reject decision and the identified incomplete fields are equivalent to current behavior for all 13 completeness rules.

**Acceptance Scenarios**:

1. **Given** a draft innovation with a placeholder title (containing "Untitled" or "TODO"), **When** the owner submits it for publication, **Then** the system rejects with a validation error identifying the title field (idea-summary completeness fails)
2. **Given** a draft innovation with research background under 50 characters, **When** submitted, **Then** the system rejects identifying the research background field
3. **Given** a draft innovation missing product description, technology description, or target beneficiaries, **When** submitted, **Then** the system rejects identifying each missing product field (product completeness fails)
4. **Given** a draft innovation with relevant or potential market size absent or not greater than zero, or with no target industries, **When** submitted, **Then** the system rejects identifying the failing market fields (market completeness fails)
5. **Given** a draft innovation with no partners needed declared, **When** submitted, **Then** the system rejects identifying the partners-needed field (collaboration requirement fails)
6. **Given** a draft innovation satisfying all 13 completeness rules, **When** the owner submits it, **Then** the innovation becomes Published with a submission timestamp, exactly as today
7. **Given** an already-published innovation, **When** submitted again, **Then** the system rejects with a conflict error (unchanged behavior)
8. **Given** a non-owner, **When** they attempt to submit someone else's innovation, **Then** the system rejects with a forbidden error (unchanged behavior)

---

### User Story 3 — Existing Innovation Data Survives the Upgrade (Priority: P1)

As a **platform operator**, I want every existing innovation record to retain all its field values through the structural upgrade so that no user data is lost and no re-entry is required.

**Why this priority**: P1 — data loss is unacceptable (constitution: Quality is Non-Negotiable). The 005 feature set the precedent of preserving rows through a structural rename.

**Independent Test**: Can be fully tested by capturing all innovation rows before the upgrade, applying it, and verifying row counts match and every field value is readable at its new location with identical content.

**Acceptance Scenarios**:

1. **Given** a database containing innovation rows created before this feature, **When** the upgrade is applied, **Then** every row remains present with all field values intact at their new section locations
2. **Given** an innovation that was Published before the upgrade, **When** retrieved afterward, **Then** its status, submission timestamp, partner-selection audit fields, and formal responses are unchanged
3. **Given** the upgrade has been applied, **When** it is rolled back, **Then** the database returns to the prior shape without data loss (reversible migration)

---

### User Story 4 — Existing Client Screens Keep Working (Priority: P2)

As an **end user of the web client**, I want the innovation screens I use today to keep functioning after the restructure so that the upgrade is invisible to me.

**Why this priority**: P2 — required for the feature to ship (client tests must pass per Definition of Done), but it is adaptation work driven by the P1 structural change, not new user value.

**Independent Test**: Can be fully tested by running the existing client unit tests and E2E journeys against the updated client and verifying they pass without reducing coverage or removing assertions.

**Acceptance Scenarios**:

1. **Given** the restructured innovation payloads, **When** the existing innovation views load innovation data, **Then** all previously displayed fields render with correct values
2. **Given** the existing client test suites, **When** run after the client model/service updates, **Then** all previously passing tests pass (updated only to reflect the nested shape, not weakened)
3. **Given** the client, **When** inspected for scope, **Then** no new screens, multi-step forms, or draft-save capabilities have been added

---

### Edge Cases

- **Placeholder title variants**: Titles containing "Untitled" or "TODO" (case-insensitive) are rejected at publish, as today; the check lives in idea-summary completeness
- **Draft with absent market sizes**: Market size fields remain optional on drafts; market completeness returns false without error — the draft is still storable and retrievable
- **Draft with no partners declared**: Collaboration requirement holds an empty/absent partners-needed value on drafts; publish is blocked until at least one partner type is declared
- **Maximum-length values**: Rows carrying values at current maximum lengths survive relocation without truncation (length limits are unchanged)
- **Innovation with active formal responses**: Restructuring does not disturb the innovation's formal responses, partner-selection state, or audit fields — those relationships stay on the innovation root
- **Concurrent submit attempts**: Behavior unchanged — second submit of a published innovation receives a conflict response
- **Rollback after partial adoption**: The migration is a single reversible unit; there is no state where some rows are restructured and others are not

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The Innovation record MUST be composed of exactly four logical sections — IdeaSummary, Product, Market, and CollaborationRequirement — each existing only as part of its owning innovation with no independent identity, lifecycle, or shareability
- **FR-002**: Every current Innovation field MUST be preserved (no field dropped, no semantic rename) and relocated per the field-mapping table below (FR-002a–FR-002e)
- **FR-003**: The Innovation MUST expose four named completeness capabilities: `IsIdeaSummaryComplete`, `IsProductDetailsComplete`, `IsMarketDetailsSectionComplete`, and `IsReadyForSubmission`, where `IsReadyForSubmission` is satisfied only when all three section checks pass AND at least one needed partner type is declared (preserving today's rule 13)
- **FR-004**: The completeness capabilities MUST reproduce the existing 13 publish-time rules with identical accept/reject outcomes (see rule-mapping table below); no rule may be added, removed, or loosened in this feature
- **FR-005**: The existing publish flow MUST delegate its completeness decision to `IsReadyForSubmission` (replacing the inline rule checks) and MUST continue to report which specific fields are incomplete when rejecting
- **FR-006**: Publish-flow behaviors other than completeness checking MUST be unchanged: authentication required, owner-only access, not-found handling, already-published conflict, and the Published-status + submission-timestamp transition on success
- **FR-007**: Innovation create, update, retrieve, and list operations MUST accept and return the nested four-section structure; endpoint addresses and authorization rules are unchanged
- **FR-008**: Validation error responses for innovation operations MUST identify the specific offending field, including which section it belongs to
- **FR-009**: The upgrade MUST preserve all existing innovation rows and field values via in-place relocation (rename/move), MUST be reversible, and MUST NOT require re-entry of any data
- **FR-010**: Innovation-root concerns MUST remain on the root, untouched: identity, tracking token, ownership, status, timestamps, owner and formal-response relationships, target-industry associations (physical placement of the association is a planning decision; logically it belongs to the Market section's completeness check), partners-selection audit fields
- **FR-011**: The web client MUST be updated only as far as needed for existing screens and existing tests to work against the nested structure; no new screens, multi-step forms, or draft-save endpoints
- **FR-012**: Legacy §R3.5 fields absent from the current model (`HasRightToUse`, `BriefSketch`, `CollaborationType`, `DesiredTimeline`) MUST NOT be added in this feature

### Field-Mapping Table (FR-002 detail)

Current code names are authoritative (§R3.5's `ResearchType` is realized as the existing `ResearchCategory`; §R3.5's "Product details" fields map to the richer current set).

| # | Current field | New section | Notes |
|---|---------------|-------------|-------|
| 1 | `Title` | **IdeaSummary** | Placeholder check (rule 1) lives here |
| 2 | `ProductType` | **IdeaSummary** | |
| 3 | `ResearchCategory` | **IdeaSummary** | Current enum name kept (not §R3.5's `ResearchType`) |
| 4 | `ResearchBackground` | **IdeaSummary** | ≥50-char rule (rule 4) lives here |
| 5 | `IprStatus` | **IdeaSummary** | Covers §R3.5 `HasRightToUse` intent; no new field |
| 6 | `ProductDescription` | **Product** | |
| 7 | `TechnologyDescription` | **Product** | |
| 8 | `ProductAdvantages` | **Product** | Not in §R3.5; kept per clarification Q1 |
| 9 | `DevelopmentPhase` | **Product** | Not in §R3.5; kept |
| 10 | `DevelopmentProcess` | **Product** | Not in §R3.5; kept |
| 11 | `TargetBeneficiaries` | **Product** | Per §R3.5 placement |
| 12 | `ProductKeywords` | **Product** | Discovery metadata; kept |
| 13 | `AdvantageKeywords` | **Product** | Discovery metadata; kept |
| 14 | `TargetMarket` | **Market** | Not in §R3.5; kept |
| 15 | `TargetCustomerBase` | **Market** | Not in §R3.5; kept |
| 16 | `TargetCustomerType` | **Market** | Not in §R3.5; kept |
| 17 | `RelevantMarketSize` | **Market** | >0 rule (rule 10) lives here |
| 18 | `PotentialMarketSize` | **Market** | >0 rule (rule 11) lives here |
| 19 | `TargetIndustries` (association) | **Market** (logical) | ≥1 rule (rule 12); physical placement is a plan decision (FR-010) |
| 20 | `PartnersNeeded` | **CollaborationRequirement** | Stays comma-separated in this feature; ≥1 rule (rule 13) |
| 21 | `Id` | Innovation root | Unchanged |
| 22 | `IdeaToken` | Innovation root | Unchanged |
| 23 | `OwnerId` / `Owner` | Innovation root | Unchanged |
| 24 | `Status` | Innovation root | Unchanged |
| 25 | `CreatedAt` | Innovation root | Unchanged |
| 26 | `SubmittedAt` | Innovation root | Unchanged |
| 27 | `FormalResponses` | Innovation root | Unchanged (005 relationship) |
| 28 | `PartnerSelectionCompletedOn`, `SelectedByActorId` | Innovation root | Unchanged (004 audit trail) |

### Rule-Mapping Table (FR-004 detail)

| Existing publish rule | New home |
|-----------------------|----------|
| 1. Title non-empty, not placeholder | `IsIdeaSummaryComplete` |
| 2. ProductType provided | `IsIdeaSummaryComplete` |
| 3. ResearchCategory selected | `IsIdeaSummaryComplete` |
| 4. ResearchBackground ≥ 50 chars | `IsIdeaSummaryComplete` |
| 5–6. IPR status explicitly set | `IsIdeaSummaryComplete` |
| 7. ProductDescription provided | `IsProductDetailsComplete` |
| 8. TechnologyDescription provided | `IsProductDetailsComplete` |
| 9. TargetBeneficiaries provided | `IsProductDetailsComplete` |
| 10. RelevantMarketSize > 0 | `IsMarketDetailsSectionComplete` |
| 11. PotentialMarketSize > 0 | `IsMarketDetailsSectionComplete` |
| 12. TargetIndustries ≥ 1 | `IsMarketDetailsSectionComplete` |
| 13. PartnersNeeded ≥ 1 | `IsReadyForSubmission` (collaboration-requirement check) |

### Key Entities

- **Innovation** (root): The aggregate anchor. Retains identity, ownership, tracking token, workflow status, timestamps, partner-selection audit fields, and relationships to Owner, TargetIndustries, and FormalResponses. Gains the four completeness capabilities.
- **IdeaSummary** (section, no identity): What the idea is — title, product type, research category, research background, IPR status.
- **Product** (section, no identity): What is being built — descriptions, advantages, development phase/process, beneficiaries, discovery keywords.
- **Market** (section, no identity): Who it serves and how big — target market/customer fields, market sizes; logically owns the target-industry completeness rule.
- **CollaborationRequirement** (section, no identity): What partners are sought — needed partner types.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of pre-existing innovation records are readable after the upgrade with every field value intact at its new location (zero data loss, zero re-entry)
- **SC-002**: For any innovation state, the publish accept/reject decision after this feature is identical to the decision before it — verified across all 13 completeness rules by tests exercising each rule's failure and success case
- **SC-003**: The full automated test suite (backend and client — 195 backend tests as of feature start) passes after the restructure, with tests updated only to reflect the nested shape, never weakened
- **SC-004**: Innovation detail responses present 100% of mapped fields grouped under their four sections per the field-mapping table
- **SC-005**: New completeness logic meets the constitution's test-quality floor: mutation score > 70% (target ≥ 80%) on the validation capabilities
- **SC-006**: Zero regressions in untouched flows: formal-response submission, partner selection, authentication, and discovery behave identically (their existing tests pass unmodified)

---

## Assumptions

- **No external API consumers**: The only clients of the innovation endpoints are the project's own Angular client and test suites, so the nested-payload breaking change requires no versioning or deprecation window
- **Legacy-only fields deferred**: `HasRightToUse` intent is already captured by `IprStatus`; `BriefSketch`, `CollaborationType`, and `DesiredTimeline` are deferred until a user story requires them (clarification Q1)
- **`PartnersNeeded` format unchanged**: Remains a comma-separated value inside CollaborationRequirement; conversion to a structured flags type is deferred (Simplicity Over Cleverness) since 004's partner selection already consumes the current format
- **Naming precedent**: Relocated storage columns follow the same rename-migration conventions used by 005's `Bids` → `FormalResponses` migration
- **Draft integrity**: All existing rows satisfy current non-null constraints, so relocation cannot fail on required fields; optional fields (`RelevantMarketSize`, `PotentialMarketSize`, `PartnersNeeded`, `SubmittedAt`, audit fields) stay optional
- **Validation-error key format**: Error responses will key on fields within the nested structure; the exact key format (e.g., section-qualified names) is a planning decision, provided FR-008 (field + section identifiable) is met
- **Submission workflow deferral**: `Submit()` as a richer state machine, draft-save endpoints, and the multi-step client form remain roadmap items for a later feature (clarification Q2/Q3); this feature only relocates where the existing gate's decision logic lives

---

## Out of Scope

- Multi-step submission UI or any new client screens
- Draft-save / per-section update endpoints
- New completeness rules or changes to rule thresholds
- `HasRightToUse`, `BriefSketch`, `CollaborationType`, `DesiredTimeline` fields
- Converting `PartnersNeeded` to a structured type
- Industry taxonomy changes (Phase 2), business-plan generation (Phase 3), NPV valuation (Phase 3)
