# Feature Specification: Frontend CI/CD Automation

**Feature Branch**: `002-frontend-cicd`
**Created**: April 12, 2026
**Status**: Draft
**Input**: Automated CI/CD pipeline for Angular 19 frontend deployment to Azure Static Web Apps. Must include: GitHub Actions workflow with build/test/deploy/validate/swap jobs, Terraform module for SWA provisioning, integration with existing proxy configuration, PR preview environments, manual production approval gate for Main branch.

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
- **Q**: Should failing frontend tests block deployment in Phase 1? → **A**: Allow deployment with warnings for Phase 1, stabilize tests during Phase 1, require passing tests starting Phase 2 (Option B).
- **Q**: What should the production approval timeout be? → **A**: 24 hours (Option C - maximum flexibility for small team).
- **Q**: What notification channel for deployment failures? → **A**: GitHub Actions native notifications only (email, UI, mobile) - defer Slack/Teams to Phase 2+ (Option A).

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Automated Deployment on Push (Priority: P1)

As a **developer**, I need frontend changes automatically built, tested, and deployed when I push to the feature branch so that I can iterate quickly without manual deployment steps.

**Why this priority**: CRITICAL - Core functionality that eliminates the Phase 0 deferred item. Without this, all other stories have no foundation.

**Independent Test**: Can be fully tested by pushing a commit to feature branch, observing GitHub Actions workflow execution, and verifying deployed changes at the SWA staging URL.

**Acceptance Scenarios**:

1. **Given** a feature branch with frontend changes, **When** developer pushes commit, **Then** GitHub Actions workflow triggers automatically
2. **Given** workflow triggered, **When** build job executes, **Then** Angular app builds successfully with production configuration
3. **Given** build succeeds, **When** test job executes, **Then** all Jest unit tests pass (target: 28/28)
4. **Given** tests pass, **When** deploy job executes, **Then** artifact uploaded to Azure Static Web Apps staging slot
5. **Given** deployment complete, **When** validate job executes, **Then** staging URL returns HTTP 200 and serves Angular app

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

**Independent Test**: Can be fully tested by merging to Main branch, observing workflow pause at approval gate, manually approving, and verifying production swap completes.

**Acceptance Scenarios**:

1. **Given** changes merged to Main branch, **When** deploy workflow runs, **Then** workflow pauses at production approval gate
2. **Given** workflow awaiting approval, **When** authorized operator reviews staging deployment, **Then** operator can approve or reject via GitHub UI
3. **Given** approval granted, **When** production deploy job executes, **Then** artifacts deployed directly to production environment
4. **Given** deployment completes, **When** production URL accessed, **Then** new frontend version served at `https://innoventity-dev-web.azurestaticapps.net`
5. **Given** approval rejected or timed out (24 hours), **When** workflow concludes, **Then** workflow fails, production unchanged

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

As a **quality assurance engineer**, I need automated health checks after deployment so that critical failures are detected before production swap.

**Why this priority**: LOW - Nice-to-have quality gate. Can initially proceed with manual validation if time-constrained.

**Independent Test**: Can be fully tested by deploying frontend with broken configuration, observing validation job fails, and verifying deployment does not proceed to production.

**Acceptance Scenarios**:

1. **Given** deployment to staging complete, **When** validation job executes, **Then** HTTP GET request sent to staging URL
2. **Given** staging URL responds, **When** response validated, **Then** status code must be 200 and Content-Type must be `text/html`
3. **Given** health check passes, **When** validation job completes, **Then** workflow proceeds to approval gate (for Main) or succeeds (for feature branches)
4. **Given** staging URL returns 404 or 500, **When** validation job evaluates response, **Then** workflow fails and production swap prevented
5. **Given** validation failure, **When** developer notified, **Then** GitHub Actions sends notification with failure details

---

### Edge Cases

- **What happens when Azure Static Web Apps deployment quota exhausted?** Workflow fails with clear error message indicating quota limit reached. Operator must manually delete unused preview environments or upgrade SKU.
- **What happens when GitHub Actions workflow fails mid-deployment?** Preview environment may have partial deployment. Workflow retry will overwrite with fresh deployment. Production environment remains unchanged.
- **What happens when Playwright E2E tests fail during test job?** Workflow fails and deployment does not proceed. Developer must fix tests and push new commit to retry.
- **What happens when two developers push to Main simultaneously?** GitHub Actions queues workflows sequentially. Second workflow waits for first to complete production swap.
- **What happens when deployment token expires or is rotated?** Workflow fails with authentication error. Operator must update `AZURE_STATIC_WEB_APPS_API_TOKEN` secret with new token from Azure Portal.
- **What happens when approval gate times out (24 hours)?** Workflow automatically fails before production deployment. Production environment unchanged. Operator must re-run workflow to retry.

