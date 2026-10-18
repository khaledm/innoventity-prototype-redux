<!--
Thanks for contributing to Innoventity! Fill in every section below —
replace each italic prompt with real content. Keep the PR focused on one
logical change; split unrelated work into separate PRs.
-->

# Summary

_1-3 sentences, business-focused. What does this do for users or the platform?_

# Why is this change needed?

_The problem being solved. Link the spec/issue/user story — don't restate it here._

# What changed?

_Bullet list of key changes. Keep each line scannable; group backend/frontend/infra if mixed._

-
-

# Impact Areas

- [ ] Backend feature endpoint(s) (`src/Innoventity.API/Features/**`)
- [ ] Domain entities / business rules (`src/Innoventity.API/Domain`)
- [ ] EF Core migration (new file(s) under `Infrastructure/Persistence/Migrations`)
- [ ] Seed data
- [ ] Authentication / Authorization
- [ ] Public API response shape or route (breaking or additive)
- [ ] OpenAPI/Swagger contract (`AddSwaggerGen` schema, Scalar reference)
- [ ] Frontend SPA (`src/Innoventity.Client`)
- [ ] Infrastructure (Terraform / `infrastructure/`)
- [ ] GitHub Actions workflows (`.github/workflows`)
- [ ] Spec Kit artifacts (`specs/`)
- [ ] Dependencies (NuGet / npm)
- [ ] Documentation only

# Change Type

- [ ] `feat` — new feature
- [ ] `fix` — bug fix
- [ ] `refactor` — no behavior change
- [ ] `perf` — performance improvement
- [ ] `docs` — documentation only
- [ ] `chore`/`ci` — tooling, build, infra
- [ ] `test` — test-only change
- [ ] **Breaking change** (`!` in commit type, `BREAKING CHANGE:` footer present)

# Testing Performed

**Test-First Confirmation (Constitution Principle 5):**

- [ ] New tests were written **before** implementation and observed **failing** (red), then made to pass (green)
- [ ] For changes to existing code: tests were validated via characterization (deliberately broke the logic, confirmed the test caught it)
- [ ] Assertions validate business rules/outcomes, not just HTTP status codes or "no exception thrown"
- [ ] No tests were written or adjusted _after the fact_ just to match final implementation output

_How was red → green observed? e.g. "ran `dotnet test --filter ...`, saw N failures before implementing X":_

**Suite Results:**

- [ ] `dotnet test` passes locally (all suites, no regressions in pre-existing count)
- [ ] New/changed feature endpoints have xUnit integration tests (`tests/Innoventity.API.Tests/Integration`, InMemory EF)
- [ ] `dotnet build -c Release --no-restore` succeeds with **zero warnings**
- [ ] `dotnet stryker` run if this touches a file in `stryker-config.json`'s `mutate` list — score ≥80% target (CI breaks below 71%; attach report or score)
- [ ] EF Core migration applied and verified locally (`dotnet ef database update`); `Down()` method reviewed
- [ ] OpenAPI schema reviewed in Swagger/Scalar — no unintended contract drift
- [ ] Frontend: `npm test` passes (if UI changed)
- [ ] Frontend: `npm run e2e` passes (if user-facing flow changed)

_Manual verification notes, if any:_

# Evidence

_Paste or link: test run output, Stryker HTML report, coverage %, screenshots/GIFs for UI,
API response samples, benchmark numbers. Prefer links to CI artifacts over pasted logs._

# Risks

_Call out anything from AGENTS.md "Do Not Do Without Approval" or the constitution's Risk
Assessment Checklist. Delete rows that don't apply._

- **Auth/Authz impact**:
- **Data loss / migration risk** (reversible? seed data affected?):
- **User-visible behavior change** (response shape, validation, error messages, UI):
- **Performance** (N+1 queries, large pages, load impact):
- **Breaking compatibility** (API/DB/frontend contract):

# Reviewer Guidance

_Point reviewers at the risky bits. Call out, where relevant:_

- _Feature endpoint logic that deviates from the vertical-slice pattern used elsewhere in `Features/`_
- _New/edited EF Core migration — is it additive-only? does `Down()` actually reverse `Up()`?_
- _Any change to `Program.cs` endpoint registration, middleware order, or route conventions_
- _Architecture boundary crossings (e.g. a feature reaching into another feature's internals)_
- _Workflow/pipeline changes (`.github/workflows`) — which gate do they add, skip, or weaken?_
- _Alternatives considered and rejected, and why_

_Architectural decisions made (if any) — note here or link an ADR:_

# Checklist

- [ ] Commit messages follow Conventional Commits (`<type>(<scope>): <description>`, imperative, lowercase, ≤50 chars)
- [ ] Breaking changes marked with `!` and have a `BREAKING CHANGE:` footer with migration notes
- [ ] No production dependency added without prior discussion
- [ ] No schema/migration change without reversibility noted in **Risks**
- [ ] No auth/authz/credential code touched without explicit review flagged above
- [ ] Spec/plan/tasks under `specs/` updated if this implements or deviates from an active feature
- [ ] `specs/ROADMAP.md` updated if this completes or starts a phase item
- [ ] No secrets, connection strings, or credentials committed
- [ ] Self-review completed against `.specify/memory/constitution.md` Code Review Checklist

# Links

_Related issue / spec / ADR / roadmap item. Use "Closes #123" to auto-close on merge._

- Spec: `specs/___/spec.md`
- Issue:
- Roadmap item:
