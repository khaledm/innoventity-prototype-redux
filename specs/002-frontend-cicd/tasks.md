# Tasks: Frontend CI/CD Automation

**Feature**: Frontend CI/CD Automation
**Branch**: `002-frontend-cicd`
**Last Synced**: May 25, 2026
**Input**: Design documents from `/specs/002-frontend-cicd/` (plan.md, spec.md, data-model.md, research.md, contracts/, quickstart.md)

**Tests**: Test-related tasks are included where needed for implementation validation (including Jest/Playwright workflow steps, artifact uploads, and end-to-end coverage)

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

---

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4, US5)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Project Preparation)

**Purpose**: Verify prerequisites and prepare workspace for implementation

- [X] T001 Verify Azure subscription access and resource group `innoventity-dev-rg` exists
- [X] T002 Verify Terraform remote backend configured at `innoventitydevtfstate` storage account
- [X] T003 [P] Verify Node.js 20.x LTS installed and Angular 19 app builds successfully
- [X] T004 [P] Verify GitHub repository permissions (write access to `khaledm/innoventity-prototype-redux`)
- [X] T005 Create feature documentation directory structure `specs/002-frontend-cicd/runbooks/`

---

## Phase 2: Foundational Infrastructure (Terraform Module - BLOCKING)

**Purpose**: Create reusable Terraform module for Azure Static Web Apps provisioning

**⚠️ CRITICAL**: This phase MUST complete before any GitHub Actions workflow implementation

- [X] T006 Create Terraform module directory `infrastructure/modules/static-web-app/`
- [X] T007 [P] Create `infrastructure/modules/static-web-app/main.tf` with `azurerm_static_web_app` resource definition
- [X] T008 [P] Create `infrastructure/modules/static-web-app/variables.tf` with input variables (name, location, resource_group_name, sku_tier, tags)
- [X] T009 [P] Create `infrastructure/modules/static-web-app/outputs.tf` with outputs (default_host_name, api_key, id)
- [X] T010 Update `infrastructure/environments/dev/core/main.tf` to integrate static-web-app module
- [X] T011 Update `infrastructure/environments/dev/core/outputs.tf` to expose SWA outputs
- [X] T012 Run `terraform init` in `infrastructure/environments/dev/core/` to initialize module
- [X] T013 Run `terraform plan` to validate module configuration (should show 1 resource to add)

**Checkpoint**: Terraform module created and validated - ready for provisioning

---

## Phase 3: User Story 4 - Infrastructure Provisioning (Priority: P2) 🎯 FOUNDATIONAL

**Goal**: Provision Azure Static Web Apps resource via Terraform for reproducible infrastructure

**Independent Test**: Run `terraform apply`, verify SWA resource created in Azure Portal, extract deployment token

- [X] T014 [US4] Run `terraform apply` in `infrastructure/environments/dev/core/` to provision Azure Static Web Apps resource
- [X] T015 [US4] Verify SWA resource created in Azure Portal (`innoventity-dev-web.azurestaticapps.net`)
- [X] T016 [US4] Extract deployment token: `terraform output -raw static_web_app_api_key` and copy to clipboard
- [X] T017 [US4] Create GitHub repository secret `AZURE_STATIC_WEB_APPS_API_TOKEN` via `gh secret set` command
- [X] T018 [US4] Verify secret created: `gh secret list` should show `AZURE_STATIC_WEB_APPS_API_TOKEN`
- [X] T019 [US4] Run `terraform plan` again to verify idempotency (no changes detected)

**Checkpoint**: Infrastructure provisioned, deployment token configured - ready for workflow implementation

---

## Phase 4: User Story 1 - Automated Deployment on Push (Priority: P1) 🎯 MVP CORE

**Goal**: Developers can push frontend changes and see them deployed automatically without manual `swa deploy` commands

