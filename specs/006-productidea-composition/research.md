# Phase 0 Research: ProductIdea Composition Pattern

## R1: EF Core mapping strategy for the four sections

**Decision**: Map `IdeaSummary`, `Product`, `Market`, `CollaborationRequirement` as EF Core **owned entity types** (`OwnsOne`) on `Innovation`, each with explicit `HasColumnName(...)` calls that reproduce the original flat column names, on the same `Innovations` table.

**Rationale**: Owned types are EF Core's built-in mechanism for value objects with no independent identity or lifecycle — exactly FR-001's requirement. Because owned types default to being stored inline on the owner's table (not a separate table) when no explicit `ToTable()` is called, this achieves FR-009 (preserve existing rows) as a pure C#-level reshape: the generated migration (`20260823142911_ComposeInnovationSections`) has empty `Up()`/`Down()` bodies, confirming zero physical schema change. This was verified directly: `dotnet ef migrations add` initially reported "operation may result in loss of data" until column lengths were corrected to match the last-committed `AppDbContextModelSnapshot.cs` exactly (`TechnologyDescription` nvarchar(max)/5000, `PartnersNeeded` nvarchar(500)), at which point the scaffolded migration became a verified no-op.

**Alternatives considered**:
- *Separate tables per section (`ToTable`)*: Rejected — adds join overhead to every read, and gives each section an implicit identity/table, contradicting FR-001 ("no independent identity, lifecycle, or shareability").
- *Value objects without EF mapping (manual JSON column)*: Rejected — loses queryability (e.g., `ListInnovations`'s `researchCategory` filter needs `WHERE` support) and violates Simplicity Over Cleverness (constitution Principle 3) by introducing custom serialization where EF's native feature suffices.

## R2: Where completeness-rule evaluation lives

**Decision**: Each owned type exposes an `IsComplete()` method covering only the fields the spec's rule-mapping table assigns to it (e.g., `Product.IsComplete()` checks only `ProductDescription`/`TechnologyDescription`/`TargetBeneficiaries` — rules 7-9 — not all 8 Product fields). `Innovation` exposes the four named methods from FR-003, composing the section-level checks; `SubmitInnovation.cs`'s endpoint retains per-field checks **only** for building the `validationErrors` dictionary (FR-008 field identification), but the actual accept/reject branch calls `innovation.IsReadyForSubmission()` — not `validationErrors.Any()`.

**Rationale**: FR-005 requires the publish flow to delegate its completeness *decision* to `IsReadyForSubmission()` while still reporting per-field errors (FR-008). A single `if (!IsReadyForSubmission()) reject` would satisfy FR-005 but lose per-field detail; keeping only the inline checks (as an earlier revision of this branch did) satisfies FR-008 but left the entity methods as dead code — which is how the placeholder-title bug (`||` instead of `&&` in `IdeaSummary.IsComplete()`) went undetected: the two logic paths could diverge silently. Splitting responsibility — field-level checks build the error list, but `IsReadyForSubmission()` gates the response — was chosen so the two paths cannot diverge without a test catching it (see `SubmitInnovation_PlaceholderTitleOnly_Returns400AndMatchesIsReadyForSubmission`, which asserts the endpoint's decision equals `innovation.IsReadyForSubmission()`'s decision for a case the naive field checks alone would get right but a buggy completeness method would get wrong).

**Alternatives considered**:
- *Field checks derive from calling each section's `IsComplete()` and reporting a generic "section X incomplete" error*: Rejected for this feature — would reduce error granularity below current behavior (FR-006 requires unchanged behaviors other than completeness *checking itself*; existing tests assert specific field names like `"ResearchBackground"`, `"RelevantMarketSize"` in error bodies).
- *Move rule 13 (PartnersNeeded) fully into `CollaborationRequirement.IsComplete()`*: Not adopted — `CollaborationRequirement` has no `IsComplete()` method; the check stays inline in `Innovation.IsReadyForSubmission()` (`!string.IsNullOrWhiteSpace(CollaborationRequirement.PartnersNeeded)`), matching the spec's own rule-mapping table which files rule 13 under `IsReadyForSubmission` directly, not a section method.

## R3: Response payload shape for Get/List/Update

**Decision**: `GetInnovation`, `ListInnovations`, and `UpdateInnovation` return anonymous objects with `ideaSummary`, `product`, `market`, `collaborationRequirement` as nested objects; root-level fields (`id`, `status`, `createdAt`, `submittedAt`, `owner`, `targetIndustries`) stay flat per FR-010.

**Rationale**: FR-007 requires nested create/update/retrieve/list payloads; SC-004 requires 100% of mapped fields present under their section. The prior (in-diff, now-fixed) implementation had mechanically updated field *sources* (`innovation.IdeaSummary.Title`) while leaving the JSON *shape* flat — passing compilation but failing the spec's breaking-change contract. `ListInnovations`'s list-item DTO nests only `ideaSummary` (not all four sections) since the list view is a summary, matching the existing `InnovationListItem` record's minimal field set (title/productType/researchCategory) — full nesting is reserved for the detail (`GetInnovation`) response.

**Alternatives considered**:
- *Flat response, nested request only*: Rejected — FR-007 explicitly says "accept and return," and SC-004 measures response grouping.
- *Nest all sections in the list response too*: Considered, but the existing `InnovationListItem`/`InnovationListResponse` record types (still present, currently unused by the actual anonymous-object response) only declare `Title`/`ProductType`/`ResearchCategory` for list items — expanding list payload size for a discovery/browse view isn't requested by any FR and risks an unrequested change; deferred.

## R4: Angular client update scope

**Decision**: Update `innovation.model.ts`'s `InnovationDetail`/`InnovationListItem` interfaces to nest `ideaSummary`/`product`/`market`/`collaborationRequirement`, then fix the two consuming components (`innovation-detail.component.ts`, `innovations-list.component.ts`) to read through the new paths (e.g., `innovation.ideaSummary.title` instead of `innovation.title`). `innovations.service.ts` needs no logic change — it only passes the typed model through `HttpClient`, so once the interface is corrected, TypeScript's compiler will surface every now-broken field access in the two components as a compile error, which is the mechanism used to find all call sites exhaustively.

**Rationale**: FR-011 scopes client work to "only as far as needed for existing screens and existing tests to work against the nested structure" — confirmed by inspecting the client tree: exactly one model file and two components/their specs reference innovation fields (`grep` across `src/Innoventity.Client/src/app` found no other consumers). This is a mechanical, compiler-guided update, not new UI work, consistent with US4's independent test ("existing client unit tests and E2E journeys... pass without reducing coverage or removing assertions").

**Alternatives considered**:
- *Keep client model flat and have the API return both flat and nested fields (dual shape)*: Rejected — spec explicitly calls this a breaking change requiring client updates (Context & Background: "Breaking changes: YES"); a dual-shape response is exactly the kind of complexity Principle 3 (Simplicity Over Cleverness) warns against, and the spec's Assumptions section states there are no external API consumers requiring a deprecation window.
