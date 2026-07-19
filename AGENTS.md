# AGENTS.md

## Project Context

**Innoventity Platform** — A SaaS for managing innovation partnership selection and business planning.

### Core Architecture

- **Backend**: C# 12 / .NET 8 (ASP.NET Core Minimal APIs)
- **Frontend**: Angular (SPA) with TypeScript, Jest, Playwright for E2E testing
- **Pattern**: Vertical Slice Architecture (backend); feature-driven structure (frontend)
- **Database**: SQL Server via EF Core 8 (TPH for polymorphism, JSON columns for structured data)
- **Testing**: xUnit integration tests (backend) + Playwright E2E (full stack); InMemory EF for unit tests
- **Quality**: Stryker.NET mutation testing (backend, target ≥80%), Conventional Commits, Constitution-driven development

### Governance

- **Constitution v1.1.0**: 8 non-negotiable principles including Specification Drives Implementation, Tests Must Prove They Work, and Conventional Commits (v1.0.x)
- **Spec Kit Workflow**: SPECIFY → PLAN → TASKS → IMPLEMENT (all artifacts in `/specs/`)
- **Definition of Ready**: Spec + Plan + Tasks complete before implementation begins
- **Quality Gates**: All tests pass, mutation score ≥80%, no regressions on pre-existing features

---

## Repository Map

- **src/Innoventity.API**: Backend API (C# 12 / .NET 8)
  - `Features/`: Vertical slices — one feature class per endpoint
  - `Domain/Entities/`: Core domain models (Innovations, Actors, FormalResponses, etc.)
  - `Infrastructure/Persistence/`: EF Core context, migrations, seed data
  - `Program.cs`: Endpoint registration and middleware setup
- **src/Innoventity.Client**: Frontend SPA (Angular + TypeScript)
  - `src/app/`: Routes and page composition
  - `src/features/`: User-facing feature modules (partner-selection, bidding, etc.)
  - `src/lib/`: Shared utilities with stable HTTP/auth contracts
- **specs/**: Design artifacts (Spec Kit workflow)
  - `001-`, `002-`, etc.: Feature directories with spec.md, plan.md, tasks.md
  - `.specify/`: Constitution, memory, and script infrastructure
- **tests/**: Test suites
  - `Innoventity.API.Tests/Integration/`: xUnit integration tests with InMemory EF
  - `Innoventity.API.Tests/E2E/`: Full-stack journey tests
  - `Innoventity.Client/`: Angular Jest and Playwright specs
- **infrastructure/**: Infrastructure-as-Code (Terraform, helper SQL and PS scripts, deployment)

---

## Before Editing

1. **Identify the feature**: Confirm which `specs/NNN-feature-name/` contains the spec, plan, and tasks.
2. **Search for patterns**: Look for similar endpoints in `Features/` and existing tests in `tests/Integration/Features/`.
3. **State intent**: Before broad changes, list the files you intend to modify and the checks you'll run.
4. **Test-first discipline** (Principle 5): Write failing tests before implementing; verify red → green → refactor.
5. **Backward compatibility**: Confirm pre-existing tests pass; flag breaking changes in commit message with `!` and `BREAKING CHANGE:` footer.

---

## Do Not Do Without Approval

- **Add production dependencies** — must be reviewed for security and compatibility
- **Rewrite large backend modules** — impacts Vertical Slice boundaries and testability
- **Change database schema** — requires migration planning and reversibility documentation
- **Change public API response shapes** — breaking changes affect all consumers (frontend, external clients)
- **Touch authentication, authorization, or credential handling** — security-critical; needs human review
- **Modify seed data or migrations** — impacts test determinism and data integrity
- **Deploy to production** — requires full quality gate pass and explicit approval

---

## Evidence Required

### Before Merging to `main`

1. **All tests pass**: `dotnet test` from repo root — target ≥166 passing tests (131 pre-existing + new feature tests)
2. **Mutation score ≥80%**: `dotnet stryker` from `src/Innoventity.API/` for all modified/new handlers
3. **No regressions**: Pre-existing test count unchanged unless feature explicitly deprecates functionality
4. **Release build clean**: `dotnet build -c Release --no-restore` succeeds with zero warnings
5. **Conventional Commits**: All commits follow format `<type>(<scope>): <description>` with breaking changes marked `!` and `BREAKING CHANGE:` footer
6. **Frontend tests pass** (if UI changes): `npm test` and `npm run e2e` from `src/Innoventity.Client/`

### For Breaking Changes (marked with `!`)

- Explicitly list all removed endpoints, parameter renames, and type changes in commit body
- Verify all callers (tests, frontend, documentation) updated
- Confirm migration strategy if database schema changed (up/down reversibility documented)
- Document deprecated patterns for 1–2 releases before removal

### Risk Assessment Checklist

Flag and explain:

- **Auth/Authz changes**: Who gains/loses access? Test new guards in isolation.
- **Data loss risk**: Migrations reversible? Seed data backed up? 
- **User-visible behavior**: Changes to response shapes, validation, error messages, or UI flows?
- **Performance**: Large data migrations? N+1 queries? Load-test if projections > 100 records/page.
- **Breaking compatibility**: API, database, or frontend contract changes? Clearly marked in commit.