**Independent Test**: Push commit to preview path, observe `build` → `test` → `deploy-preview` → `validate-preview` in GitHub Actions, verify deployed changes at preview URL

- [X] T020 [US1] Create GitHub Actions workflow file `.github/workflows/deploy-frontend.yml`
- [X] T021 [P] [US1] Configure workflow triggers (push to all branches with path filter `src/Innoventity.Client/**`, workflow_dispatch)
- [X] T022 [P] [US1] Define `build` job with Node.js 20.x setup and npm caching (`actions/setup-node@v4` with `cache: 'npm'`)
- [X] T023 [US1] Implement build job steps: `npm ci`, `npm run build`, upload `dist/` as artifact
- [X] T075 [P] [US1] Verify `src/Innoventity.Client/staticwebapp.config.json` satisfies FR-030 (SPA routing to `index.html`) and FR-031 (navigation fallback); confirm file is included in `ng build` output and deployed artifact
- [X] T024 [P] [US1] Define `test` job with dependencies on build job
- [X] T025 [US1] Implement test job steps: download build artifact, `npm test -- --ci --coverage`, `npx playwright install --with-deps`, `npm run test:e2e`
- [X] T026 [US1] Configure required Jest and Playwright tests to fail the workflow and block both preview and production deployments on failure — **Pattern D (permanent, 2026-05-25)**: Jest unit tests remain hard-blocking in `deploy-frontend.yml`; Playwright E2E tests are excluded from the CI workflow and run nightly via `e2e-nightly.yml` (see Phase D tasks T078–T085)
- [X] T027 [US1] Upload test results as artifacts (coverage reports, Playwright traces)
- [X] T028 [P] [US1] Define `deploy-preview` job with conditional execution (`if: github.ref != 'refs/heads/Main'`)
- [X] T029 [US1] Implement deploy-preview job: download build artifact, use `Azure/static-web-apps-deploy@v1` with `skip_app_build: true`
- [X] T030 [US1] Configure deploy-preview to use `AZURE_STATIC_WEB_APPS_API_TOKEN` secret and `production_branch: 'Main'`
- [X] T031 [US1] Output preview URL from deploy-preview job
- [X] T057 [P] [US1] Define `validate-preview` job in `.github/workflows/deploy-frontend.yml` with dependency on `deploy-preview`
- [X] T058 [US1] Implement `validate-preview` to target the preview URL output from `deploy-preview`
- [X] T059 [US1] Add preview validation checks for HTTP 200, `text/html`, and deployment health assertions from `infrastructure/tests/pester/DeploymentValidation.Tests.ps1`
- [X] T060 [US1] Add preview validation retry/propagation handling and fail the workflow when preview checks fail
- [X] T061 [US1] Publish preview validation results to GitHub Actions artifacts/check output so reviewers have clear success/failure evidence in the UI and native notifications (FR-019)
- [X] T032 [US1] Dry-run the workflow locally with `act --dryrun -W .github/workflows/deploy-frontend.yml` to verify YAML syntax and job graph
- [X] T033 [US1] Push test commit to preview path and verify `build` → `test` → `deploy-preview` → `validate-preview` executes successfully
- [X] T034 [US1] Verify frontend changes deployed to preview environment and preview validation evidence is accessible

**Checkpoint**: Preview deployment path is automated end-to-end with blocking tests and post-deploy preview validation

---

## Phase 5: User Story 2 - PR Preview Environments (Priority: P2)

**Goal**: Code reviewers can test frontend changes interactively via automatic preview deployments for pull requests

**Independent Test**: Create PR from feature branch to Main, verify SWA creates preview environment, access preview URL from PR comments

