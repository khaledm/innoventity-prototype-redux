# Specification: Partner Selection Workflow

## Metadata
- Feature: 004-partner-selection
- Status: Draft (ready for planning)
- Created: 2026-06-05
- Parent context: Phase 0.6 and 002-domain-enhancements deferrals

## Clarifications

### Session 2026-06-06
- Q: What is the minimum readiness threshold before partner selection is allowed? -> A: At least 1 eligible bid in each required type (Manufacturing, SalesMarketing, RD).
- Q: Which innovation state is selectable for partner selection in Phase 1c? -> A: Published only.

## Clarified Scope
- Phase alignment: Phase 1c Partner Selection Foundation.
- Core guarantees: irreversibility and one-partner-per-type enforcement.
- Required actor types for this phase are fixed: Manufacturing, SalesMarketing, and RD.

## Context
After bids are submitted, the innovation owner must select exactly one partner per required actor type to move into collaboration. Selection is permanent and must enforce minimum bid readiness and ownership/security checks.

## User Stories

### US SelectPartner-2: Structured Partner Selection
As an innovation owner, I can complete partner selection through a single structured submission so the innovation can transition into collaboration.

Acceptance criteria:
1. Endpoint accepts selected bid IDs scoped to one innovation.
2. Selection request is validated as a complete set before any state mutation occurs.
3. On success, selected bids are accepted, non-selected bids are rejected, and innovation status transitions to PartnersSelected.

### US SelectPartner-3: One Partner Per Required Type
As the platform, partner selection enforces exactly one selected bid per required actor type so collaboration roles are complete and non-duplicated.

Acceptance criteria:
1. Selection fails when any required actor type is missing.
2. Selection fails when duplicate actor types are present in the selection payload.
3. Selection fails when selected bids do not belong to the target innovation.

### US SelectPartner-4: Irreversible Commitment
As the platform, partner selection cannot be changed after commitment so collaboration integrity is preserved.

Acceptance criteria:
1. A second selection attempt returns 403 Forbidden.
2. Error message states partner selection is final and cannot be changed.
3. Existing accepted/rejected outcomes remain unchanged after a repeated attempt.

## Preconditions and Entry Criteria
- Innovation has bids submitted and is eligible for selection.
- Minimum readiness threshold is satisfied before selection is allowed.
- Caller is authenticated and is the owner of the innovation.
- Eligible selection set includes exactly one selected bid for each fixed required actor type: Manufacturing, SalesMarketing, and RD.

## Functional Requirements

- FR-001: Provide POST /innovations/{id}/select-partners endpoint.
- FR-002: Caller must be authenticated and must own the innovation.
- FR-003: Innovation must be in a selectable state.
- FR-004: Request must include exactly one selected bid per required actor type.
- FR-005: System must validate minimum bid readiness before selection.
- FR-006: Selected bids transition to Accepted and non-selected bids transition to Rejected atomically.
- FR-007: If selection already completed, return 403 with immutable-selection error.
- FR-008: All validations must return deterministic, contract-defined error responses.
- FR-009: Return 409 when minimum readiness threshold is not met.
- FR-010: Return 422 when required actor type coverage is invalid.
- FR-011: Return 403 for non-owner callers.
- FR-012: Return 200 only when full selection succeeds with a single atomic mutation.
- FR-013: Required actor types are fixed for Phase 1c foundation: Manufacturing, SalesMarketing, and RD.
- FR-014: Selection payload must not contain duplicate actor types and must not include unsupported actor types.
- FR-015: Minimum readiness threshold for Phase 1c is exactly one eligible bid available in each required actor type before selection is permitted.
- FR-016: Selectable innovation state for Phase 1c is Published only; all other states are ineligible.

## Validation Rules (Legacy-aligned)
- MustNotHaveCollabSelectionProcessCompleted
- MustSelectBidsFromAllCollabTypes
- MustHaveEnoughFormalResponsesForSelection
- MustBeInnovationOwner
- MustBeSelectableInnovationState

## Required Actor-Type Matrix (Phase 1c)
- Manufacturing: exactly 1 selected bid required.
- SalesMarketing: exactly 1 selected bid required.
- RD: exactly 1 selected bid required.
- Investor and other types: optional for participation, not part of mandatory partner selection coverage in this phase.

## State Transitions
- Published -> PartnersSelected when selection succeeds.
- Published remains unchanged when validation fails.
- PartnersSelected (PartnerSelectionCompletedOn non-null) returns 403 without mutation for repeated selection attempts.
- Any state other than Published -> selection rejected with deterministic validation error and no mutation.

## Non-Functional Requirements
- NFR-001: Operation is transactional and idempotent for repeated identical attempts after completion (returns 403 without mutation).
- NFR-002: Response time target p95 under 500ms for typical bid volumes.
- NFR-003: Audit trail records selector actor, timestamp, and selected bid IDs.

## API Response Contract

### Success (200)
```json
{
  "innovationId": "<guid>",
  "status": "PartnersSelected",
  "partnerSelectionCompletedOn": "<DateTimeOffset ISO-8601>",
  "acceptedBids": [
    { "bidId": "<guid>", "actorId": "<guid>", "actorType": "<string>" }
  ]
}
```
`actorType` values match the `ActorType` enum string: `"Manufacturing"`, `"SalesMarketing"`, `"RD"`.
`acceptedBids` contains exactly the selected bids (one per required actor type).

### Error Responses
| Status | Condition |
|--------|-----------|
| 401 | No valid authenticated user |
| 403 | Caller is not the innovation owner |
| 403 | Selection already completed (`PartnerSelectionCompletedOn` non-null); body includes "partner selection is final and cannot be changed" |
| 404 | Innovation not found |
| 409 | Innovation is not in Published status |
| 409 | Minimum readiness threshold not met (missing eligible bid for one or more required actor types) |
| 422 | Payload count != 3, duplicate bid IDs, foreign bid IDs, duplicate actor types, or unsupported actor type |

All error responses use RFC 7807 Problem Details format (`title`, `detail`, `status`). Validation errors (422) use `errors` map keyed by `"SelectedBidIds"`.

## Verification Scenarios
1. Insufficient readiness threshold -> 409, no mutations.
2. Missing required actor type in payload -> 422, no mutations.
3. Successful structured selection -> 200, accepted/rejected transitions and innovation status transition.
4. Repeated selection attempt after commitment -> 403 with immutable-selection message, no further mutations.
5. Duplicate selected bids from the same required actor type -> 422, no mutations.
6. Selection payload includes unsupported actor type for mandatory coverage -> 422, no mutations.
7. Exactly one eligible bid exists in each required type -> readiness check passes and selection can proceed to other validations.
8. Innovation state is not Published -> selection rejected, no mutations.

## Out of Scope
- Multi-round negotiation.
- Partial acceptance by actor type.
- Post-selection reversal workflows.

## Assumptions
1. No admin override is allowed in this phase.
2. Notification fan-out behavior is specified in the dependent notifications scope and does not alter selection correctness rules.
3. Rejected bids do not require additional machine-readable rejection reasons for this phase.
4. Required actor-type coverage remains fixed in Phase 1c and is not dynamic per innovation.
