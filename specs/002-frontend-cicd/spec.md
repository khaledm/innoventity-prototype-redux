# Feature Specification: Frontend CI/CD Automation

**Feature Branch**: `002-frontend-cicd`
**Created**: April 12, 2026
**Status**: Draft
**Input**: Automated CI/CD pipeline for Angular 19 frontend deployment to Azure Static Web Apps. Must include: GitHub Actions workflow with build/test/deploy/validate jobs, Terraform module for SWA provisioning, integration with existing proxy configuration, PR preview environments, manual production approval gate for Main branch.

---

## Context & Background

**Current State**: Phase 0 MVP delivered a functional Angular 19 frontend, but deployment requires manual execution of `swa deploy` command. This creates operational friction and prevents rapid iteration.

**Problem**:

- Manual deployment is error-prone and time-consuming
- No automated testing before production deployment
- No preview environments for PR reviews
- Frontend deployment not tracked in CI/CD pipeline
- Deployment token management is manual

**Constitutional Impact**: Completes operational excellence requirements for 99/100 constitutional score by automating the final manual deployment step deferred from Phase 0.

**Reference**: [NEXT_ACTIONS.md P1-Priority-1](../../NEXT_ACTIONS.md), [Phase 0 MVP limitation](../../specs/001-platform-core/spec.md)

---

## Clarifications

### Session 2026-04-12

- **Q**: Deployment strategy - how should production deployments work given Azure SWA doesn't support slot swapping? → **A**: Deploy directly to production environment after approval gate (Option A - standard SWA pattern). Azure Static Web Apps uses preview/production environments, not staging/production slots.
- **Q**: What should the production approval timeout be? → **A**: 24 hours (Option C - maximum flexibility for small team).
- **Q**: What notification channel for deployment failures? → **A**: GitHub Actions native notifications only (email, UI, mobile) - defer Slack/Teams to Phase 2+ (Option A).

### Session 2026-04-18

- **Q**: After a PR is merged or closed, should the preview environment be deleted immediately or persist for post-merge review? → **A**: Delete immediately on PR close/merge using native SWA `action: 'close'` on `pull_request: [closed]` event. Persisting risks exhausting the Free tier 3-preview limit.
- **Q**: When the Free tier's 3-preview limit is hit, what should the workflow do? → **A**: Fail workflow with clear error message (Option A). Developer must manually delete a stale preview environment. No automatic SKU upgrade or silent continue-on-error.
- **Q**: What URL should the validation job target after deployment? → **A**: Use the output URL from the preceding deploy job — preview URL for feature branches, production URL for Main (Option A). Hardcoding a URL would validate the wrong environment.

### Session 2026-05-09

- **Q**: Which rule should the spec enforce for failed required tests? → **A**: Failed required tests block all deployments in Phase 1 and later.
- **Q**: Which sequencing rule should the spec use for Main deployments? → **A**: Main runs: build, test, approval, deploy production, validate production; pre-production validation happens on PR preview before merge.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Automated Deployment on Push (Priority: P1)

As a **developer**, I need frontend changes automatically built, tested, and deployed when I push to the feature branch so that I can iterate quickly without manual deployment steps.

**Why this priority**: CRITICAL - Core functionality that eliminates the Phase 0 deferred item. Without this, all other stories have no foundation.

**Independent Test**: Can be fully tested by pushing a commit to a feature branch, observing GitHub Actions workflow execution, and verifying deployed changes at the SWA preview URL.

**Acceptance Scenarios**:

1. **Given** a feature branch with frontend changes, **When** developer pushes commit, **Then** GitHub Actions workflow triggers automatically
2. **Given** workflow triggered, **When** build job executes, **Then** Angular app builds successfully with production configuration
3. **Given** build succeeds, **When** test job executes, **Then** all required Jest unit tests and Playwright E2E tests pass before any deployment begins
4. **Given** any required test fails, **When** the test job concludes, **Then** the workflow fails and no deploy job executes
5. **Given** required tests pass, **When** the deploy job executes, **Then** the artifact is uploaded to the Azure Static Web Apps preview environment
6. **Given** deployment complete, **When** the validate job executes, **Then** the preview URL returns HTTP 200 and serves the Angular app