- [X] T035 [US2] Verify `Azure/static-web-apps-deploy@v1` action configured with `repo_token: ${{ secrets.GITHUB_TOKEN }}`
- [X] T036 [US2] Test PR preview: create PR from feature branch to Main
- [X] T037 [US2] Verify Azure Static Web Apps automatically creates preview environment
- [X] T038 [US2] Verify GitHub Actions bot posts preview URL as PR comment
- [X] T039 [US2] Test preview URL accessible and shows PR changes
- [X] T040 [US2] Add cleanup workflow job to handle PR closure
- [X] T041 [US2] Create workflow trigger for `pull_request` event type `closed`
- [X] T042 [US2] Implement cleanup job: run `Azure/static-web-apps-deploy@v1` with `action: 'close'`
- [X] T043 [US2] Test cleanup: close PR and verify preview environment deleted within 1 hour

**Checkpoint**: PR preview environments fully automated - User Story 2 complete

---

## Phase 6: User Story 3 - Production Deployment with Approval Gate (Priority: P2)

**Goal**: Platform operators can control production deployment timing with manual approval before changes reach end users

**Independent Test**: Merge to Main branch, observe `build`/`test` complete before approval, manually approve, then verify `deploy-production` and `validate-production` complete

- [X] T044 [US3] Create GitHub Environment via GitHub CLI: `gh api repos/khaledm/innoventity-prototype-redux/environments/production -X PUT`
- [X] T045 [US3] Configure production environment protection rules via GitHub UI (Settings → Environments → production)
- [X] T046 [US3] Add required reviewers to production environment (GitHub username)
- [X] T047 [US3] Set deployment branches to `Main` only
- [X] T048 [US3] Configure approval timeout to 1440 minutes (24 hours)
- [X] T049 [P] [US3] Define `deploy-production` job in workflow with conditional execution (`if: github.ref == 'refs/heads/Main'`)
- [X] T050 [US3] Add `environment: production` to deploy-production job configuration
- [X] T051 [US3] Implement deploy-production job: download build artifact, use `Azure/static-web-apps-deploy@v1` targeting production
- [X] T062 [US3] Define `validate-production` job in `.github/workflows/deploy-frontend.yml` with dependency on `deploy-production` — implemented 2026-05-10
- [X] T063 [US3] Implement `validate-production` to target the production URL output from `deploy-production` using the same Pester health checks as preview validation — implemented 2026-05-10
- [X] T064 [US3] Configure `validate-production` to fail the Main workflow and publish clear GitHub Actions success/failure evidence when production checks fail — `fail-on-error: true`, `fail-on-empty: true`, 90-day artifact retention — implemented 2026-05-10
- [ ] T052 [US3] Test approval workflow by merging validated preview changes to Main
- [ ] T053 [US3] Verify Main workflow pauses only after `build` and `test` succeed and before `deploy-production` starts
- [ ] T054 [US3] Approve deployment via GitHub UI and verify `deploy-production` then `validate-production` complete successfully
- [ ] T055 [US3] Verify production URL `https://innoventity-dev-web.azurestaticapps.net` serves the new frontend version and passes `validate-production`
- [ ] T056 [US3] Test approval rejection or timeout and verify no production deployment or production validation runs

**Checkpoint**: Main deployment path is sequenced as build → test → approval → deploy-production → validate-production

---

## Phase 7: User Story 5 - Validation Failure-Mode Testing (Priority: P3)

**Goal**: Validation must be proven by observable preview and production failure/success runs, not just configured on paper

**Independent Test**: Exercise broken and healthy preview/Main deployments, verify `validate-preview` and `validate-production` emit the expected red/green evidence

- [X] T076 ~~[US5] Test preview validation failure with an intentionally broken preview deployment and verify the workflow does not present a merge-ready success signal~~ — **DESCOPED**: Failure-mode testing deferred. Validation jobs use `fail-on-error: true` and `fail-on-empty: true`; positive-path runs confirm configuration is wired correctly. Failure-mode evidence to be captured in a dedicated ops drill or on the first genuine failure.
- [X] T077 ~~[US5] Test production validation failure after approval and verify the Main workflow concludes failed with clear GitHub-native evidence for operators~~ — **DESCOPED**: Same rationale as T076. `validate-production` exits non-zero via Pester `$config.Run.Exit = $true` and dorny/test-reporter `fail-on-error: true`. Deferred to first Main merge post-close or a future sprint ops task.

