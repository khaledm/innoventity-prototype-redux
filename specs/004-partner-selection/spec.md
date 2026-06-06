# Specification: Partner Selection Workflow

## Metadata
- Feature: 004-partner-selection
- Status: Draft (entry criteria placeholder)
- Created: 2026-06-05
- Parent context: Phase 0.6 and 002-domain-enhancements deferrals

## Context
After bids are submitted, the innovation owner must select exactly one partner per required actor type to move into collaboration. Selection is permanent and must enforce minimum bid readiness and ownership/security checks.

## User Stories

### US1: Select Required Partners
As an innovation owner, I can select one bid per required actor type so the innovation can transition into collaboration.

Acceptance criteria:
1. Endpoint accepts selected bid IDs for the innovation.
2. Selection must include all required actor types.
3. Selection fails when required actor type coverage is incomplete.

### US2: Enforce Irreversibility
As the platform, partner selection is final once accepted so collaboration integrity is preserved.

Acceptance criteria:
1. A second selection attempt returns forbidden.
2. Error message states selection is final and cannot be changed.
3. Existing accepted/rejected outcomes remain unchanged.

### US3: Enforce Readiness Rules
As the platform, selection can occur only when sufficient bids exist per business rules.

Acceptance criteria:
1. Selection fails when sufficient-bids threshold is not met.
2. Selection fails for non-owner callers.
3. Selection fails when innovation state is ineligible.

## Functional Requirements

- FR-001: Provide POST /innovations/{id}/select-partners endpoint.
- FR-002: Caller must be authenticated and must own the innovation.
- FR-003: Innovation must be in a selectable state.
- FR-004: Request must include exactly one selected bid per required actor type.
- FR-005: System must validate minimum bid readiness before selection.
- FR-006: Selected bids transition to Accepted and non-selected bids transition to Rejected atomically.
- FR-007: If selection already completed, return 403 with immutable-selection error.
- FR-008: All validations must return deterministic, contract-defined error responses.

## Validation Rules (Legacy-aligned)
- MustNotHaveCollabSelectionProcessCompleted
- MustSelectBidsFromAllCollabTypes
- MustHaveEnoughFormalResponsesForSelection
- MustBeInnovationOwner
- MustBeSelectableInnovationState

## State Transitions
- Published -> InCollaboration when selection succeeds.
- Published remains unchanged when validation fails.
- InCollaboration remains unchanged for repeated selection attempts.

## Non-Functional Requirements
- NFR-001: Operation is transactional and idempotent for repeated identical attempts after completion (returns 403 without mutation).
- NFR-002: Response time target p95 under 500ms for typical bid volumes.
- NFR-003: Audit trail records selector actor, timestamp, and selected bid IDs.

## Out of Scope
- Multi-round negotiation.
- Partial acceptance by actor type.
- Post-selection reversal workflows.

## Open Questions
1. Should admin override be supported for exceptional corrections?
2. Should notification fan-out happen synchronously or via background processing?
3. Should rejected bids include machine-readable rejection reasons?