---

### User Story 2 - PR Preview Environments (Priority: P2)

As a **code reviewer**, I need automatic preview deployments for each pull request so that I can test frontend changes interactively before approving the merge.

**Why this priority**: HIGH - Significantly improves code review quality by allowing hands-on testing. Leverages Azure Static Web Apps' built-in PR preview feature.

**Independent Test**: Can be fully tested by creating a PR from feature branch to Main, verifying SWA creates preview environment, and accessing the preview URL from PR comments.

**Acceptance Scenarios**:

1. **Given** a pull request opened to Main, **When** PR creation detected, **Then** Azure Static Web Apps automatically creates preview environment
2. **Given** preview environment created, **When** reviewer visits preview URL, **Then** frontend changes visible with isolated configuration
3. **Given** PR updated with new commits, **When** changes pushed, **Then** preview environment automatically updates
4. **Given** PR closed or merged, **When** PR status changes, **Then** preview environment automatically deleted (cleanup)

---

### User Story 3 - Production Deployment with Approval Gate (Priority: P2)

As a **platform operator**, I need a manual approval step before production deployment so that release timing is controlled and validated changes reach end users.

**Why this priority**: HIGH - Critical for production safety. Prevents accidental deployments and allows coordination with stakeholders.

**Independent Test**: Can be fully tested by merging to Main branch, observing workflow pause at approval gate, manually approving, and verifying production deployment completes.

**Acceptance Scenarios**:

1. **Given** changes merged to Main branch, **When** the workflow runs, **Then** `build` and all required tests complete before the workflow pauses at the production approval gate
2. **Given** workflow awaiting approval, **When** an authorized operator reviews the Main branch workflow results and PR preview validation evidence, **Then** the operator can approve or reject via GitHub UI
3. **Given** approval granted, **When** the `deploy-production` job executes, **Then** the approved artifact is deployed directly to the production environment
4. **Given** production deployment completes, **When** the production validation step executes, **Then** the production URL returns HTTP 200 and serves the new frontend version at `https://innoventity-dev-web.azurestaticapps.net`
5. **Given** approval is rejected, times out, or required tests failed earlier in the workflow, **When** the workflow concludes, **Then** production remains unchanged and no production deployment occurs

---

### User Story 4 - Infrastructure Provisioning (Priority: P2)

As a **DevOps engineer**, I need Azure Static Web Apps provisioned via Terraform so that infrastructure is version-controlled and reproducible across environments.

**Why this priority**: MEDIUM - Important for infrastructure consistency, but can be manually provisioned initially if needed. Terraform module enables future environment replication.

**Independent Test**: Can be fully tested by running `terraform apply` in dev/core environment, verifying SWA resource created, and confirming deployment token output as Terraform output.

**Acceptance Scenarios**:

1. **Given** Terraform module defined at `infrastructure/modules/static-web-app/`, **When** operator runs `terraform plan` in dev/core, **Then** SWA resource planned with correct configuration
2. **Given** Terraform plan approved, **When** operator runs `terraform apply`, **Then** Azure Static Web Apps resource provisioned in dev resource group
3. **Given** SWA provisioned, **When** Terraform execution completes, **Then** deployment token output as sensitive value
4. **Given** deployment token available, **When** operator configures GitHub secret `AZURE_STATIC_WEB_APPS_API_TOKEN`, **Then** workflow can authenticate to SWA
5. **Given** SWA resource exists, **When** operator runs second `terraform apply`, **Then** no changes detected (idempotency)

---

### User Story 5 - Validation and Health Checks (Priority: P3)

As a **quality assurance engineer**, I need automated health checks after deployment so that critical failures are detected before production deployment.

**Why this priority**: LOW - Nice-to-have quality gate. Can initially proceed with manual validation if time-constrained.

**Independent Test**: Can be fully tested by deploying frontend with broken configuration, observing validation job fails, and verifying deployment does not proceed to production.

**Acceptance Scenarios**:

1. **Given** deployment to preview or production complete, **When** validation job executes, **Then** HTTP GET request sent to the URL output from the preceding deploy job
2. **Given** deployment URL responds, **When** response validated, **Then** status code must be 200 and Content-Type must be `text/html`
3. **Given** PR preview validation passes before merge, **When** reviewers inspect workflow evidence, **Then** that preview validation serves as the pre-production validation signal for Main
4. **Given** production deployment is approved and executed on Main, **When** production validation completes, **Then** the workflow only succeeds if the production URL passes the same health checks
5. **Given** deployment URL returns 404 or 500, **When** validation job evaluates response, **Then** workflow fails and any downstream deployment or successful completion step is prevented
6. **Given** validation failure, **When** developer or operator is notified, **Then** GitHub Actions sends notification with failure details

---

### Edge Cases

- **What happens when Azure Static Web Apps deployment quota exhausted?** Workflow MUST fail with a clear error message (e.g., "SWA preview quota reached: 3/3 environments in use"). Developer must manually close a stale PR or run the cleanup workflow (`action: 'close'`) to free a slot. No automatic SKU upgrade or silent skip permitted.
- **What happens when GitHub Actions workflow fails mid-deployment?** Preview environment may have partial deployment. Workflow retry will overwrite with fresh deployment. Production environment remains unchanged.
- **What happens when Playwright E2E tests fail during test job?** Workflow fails and neither preview nor production deployment proceeds in Phase 1 and later. Developer must fix tests and push new commit to retry.
- **What happens when two developers push to Main simultaneously?** GitHub Actions queues workflows sequentially. Second workflow waits for the first to complete approval, production deployment, and production validation.
- **What happens when deployment token expires or is rotated?** Workflow fails with authentication error. Operator must update `AZURE_STATIC_WEB_APPS_API_TOKEN` secret with new token from Azure Portal.
- **What happens when approval gate times out (24 hours)?** Workflow automatically fails before production deployment. Production environment unchanged. Operator must re-run workflow to retry.

---

## Requirements *(mandatory)*

### Functional Requirements

#### GitHub Actions Workflow

- **FR-001**: Workflow MUST trigger on push to any branch containing frontend changes (`src/Innoventity.Client/**`)
- **FR-002**: Workflow MUST trigger on manual dispatch (`workflow_dispatch`) with configurable environment parameter
- **FR-003**: Workflow MUST execute branch-appropriate sequential stages: feature branch and PR preview runs follow `build` → `test` → `deploy-preview` → `validate-preview`, while Main runs follow `build` → `test` → `approval` → `deploy-production` → `validate-production`
- **FR-004**: `build` job MUST execute `npm ci` to install dependencies using lockfile
- **FR-005**: `build` job MUST execute `npm run build` with production configuration
- **FR-006**: `build` job MUST upload `dist/` folder as GitHub Actions artifact
- **FR-007**: `test` job MUST execute Jest unit tests (`npm test -- --coverage`)
- **FR-008**: `test` job MUST execute Playwright E2E tests (`npm run test:e2e`)
- **FR-009**: `test` job MUST report test results and MUST fail the workflow on any required unit or E2E test failure in Phase 1 and later, blocking both preview and production deployments (target: 28/28 unit, 3/3 E2E)
- **FR-010**: `deploy-preview` job MUST download build artifact from `build` job
- **FR-011**: `deploy-preview` job MUST authenticate to Azure Static Web Apps using `AZURE_STATIC_WEB_APPS_API_TOKEN` secret
- **FR-012**: `deploy-preview` job MUST deploy to preview environment for feature branches OR skip for Main branch
- **FR-013**: `deploy-preview` job MUST output preview URL as job output (for feature branches)
- **FR-014**: `deploy-production` job MUST only execute when branch is `Main` (conditional execution)
- **FR-015**: `deploy-production` job MUST require manual approval via GitHub Environment protection rule BEFORE deployment
- **FR-016**: `deploy-production` job MUST download build artifact from `build` job
- **FR-017**: `deploy-production` job MUST deploy directly to production environment (NOT preview)
- **FR-018**: `validate-production` job MUST execute only after `deploy-production` succeeds on Main and MUST validate that the production URL (output from deploy step) returns HTTP 200, Content-Type `text/html`, and the Angular app bootstraps successfully (response body contains `<app-root>`) — *amended 2026-05-10: "serves the new version" tightened to observable, testable assertions already implemented in `DeploymentValidation.Tests.ps1`*
- **FR-019**: Workflow MUST send deployment status notification on success or failure via GitHub Actions native notifications (email, UI, mobile app); Slack/Teams integrations deferred to Phase 2+