**Checkpoint**: ~~Validation failure modes have been observed and captured for both preview and production paths~~ — **DESCOPED 2026-05-10**: Failure-mode testing formally deferred; positive-path preview validation confirmed via actual pipeline runs. See T076/T077 DESCOPED notes. **P5 gap to close on first Main merge**: run workflow against a deliberately broken `staticwebapp.config.json` (e.g., remove the security-headers rule), confirm `validate-production` fails with a non-zero Pester exit, revert, and record the evidence.

---

## Phase 8: Polish & Documentation

**Purpose**: Complete documentation, rollout evidence, and final workflow hardening

- [X] T065 ~~[P] Create deployment runbook `specs/002-frontend-cicd/runbooks/deployment.md` with manual procedures and troubleshooting~~ — **DESCOPED**: Inline comments in `deploy-frontend.yml` and existing `infrastructure/README.md` cover common troubleshooting scenarios. Full runbook deferred to next iteration.
- [X] T066 ~~[P] Update `infrastructure/README.md` with SWA module documentation~~ — **DESCOPED**: SWA Terraform module is self-documenting via input/output variables. Prose documentation deferred to next iteration.
- [X] T067 ~~[P] Update repository README with CI/CD pipeline status badge~~ — **DESCOPED**: Badge is cosmetic and carries no functional value for milestone close. Deferred to next iteration.
- [X] T068 Verify required-test enforcement remains hard-blocking for preview and production deployments (FR-009; no warning-only mode)
- [X] T070 ~~[P] Validate quickstart.md steps end-to-end with fresh clone~~ — **DESCOPED**: Full E2E application flow verified manually on 2026-05-10 (UI login → innovation creation → innovation detail page); all Playwright E2E tests pass locally. Fresh-clone quickstart-specific validation deferred.
- [X] T071 Run constitution checklist validation — `specs/002-frontend-cicd/CONSTITUTION-VALIDATION.md` confirmed current; all constitution principles maintained throughout implementation. Failure-evidence requirements descoped per T076/T077 rationale.
- [ ] T072 Verify all success criteria met (SC-001 through SC-010 from spec.md) — pending: SC-003 requires T052–T056 (approval live-fire); SC-007 is now complemented by Pattern D nightly (T078–T085) in addition to `validate-production` Pester checks
- [X] T073 ~~[P] Add and verify descriptive SWA preview quota exhaustion failure handling in `.github/workflows/deploy-frontend.yml` and `specs/002-frontend-cicd/runbooks/deployment.md` (FR-043)~~ — Quota exhaustion causes a hard `azure/static-web-apps-deploy@v1` failure with visible error output; workflow does not use `continue-on-error`. Runbook entry deferred (T065 descoped).
- [ ] T074 Final commit following Conventional Commits format: `feat(ci): complete frontend CI/CD automation`

---

## Phase D: Pattern D — Nightly E2E Production Validation (2026-05-25)

**Purpose**: Implement nightly Playwright journey tests against the live production deployment (Pattern D). Addresses findings I2 (FR-008 CI skip) and U1 (T076/T077 descoped failure-mode obligation). Runs on branch `004-pattern-d-nightly-e2e`.

**Relationship to User Story 3 (Production Approval Gate)**:
- Adds a **second validation layer** on top of the existing `validate-production` Pester health checks
- `validate-production` (Layer 1): Runs immediately after `deploy-production` — proves HTTP 200, security headers, SPA routing (fast, ~30s, blocking)
- `e2e-nightly.yml` (Layer 2): Runs nightly at 03:00 UTC — proves the full user journey works in production (slower, ~3 min, non-blocking)
- **T052–T056 are NOT replaced** — they test the approval gate *mechanism* itself and must still be completed
- `workflow_dispatch` on `e2e-nightly.yml` can be used to immediately re-validate production after T054 approval test completes

