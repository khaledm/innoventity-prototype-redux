# Implementation Plan: Frontend CI/CD Automation

**Branch**: `002-frontend-cicd` | **Date**: April 12, 2026 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-frontend-cicd/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command.

## Summary

Automate Angular 19 frontend deployment to Azure Static Web Apps via GitHub Actions. Eliminates manual `swa deploy` commands and enables PR preview environments. Primary components: (1) GitHub Actions workflow for build/test/deploy/validate with production approval gate, (2) Terraform module for SWA provisioning, (3) integration with existing Angular configurations and backend API proxy.

## Technical Context

**Language/Version**: TypeScript 5.7.2, Angular 19.2.0, Node.js 18.x/20.x LTS (GitHub Actions hosted runners)
**Primary Dependencies**:
  - Frontend: Angular CLI 19.2.23, Angular Material 19.2.19, Jest 29.7.0, Playwright 1.59.1
  - CI/CD: GitHub Actions (Azure/static-web-apps-deploy@v1)
  - IaC: Terraform 1.6+, azurerm provider ~>3.116
**Storage**: Azure Static Web Apps (global CDN-backed static hosting), Azure Blob Storage (Terraform state), GitHub Actions artifacts (build outputs, 90-day retention)
**Testing**: Jest (unit tests, 28 tests, ~80% coverage target), Playwright (E2E tests, 3 tests), Pester (infrastructure tests)
**Target Platform**: Azure Static Web Apps Free tier (1 production + 3 preview environments), browser targets per Angular 19 defaults (ES2022)
**Project Type**: Web application deployment automation (CI/CD pipeline + infrastructure provisioning)
**Performance Goals**:
  - Build time: <5 minutes (npm ci + ng build)
  - Total workflow time: <15 minutes (build+test+deploy+validate)
  - Production deployment: <5 minutes (approval to live)
  - Preview validation: <30 seconds (HTTP health check)
**Constraints**:
  - Free tier Azure SWA: 1 production + 3 preview environments (PR preview quota)
  - GitHub Actions free tier: 2000 minutes/month (optimize build caching)
  - Test enforcement: Failed required **Jest unit** tests block preview and production deployment paths in Phase 1 and later — **Pattern D (permanent, 2026-05-25)**: Playwright E2E tests run nightly via `e2e-nightly.yml` and do not block the CI pipeline
  - Approval timeout: 24 hours (workflow auto-fails if not approved)
  - Zero breaking changes to existing Angular app or backend API
**Scale/Scope**: Single-environment deployment (dev), ~10 deployments/week, 3-5 developers, Angular SPA (~500kB initial bundle size)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Principle Alignment

**✅ Principle 1 — User Experience First**: CI/CD automation directly serves developer users by eliminating manual deployment friction (main pain point from Phase 0 deferred item). Preview environments enable reviewers to test changes interactively. Approval gates provide control. Solution is user-centric.

**⚠️ Principle 2 — Quality is Non-Negotiable**: Required Jest unit tests block all CI deployment paths in Phase 1 and later. Pester HTTP health checks validate every production deployment (Layer 1). Playwright E2E journey tests run nightly against the production deployment via Pattern D (`e2e-nightly.yml`) and trigger GitHub native notifications on failure (Layer 2). **Pattern D trade-off acknowledged**: a regression breaking the user journey can reach production and remain undetected for up to ~23 hours (next nightly run). This is a conscious trade-off accepted per Constraint 1 (Solo Project Realities) — the cost of adding service containers or deployment slots to enable pre-production E2E is not justified at current team scale. The production approval gate + Pester Layer 1 checks guard against infrastructure-level failures within the approval window.

**✅ Principle 3 — Simplicity Over Cleverness**: Uses Azure SWA's native preview environment feature (no custom infrastructure). GitHub Actions follows standard branch-appropriate patterns (build→test→deploy→validate, with approval only on Main). No complex orchestration or custom tooling. Leverages platform capabilities as intended. Boring, reliable automation.

**✅ Principle 4 — Specification Drives Implementation**: Comprehensive 898-line spec exists with 42 FRs, 20 scenarios, 10 success criteria. This plan follows spec workflow (SPECIFY→PLAN→TASKS→IMPLEMENT). All requirements traceable.

**⚠️ Principle 5 — Tests Must Prove They Work**: Infrastructure tests (Pester) will validate Terraform outputs. Workflow tests will use act (GitHub Actions local runner) or manual test runs to prove required test failures stop `deploy-preview`, `deploy-production`, and downstream validation until issues are fixed. **CRITICAL**: Must observe workflow failures before trusting success paths or approval-driven production releases.