---

## Requirements *(mandatory)*

### Functional Requirements

#### GitHub Actions Workflow

- **FR-001**: Workflow MUST trigger on push to any branch containing frontend changes (`src/Innoventity.Client/**`)
- **FR-002**: Workflow MUST trigger on manual dispatch (`workflow_dispatch`) with configurable environment parameter
- **FR-003**: Workflow MUST include four sequential jobs: `build`, `test`, `deploy-preview`, `deploy-production` (production conditional on Main branch + approval)
- **FR-004**: `build` job MUST execute `npm ci` to install dependencies using lockfile
- **FR-005**: `build` job MUST execute `npm run build` with production configuration
- **FR-006**: `build` job MUST upload `dist/` folder as GitHub Actions artifact
- **FR-007**: `test` job MUST execute Jest unit tests (`npm test -- --coverage`)
- **FR-008**: `test` job MUST execute Playwright E2E tests (`npm run test:e2e`)
- **FR-009**: `test` job MUST report test results; Phase 1: allow failures with warnings, Phase 2+: fail workflow on test failures (target: 28/28 unit, 3/3 E2E)
- **FR-010**: `deploy-preview` job MUST download build artifact from `build` job
- **FR-011**: `deploy-preview` job MUST authenticate to Azure Static Web Apps using `AZURE_STATIC_WEB_APPS_API_TOKEN` secret
- **FR-012**: `deploy-preview` job MUST deploy to preview environment for feature branches OR skip for Main branch
- **FR-013**: `deploy-preview` job MUST output preview URL as job output (for feature branches)
- **FR-014**: `deploy-production` job MUST only execute when branch is `Main` (conditional execution)
- **FR-015**: `deploy-production` job MUST require manual approval via GitHub Environment protection rule BEFORE deployment
- **FR-016**: `deploy-production` job MUST download build artifact from `build` job
- **FR-017**: `deploy-production` job MUST deploy directly to production environment (NOT preview)
- **FR-018**: `deploy-production` job MUST validate production URL returns HTTP 200 and serves new version
- **FR-019**: Workflow MUST send deployment status notification on success or failure

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
- **FR-033**: Development/staging configuration MUST use proxy configuration for local testing (existing `proxy.conf.json`)

#### PR Preview Environments

- **FR-034**: Azure Static Web Apps MUST automatically create preview environment for each PR to Main branch
- **FR-035**: Preview environment MUST have unique URL format: `https://<swa-name>-<pr-number>.azurestaticapps.net`
- **FR-036**: GitHub Actions bot MUST post preview URL as comment on PR
- **FR-037**: Preview environment MUST automatically delete when PR is closed or merged
- **FR-038**: Preview environments MUST not count against production deployment quota

#### Security & Access Control

- **FR-039**: Deployment token (`AZURE_STATIC_WEB_APPS_API_TOKEN`) MUST be stored as GitHub repository secret (NOT hardcoded)
- **FR-040**: Production approval gate MUST restrict approvers to repository owners or designated environments reviewers
- **FR-041**: Approval gate MUST have 24-hour timeout (workflow auto-fails if no approval)
- **FR-042**: Terraform state for SWA module MUST use same remote backend as existing infrastructure (`azurerm` backend)

### Non-Functional Requirements

- **NFR-001**: Workflow execution time MUST complete within 15 minutes for feature branches (build+test+deploy+validate)
- **NFR-002**: Production swap MUST complete within 5 minutes after approval granted
- **NFR-003**: Deployment must achieve 99.9% success rate (excluding user errors like test failures)
- **NFR-004**: Workflow logs MUST be retained for 90 days minimum
- **NFR-005**: Failed deployments MUST send notification via GitHub Actions native notifications (email, UI, mobile app)

### Key Entities

- **GitHub Actions Workflow** (`deploy-frontend.yml`): Orchestrates build, test, deploy-preview, deploy-production pipeline
- **Azure Static Web Apps Resource**: Hosts Angular SPA with global CDN distribution
- **Deployment Token**: Authenticates GitHub Actions to Azure SWA
- **Preview Environment**: Temporary deployment for feature branches and PR reviews (auto-created by SWA)
- **Production Environment**: Live user-facing deployment at primary SWA URL
- **GitHub Environment**: Defines approval gate and reviewer list for production deployments

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Developer can push frontend changes and see them deployed to staging within 10 minutes without manual intervention
- **SC-002**: Pull requests automatically generate preview URLs posted in PR comments within 5 minutes of PR creation
- **SC-003**: Production deployments require manual approval and complete within 5 minutes after approval granted
- **SC-004**: Zero manual `swa deploy` commands required for any deployment (complete automation)
- **SC-005**: Infrastructure provisioning via Terraform completes in under 3 minutes and is idempotent
- **SC-006**: Frontend test results visible in workflow logs; Phase 2+: test failures prevent deployment 100% of the time
- **SC-007**: Staging validation health checks detect critical failures (HTTP 404/500) and prevent production swap 100% of the time
- **SC-008**: All frontend environment configurations (dev, staging, production) correctly route to backend API endpoints
- **SC-009**: Preview environments for PRs automatically clean up within 1 hour of PR closure
- **SC-010**: Workflow execution logs provide clear error messages enabling developers to resolve failures within 15 minutes

---

## Assumptions

1. **GitHub Actions runner availability**: Free tier provides sufficient minutes; no need for self-hosted runners
2. **Azure Static Web Apps SKU**: Free tier provides sufficient quota for dev environment (1 production + 3 preview environments)
3. **Deployment token validity**: Token does not expire automatically; manual rotation only if compromised
4. **Backend API stability**: `https://innoventity-dev-api.azurewebsites.net` remains available and unchanged during frontend deployment
5. **Angular build process**: Existing `npm run build` command produces production-ready artifacts in `dist/` folder
6. **Test stability**: Frontend tests (28 unit, 3 E2E) will be stabilized during Phase 1; test failures allowed with warnings initially, enforcement deferred to Phase 2
7. **Git branch strategy**: Main branch is protected with required PR reviews; direct pushes disabled
8. **Terraform state**: Existing remote state backend (`azurerm`) configured and accessible

---

## Out of Scope

### Explicitly Deferred

- ❌ **Multi-environment deployments** (dev, staging, prod): Only dev environment in Phase 1; multi-env in Phase 2+
- ❌ **Blue-green deployment strategy**: SWA staging/production slot swap is sufficient for Phase 1
- ❌ **Automated rollback on production errors**: Manual rollback via Azure Portal or Terraform; automation deferred
- ❌ **Performance testing in CI/CD**: Lighthouse scores validated manually; CI integration in Phase 2+
- ❌ **Slack/Teams notifications**: GitHub Actions native notifications only in Phase 1; Slack/Teams integrations deferred to Phase 2+
- ❌ **Canary deployments**: Not supported by Azure SWA Free tier; requires Standard tier + custom logic
- ❌ **Test enforcement in Phase 1**: Tests run and report results but don't block deployment; enforcement begins in Phase 2

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
| **Frontend tests unstable** | Test results not reliable for quality gates | High | Phase 1: allow failures with warnings; stabilize tests during Phase 1; Phase 2: enforce passing tests |
| **Terraform state lock conflict** | Concurrent applies fail | Low | Use remote state locking (already configured); coordinate manual Terraform runs |
| **SWA global CDN delay** | Deployed changes not visible immediately | Medium | Document 2-5 minute CDN propagation time; add retry logic to validation job |

---

## Open Questions

*All open questions resolved during clarification session (2026-04-12)*

4. **Preview environment retention**: Should preview environments persist after PR merge for post-merge review, or delete immediately?

---

## Acceptance Validation

This feature is **COMPLETE** when:

1. ✅ GitHub Actions workflow exists at `.github/workflows/deploy-frontend.yml`
2. ✅ Workflow triggers on push to any branch with `src/Innoventity.Client/**` changes
3. ✅ Terraform module exists at `infrastructure/modules/static-web-app/`
4. ✅ Azure Static Web Apps resource provisioned via `terraform apply` in dev/core
5. ✅ Deployment token stored as `AZURE_STATIC_WEB_APPS_API_TOKEN` GitHub secret
6. ✅ Push to feature branch deploys to staging and passes validation
7. ✅ Pull request to Main creates preview environment with URL posted in PR comment
8. ✅ Merge to Main triggers approval gate; manual approval swaps to production
9. ✅ Production URL (`https://innoventity-dev-web.azurestaticapps.net`) serves latest frontend version
10. ✅ Zero manual `swa deploy` commands required for any deployment scenario
11. ✅ Documentation updated: `infrastructure/README.md`, `specs/001-platform-core/quickstart.md`, `specs/001-platform-core/frontend-architecture.md`

---

## References

- **Phase 0 MVP Limitation**: [specs/001-platform-core/spec.md](../001-platform-core/spec.md) - Frontend CI/CD deferred
- **Next Actions Plan**: [NEXT_ACTIONS.md](../../NEXT_ACTIONS.md) - P1-Priority-1 detailed requirements
- **Existing Frontend Architecture**: [specs/001-platform-core/frontend-architecture.md](../001-platform-core/frontend-architecture.md)
- **Azure Static Web Apps Docs**: <https://learn.microsoft.com/en-us/azure/static-web-apps/>
- **GitHub Actions - Azure SWA Deploy**: <https://github.com/Azure/static-web-apps-deploy>
- **Terraform azurerm_static_web_app**: <https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/static_web_app>