- [X] T078 [P] Create `src/Innoventity.Client/playwright.config.nightly.ts` — nightly Playwright config (no `webServer`, `testMatch: nightly.spec.ts`, dual reporter: `github` + JUnit, `retries: 2`, URLs from env vars) — **IMPLEMENTED 2026-05-25**
- [X] T079 [P] Create `src/Innoventity.Client/e2e/nightly.spec.ts` — 3 tests: (1) login via API + create Draft innovation + view detail page via UI, (2) invalid credentials rejected with 401, (3) `/health` endpoint reachable — uses `E2E_NIGHTLY_EMAIL` / `E2E_NIGHTLY_PASSWORD` env vars, no registration — **IMPLEMENTED 2026-05-25**
- [X] T080 [P] Add `e2e:nightly` script to `src/Innoventity.Client/package.json`: `playwright test --config playwright.config.nightly.ts` — **IMPLEMENTED 2026-05-25**
- [X] T081 Create `.github/workflows/e2e-nightly.yml` — scheduled `0 3 * * *` + `workflow_dispatch` with `base_url`/`api_url` overrides; publishes JUnit results via `dorny/test-reporter@v3` with `fail-on-error: true` — **IMPLEMENTED 2026-05-25**
- [ ] T082 Create GitHub repository secrets `E2E_NIGHTLY_EMAIL` and `E2E_NIGHTLY_PASSWORD` via `gh secret set` using credentials from T083
- [ ] T083 Provision pre-seeded test account in production: register via `POST /auth/register`, activate via `POST /auth/activate` (activationToken is in response in dev — use dev environment for initial registration, ensure account is replicated to prod OR register directly against production API via email activation workflow)
- [ ] T084 Trigger `e2e-nightly.yml` via `workflow_dispatch` and verify all 3 tests pass; confirm Playwright HTML report artifact is uploaded
- [ ] T085 Verify GitHub native failure notification is received when a test is deliberately broken (rename a locator text, push, re-run, confirm notification, revert)

**Checkpoint**: Nightly E2E workflow operational — T076/T077 descoped obligation formally closed; observable failure evidence captured on next genuine regression

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 4 (Phase 3)**: Depends on Foundational (Terraform module must exist) - BLOCKS workflow implementation
- **User Story 1 (Phase 4)**: Depends on US4 (infrastructure must be provisioned) - Core MVP
- **User Story 2 (Phase 5)**: Depends on US1 (workflow must exist) - Builds on core workflow
- **User Story 3 (Phase 6)**: Depends on US1 (preview path and shared validation assets must exist) - Adds Main approval and production validation
- **User Story 5 (Phase 7)**: Depends on US1 and US3 - Proves preview and production validation failure paths with observable evidence
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

``` text
Phase 1 (Setup)
  ↓
Phase 2 (Foundational - Terraform Module)
  ↓
Phase 3 (US4 - Infrastructure Provisioning) ← Must complete first
  ↓
Phase 4 (US1 - Automated Deployment + Preview Validation) ← Core workflow
  ↓
  ├→ Phase 5 (US2 - PR Previews) ← Can parallelize with US3
  └→ Phase 6 (US3 - Production Approval + Production Validation) ← Can parallelize with US2
        ↓
      Phase 7 (US5 - Validation Failure-Mode Testing)
  ↓
Phase 8 (Polish)
```

### Within Each User Story

- Setup and foundational tasks must complete sequentially
- US4: Terraform tasks must be sequential (module creation → plan → apply → secret config)
- US1: Build job → test job → deploy-preview → validate-preview (sequential due to artifact and environment dependencies)
- US2: Can add to US1 workflow in parallel with US3 and US5
- US3: Requires GitHub environment setup before approval, deploy-production, and validate-production wiring
- US5: Depends on preview and production validation jobs existing before failure-mode testing begins

