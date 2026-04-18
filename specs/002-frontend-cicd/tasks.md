# Tasks: Frontend CI/CD Automation

**Feature**: Frontend CI/CD Automation
**Branch**: `002-frontend-cicd`
**Generated**: April 18, 2026
**Input**: Design documents from `/specs/002-frontend-cicd/` (plan.md, spec.md, data-model.md, research.md, contracts/, quickstart.md)

**Tests**: No test tasks included (not explicitly requested in specification)

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

---

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4, US5)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Project Preparation)

**Purpose**: Verify prerequisites and prepare workspace for implementation

- [ ] T001 Verify Azure subscription access and resource group `innoventity-dev-rg` exists
- [ ] T002 Verify Terraform remote backend configured at `innoventitydevtfstate` storage account
- [ ] T003 [P] Verify Node.js 20.x LTS installed and Angular 19 app builds successfully
- [ ] T004 [P] Verify GitHub repository permissions (write access to `khaledm/innoventity-prototype-redux`)
- [ ] T005 Create feature documentation directory structure `specs/002-frontend-cicd/runbooks/`

---

## Phase 2: Foundational Infrastructure (Terraform Module - BLOCKING)

**Purpose**: Create reusable Terraform module for Azure Static Web Apps provisioning

**⚠️ CRITICAL**: This phase MUST complete before any GitHub Actions workflow implementation

- [ ] T006 Create Terraform module directory `infrastructure/modules/static-web-app/`
- [ ] T007 [P] Create `infrastructure/modules/static-web-app/main.tf` with `azurerm_static_web_app` resource definition
- [ ] T008 [P] Create `infrastructure/modules/static-web-app/variables.tf` with input variables (name, location, resource_group_name, sku_tier, tags)
- [ ] T009 [P] Create `infrastructure/modules/static-web-app/outputs.tf` with outputs (default_host_name, api_key, id)
- [ ] T010 Update `infrastructure/environments/dev/core/main.tf` to integrate static-web-app module
- [ ] T011 Update `infrastructure/environments/dev/core/outputs.tf` to expose SWA outputs
- [ ] T012 Run `terraform init` in `infrastructure/environments/dev/core/` to initialize module
- [ ] T013 Run `terraform plan` to validate module configuration (should show 1 resource to add)

**Checkpoint**: Terraform module created and validated - ready for provisioning

---

## Phase 3: User Story 4 - Infrastructure Provisioning (Priority: P2) 🎯 FOUNDATIONAL

**Goal**: Provision Azure Static Web Apps resource via Terraform for reproducible infrastructure

**Independent Test**: Run `terraform apply`, verify SWA resource created in Azure Portal, extract deployment token

- [ ] T014 [US4] Run `terraform apply` in `infrastructure/environments/dev/core/` to provision Azure Static Web Apps resource
- [ ] T015 [US4] Verify SWA resource created in Azure Portal (`innoventity-dev-web.azurestaticapps.net`)
- [ ] T016 [US4] Extract deployment token: `terraform output -raw static_web_app_api_key` and copy to clipboard
- [ ] T017 [US4] Create GitHub repository secret `AZURE_STATIC_WEB_APPS_API_TOKEN` via `gh secret set` command
- [ ] T018 [US4] Verify secret created: `gh secret list` should show `AZURE_STATIC_WEB_APPS_API_TOKEN`
- [ ] T019 [US4] Run `terraform plan` again to verify idempotency (no changes detected)

**Checkpoint**: Infrastructure provisioned, deployment token configured - ready for workflow implementation

---

## Phase 4: User Story 1 - Automated Deployment on Push (Priority: P1) 🎯 MVP CORE

**Goal**: Developers can push frontend changes and see them deployed automatically without manual `swa deploy` commands

**Independent Test**: Push commit to feature branch, observe workflow execution in GitHub Actions, verify deployed changes at staging URL

- [ ] T020 [US1] Create GitHub Actions workflow file `.github/workflows/deploy-frontend.yml`
- [ ] T021 [P] [US1] Configure workflow triggers (push to all branches with path filter `src/Innoventity.Client/**`, workflow_dispatch)
- [ ] T022 [P] [US1] Define `build` job with Node.js 20.x setup and npm caching (`actions/setup-node@v4` with `cache: 'npm'`)
- [ ] T023 [US1] Implement build job steps: `npm ci`, `npm run build`, upload `dist/` as artifact
- [ ] T075 [P] [US1] Verify `src/Innoventity.Client/staticwebapp.config.json` satisfies FR-030 (SPA routing to `index.html`) and FR-031 (navigation fallback); confirm file is included in `ng build` output and deployed artifact
- [ ] T024 [P] [US1] Define `test` job with dependencies on build job
- [ ] T025 [US1] Implement test job steps: download build artifact, `npm test -- --ci --coverage`, `npx playwright install --with-deps`, `npm run test:e2e`
- [ ] T026 [US1] Configure test job to allow failures in Phase 1 (`continue-on-error: true`)
- [ ] T027 [US1] Upload test results as artifacts (coverage reports, Playwright traces)
- [ ] T028 [P] [US1] Define `deploy-preview` job with conditional execution (`if: github.ref != 'refs/heads/Main'`)
- [ ] T029 [US1] Implement deploy-preview job: download build artifact, use `Azure/static-web-apps-deploy@v1` with `skip_app_build: true`
- [ ] T030 [US1] Configure deploy-preview to use `AZURE_STATIC_WEB_APPS_API_TOKEN` secret and `production_branch: 'Main'`
- [ ] T031 [US1] Output preview URL from deploy-preview job
- [ ] T032 [US1] Test workflow locally using `act` tool: `act push -W .github/workflows/deploy-frontend.yml`
- [ ] T033 [US1] Push test commit to feature branch and verify workflow executes successfully
- [ ] T034 [US1] Verify frontend changes deployed to staging environment and accessible

**Checkpoint**: Feature branch deployments fully automated - User Story 1 complete

---

## Phase 5: User Story 2 - PR Preview Environments (Priority: P2)

**Goal**: Code reviewers can test frontend changes interactively via automatic preview deployments for pull requests

**Independent Test**: Create PR from feature branch to Main, verify SWA creates preview environment, access preview URL from PR comments

- [ ] T035 [US2] Verify `Azure/static-web-apps-deploy@v1` action configured with `repo_token: ${{ secrets.GITHUB_TOKEN }}`
- [ ] T036 [US2] Test PR preview: create PR from feature branch to Main
- [ ] T037 [US2] Verify Azure Static Web Apps automatically creates preview environment
- [ ] T038 [US2] Verify GitHub Actions bot posts preview URL as PR comment
- [ ] T039 [US2] Test preview URL accessible and shows PR changes
- [ ] T040 [US2] Add cleanup workflow job to handle PR closure
- [ ] T041 [US2] Create workflow trigger for `pull_request` event type `closed`
- [ ] T042 [US2] Implement cleanup job: run `Azure/static-web-apps-deploy@v1` with `action: 'close'`
- [ ] T043 [US2] Test cleanup: close PR and verify preview environment deleted within 1 hour

**Checkpoint**: PR preview environments fully automated - User Story 2 complete

---

## Phase 6: User Story 3 - Production Deployment with Approval Gate (Priority: P2)

**Goal**: Platform operators can control production deployment timing with manual approval before changes reach end users

**Independent Test**: Merge to Main branch, observe workflow pause at approval gate, manually approve, verify production deployment completes

- [ ] T044 [US3] Create GitHub Environment via GitHub CLI: `gh api repos/khaledm/innoventity-prototype-redux/environments/production -X PUT`
- [ ] T045 [US3] Configure production environment protection rules via GitHub UI (Settings → Environments → production)
- [ ] T046 [US3] Add required reviewers to production environment (GitHub username)
- [ ] T047 [US3] Set deployment branches to `Main` only
- [ ] T048 [US3] Configure approval timeout to 1440 minutes (24 hours)
- [ ] T049 [P] [US3] Define `deploy-production` job in workflow with conditional execution (`if: github.ref == 'refs/heads/Main'`)
- [ ] T050 [US3] Add `environment: production` to deploy-production job configuration
- [ ] T051 [US3] Implement deploy-production job: download build artifact, use `Azure/static-web-apps-deploy@v1` targeting production
- [ ] T052 [US3] Test approval workflow: merge feature branch to Main
- [ ] T053 [US3] Verify workflow pauses at production approval gate
- [ ] T054 [US3] Approve deployment via GitHub UI and verify production deployment completes
- [ ] T055 [US3] Verify production URL `https://innoventity-dev-web.azurestaticapps.net` serves new frontend version
- [ ] T056 [US3] Test approval rejection: trigger workflow, reject approval, verify workflow fails without deploying

**Checkpoint**: Production approval gate functional - User Story 3 complete

---

## Phase 7: User Story 5 - Validation and Health Checks (Priority: P3)

**Goal**: Automated health checks detect critical failures before production deployment to prevent broken deployments

**Independent Test**: Deploy frontend with broken configuration, observe validation job fails, verify deployment does not proceed

- [ ] T057 [P] [US5] Define `validate` job in workflow with dependencies on deploy jobs
- [ ] T058 [US5] Implement validation job: HTTP GET request to deployment URL using `curl` or PowerShell
- [ ] T059 [US5] Add validation checks: HTTP status must be 200, Content-Type must be `text/html`
- [ ] T060 [US5] Add retry logic (3 attempts with 10-second delay) to handle CDN propagation
- [ ] T061 [US5] Configure validation job to fail workflow on check failure
- [ ] T062 [US5] Test validation failure: deploy intentionally broken build (missing `index.html`)
- [ ] T063 [US5] Verify validation job fails and prevents production approval
- [ ] T064 [US5] Test validation success: deploy working build and verify validation passes

**Checkpoint**: Health checks validate deployments - User Story 5 complete

---

## Phase 8: Polish & Documentation

**Purpose**: Complete documentation, optimize workflow, finalize implementation

- [ ] T065 [P] Create deployment runbook `specs/002-frontend-cicd/runbooks/deployment.md` with manual procedures and troubleshooting
- [ ] T066 [P] Update `infrastructure/README.md` with SWA module documentation
- [ ] T067 [P] Update repository README with CI/CD pipeline status badge
- [ ] T068 Remove `continue-on-error: true` from test job (Phase 2+ enforcement per FR-009)
- [ ] T069 Add npm dependency caching optimization to workflow
- [ ] T070 [P] Validate quickstart.md steps end-to-end with fresh clone
- [ ] T071 Run constitution checklist validation (Principle 5: observe workflow failures)
- [ ] T072 Verify all success criteria met (SC-001 through SC-010 from spec.md)
- [ ] T073 [P] Update `.github/agents/copilot-instructions.md` with CI/CD workflow patterns
- [ ] T074 Final commit following Conventional Commits format: `feat(ci): complete frontend CI/CD automation`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 4 (Phase 3)**: Depends on Foundational (Terraform module must exist) - BLOCKS workflow implementation
- **User Story 1 (Phase 4)**: Depends on US4 (infrastructure must be provisioned) - Core MVP
- **User Story 2 (Phase 5)**: Depends on US1 (workflow must exist) - Builds on core workflow
- **User Story 3 (Phase 6)**: Depends on US1 (workflow must exist) - Adds approval to existing workflow
- **User Story 5 (Phase 7)**: Depends on US1 (workflow must exist) - Adds validation to existing workflow
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

``` text
Phase 1 (Setup)
  ↓
Phase 2 (Foundational - Terraform Module)
  ↓
Phase 3 (US4 - Infrastructure Provisioning) ← Must complete first
  ↓
Phase 4 (US1 - Automated Deployment) ← Core workflow
  ↓
  ├→ Phase 5 (US2 - PR Previews) ← Can parallelize with US3 and US5
  ├→ Phase 6 (US3 - Production Approval) ← Can parallelize with US2 and US5
  └→ Phase 7 (US5 - Health Checks) ← Can parallelize with US2 and US3
  ↓
Phase 8 (Polish)
```

### Within Each User Story

- Setup and foundational tasks must complete sequentially
- US4: Terraform tasks must be sequential (module creation → plan → apply → secret config)
- US1: Build job → test job → deploy job (sequential due to artifact dependencies)
- US2: Can add to US1 workflow in parallel with US3 and US5
- US3: Requires GitHub environment setup before workflow changes
- US5: Validation job can be added in parallel with US2 and US3 changes

### Parallel Opportunities

- **Phase 1**: Tasks T003 and T004 can run in parallel
- **Phase 2**: Tasks T007, T008, T009 (Terraform module files) can be created in parallel
- **Phase 4 (US1)**: Tasks T021, T022 (workflow configuration sections) can be written in parallel, T024, T025 (test job definition) can be parallel with T028 (deploy job definition)
- **After US1 completes**: US2, US3, and US5 can be implemented in parallel by different developers (different sections of same workflow file may require coordination)
- **Phase 8**: Tasks T065, T066, T067, T070, T073 can run in parallel (different files)

---

## Parallel Example: After US1 Complete

Once User Story 1 is complete, multiple developers can work in parallel:

```bash
# Developer A: Add PR preview cleanup
Task T040-T043: PR preview environment cleanup workflow

# Developer B: Add production approval gate
Task T044-T056: GitHub Environment setup and production deployment

# Developer C: Add health check validation
Task T057-T064: Validation job with HTTP health checks
```

**Coordination Note**: All three developers are modifying `.github/workflows/deploy-frontend.yml`, so coordination is needed to avoid merge conflicts. Alternative: implement sequentially in priority order (US3 → US5 → US2 if single developer).

