# Implementation Plan: FormalResponse Polymorphic Hierarchy

**Branch**: `005-formalresponse-hierarchy` | **Date**: 2026-06-07 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/005-formalresponse-hierarchy/spec.md`

## Summary

Replace the flat `Bid` entity (free-text proposal only) with a polymorphic `FormalResponse` hierarchy: `ManufacturingResponse`, `SalesMarketingResponse`, `ResearchDevelopmentResponse`, and `InvestorResponse`. Each financial type carries structured yearly projection data stored in EF Core 8 JSON columns (TPH, one `FormalResponses` table). New type-specific submission endpoints replace the generic `POST /bids`. The existing `SelectPartners` endpoint is updated to use `selectedResponseIds` (renamed from `selectedBidIds`). All existing `Bid` records are migrated via a single EF Core migration that drops the old table.

**Breaking changes**: `Bid` entity removed, `BidStatus` replaced by `ResponseStatus`, generic bid submission endpoint removed, `UpdateBid` removed, `SelectPartnersRequest.SelectedBidIds` renamed.

---

## Technical Context

**Language/Version**: C# 12 / .NET 8 (ASP.NET Core Minimal APIs)

**Primary Dependencies**:
- ASP.NET Core 8 Minimal APIs (`MapPost`, `IResult`)
- Entity Framework Core 8 (TPH, `OwnsMany().ToJson()` for JSON columns, migrations)
- BCrypt.Net-Next (password hashing in test seed data — unchanged)
- Microsoft.AspNetCore.Mvc.Testing (integration test host — unchanged)
- xUnit 2.x (test runner — unchanged)
- Stryker.NET 4.x (mutation testing — stryker-config.json updated for new feature files)

**Storage**: SQL Server via EF Core — single migration creates `FormalResponses` table (TPH with `ResponseType` discriminator column + JSON columns for projection data), drops `Bids` table. `Innovations` table navigation renamed (FK column unchanged).

**Testing**: xUnit integration tests via `WebApplicationFactory<Program>` with EF Core InMemory. ~35 new tests for 4 typed submission endpoints (happy path + validation + authorization per type) + updated SelectPartners tests (field rename + typed seed data).

**Target Platform**: Azure App Service (.NET 8 runtime — unchanged)

**Project Type**: Web service — Vertical Slice Architecture, one feature class per endpoint

**Performance Goals**: p95 < 200ms for typical response volumes (≤50 responses per innovation). JSON column reads add minimal overhead — full rows are loaded, no joins.

**Constraints**:
- EF Core 8 `OwnsMany().ToJson()` requires owned navigation items to have no identity key configured (value object pattern)
- TPH discriminator must be configured before subtype registrations in `OnModelCreating`
- `Innovation.FormalResponses` navigation rename requires updating all callers (`SelectPartners.cs`, `GetBids.cs`, seed data, tests)
- Year contiguity check must be server-side (cannot be expressed as a data annotation)
- Rationale minimum length (20 chars) is a business rule — enforce in endpoint handler, not just data annotations

**Scale/Scope**: 4 new endpoints + updates to 3 existing; ~35 new tests + updates to ~25 existing tests; one EF Core migration.

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Principle Alignment

**✅ Principle 1 — User Experience First**: Each actor type gets a dedicated endpoint with a request schema tailored to their data (no generic blob). Validation errors identify the specific field and year entry that failed — actors get actionable error messages. The `type` filter on `GET /bids` lets owners quickly compare all manufacturers without scrolling through unrelated responses.

**✅ Principle 2 — Quality is Non-Negotiable**: ~35 new integration tests cover all four submit endpoints (happy path, wrong actor type, duplicate, rationale too short, year gap, innovation not published, owns the innovation). Stryker added for all new feature files. Mutation thresholds maintained at break: 71, low: 70, high: 80.

**✅ Principle 3 — Simplicity Over Cleverness**: TPH (single table) is simpler than TPT (joins). `OwnsMany().ToJson()` is the idiomatic EF Core 8 approach. Year contiguity check is 3 lines of LINQ. No mediator, no pipeline abstractions.

**✅ Principle 4 — Specification Drives Implementation**: Spec written before code. Research resolved all design decisions (TPH vs TPT, JSON column approach, field rename, migration strategy) before coding began.

**✅ Principle 5 — Tests Must Prove They Work**: All new tests are integration tests that exercise the full EF Core stack with InMemory DB. Each test asserts both HTTP response **and** that the correct discriminator and JSON projection data were persisted. Rationale enforcement tests confirm the correct field name appears in the validation error.

**✅ Principle 6 — AI Augments, Humans Decide**: Domain model design (abstract base, JSON column strategy, year-list vs dictionary in EF), migration safety decisions, and actor-type enforcement logic are architectural decisions documented in research.md.

**✅ Principle 7 — Architecture Must Support Evolution**: FormalResponse hierarchy is extensible — adding a new actor type requires adding a new subclass, a new JSON column, and a new endpoint. The `SelectPartners` required-types array remains a named constant, easily parameterized later.

**✅ Principle 8 — Commit Messages Are Documentation**: Breaking changes (Bid entity removal, endpoint removal) will use `feat(bids)!:` with `BREAKING CHANGE:` footer per Conventional Commits.

### Constraint Compliance

**✅ Constraint 1 — Solo Project Realities**: Scope is realistic. Each endpoint follows the established Vertical Slice pattern. Migration is a single file.

**✅ Constraint 2 — No Data Migration** (user data): Seed/test data only — no production data migration risk.

**✅ Constraint 3 — Learning Objectives**: Demonstrates EF Core 8 TPH, JSON owned collections, polymorphic query patterns, and migration data transforms.

**✅ Constraint 4 — Production-Ready Mindset**: Full RFC 7807 Problem Details on all error paths. Year contiguity enforced server-side. Actor-type enforcement prevents type confusion. Owner-only financial data visibility.

### Quality Gates

**MUST before implementation:**
- [x] TPH vs TPT decision documented (research.md Decision 1)
- [x] JSON column approach confirmed (research.md Decision 2)
- [x] SelectedBidIds → SelectedResponseIds strategy confirmed (research.md Decision 3)
- [x] Migration strategy documented (research.md Decision 4)
- [x] GeographicRegion enum design documented (research.md Decision 5)
- [x] ResponseStatus rename documented (research.md Decision 6)

**PASSED** — All constitution gates satisfied.

---

## Project Structure

### Documentation (this feature)

```text
specs/005-formalresponse-hierarchy/
├── plan.md                  # This file
├── spec.md                  # Feature specification
├── research.md              # Design decisions (TPH, JSON, migration, rename)
├── data-model.md            # Entity/table design + file change map
├── quickstart.md            # Validation scenarios
├── contracts/
│   └── api-contracts.md     # Endpoint request/response contracts
├── checklists/
│   └── requirements.md      # Spec quality checklist
└── tasks.md                 # Generated by /speckit-tasks
```

### Source Code

```text
src/Innoventity.API/
├── Domain/
│   ├── Common/                                        (unchanged)
│   └── Entities/
│       ├── Bid.cs                                     DELETE
│       ├── BidStatus.cs                               DELETE
│       ├── GeographicRegion.cs                        NEW  — enum
│       ├── ResponseStatus.cs                          NEW  — enum
│       ├── FormalResponse.cs                          NEW  — abstract base EntityOfGuid
│       ├── ManufacturingResponse.cs                   NEW
│       ├── SalesMarketingResponse.cs                  NEW
│       ├── ResearchDevelopmentResponse.cs             NEW
│       ├── InvestorResponse.cs                        NEW
│       ├── YearlyManufacturingCost.cs                 NEW  — value object for JSON
│       ├── YearlySale.cs                              NEW  — value object for JSON
│       ├── YearlyDevelopmentCost.cs                   NEW  — value object for JSON
│       ├── Innovation.cs                              MODIFY — Bids → FormalResponses nav
│       └── (all other entities unchanged)
│
├── Features/Bids/
│   ├── SubmitBid.cs                                   DELETE
│   ├── UpdateBid.cs                                   DELETE
│   ├── GetBids.cs                                     MODIFY — typed FormalResponse DTOs
│   ├── SelectPartners.cs                              MODIFY — FormalResponses nav + field rename
│   ├── SubmitManufacturingResponse.cs                 NEW
│   ├── SubmitSalesMarketingResponse.cs                NEW
│   ├── SubmitResearchDevelopmentResponse.cs           NEW
│   └── SubmitInvestorResponse.cs                      NEW
│
├── Infrastructure/Persistence/
│   ├── AppDbContext.cs                                MODIFY — TPH config + remove Bids
│   ├── SeedData.cs                                    MODIFY — typed FormalResponse seeds
│   └── Migrations/
│       └── YYYYMMDD_AddFormalResponseHierarchy.cs     NEW
│
├── Program.cs                                         MODIFY — swap endpoint registrations
└── stryker-config.json                                MODIFY — add new feature files