#### Terraform Infrastructure

- **FR-020**: Terraform module MUST exist at `infrastructure/modules/static-web-app/main.tf`
- **FR-021**: Module MUST provision `azurerm_static_web_app` resource (NOT deprecated `azurerm_static_site`)
- **FR-022**: SWA MUST be configured with SKU `Free` or `Standard` (configurable via variable)
- **FR-023**: SWA MUST enable GitHub integration for PR preview environments
- **FR-024**: SWA MUST be created in same resource group as backend App Service (`innoventity-dev-rg`)
- **FR-025**: Module MUST output `default_host_name` (SWA URL)
- **FR-026**: Module MUST output `api_key` (deployment token) marked as `sensitive = true`
- **FR-027**: SWA resource MUST include `tags` with `Environment`, `ManagedBy = "Terraform"`, `Component = "Frontend"`
- **FR-028**: Module MUST be integrated into `infrastructure/environments/dev/core/main.tf`

#### Configuration Integration

- **FR-029**: Deployed frontend MUST use existing Angular environment configurations (`environment.ts`, `environment.production.ts`)
- **FR-030**: SWA `staticwebapp.config.json` MUST define routing rules for SPA (redirect all to `index.html`)
- **FR-031**: SWA `staticwebapp.config.json` MUST define navigation fallback for Angular routing
- **FR-032**: Production configuration MUST point to backend API at `https://innoventity-dev-api.azurewebsites.net`
- **FR-033**: Local development configuration MUST use the existing proxy configuration for local testing (`proxy.conf.js`) — *amended 2026-05-10: changed from `proxy.conf.json` to `proxy.conf.js`; JS format required to support `bypass()` function that prevents Angular dev server from proxying browser HTML navigations to the API (commit `753f85b`)*

#### PR Preview Environments

- **FR-034**: Azure Static Web Apps MUST automatically create preview environment for each PR to Main branch
- **FR-035**: Preview environment MUST have unique URL format: `https://<swa-name>-<pr-number>.azurestaticapps.net`
- **FR-036**: GitHub Actions bot MUST post preview URL as comment on PR
- **FR-037**: Preview environment MUST automatically delete immediately when PR is closed or merged, triggered by `pull_request: [closed]` event using `Azure/static-web-apps-deploy@v1` with `action: 'close'`
- **FR-038**: Preview environments MUST NOT count against production deployment quota
- **FR-043**: When SWA Free tier preview quota (3 environments) is exhausted, workflow MUST fail with a descriptive error message; no silent skip or automatic SKU upgrade permitted

#### Security & Access Control

- **FR-039**: Deployment token (`AZURE_STATIC_WEB_APPS_API_TOKEN`) MUST be stored as GitHub repository secret (NOT hardcoded)
- **FR-040**: Production approval gate MUST restrict approvers to repository owners or designated environments reviewers
- **FR-041**: Approval gate MUST have 24-hour timeout (workflow auto-fails if no approval)
- **FR-042**: Terraform state for SWA module MUST use same remote backend as existing infrastructure (`azurerm` backend)

### Non-Functional Requirements

- **NFR-001**: Workflow execution time MUST complete within 15 minutes for feature branches (build+test+deploy+validate)
- **NFR-002**: Production deployment and production validation MUST complete within 5 minutes after approval granted
- **NFR-003**: Deployment must achieve 99.9% success rate (excluding user errors like test failures)
- **NFR-004**: Workflow logs MUST be retained for 90 days minimum
- **NFR-005**: Failed deployments MUST send notification via GitHub Actions native notifications (email, UI, mobile app)

