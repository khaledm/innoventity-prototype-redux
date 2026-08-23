# Implementation Plan: ProductIdea Composition Pattern

**Branch**: `006-productidea-composition` | **Date**: 2026-08-23 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/006-productidea-composition/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Restructure the flat, ~28-field `Innovation` entity into a composed aggregate of four EF Core owned entities — `IdeaSummary`, `Product`, `Market`, `CollaborationRequirement` — with no independent identity, lifecycle, or shareability. Completeness logic that today lives as 13 inline rule checks inside the publish endpoint moves onto four named methods on `Innovation` (`IsIdeaSummaryComplete`, `IsProductDetailsComplete`, `IsMarketDetailsSectionComplete`, `IsReadyForSubmission`), and the publish flow delegates its accept/reject decision to `IsReadyForSubmission()`. Existing rows are preserved via an in-place EF Core mapping change (owned types mapped onto the same `Innovations` table with the same column names/lengths), not a physical rename, keeping the migration a verified no-op at the schema level. Backend domain, persistence, and endpoint work is done and merged to this branch; the Angular client (`innovation.model.ts` and its two consuming components/services) still reads the old flat response shape and is the remaining scope.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (backend), TypeScript 5 / Angular 19 (frontend, standalone components + signals)

**Primary Dependencies**: ASP.NET Core 8 Minimal APIs, EF Core 8 (SQL Server provider, owned-entity/`OwnsOne` mapping), xUnit + `WebApplicationFactory` (integration tests), Angular `HttpClient` + signals

**Storage**: SQL Server (LocalDB for dev/test), EF Core 8 Code-First migrations. Test suite uses EF Core's `InMemory` provider via `WebApplicationFactory`.

**Testing**: `dotnet test` (xUnit) for backend unit/integration/E2E; `npm test` (Karma/Jasmine, per existing client convention) for Angular unit specs; `npm run e2e` (Playwright) for client E2E — not exercised by this plan since no new screens are added.

**Target Platform**: ASP.NET Core Web API (Windows/Linux server), Angular SPA served separately.

**Project Type**: Web application (backend API + frontend SPA), matching the existing repo layout — not a new structure decision.

**Performance Goals**: No new performance targets introduced; constitution's existing API p95 <200ms applies unchanged (composition is a mapping change, not a query-shape change — `OwnsOne` types load in the same single-table query as before).

**Constraints**: Zero data loss on existing `Innovations` rows (FR-009); zero behavior change in the 13 publish-completeness rules (FR-004); no new client screens, multi-step forms, or draft-save endpoints (FR-011, Out of Scope).

**Scale/Scope**: Single entity (`Innovation`) and its 4 new owned types; 5 backend endpoint files; 1 EF migration; 2 Angular components + 1 service + 1 model file on the client side.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Gate | Status |
|---|---|---|
| 2. Quality is Non-Negotiable | Tests validate real business rules, not just happy paths | ✅ PASS — completeness rules covered per-rule (accept + reject cases) in `IdeaSummaryTests`/`ProductTests`/`MarketTests`/`InnovationTests`; publish-gate integration tests cover all 13 rules |
| 3. Simplicity Over Cleverness | Choose obvious solutions over clever abstractions | ✅ PASS — owned entities are EF Core's standard mechanism for value-object composition; no custom ORM layer or reflection-based mapping introduced |
| 4. Specification Drives Implementation | Spec exists before code; code validated against spec | ⚠️ PARTIAL — backend implementation (domain, persistence, endpoints, tests) was already written and committed against `spec.md` directly, ahead of this `plan.md`/`tasks.md` pair being generated. This plan documents the architecture retroactively for the backend slice and prescribes forward, spec-first tasks for the remaining Angular client slice. No principle violation (spec existed first), but process ordering (`SPECIFY → PLAN → TASKS → IMPLEMENT`) was not followed for the backend slice. Documented here rather than silently ignored. |
| 5. Tests Must Prove They Work | Observed failing before implementation, or characterization-tested | ✅ PASS — backend fixes in this session were test-first (failing test written and run red before each fix: placeholder-title bug, publish delegation, nested response shape) |
| 7. Architecture Must Support Evolution | v1.0 shouldn't block v2.0, shouldn't build v2.0 prematurely | ✅ PASS — owned entities have no independent identity/table, so a future multi-step submission or draft-save feature (explicitly deferred, per Out of Scope) can add per-section endpoints without a schema rewrite |
| 8. Commit Messages Are Documentation | Conventional Commits format | ✅ PASS — backend slice committed as 5 Conventional Commits–formatted commits (`feat(innovation):`, `test(innovation):` ×2) |

