# Implementation Plan: Partner Selection Workflow

**Branch**: `004-partner-selection` | **Date**: 2026-06-07 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-partner-selection/spec.md`
**Note**: Reverse-engineered post-implementation to codify design decisions and task history.

---

## Summary

Implement partner selection for published innovations: a single atomic `POST /innovations/{id}/select-partners` endpoint that validates ownership, innovation state, minimum bid readiness, and payload coverage before transitioning the innovation to `PartnersSelected` and settling all bids (selected → Accepted, remaining Pending → Rejected). Core guarantees are irreversibility and one-partner-per-required-type enforcement for the fixed Phase 1c actor set (Manufacturing, SalesMarketing, RD).

---

## Technical Context

**Language/Version**: C# 12 / .NET 8 (ASP.NET Core Minimal APIs)
**Primary Dependencies**:
- ASP.NET Core 8 Minimal APIs (`MapPost`, `IResult`)
- Entity Framework Core 8 (`DbContext`, `Include/ThenInclude`, `SaveChangesAsync`)
- BCrypt.Net-Next (password hashing in test seed data)
- Microsoft.AspNetCore.Mvc.Testing (integration test host)
- xUnit 2.x (test runner)
- Stryker.NET (mutation testing, added `SelectPartners.cs` to mutate list)

**Storage**: SQL Server via EF Core — single `Innovations` table column added (`PartnerSelectionCompletedOn datetimeoffset NULL`); `Bids` table unchanged (status transitions via EF tracking)

**Testing**: xUnit integration tests using `WebApplicationFactory<Program>` with in-memory SQLite/EF InMemory database; 12 test methods covering all 8 spec verification scenarios plus 4 guard tests; Stryker mutation testing with `break: 60`, `low: 70`, `high: 80` thresholds

**Target Platform**: Azure App Service (.NET 8 runtime, same as existing API)

**Project Type**: Web service — Vertical Slice Architecture, one feature class per endpoint

**Performance Goals**: NFR-002: p95 < 500ms for typical bid volumes (≤50 bids per innovation)

**Constraints**:
- Operation must be fully atomic: no partial state transitions (`SaveChangesAsync` called once after all mutations)
- Immutability enforced via `PartnerSelectionCompletedOn` sentinel field (non-null = committed)
- Required actor types are a compile-time constant for Phase 1c: `ActorType[]{ Manufacturing, SalesMarketing, RD }`
- Validation ordering must preserve the separation between precondition checks (409) and payload structural checks (422)

**Scale/Scope**: Single endpoint, single innovation per call; ~10 concurrent users at launch; no batch operations

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Principle Alignment

**✅ Principle 1 — User Experience First**: Partner selection is a high-stakes, one-time action. All validation failures return deterministic, contract-defined error responses (FR-008) with actionable messages. The irreversibility commitment (FR-007) surfaces a clear human-readable message ("partner selection is final and cannot be changed").

**✅ Principle 2 — Quality is Non-Negotiable**: 12 integration tests cover all 8 spec verification scenarios and all 4 guard paths. Tests were structured to validate business rules (bid state transitions, innovation status change, atomicity) not just HTTP status codes. Stryker mutation testing added for `SelectPartners.cs` with constitution-compliant thresholds (low: 70, break: 60).

**✅ Principle 3 — Simplicity Over Cleverness**: Single static handler class with inline validation steps (no abstractions, no pipeline pattern, no mediator). Validation ordering follows a natural guard-clause sequence. `RequiredActorTypes` is a file-level constant — no dynamic configuration.

**✅ Principle 4 — Specification Drives Implementation**: Spec written first; spec analysis (`/speckit-analyze`) identified and resolved all inconsistencies (C1 state name, I1 terminology, C2 threshold, U2 test gap, I3 error messages, U3 missing response contract) before implementation was considered complete.

**✅ Principle 5 — Tests Must Prove They Work**: All 12 tests were written as integration tests that exercise the real EF Core stack with in-memory DB. Each test asserts both HTTP response and database state. Scenario 4 specifically seeds accepted/rejected bids and asserts they are unchanged after a 403 — proving the mutation guard works, not just the status code.

**✅ Principle 6 — AI Augments, Humans Decide**: Validation ordering (immutability before state check, readiness before payload validation) was a deliberate architectural decision. The `PartnerSelectionCompletedOn` sentinel field design was chosen over a dedicated `IsLocked` boolean for richer audit information.

**✅ Principle 7 — Architecture Must Support Evolution**: Required actor types expressed as a named constant (`RequiredActorTypes`) rather than hardcoded inline — isolates the Phase 1c fixed set for easy future parameterization. `PartnerSelectionCompletedOn` is innovation-level state; future phases can extend without breaking existing callers.

**✅ Principle 8 — Commit Messages Are Documentation**: All task references follow Conventional Commits format (e.g., `feat(bids): add select-partners endpoint`).

### Constraint Compliance

**✅ Constraint 1 — Solo Project Realities**: Single endpoint, isolated vertical slice, no cross-team dependencies. Scope is realistic for one session.

**N/A Constraint 2 — No Data Migration**: New nullable column only; no data migration required.

**✅ Constraint 3 — Learning Objectives**: Demonstrates Minimal API patterns, EF Core eager loading with `ThenInclude`, atomic mutation via change tracking, and the guard-clause validation pattern for complex business operations.

**✅ Constraint 4 — Production-Ready Mindset**: Full error contract (401/403/404/409/422), RFC 7807 Problem Details for all errors, atomicity guarantee, immutability enforcement, OpenAPI metadata via `WithOpenApi()`.

### Quality Gates

**MUST before implementation:**
- [x] Validation ordering defined (immutability → state → readiness → payload)
- [x] Atomic mutation strategy confirmed (single `SaveChangesAsync` after all mutations)
- [x] Required actor type set fixed for Phase 1c (Manufacturing, SalesMarketing, RD)
- [x] Test strategy defined (12 integration tests covering all 8 scenarios + 4 guards)

**PASSED** — All constitution gates satisfied.

---

## Project Structure

### Documentation (this feature)

```text
specs/004-partner-selection/
├── spec.md              # Feature specification (updated post-analysis)
├── plan.md              # This file (reverse-engineered post-implementation)
└── tasks.md             # Reverse-engineered task history
```

### Source Code

```text
src/Innoventity.API/
├── Domain/Entities/
│   └── Innovation.cs                          # MODIFIED: +PartnerSelectionCompletedOn, +Bids nav
├── Features/Bids/
│   └── SelectPartners.cs                      # NEW: POST /innovations/{id}/select-partners
├── Infrastructure/Persistence/
│   ├── AppDbContext.cs                        # MODIFIED: EF config for new property + relationship
│   └── Migrations/
│       ├── 20260606231900_AddPartnerSelectionCompletedOn.cs  # NEW: schema migration
│       └── AppDbContextModelSnapshot.cs       # MODIFIED: snapshot updated
├── Program.cs                                 # MODIFIED: +app.MapSelectPartners() (T094)
├── appsettings.Development.json               # MODIFIED: DB name InnoventityDev → InnoventityDevelopment
└── stryker-config.json                        # MODIFIED: +SelectPartners.cs, thresholds restored