### Key Entities

- **GitHub Actions Workflow** (`deploy-frontend.yml`): Orchestrates build, test, approval, deploy-preview, validate-preview, deploy-production, and validate-production stages
- **Azure Static Web Apps Resource**: Hosts Angular SPA with global CDN distribution
- **Deployment Token**: Authenticates GitHub Actions to Azure SWA
- **Preview Environment**: Temporary deployment for feature branches and PR reviews (auto-created by SWA)
- **Production Environment**: Live user-facing deployment at primary SWA URL
- **GitHub Environment**: Defines approval gate and reviewer list for production deployments

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Developer can push frontend changes and see them deployed to the preview environment within 10 minutes without manual intervention
- **SC-002**: Pull requests automatically generate preview URLs posted in PR comments within 5 minutes of PR creation
- **SC-003**: Production deployments require manual approval, then deploy and pass production validation within 5 minutes after approval is granted
- **SC-004**: Zero manual `swa deploy` commands required for any deployment (complete automation)
- **SC-005**: Infrastructure provisioning via Terraform completes in under 3 minutes and is idempotent
- **SC-006**: Frontend test results are visible in workflow logs, and failed required tests prevent deployment 100% of the time in Phase 1 and later
- **SC-007**: PR preview validation detects critical failures (HTTP 404/500) before merge, and production validation detects them after Main deployment, 100% of the time
- **SC-008**: All in-scope frontend environment configurations (local development, preview, production) correctly route to backend API endpoints
- **SC-009**: Preview environments for PRs automatically clean up within 1 hour of PR closure
- **SC-010**: Workflow execution logs provide clear error messages enabling developers to resolve failures within 15 minutes

---

## Assumptions

1. **GitHub Actions runner availability**: Free tier provides sufficient minutes; no need for self-hosted runners
2. **Azure Static Web Apps SKU**: Free tier provides sufficient quota for dev environment (1 production + 3 preview environments)
3. **Deployment token validity**: Token does not expire automatically; manual rotation only if compromised
4. **Backend API stability**: `https://innoventity-dev-api.azurewebsites.net` remains available and unchanged during frontend deployment
5. **Angular build process**: Existing `npm run build` command produces production-ready artifacts in `dist/` folder
6. **Test stability**: Frontend tests (28 unit, 3 E2E) may require stabilization during Phase 1, but required test failures still block deployment until fixed
7. **Git branch strategy**: Main branch is protected with required PR reviews; direct pushes disabled
8. **Terraform state**: Existing remote state backend (`azurerm`) configured and accessible

---

## Out of Scope

### Explicitly Deferred

- ❌ **Multiple persistent release environments** (dev/staging/prod): Only the current dev subscription plus SWA preview environments are in scope for Phase 1; additional persistent environments defer to Phase 2+
- ❌ **Blue-green or slot-swap deployment strategy**: Phase 1 uses direct production deployment after approval; advanced release patterns are deferred
- ❌ **Automated rollback on production errors**: Manual rollback via Azure Portal or Terraform; automation deferred
- ❌ **Performance testing in CI/CD**: Lighthouse scores validated manually; CI integration in Phase 2+
- ❌ **Slack/Teams notifications**: GitHub Actions native notifications only in Phase 1; Slack/Teams integrations deferred to Phase 2+
- ❌ **Canary deployments**: Not supported by Azure SWA Free tier; requires Standard tier + custom logic
- ❌ **Additional non-required deployment gates** (for example performance or visual regression suites): Deferred to Phase 2+

### Permanently Out of Scope

- ❌ **Backend API deployment**: Already automated in `deploy.yml` workflow from Phase 0
- ❌ **Infrastructure for backend resources**: Already provisioned via Terraform in Phase 0
- ❌ **Mobile app deployment**: No mobile apps in scope
- ❌ **Desktop app deployment**: No desktop apps in scope

---

## Dependencies

### Upstream Dependencies (Must Exist Before Implementation)