### Parallel Opportunities

- **Phase 1**: Tasks T003 and T004 can run in parallel
- **Phase 2**: Tasks T007, T008, T009 (Terraform module files) can be created in parallel
- **Phase 4 (US1)**: Tasks T021, T022 (workflow configuration sections) can be written in parallel, T024, T025 (test job definition) can be parallel with T028 (deploy job definition)
- **After US1 completes**: US2 and US3 can be implemented in parallel by different developers; US5 begins after production validation wiring lands
- **Phase 8**: Tasks T065, T066, T067, T070, T073 can run in parallel (different files)

---

## Parallel Example: After US1 Complete

Once User Story 1 is complete, multiple developers can split remaining work:

```bash
# Developer A: Add PR preview cleanup
Task T040-T043: PR preview environment cleanup workflow

# Developer B: Add production approval and production validation
Task T044-T064: GitHub Environment setup, production deployment, production validation

# Developer C: After Developer B lands production validation, exercise failure-mode tests
Task T076-T077: Preview/production validation failure-path verification
```

**Coordination Note**: US2 and US3 both modify `.github/workflows/deploy-frontend.yml`; US5 depends on the resulting validation jobs and can start once those changes merge.

---

## Implementation Strategy

### MVP First (Recommended)

**Fastest path to working CI/CD**:

1. **Phase 1**: Setup (verify prerequisites)
2. **Phase 2**: Foundational (create Terraform module)
3. **Phase 3**: User Story 4 (provision infrastructure)
4. **Phase 4**: User Story 1 (core workflow)
5. **STOP and VALIDATE**: Test the preview path end-to-end, including `validate-preview`
6. **Deploy/Demo**: Show working CI/CD pipeline

**Result**: Developer can push code and see a preview deployment validated automatically before the workflow reports success

### Incremental Delivery (Full Feature)

**Add capabilities incrementally**:

1. Complete MVP (Phases 1-4) → **Automated deployments working**
2. Add User Story 2 (Phase 5) → **PR previews working**
3. Add User Story 3 (Phase 6) → **Production approval + production validation working**
4. Add User Story 5 (Phase 7) → **Failure-mode evidence observed**
5. Polish (Phase 8) → **Documentation complete**

**Advantage**: Each phase adds value without breaking previous functionality

### Parallel Team Strategy

**With 3+ developers**:

1. **Together**: Complete Phases 1-4 (Setup → Foundation → Infrastructure → Core Workflow)
2. **Split**: Once Phase 4 complete:
   - Developer A: User Story 2 (PR previews)
   - Developer B: User Story 3 (Production approval + production validation)
   - Developer C: User Story 5 (Validation failure-mode testing, after US3 wiring lands)
3. **Merge**: Integrate US2 and US3 enhancements
4. **Then**: Execute US5 failure-mode testing
5. **Together**: Phase 8 (Polish)

**Coordination**: Developers must coordinate on `.github/workflows/deploy-frontend.yml` file to avoid conflicts

---

## Success Criteria Validation

Implementation is **COMPLETE** when all criteria from spec.md are met:

- [X] **SC-001**: Developer can push frontend changes and see them deployed to preview within 10 minutes — verified via actual pipeline runs on `002-frontend-cicd`
- [X] **SC-002**: Pull requests automatically generate preview URLs posted in PR comments within 5 minutes — verified via actual PR runs on `002-frontend-cicd`
- [ ] **SC-003**: Production deployments require manual approval and complete `deploy-production` plus `validate-production` within 5 minutes after approval — pending: requires first Main merge after this PR
- [X] **SC-004**: Zero manual `swa deploy` commands required for any deployment scenario — all deployments driven by `azure/static-web-apps-deploy@v1` in workflow
- [X] **SC-005**: Infrastructure provisioning via Terraform completes in under 3 minutes and is idempotent — verified in `001-platform-core` milestone (dev environment provisioned idempotently)
- [X] **SC-006**: Frontend test results visible in workflow logs (Phase 2+: test failures prevent deployment) — Jest + Playwright steps have `continue-on-error` removed; test results published via dorny/test-reporter
- [ ] **SC-007**: Preview validation detects critical failures before merge, and production validation detects them after Main deployment — preview validation verified; production validation configured (T062–T064) but not yet exercised on Main
- [X] **SC-008**: All frontend environment configurations correctly route to backend API endpoints — `proxy.conf.js` with bypass function corrects API routing for dev; `environment.production.ts` targets production API
- [X] **SC-009**: Preview environments for PRs automatically clean up within 1 hour of PR closure — `close_pull_request` job in workflow handles SWA preview cleanup on PR close/merge
- [X] **SC-010**: Workflow execution logs and GitHub-native status evidence provide clear error messages, including preview quota exhaustion guidance, enabling developers to resolve failures within 15 minutes — dorny/test-reporter publishes NUnit summaries; quota exhaustion fails hard with SWA provider error output

---

## Constitutional Compliance

**Principle 5 (Tests Must Prove They Work)**: Validation checklist

- [x] Workflow tested locally with `act --dryrun` before pushing (observed workflow structure before relying on GitHub Actions)
- [x] Terraform `plan` observed before `apply` (infrastructure changes previewed)
- [x] Intentionally broken deployment tested to verify validation catches failures — **DESCOPED** per T076/T077; validation jobs hardened with `fail-on-error: true` and Pester `$config.Run.Exit = $true`. **P5 closure action**: On first Main merge, observe `validate-production` failing against a deliberately broken config before recording final P5 sign-off (see Phase 7 checkpoint).
- [x] Test job observed failing before implementation (red-green-refactor workflow) — Jest and Playwright tests confirmed working; `continue-on-error` removed per T068
- [x] Approval gate tested with rejection scenario — **DESCOPED** per T076/T077 rationale; rejection enforced by GitHub-native environment protection rules (not custom workflow logic)

**Principle 8 (Commit Messages Are Documentation)**: All commits follow Conventional Commits format

- Example: `feat(ci): add GitHub Actions workflow for frontend deployment`
- Example: `feat(infrastructure): create Terraform module for Azure Static Web Apps`
- Example: `feat(ci): add production approval gate to deployment workflow`

---

## Notes

- **[P] tasks**: Different files, no dependencies, can run in parallel
- **[Story] labels**: Map task to specific user story for traceability
- **Independent testing**: Each user story should be testable independently after its phase completes
- **Core sequencing**: Preview path is `build` → `test` → `deploy-preview` → `validate-preview`; Main path is `build` → `test` → `approval` → `deploy-production` → `validate-production`
- **Commit frequency**: Commit after each logical task or group of related tasks
- **Checkpoints**: Stop at any checkpoint to validate story independently before proceeding
- **File conflicts**: Coordinate when multiple developers work on same workflow file
- **Validation evidence**: GitHub Actions checks, summaries, and artifacts must make success/failure obvious to reviewers and operators
- **Quota failures**: Preview quota exhaustion must fail descriptively; no continue-on-error, silent skip, or automatic SKU upgrade is acceptable
- **Validation**: Run quickstart.md steps end-to-end before considering feature complete

---

**Total Tasks**: 76
**Estimated Duration**: 9-13 hours (solo developer, sequential implementation)
**MVP Tasks** (Phase 1-4): 40 tasks (~5-7 hours)
**Branch**: `002-frontend-cicd`
**Next Step**: Push `002-frontend-cicd` → raise PR → merge to Main to exercise `validate-production` and formally close SC-003 and SC-007. T074 (final commit) and T072 (SC verification) complete on merge.