**✅ Principle 6 — AI Augments, Humans Decide**: GitHub Actions workflow structure is human-designed (job sequencing, approval gates, conditional logic). Terraform module design is human-architected (resource dependencies, output design). AI may generate boilerplate YAML/HCL syntax after architecture defined.

**✅ Principle 7 — Architecture Must Support Evolution**: Design supports future multi-environment expansion (dev/staging/prod) via Terraform workspace pattern and GitHub environment strategy. No hardcoded single-environment assumptions. Can add production-grade features (Slack notifications, advanced health checks) without rewrite.

### Constraint Compliance

**✅ Constraint 1 — Solo Project Realities**: Realistic for one person. GitHub Actions workflows are standard DevOps skill. Terraform modules reuse existing patterns. Timeline: ~1-2 weeks for core automation, no dependencies on external teams.

**N/A Constraint 2 — No Data Migration**: Not applicable (infrastructure automation, not data).

**✅ Constraint 3 — Learning Objectives**: Demonstrates CI/CD best practices (pipeline as code, infrastructure as code, approval gates, health checks). Portfolio-worthy example of modern DevOps automation. Transferable skills for any Azure + GitHub environment.

**✅ Constraint 4 — Production-Ready Mindset**: Includes security (secret management, approval gates), monitoring (workflow logs, deployment telemetry), error handling (validation failures, rollback capability), documentation (README updates, runbook). This is production automation, not prototype scripts.

### Quality Gates

**MUST** before implementation:
- [x] Test validation strategy defined (see research.md: act for local testing, Pester for Terraform, red-green-refactor workflow)
- [x] Terraform state locking verified (existing azurerm backend has automatic lease-based locking)
- [x] GitHub Actions quota estimated (free tier 2000 min/month sufficient: ~15 min/workflow × 10 deployments/week = ~600 min/month)
- [x] SWA free tier limits confirmed (1 production + 3 preview environments sufficient for 3-5 developers with PR cleanup)

**WARNING** items to monitor:
- ⚠️ Required test failures will block deployments immediately — stabilize flaky suites early to avoid release delays
- ⚠️ 24-hour approval timeout is generous — may delay deployments if team unavailable
- ⚠️ Free tier SWA preview quota (3 environments) can exhaust quickly — cleanup strategy needed

### Verdict

**✅ PASS** — All quality gates satisfied. Research phase completed test validation strategy. Ready to proceed with implementation.

## Project Structure

### Documentation (this feature)

```text
specs/002-frontend-cicd/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── workflow-schema.md  # GitHub Actions workflow contract definition
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
.github/workflows/
├── deploy-frontend.yml         # NEW: Frontend CI/CD workflow (build/test/deploy)
├── deploy.yml                  # EXISTS: Backend API deployment (unchanged)
├── infra.yml                   # EXISTS: Infrastructure provisioning (unchanged)
└── drift.yml                   # EXISTS: Terraform drift detection (MODIFIED: enhanced for SWA api_key)

infrastructure/
├── modules/
│   ├── static-web-app/         # NEW: Terraform module for Azure Static Web Apps
│   │   ├── main.tf            # SWA resource definition
│   │   ├── variables.tf       # Input variables (SKU, location, tags)
│   │   └── outputs.tf         # Outputs (hostname, api_key, id)
│   ├── app-service/           # EXISTS: Backend App Service module (unchanged)
│   └── monitoring/            # EXISTS: Application Insights module (unchanged)
└── environments/
    └── dev/
        └── core/
            ├── main.tf        # MODIFIED: Add static-web-app module integration
            ├── backend.tf     # EXISTS: Remote state config (unchanged)
            └── outputs.tf     # MODIFIED: Add SWA outputs

src/Innoventity.Client/
├── src/
│   ├── environments/
│   │   ├── environment.ts             # EXISTS: Development environment config (unchanged)
│   │   └── environment.production.ts  # EXISTS: Production environment config (unchanged)
│   └── ...                    # EXISTS: Angular source code (unchanged)
├── angular.json               # EXISTS: Angular CLI config (unchanged)
├── package.json               # EXISTS: npm dependencies (unchanged)
└── staticwebapp.config.json   # EXISTS: SWA routing config (Phase 0, may need review)

specs/002-frontend-cicd/
└── runbooks/
    └── deployment.md          # NEW: Deployment runbook (manual procedures, troubleshooting)
```

