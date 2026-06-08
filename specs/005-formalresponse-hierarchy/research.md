# Research: FormalResponse Polymorphic Hierarchy

**Branch**: `005-formalresponse-hierarchy`
**Date**: 2026-06-07
**Phase**: Phase 0 Research (pre-design)

---

## Decision 1 — EF Core Inheritance Strategy: TPH vs TPT

**Decision**: Table-Per-Hierarchy (TPH) with JSON columns for yearly projection dictionaries.

**Rationale**:
- TPH keeps all FormalResponse rows in one `FormalResponses` table with a `Discriminator` string column. Reads require no joins (performance win for the SelectPartners query that loads all bids for an innovation).
- TPT (one table per concrete type) would require 4-way outer joins to reconstruct polymorphic lists, adding complexity with no concrete benefit for this domain size.
- EF Core 8 TPH is the default for class hierarchy mapping — no extra configuration needed beyond the hierarchy declaration.
- JSON columns (EF Core 8 `ToJson()` / `HasColumnType("nvarchar(max)")` with JSON serialization) store the `Dictionary<int, ManufacturingInformation>` structures directly in the row, eliminating a secondary table per projection year.

**Alternatives considered**:
- TPT: Rejected. Query complexity and join overhead outweigh the benefit of column-only nullable avoidance.
- Table-Per-Concrete-Class (TPC): Rejected. Breaks polymorphic list queries (`db.FormalResponses.Include(...)` would require UNION ALL internally). EF Core 8 supports TPC but it's more complex than needed here.
- Separate `ProjectionYear` table: Rejected. Adds a foreign key join for every projection read. JSON column approach (EF Core 8 `ToJson`) is idiomatic and keeps data co-located.

---

## Decision 2 — JSON Column Strategy for Yearly Projection Dictionaries

**Decision**: Store yearly projection dictionaries as EF Core 8 JSON columns using `entity.Property(x => x.YearlyManufacturingCosts).HasColumnType("nvarchar(max)")` with a custom JSON value converter, OR use EF Core's `ToJson()` owned entity approach on a list of year entries.

**Rationale**:
- EF Core 8 supports `OwnsMany(..., ownedNav => ownedNav.ToJson())` for owned collections stored as JSON. However, `Dictionary<int, T>` is not directly supported as an owned navigation — owned navigations must be collections of entities with identity.
- The cleaner approach for this domain: model each yearly entry as a list of `YearlyManufacturingCost` records (with a `Year` int property) rather than a dictionary. EF Core 8 `OwnsMany(...).ToJson()` then stores the list as a JSON array in a single column.
- Alternatively, use a `JsonConverter<Dictionary<int, ManufacturingInformation>>` registered via `HasConversion`. This is simpler but loses EF Core's native JSON query support.
- **Chosen**: `OwnsMany(...).ToJson()` with a list of typed yearly entries (renaming from Dictionary to List internally, exposing as a dictionary accessor in domain logic if needed). This is idiomatic EF Core 8 and retains query capabilities.

**Alternatives considered**:
- Raw JSON string column with manual serialization: Rejected. Loses type safety and EF query composition.
- `HasConversion` with `System.Text.Json`: Viable fallback if `ToJson()` issues arise, but less idiomatic.

---

## Decision 3 — SelectPartners Field Rename Strategy

**Decision**: Clean breaking rename — `SelectedBidIds` → `SelectedResponseIds` in the `SelectPartnersRequest` record. No backward-compatibility alias.

**Rationale**:
- This is a Phase 1 refactoring. The `004-partner-selection` branch was merged to Main but the API is in early development with no external consumers beyond the test suite.
- A clean rename avoids the confusion of `BidIds` referring to `FormalResponse` records indefinitely.
- All 14 `SelectPartnersTests.cs` tests reference the field by name — they must be updated regardless of whether an alias is used. A clean rename is simpler than maintaining dual properties.
- The `SelectPartners.cs` endpoint logic changes only at the field access point (`request.SelectedResponseIds`) — the validation pipeline logic is unchanged.

**Alternatives considered**:
- Keep `SelectedBidIds` as an alias (backward-compat property delegating to `SelectedResponseIds`): Rejected. Adds dead code, confuses future readers.
- JSON property name alias (`[JsonPropertyName("selectedBidIds")]`): Rejected. Same reasoning.

