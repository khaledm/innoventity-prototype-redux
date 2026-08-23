# Quickstart: Validating ProductIdea Composition Pattern

## Prerequisites

- .NET 8 SDK, Node 18+, SQL Server LocalDB (for the API's dev connection string)
- Repo checked out on branch `006-productidea-composition`
- Backend composition work (domain, persistence, endpoints) already implemented and committed on this branch

## Backend validation

From repo root:

```sh
dotnet build -c Release --no-restore
dotnet test
```

**Expected**: zero build warnings, all tests pass (241 as of this feature; see SC-003's baseline of 195 pre-feature — the delta reflects new owned-section and regression tests added in this feature).

### Validate the composition + migration didn't lose data

```sh
cd src/Innoventity.API
dotnet ef migrations list
```

**Expected**: `20260823142911_ComposeInnovationSections` appears after `20260718061742_AddFormalResponseHierarchy`, with empty `Up()`/`Down()` bodies (confirm by opening the migration file) — proving the composition is a pure C#-level reshape with zero physical schema change (FR-009).

### Validate the 13 completeness rules end-to-end

Run the targeted publish-gate tests:

```sh
dotnet test --filter "FullyQualifiedName~SubmitInnovationTests"
```

**Expected**: all pass, including `SubmitInnovation_Complete_Returns200AndPublishes`, `SubmitInnovation_Incomplete_Returns400WithErrors` (asserts all missing-field keys appear), and `SubmitInnovation_PlaceholderTitleOnly_Returns400AndMatchesIsReadyForSubmission` (regression guard for the placeholder-title logic bug found and fixed in this feature).

### Manually inspect the nested response shape

```sh
cd src/Innoventity.API
dotnet run
```

Then, with a valid JWT (see `AGENTS.md`/`Swagger` for the login flow), `GET /innovations/{id}` via Swagger (`/swagger`) or Scalar (`/scalar/v1`) and confirm the JSON body groups fields under `ideaSummary`, `product`, `market`, `collaborationRequirement` per [contracts/innovations-api.md](./contracts/innovations-api.md) — not flat.

## Frontend validation (remaining scope — see tasks.md)

Once `innovation.model.ts` and its two consuming components are updated to the nested shape:

```sh
cd src/Innoventity.Client
npm test
```

**Expected**: `innovations.service.spec.ts`, `innovation-detail.component.spec.ts`, `innovations-list.component.spec.ts` all pass against the nested model, with no reduction in assertion count (per US4's independent test).

```sh
npm run e2e
```

**Expected**: any Playwright journey touching innovation detail/list screens still passes (existing screens, no new ones — Out of Scope confirms no multi-step form or draft-save UI is added).

## Success criteria checkpoint (from spec.md)

- SC-001/SC-003: `dotnet test` green, seed data (`SeedData.cs`) round-trips through the new owned-entity shape without error
- SC-002: `SubmitInnovationTests` collectively exercise all 13 rules' failure and success cases
- SC-004: manual `GET /innovations/{id}` inspection (above) shows 100% of mapped fields under their section
- SC-005: run `dotnet stryker` from `src/Innoventity.API/` scoped to the new owned-entity `IsComplete()`/`IsReadyForSubmission()` methods; target ≥80%, floor >70%
- SC-006: full `dotnet test` run shows zero regressions in Bids/formal-response/auth/discovery test files (unmodified assertions still pass)