tests/Innoventity.API.Tests/
├── Integration/Features/Bids/
│   ├── SubmitBidTests.cs                              DELETE
│   ├── UpdateBidTests.cs                              DELETE
│   ├── GetBidsTests.cs                                MODIFY
│   ├── SelectPartnersTests.cs                         MODIFY — seed + field rename
│   ├── SubmitManufacturingResponseTests.cs            NEW  (~8 tests)
│   ├── SubmitSalesMarketingResponseTests.cs           NEW  (~8 tests)
│   ├── SubmitResearchDevelopmentResponseTests.cs      NEW  (~8 tests)
│   └── SubmitInvestorResponseTests.cs                 NEW  (~5 tests)
└── E2E/Journeys/
    └── Journey2_BiddingTests.cs                       MODIFY
```

**Structure Decision**: Single web service project (existing structure). Feature files follow the established Vertical Slice pattern in `src/Innoventity.API/Features/Bids/`.

---

## Implementation Phases

### Phase 1 — Domain Layer (prerequisite for everything else)

Add all new entity types and remove old ones. No persistence changes yet.

1. Add `GeographicRegion.cs` enum (Asia, Americas, Europe, Africa, Oceania)
2. Add `ResponseStatus.cs` enum (Pending, Accepted, Rejected)
3. Add value objects: `YearlyManufacturingCost.cs`, `YearlySale.cs`, `YearlyDevelopmentCost.cs`
4. Add `FormalResponse.cs` abstract class (EntityOfGuid) with base properties
5. Add `ManufacturingResponse.cs`, `SalesMarketingResponse.cs`, `ResearchDevelopmentResponse.cs`, `InvestorResponse.cs`
6. Modify `Innovation.cs` — rename `Bids: ICollection<Bid>` → `FormalResponses: ICollection<FormalResponse>`
7. Delete `Bid.cs` and `BidStatus.cs`

**Build gate**: `dotnet build` — compilation errors expected in consumers (Phases 2–4 fix these).

---

### Phase 2 — Persistence Layer

Wire EF Core TPH, JSON columns, generate and apply the migration.

1. Update `AppDbContext.cs` — remove Bids DbSet/config, add FormalResponses TPH config with `HasDiscriminator`, `OwnsMany().ToJson()` for each projection type, unique index
2. Update `SeedData.cs` — replace `Bid` objects with typed `FormalResponse` subtype objects
3. Generate migration: `dotnet ef migrations add AddFormalResponseHierarchy`
4. Review and augment migration with manual `migrationBuilder.Sql` for data move (Bids → FormalResponses)
5. Apply: `dotnet ef database update`
6. Build gate: `dotnet build`

---

### Phase 3 — New Endpoints

Add 4 typed submission handlers. Each follows the guard-clause pattern from `SubmitBid.cs`:
1. JWT claim extraction → actor lookup → type check (403) → innovation lookup (404) → ownership guard (403) → duplicate guard (409) → proposal length validation (400) → projection validation (422) → persist → 201

**Actor identity resolution is centralized**: the JWT-claim → `Actor` lookup (formerly inline in each handler) is performed by `ActorResolutionFilter` (an `IEndpointFilter`, covered by `tests/Innoventity.API.Tests/Integration/Features/Bids/ActorResolutionFilterTests.cs`). Each submission handler receives the resolved `Actor` and performs only the type/ownership/duplicate guards — it does not re-parse the token. Task descriptions that say "extract actorId from JWT → look up Actor" refer to consuming this filter's result, not re-implementing it per handler.

Files: `SubmitManufacturingResponse.cs`, `SubmitSalesMarketingResponse.cs`, `SubmitResearchDevelopmentResponse.cs`, `SubmitInvestorResponse.cs`

---

### Phase 4 — Update Existing Endpoints

1. Delete `SubmitBid.cs`, `UpdateBid.cs`
2. Update `GetBids.cs` — query `db.FormalResponses`, return typed DTOs, owner-visibility gating, `?type=` filter
3. Update `SelectPartners.cs` — `innovation.FormalResponses`, `SelectedResponseIds`, `ResponseStatus`
4. Update `Program.cs` — remove old `Map*` calls, add 4 new
5. Update `stryker-config.json` — swap file references

---

### Phase 5 — Tests

1. Delete `SubmitBidTests.cs`, `UpdateBidTests.cs`
2. Update `SelectPartnersTests.cs` — typed seeds + `SelectedResponseIds`
3. Update `GetBidsTests.cs`, `Journey2_BiddingTests.cs`
4. Add `SubmitManufacturingResponseTests.cs` (~8 tests)
5. Add `SubmitSalesMarketingResponseTests.cs` (~8 tests)
6. Add `SubmitResearchDevelopmentResponseTests.cs` (~8 tests)
7. Add `SubmitInvestorResponseTests.cs` (~5 tests)

---

### Phase 6 — Quality Gate

1. `dotnet test` — all tests pass (target: ~166 total, 3 pre-existing skips)
2. `dotnet stryker` — mutation score ≥ 80%
3. `dotnet build -c Release --no-restore`

---

## Key Risk: EF Core `OwnsMany().ToJson()` Compatibility

**Risk**: EF Core 8 `ToJson()` for owned collections requires careful key configuration. Value objects in JSON arrays must either have a shadow key or use keyless configuration.

**Mitigation**: If `OwnsMany().ToJson()` causes issues with key resolution, fall back to `HasConversion<string>` with `System.Text.Json` serialization (a `ValueConverter<IList<T>, string>`). This is a mechanical change to `AppDbContext.cs` only — domain model unaffected. Document the decision in a code comment.

---

## Complexity Tracking

No constitution violations requiring justification.