1. **Angular 19 Application**: `src/Innoventity.Client/` folder with functional app (✅ Complete - Phase 0)
2. **Backend API**: `https://innoventity-dev-api.azurewebsites.net` deployed and accessible (✅ Complete - Phase 0)
3. **Azure Subscription**: Active subscription with permissions to create Static Web Apps (✅ Available)
4. **Terraform Backend**: Remote state configured at `innoventity-dev-tfstate` storage account (✅ Complete - Phase 0)
5. **GitHub Repository**: `khaledm/innoventity-prototype-redux` with Actions enabled (✅ Available)

### Downstream Dependencies (Unblocked After Implementation)

1. **Phase 1 Domain Richness**: Can proceed with frontend UI updates knowing CI/CD pipeline exists
2. **Phase 2+ Features**: Any feature requiring frontend changes benefits from automated deployment
3. **User Acceptance Testing**: Stakeholders can test preview environments without dev team involvement

---

## Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **Azure SWA deployment quota exhausted** | Workflow fails; no new deployments | Medium | Monitor quota usage; delete unused preview environments; upgrade to Standard tier if needed |
| **GitHub Actions minutes exhausted** | CI/CD pipeline stops; manual deployment required | Low | Monitor usage; optimize workflow (cache npm dependencies); consider self-hosted runner |
| **Deployment token leaked** | Unauthorized deployments possible | Low | Store as encrypted secret; rotate token immediately if repo compromised; limit token scope to single SWA |
| **Frontend tests unstable** | Deployments blocked until required tests are fixed | High | Stabilize failing tests early in Phase 1; keep required tests as blocking gates in every deployment path |
| **Terraform state lock conflict** | Concurrent applies fail | Low | Use remote state locking (already configured); coordinate manual Terraform runs |
| **SWA global CDN delay** | Deployed changes not visible immediately | Medium | Document 2-5 minute CDN propagation time; add retry logic to validation job |

---

## Open Questions

*All open questions resolved. See Clarifications section for decisions.*

---

## Acceptance Validation

This feature is **COMPLETE** when:

1. ✅ GitHub Actions workflow exists at `.github/workflows/deploy-frontend.yml`
2. ✅ Workflow triggers on push to any branch with `src/Innoventity.Client/**` changes
3. ✅ Terraform module exists at `infrastructure/modules/static-web-app/`
4. ✅ Azure Static Web Apps resource provisioned via `terraform apply` in dev/core
5. ✅ Deployment token stored as `AZURE_STATIC_WEB_APPS_API_TOKEN` GitHub secret
6. ✅ Push to feature branch deploys to a preview environment and passes validation
7. ✅ Pull request to Main creates preview environment with URL posted in PR comment and passing preview validation before merge
8. ✅ Merge to Main runs build and required tests, pauses at approval gate, and after manual approval deploys directly to production and passes production validation
9. ✅ Production URL (`https://innoventity-dev-web.azurestaticapps.net`) serves latest frontend version
10. ✅ Zero manual `swa deploy` commands required for any deployment scenario
11. ~~Documentation updated: `infrastructure/README.md`, `specs/001-platform-core/quickstart.md`, `specs/001-platform-core/frontend-architecture.md`~~ — **DESCOPED 2026-05-10**: Dedicated runbook (T065), infrastructure README (T066), and status badge (T067) deferred to next iteration; pipeline automation is complete without them

---

## References

- **Phase 0 MVP Limitation**: [specs/001-platform-core/spec.md](../001-platform-core/spec.md) - Frontend CI/CD deferred
- **Next Actions Plan**: [NEXT_ACTIONS.md](../../NEXT_ACTIONS.md) - P1-Priority-1 detailed requirements
- **Existing Frontend Architecture**: [specs/001-platform-core/frontend-architecture.md](../001-platform-core/frontend-architecture.md)
- **Azure Static Web Apps Docs**: <https://learn.microsoft.com/en-us/azure/static-web-apps/>
- **GitHub Actions - Azure SWA Deploy**: <https://github.com/Azure/static-web-apps-deploy>
- **Terraform azurerm_static_web_app**: <https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/static_web_app>