tests/Innoventity.API.Tests/
└── Integration/Features/Bids/
    └── SelectPartnersTests.cs                 # NEW: 12 integration tests
```

---

## Complexity Tracking

> **No violations requiring justification**

Single endpoint, single migration column, inline validation with guard clauses. No abstractions added that aren't directly required by the business rules.

---

## Phase 0: Research

All design questions were resolved from the legacy system and spec clarifications prior to implementation.

**Key findings:**
- Legacy rule `MustNotHaveCollabSelectionProcessCompleted` → `PartnerSelectionCompletedOn != null` sentinel pattern (richer than `IsLocked` boolean: provides timestamp for audit)
- Legacy rule `MustHaveEnoughFormalResponsesForSelection` → precondition check (fires before payload validation, returns 409 not 422)
- Legacy rule `MustSelectBidsFromAllCollabTypes` → payload structural check (fires after precondition, returns 422)
- Investor and other non-required types: submit bids but are rejected as non-selected; they are NOT in the required type set
- Validation ordering precedence: ownership (403) → immutability (403) → state (409) → readiness (409) → payload count (422) → duplicate IDs (422) → foreign bids (422) → unsupported types (422) → duplicate actor types (422) → atomic mutation (200)

---

## Phase 1: Design & Contracts

### Data Model

**Modified entity: `Innovation`**

| Field | Type | Constraint | Purpose |
|-------|------|------------|---------|
| `PartnerSelectionCompletedOn` | `DateTimeOffset?` | Nullable | Sentinel for irreversibility; non-null = committed; provides timestamp for audit (NFR-003 partial) |
| `Bids` | `ICollection<Bid>?` | Inverse navigation | Eager-loadable via `Include(i => i.Bids).ThenInclude(b => b.Actor)` for validation in a single DB round-trip |

**Bid state transitions (FR-006):**
- Selected bid: `Pending → Accepted` (sets `AcceptedAt = UtcNow`)
- Non-selected Pending bid: `Pending → Rejected`
- Already Accepted/Rejected bids: unchanged by the mutation loop

**Innovation state transition:**
- `Published → PartnersSelected` on success (FR-016 precondition: must be Published)

### Endpoint Contract

```
POST /innovations/{innovationId}/select-partners
Authorization: Bearer {token}
Content-Type: application/json