**Structure Decision**: Single-project web application with separated infrastructure automation. GitHub Actions workflows live in standard `.github/workflows/` directory. Terraform modules follow existing pattern (`infrastructure/modules/<service>/`). Frontend app structure unchanged—only automation added around it. Operational runbooks co-located with feature specs (no root-level docs/ folder exists yet).

## Complexity Tracking

> **No violations requiring justification**

All constitutional principles and constraints are satisfied without requiring complexity violations. This feature adds operational automation (CI/CD pipeline and infrastructure provisioning) that aligns with:

- **Principle 2 (Quality is Non-Negotiable)**: Automated testing and validation before deployment ensures quality gates
- **Principle 3 (Simplicity Over Cleverness)**: Uses Azure SWA native features and standard GitHub Actions patterns (no custom infrastructure)
- **Principle 4 (Specification Drives Implementation)**: Comprehensive spec → plan → tasks workflow followed
- **Constraint 1 (Solo Project Realities)**: Realistic scope for one person, standard DevOps tools
- **Constraint 4 (Production-Ready Mindset)**: Includes approval gates, health checks, monitoring, documentation

No abstraction layers or architectural complexity added—leverages platform capabilities as designed.

---

## Phase 2: Implementation Planning (NOT EXECUTED BY /speckit.plan)

**Note**: The `/speckit.plan` command ends here. Phase 2 (task generation) is executed separately via the `/speckit.tasks` command.

---

## Plan Summary

### What We've Designed

**Phase 0 (Research)**:
- ✅ GitHub Actions best practices for Angular deployment
- ✅ Azure Static Web Apps deployment patterns (preview vs. production)
- ✅ Terraform azurerm_static_web_app module design
- ✅ Test validation strategy (Principle 5 compliance)
- ✅ Approval gate implementation approach
- ✅ Preview environment cleanup strategy

**Phase 1 (Design & Contracts)**:
- ✅ Data model with 8 entities (Workflow, Job, Artifact, Environment, Approval, Deployment, HealthCheck, Preview)
- ✅ State transitions and lifecycle diagrams
- ✅ Workflow contract (inputs, outputs, secrets, SLAs, failure modes)
- ✅ Quickstart guide (step-by-step first-time setup)
- ✅ Agent context updated (TypeScript, Angular 19, GitHub Actions, Azure SWA, Terraform)

### Key Design Decisions

| Decision | Rationale | Alternative Rejected |
|----------|-----------|----------------------|
| **Direct production deployment** (no slot swap) | SWA uses preview/production environments, not slots. Approval gate provides safety. | Staging → swap pattern (not supported by SWA Free tier) |
| **Required tests block all deployments** | Aligns with spec and constitutional quality gates; failed required tests must stop preview and production paths immediately in Phase 1+ | Warning-only enforcement (conflicts with required blocking behavior) |
| **Pattern D: E2E tests run nightly (not in CI)** | Playwright journey tests require live .NET API + SQL Server — unavailable on GitHub Actions hosted runners without service containers. Nightly run against production (`e2e-nightly.yml`) provides journey validation within ~23h. Accepted per Constraint 1 (Solo Project Realities). | Pattern A (service containers, adds ~5min + SQL setup), Pattern B (post-deploy slots, ~$50/mo App Service Standard), Pattern C (smoke+journey split) |
| **GitHub native notifications** | Built-in email/UI/mobile notifications sufficient for Phase 1 | Slack/Teams integration (adds complexity, deferred to Phase 2+) |
| **24-hour approval timeout** | Maximum flexibility for small team availability | Shorter timeout (may cause delays if team unavailable) |
| **Free tier SWA** | 1 production + 3 previews sufficient for dev environment | Standard tier (unnecessary cost for dev) |
| **Terraform module** | Infrastructure-as-code for reproducibility | Manual Azure Portal provisioning (not reproducible) |
| **Prebuilt artifacts** | Faster deployment (upload only), consistent builds | Azure SWA rebuild (slower, less control) |

### Architecture