No CRITICAL constitution violations. The Principle 4 ordering deviation is noted, not gated — the spec was authoritative and complete before code was written; only the intermediate `plan.md`/`tasks.md` artifacts lagged.

**Post-Phase 1 re-check**: Design artifacts (research.md, data-model.md, contracts/, quickstart.md) introduce no new entities, dependencies, or architectural patterns beyond what's already implemented and gated above. The one design decision requiring justification — splitting completeness evaluation between entity methods (decision) and endpoint-level field checks (error detail), rather than a single `IsReadyForSubmission()` call — is documented in research.md R2 with rationale tied directly to FR-005 + FR-008 both being satisfiable only via that split. No Complexity Tracking entry needed; this is not a deviation from a simpler alternative, it's the minimum design satisfying two coexisting requirements.

## Project Structure

### Documentation (this feature)

```text
specs/006-productidea-composition/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md         # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
├── checklists/
│   └── requirements.md  # Existing quality checklist
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
src/
├── Innoventity.API/
│   ├── Domain/Entities/
│   │   ├── Innovation.cs                 # Aggregate root; 4 completeness methods
│   │   ├── IdeaSummary.cs                # Owned entity (no identity)
│   │   ├── Product.cs                    # Owned entity (no identity)
│   │   ├── Market.cs                     # Owned entity (no identity)
│   │   └── CollaborationRequirement.cs   # Owned entity (no identity)
│   ├── Features/Innovations/
│   │   ├── CreateInnovation.cs
│   │   ├── GetInnovation.cs              # Returns nested ideaSummary/product/market/collaborationRequirement
│   │   ├── ListInnovations.cs            # List item nests ideaSummary
│   │   ├── UpdateInnovation.cs           # Partial update, response nests ideaSummary
│   │   └── SubmitInnovation.cs           # Delegates decision to innovation.IsReadyForSubmission()
│   └── Infrastructure/Persistence/
│       ├── AppDbContext.cs               # OwnsOne mappings, explicit HasColumnName per field
│       ├── SeedData.cs
│       └── Migrations/
│           └── 20260823142911_ComposeInnovationSections.cs   # Verified no-op Up()/Down()
│
└── Innoventity.Client/src/app/
    ├── core/
    │   ├── models/innovation.model.ts        # NEEDS UPDATE: still flat; must nest per FR-007
    │   └── services/innovations.service.ts   # Consumes InnovationDetail/InnovationListItem — no logic change needed once model is nested, only field-access sites in components
    └── features/innovations/
        ├── innovation-detail/innovation-detail.component.ts    # NEEDS UPDATE: reads innovation.title etc. flat
        └── innovations-list/innovations-list.component.ts      # NEEDS UPDATE: reads item.title etc. flat

tests/
└── Innoventity.API.Tests/
    ├── Unit/Domain/Entities/
    │   ├── InnovationTests.cs
    │   └── OwnedSections/
    │       ├── IdeaSummaryTests.cs
    │       ├── ProductTests.cs
    │       └── MarketTests.cs
    ├── Integration/Features/Innovations/
    │   ├── CreateInnovationTests.cs
    │   ├── GetInnovationTests.cs
    │   ├── ListInnovationsTests.cs
    │   ├── UpdateInnovationTests.cs
    │   └── SubmitInnovationTests.cs
    └── E2E/Journeys/Phase0JourneyTests.cs
```

**Structure Decision**: Existing Vertical Slice Architecture (backend) + Angular SPA (frontend) layout is unchanged — this feature adds files within established folders (`Domain/Entities`, `Features/Innovations`) rather than introducing new top-level structure. No new client screens are added (per Out of Scope); only the existing `innovation.model.ts` and its two consuming components require updates to match the now-nested API response.

## Complexity Tracking

*No constitution violations requiring justification. Table omitted.*