---

## Implementation Strategy

### MVP First (Recommended)

**Fastest path to working CI/CD**:

1. **Phase 1**: Setup (verify prerequisites)
2. **Phase 2**: Foundational (create Terraform module)
3. **Phase 3**: User Story 4 (provision infrastructure)
4. **Phase 4**: User Story 1 (core workflow)
5. **STOP and VALIDATE**: Test automated deployments end-to-end
6. **Deploy/Demo**: Show working CI/CD pipeline

**Result**: Developer can push code and see it deployed automatically (FR-001 through FR-006 satisfied)

### Incremental Delivery (Full Feature)

**Add capabilities incrementally**:

1. Complete MVP (Phases 1-4) → **Automated deployments working**
2. Add User Story 2 (Phase 5) → **PR previews working**
3. Add User Story 3 (Phase 6) → **Production approval working**
4. Add User Story 5 (Phase 7) → **Health checks working**
5. Polish (Phase 8) → **Documentation complete**

**Advantage**: Each phase adds value without breaking previous functionality

### Parallel Team Strategy

**With 3+ developers**:

1. **Together**: Complete Phases 1-4 (Setup → Foundation → Infrastructure → Core Workflow)
2. **Split**: Once Phase 4 complete:
   - Developer A: User Story 2 (PR previews)
   - Developer B: User Story 3 (Production approval)
   - Developer C: User Story 5 (Health checks)
3. **Merge**: Integrate all three enhancements
4. **Together**: Phase 8 (Polish)

**Coordination**: Developers must coordinate on `.github/workflows/deploy-frontend.yml` file to avoid conflicts

---

## Success Criteria Validation

Implementation is **COMPLETE** when all criteria from spec.md are met:

- [ ] **SC-001**: Developer can push frontend changes and see them deployed to staging within 10 minutes (test with commit to feature branch)
- [ ] **SC-002**: Pull requests automatically generate preview URLs posted in PR comments within 5 minutes (test by creating PR)
- [ ] **SC-003**: Production deployments require manual approval and complete within 5 minutes after approval (test by merging to Main)
- [ ] **SC-004**: Zero manual `swa deploy` commands required for any deployment scenario
- [ ] **SC-005**: Infrastructure provisioning via Terraform completes in under 3 minutes and is idempotent (run `terraform apply` twice)
- [ ] **SC-006**: Frontend test results visible in workflow logs (Phase 2+: test failures prevent deployment)
- [ ] **SC-007**: Staging validation health checks detect critical failures (test with broken deployment)
- [ ] **SC-008**: All frontend environment configurations correctly route to backend API endpoints
- [ ] **SC-009**: Preview environments for PRs automatically clean up within 1 hour of PR closure
- [ ] **SC-010**: Workflow execution logs provide clear error messages enabling developers to resolve failures within 15 minutes

---

## Constitutional Compliance

**Principle 5 (Tests Must Prove They Work)**: Validation checklist

- [ ] Workflow tested locally with `act` tool before pushing (observed syntax errors during development)
- [ ] Terraform `plan` observed before `apply` (infrastructure changes previewed)
- [ ] Intentionally broken deployment tested to verify validation catches failures
- [ ] Test job observed failing before implementation (red-green-refactor workflow)
- [ ] Approval gate tested with rejection scenario (workflow fails without deploying)

**Principle 8 (Commit Messages Are Documentation)**: All commits follow Conventional Commits format

- Example: `feat(ci): add GitHub Actions workflow for frontend deployment`
- Example: `feat(infrastructure): create Terraform module for Azure Static Web Apps`
- Example: `feat(ci): add production approval gate to deployment workflow`

---

## Notes

- **[P] tasks**: Different files, no dependencies, can run in parallel
- **[Story] labels**: Map task to specific user story for traceability
- **Independent testing**: Each user story should be testable independently after its phase completes
- **Phase 1 test warnings**: `continue-on-error: true` allows deployment despite test failures (remove in Phase 2+)
- **Commit frequency**: Commit after each logical task or group of related tasks
- **Checkpoints**: Stop at any checkpoint to validate story independently before proceeding
- **File conflicts**: Coordinate when multiple developers work on same workflow file
- **Validation**: Run quickstart.md steps end-to-end before considering feature complete

---

**Total Tasks**: 74
**Estimated Duration**: 8-12 hours (solo developer, sequential implementation)
**MVP Tasks** (Phase 1-4): 34 tasks (~4-6 hours)
**Branch**: `002-frontend-cicd`
**Next Step**: Begin with Phase 1 (Setup) task T001