```
┌──────────────────┐
│  Developer Push  │ (Code change to src/Innoventity.Client/**)
└────────┬─────────┘
         │
         ▼
┌──────────────────────────────────────────────────────────┐
│ GitHub Actions Workflow (.github/workflows/deploy-frontend.yml) │
└───┬──────────────────────────────────────────────────────┘
    │
    ├─► [Build Job]  → Compile Angular → Upload artifact
    │
    ├─► [Test Job]   → Jest unit tests (required failures stop workflow)
    │                  Playwright E2E: NOT in CI — Pattern D (nightly, see e2e-nightly.yml)
    │
    ├─► [Deploy-Preview Job] (if branch != Main)
    │   └─► Azure Static Web Apps (Preview Environment)
    │       └─► URL: https://<swa>-<pr-number>.azurestaticapps.net
    │
    ├─► [Validate-Preview Job] (if branch != Main)
    │   └─► HTTP 200 + expected version before merge evidence
    │
    └─► [Approval Gate] (if branch == Main)
        ├─► GitHub Environment, 24-hour timeout
        ├─► Required Reviewer approves/rejects
        ├─► [Deploy-Production Job]
        │   └─► Azure Static Web Apps (Production)
        │       └─► URL: https://innoventity-dev-web.azurestaticapps.net
        │
        └─► [Validate-Production Job]
            └─► HTTP 200 + expected version after production deploy
```

**Terraform Provisioning**:
```
infrastructure/modules/static-web-app/  (NEW module)
  ├── main.tf       → azurerm_static_web_app resource
  ├── variables.tf  → SKU, location, tags
  └── outputs.tf    → default_hostname, api_key (sensitive), id

infrastructure/environments/dev/core/main.tf (MODIFIED)
  └── module "static_web_app" { ... }
```

### Success Criteria

Implementation is **complete** when:

1. ✅ Terraform module exists and provisions SWA resource
2. ✅ GitHub Actions workflow exists with all jobs (build, test, deploy-preview, validate-preview, approval, deploy-production, validate-production)
3. ✅ Feature branch and PR workflows trigger preview deployment and preview validation before merge (<15 minutes total workflow time)
4. ✅ Merge to Main triggers approval gate → production deployment → production validation after approval
5. ✅ Preview environments auto-delete on PR closure (via cleanup workflow)
6. ✅ Health checks validate preview before merge and validate production after deploy (HTTP 200, Content-Type: text/html)
7. ✅ Zero manual `swa deploy` commands required for any scenario
8. ✅ Documentation updated (infrastructure README, quickstart, runbooks/deployment.md)
9. ✅ All tests observed failing before trusting (Principle 5 compliance: act for workflows, Pester for Terraform)
10. ✅ Constitutional quality gates passed (all pre-implementation MUSTs satisfied)

---

## Next Command

```bash
/speckit.tasks
```

This will generate the task breakdown (`tasks.md`) based on this implementation plan.

---

## Implementation Notes

### Post-Plan Amendments

#### drift.yml Enhancement (May 4, 2026)

**Issue**: After implementing the SWA Terraform module (T010), the daily drift detection workflow began failing with exit code 2. Investigation revealed that `azurerm_static_web_app.api_key` is a computed attribute that Azure may rotate, causing Terraform to detect output changes even when no manual resource modifications occurred.

**Root Cause**: The original drift.yml logic treated any exit code 2 from `terraform plan -detailed-exitcode` as drift requiring manual reconciliation. This was correct for actual resource changes (tags, SKU, location modified in portal), but created false positives for output-only changes like SWA `api_key` rotation.

**Solution** (Commit `150b1e7`):
- Enhanced `drift-check-core` job to parse plan JSON using `terraform show -json tfplan.binary`
- Added logic to distinguish between:
  - **Resource changes** (`resource_changes > 0`) → Real drift, workflow fails with error annotation
  - **Output-only changes** (`resource_changes == 0`) → Acceptable, workflow passes with info log
- Uses `jq` to count `resource_changes` vs `output_changes` and only fails if resources were actually modified

**Impact**:
- ✅ Prevents false positive drift alerts from computed attributes (SWA api_key, future computed values)
- ✅ Maintains detection of actual infrastructure drift (manual changes in Azure Portal)
- ✅ Workflow now passes when only outputs change, fails when resources change
- ⚠️ Adds dependency on `jq` utility (available by default on GitHub Actions ubuntu-latest runners)

**Files Modified**:
- `.github/workflows/drift.yml`: Added JSON parsing and conditional exit logic to `drift-check-core` job (commit `150b1e7`)
- `.github/workflows/drift.yml`: Refined jq filter to exclude no-op resources from count (commit `b8937a1`)

**Related Issues**: Resolves C1 CRITICAL finding from `/speckit.analyze` command

**Note**: After implementing this enhancement, drift detection revealed actual infrastructure drift in both data/ (SQL Server `object_id`/`tenant_id` nullification) and core/ (Linux Web App configuration) layers. These require reconciliation via `terraform apply` — not related to the SWA false positive issue.

---

**Plan Complete**: April 12, 2026
**Branch**: `002-frontend-cicd`
**Next Phase**: Task Generation (via `/speckit.tasks` command)