Request:
{
  "selectedBidIds": ["<guid>", "<guid>", "<guid>"]  // exactly 3 — one per required type
}

Response 200:
{
  "innovationId": "<guid>",
  "status": "PartnersSelected",
  "partnerSelectionCompletedOn": "<DateTimeOffset ISO-8601>",
  "acceptedBids": [
    { "bidId": "<guid>", "actorId": "<guid>", "actorType": "Manufacturing" },
    { "bidId": "<guid>", "actorId": "<guid>", "actorType": "SalesMarketing" },
    { "bidId": "<guid>", "actorId": "<guid>", "actorType": "RD" }
  ]
}
```

Error responses: 401, 403 (ownership or immutability), 404, 409 (state or readiness), 422 (payload validation). All use RFC 7807 Problem Details.

### Architecture

```
POST /innovations/{innovationId}/select-partners
         │
         ▼
[SelectPartners.Handle]
         │
         ├─ Step 1-2: Authenticate (sub/NameIdentifier claim → Actor lookup)
         │
         ├─ Step 3: Load Innovation + Bids + Actors (single Include chain)
         │           → 404 if not found
         │
         ├─ Step 4: Ownership check (innovation.OwnerId == actorId)
         │           → 403 "Only the innovation owner can select partners"
         │
         ├─ Step 5: Immutability check (PartnerSelectionCompletedOn != null)
         │           → 403 "Partner selection is final and cannot be changed"
         │
         ├─ Step 6: State check (Status == Published)
         │           → 409 "Partner selection requires Published status"
         │
         ├─ Step 7: Readiness check (≥1 Pending bid per RequiredActorType)
         │           → 409 "Minimum readiness threshold not met: missing {types}"
         │
         ├─ Steps 8-12: Payload structural validation (422 ValidationProblem)
         │   ├─ Step 8:  Count == 3
         │   ├─ Step 9:  No duplicate bid IDs
         │   ├─ Step 10: All bids belong to this innovation
         │   ├─ Step 11: No unsupported actor types
         │   └─ Step 12: No duplicate actor types
         │
         └─ Step 14: Atomic mutation (SaveChangesAsync once)
             ├─ selected bids → Accepted + AcceptedAt
             ├─ remaining Pending bids → Rejected
             ├─ innovation.Status → PartnersSelected
             └─ innovation.PartnerSelectionCompletedOn → UtcNow
             → 200 SelectPartnersResponse
```

### Key Design Decisions

| Decision | Rationale | Alternative Rejected |
|----------|-----------|----------------------|
| `PartnerSelectionCompletedOn` sentinel over `IsLocked` boolean | Provides timestamp for audit (NFR-003) and a single-field irreversibility guard | Separate `PartnerSelectionAudit` table (premature for Phase 1c; NFR-003 partially satisfied implicitly) |
| Readiness check (409) before payload validation (422) | Readiness is an innovation precondition independent of what the caller sends; fast-fail on infrastructure before processing payload | Payload-first validation (would return 422 before detecting readiness failure, hiding the root cause) |
| `Bids` inverse navigation on `Innovation` | Enables single DB round-trip with `Include(i => i.Bids).ThenInclude(b => b.Actor)` | Separate Bids query (two round-trips; races with concurrent bid submissions) |
| Static `RequiredActorTypes` array constant | Compile-time constant for Phase 1c; isolates future parameterization to one place | Enum flags or DB-driven config (over-engineering for fixed Phase 1c requirement) |
| Inline guard-clause validation (no pipeline/mediator) | Simplicity over cleverness (Principle 3); all validation logic readable in a single method | FluentValidation / MediatR pipeline (adds abstraction without value for a single endpoint) |
| Single `SaveChangesAsync` call | Atomicity guarantee (FR-006, FR-012); no partial state | Two saves (bid transitions then innovation status) — risks partial commit on failure |

### Success Criteria

Implementation is **COMPLETE** when:

1. [x] `POST /innovations/{id}/select-partners` returns 200 with correct response for a valid selection
2. [x] Selected bids transition to Accepted; non-selected Pending bids transition to Rejected (atomic)
3. [x] Repeated selection attempt returns 403 with immutability message; no mutations occur
4. [x] All 8 verification scenarios from spec produce the correct HTTP status and database state
5. [x] All 4 guard paths (non-owner, foreign bids, duplicate IDs, non-selected rejection) verified
6. [x] `PartnerSelectionCompletedOn` column added to `Innovations` table via EF migration
7. [x] `SelectPartners.cs` added to Stryker mutate list; constitution-compliant thresholds enforced
8. [x] Spec encoded: InCollaboration → PartnersSelected, ResearchDevelopment → RD, response contract added

---

## Plan Summary

**Completed**: 2026-06-07
**Branch**: `004-partner-selection`
**Next Phase**: Task generation → `tasks.md` (generated alongside this plan)