---

## Decision 4 — Existing Bid Record Migration

**Decision**: A single EF Core migration creates the `FormalResponses` table (TPH), inserts existing `Bids` rows as FormalResponse records (discriminated by ActorType), then drops the `Bids` table. Seed data is updated to use typed FormalResponse records.

**Rationale**:
- The platform is in early development — the `Bids` table contains only seed/test data, no production user data. No complex migration safety measures needed.
- The migration script maps ActorType → Discriminator: Manufacturing → `ManufacturingResponse`, SalesMarketing → `SalesMarketingResponse`, RD → `ResearchDevelopmentResponse`, Investor → `InvestorResponse`.
- Migrated records have empty JSON projection columns (they had no structured financial data in the flat Bid schema). This is acceptable — migrated entries exist to satisfy FK constraints in existing innovation seeds.
- The `Innovation.Bids` navigation property is renamed to `Innovation.FormalResponses` with a corresponding FK update in the migration.
- The down migration restores the `Bids` table and reverses the data move.

**Alternatives considered**:
- Parallel operation (keep Bids table, add FormalResponses table): Rejected. Adds complexity and divergent state. The breaking change scope is small and well-contained.
- Keep Bids table and add FormalResponse as separate: Rejected. Defeats the purpose of the refactoring.

---

## Decision 5 — Geographic Location Representation

**Decision**: Replace the free-text `Location (string)` in Bid with a `GeographicRegion (enum)` in FormalResponse: `Asia, Americas, Europe, Africa, Oceania`.

**Rationale**:
- The legacy system uses a `Location` enum (Asia, Americas, Europe, Africa) on FormalResponse for geographic filtering and partner matching.
- Free-text location ("Munich, Germany") is useful for display but cannot be used for filtering/matching — the enum is needed for that use case.
- A fifth value `Oceania` is added to cover Australia/New Zealand (omitted from the legacy 4-value enum, likely an oversight).
- The current `Bid.Location` test seed data uses strings like "Munich, Germany" — migration maps these to the closest region enum value; test seed data is updated directly.

**Alternatives considered**:
- Keep string location: Rejected. Loses filterable semantics.
- Add both string and enum: Rejected. Adds redundant data, complexity.

---

## Decision 6 — ResponseStatus Naming

**Decision**: Rename `BidStatus` enum to `ResponseStatus` (same values: Pending, Accepted, Rejected). The old `BidStatus` enum is removed.

**Rationale**:
- `BidStatus` is only referenced from `Bid.cs`, `SelectPartners.cs`, and related tests. After the Bid entity is removed, the enum must be renamed or removed.
- `ResponseStatus` is the natural name for the status on a `FormalResponse`.
- Values (Pending, Accepted, Rejected) remain unchanged — only the type name changes.

---

## Decision 7 — Stryker Configuration

**Decision**: Add all new feature files to the Stryker `mutate` list: all 4 `Submit*.cs` handlers, updated `GetFormalResponses.cs`, and updated `SelectPartners.cs`. Maintain existing thresholds (break: 60, low: 70, high: 80).

**Rationale**:
- Constitution Principle 5 requires mutation testing for critical business paths. The rationale validation logic (≥20 chars), year contiguity check, actor-type-to-response-type enforcement, and JSON deserialization edge cases are all critical paths.
- The existing SelectPartners threshold configuration already covers that file — no change needed there beyond updating the file path reference if the file is modified.

---

## Resolved Unknowns Summary

| Unknown | Decision | Reference |
|---------|----------|-----------|
| TPH vs TPT | TPH with JSON columns | Decision 1 |
| JSON column approach | `OwnsMany().ToJson()` with year-list model | Decision 2 |
| SelectedBidIds rename | Clean breaking rename → `SelectedResponseIds` | Decision 3 |
| Migration of Bid records | Single migration: create FR table, data move, drop Bids | Decision 4 |
| Location field type | `GeographicRegion` enum (5 values) | Decision 5 |
| BidStatus rename | → `ResponseStatus` (same values) | Decision 6 |
| Stryker config | Add all new feature files | Decision 7 |
