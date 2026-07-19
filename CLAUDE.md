<!-- SPECKIT START -->
No feature is currently in progress. Feature `005-formalresponse-hierarchy`
is complete and merged into `Main` — do not treat it as active work.

For context on completed and upcoming work, see `specs/ROADMAP.md`. When a
new feature is started (via `/speckit-specify` or `/speckit-plan`), this
section will be regenerated to point at that feature's `plan.md`.
<!-- SPECKIT END -->

## Quick Reference

**Stack**: .NET 8 Minimal API (Vertical Slice Architecture) + Angular 19 SPA, SQL Server via EF Core 8 (TPH for polymorphism).

**Commands**:

- Backend tests: `dotnet test` (repo root)
- Backend release build (must be zero warnings before merge): `dotnet build -c Release --no-restore`
- Mutation testing (target ≥80%): `dotnet stryker` (from `src/Innoventity.API/`)
- Run API locally: `cd src/Innoventity.API && dotnet run` — Swagger at `/swagger`, Scalar at `/scalar/v1`
- Frontend unit tests: `cd src/Innoventity.Client && npm test`
- Frontend E2E: `npm run e2e` (Playwright; `npm run e2e:nightly` runs against the prod config)

**Where to look for more**:

- [`AGENTS.md`](AGENTS.md) — authoritative for architecture, editing conventions, the "do not do without approval" list, and evidence required before merging. Read this before non-trivial changes.
- [`specs/ROADMAP.md`](specs/ROADMAP.md) — phase sequencing and cross-feature status; check this before assuming what's "next."
- [`.specify/memory/constitution.md`](.specify/memory/constitution.md) — the 8 non-negotiable project principles.

This file (`CLAUDE.md`) stays intentionally short: the managed block above tracks the active Speckit feature, and this section is a fast-lookup index — not a duplicate of `AGENTS.md`.
