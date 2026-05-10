# Workflow Contract: deploy-frontend.yml

**Feature**: Frontend CI/CD Automation
**Contract Type**: GitHub Actions Workflow Interface Specification
**Date**: April 12, 2026
**Purpose**: Define inputs, outputs, secrets, and behavioral contract for frontend deployment workflow

---

## Contract Overview

This document defines the interface contract for the `.github/workflows/deploy-frontend.yml` workflow. Consumers (developers, CI/CD tools, reviewers) can rely on this contract for predictable behavior.

**Contract Guarantees**:
- Workflow triggers on specified events only
- Secrets are never leaked in logs
- Outputs are available to downstream processes
- Failure modes are documented and detectable
- Performance SLAs are defined

---

## Trigger Contract

### Supported Triggers

#### 1. Push Event (Automatic)

```yaml
on:
  push:
    branches:
      - '**'  # All branches (feature branches + Main)
    paths:
      - 'src/Innoventity.Client/**'  # Frontend code changes only
      - '.github/workflows/deploy-frontend.yml'  # Workflow itself
```

**Behavior**:
- Triggers on push to ANY branch
- Filters: Only triggers if files in `src/Innoventity.Client/` or workflow file changed
- Does NOT trigger on: Backend code changes, infrastructure changes, documentation-only changes
- Branch detection: Uses `${{ github.ref }}` to determine if `Main` or feature branch

**Guarantees**:
- Workflow runs within 60 seconds of push (GitHub-hosted runner queue time)
- Concurrent workflow runs for same branch are queued (not parallel)

---

#### 2. Workflow Dispatch (Manual)

```yaml
on:
  workflow_dispatch:
    inputs:
      environment:
        description: 'Target environment (dev only in Phase 1)'
        required: false
        default: 'dev'
        type: choice
        options:
          - dev
```

**Behavior**:
- Enables manual workflow execution via GitHub Actions UI
- Can be triggered from any branch
- Input `environment` is informational only (Phase 1 only supports `dev`)

**Use Cases**:
- Re-deploy without code changes (e.g., after infrastructure change)
- Test workflow changes before merging
- Emergency deployment override

**Guarantees**:
- Manual dispatch bypasses `paths` filter (runs even if no frontend code changed)
- Requires `write` permission on repository (only authorized users can dispatch)

---

## Input Contract

### Required Secrets (Repository Level)

#### AZURE_STATIC_WEB_APPS_API_TOKEN

**Type**: `string` (sensitive)
**Source**: Terraform output from `infrastructure/modules/static-web-app/` (output: `api_key`)
**Purpose**: Authenticates GitHub Actions to Azure Static Web Apps for deployment
**Format**: Base64-encoded API key (e.g., `"abc123def456..."`, length ~100 characters)
**Scope**: Repository secret (Settings → Secrets and variables → Actions → AZURE_STATIC_WEB_APPS_API_TOKEN)

**Usage**:
```yaml
- uses: Azure/static-web-apps-deploy@v1
  with:
    azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
```

**Validation**:
- MUST be non-empty (workflow fails if secret not set)
- MUST be valid API key for target Azure SWA resource (deployment fails with `InvalidToken` error if wrong key)
- MUST NOT be logged or exposed in workflow outputs (marked `sensitive: true`)

**Rotation Policy**:
- Rotate if: Secret leaked, repository compromised, team member leaves
- Rotation process: Regenerate in Azure Portal → Update GitHub secret → No workflow changes needed

---

### Optional Inputs (Workflow Dispatch)

#### environment

**Type**: `choice` (`dev`)
**Default**: `'dev'`
**Purpose**: Selector for future multi-environment support (Phase 2+)
**Behavior**: Informational only in Phase 1 (always deploys to `dev` SWA resource)

---

### Implicit Inputs (GitHub Context)

These are not explicit inputs but are consumed by the workflow:

| Context Variable | Type | Usage | Example |
|------------------|------|-------|---------|
| `github.ref` | `string` | Determine branch name for conditional logic | `refs/heads/Main`, `refs/heads/002-frontend-cicd` |
| `github.sha` | `string` | Git commit SHA for traceability | `5b200d5a1c3e7f9d2b4a6c8e1f3d5a7b9c0e2f4` |
| `github.actor` | `string` | Username who triggered workflow | `khaledm` |
| `github.event_name` | `string` | Trigger event type | `push`, `workflow_dispatch` |
| `github.repository` | `string` | Repository name | `khaledm/innoventity-prototype-redux` |
| `github.run_id` | `string` | Workflow run ID | `1234567890` |

---

## Output Contract

### Workflow Outputs (Available to Downstream Workflows)

#### deployment_url

**Type**: `string` (URL)
**Source**: `deploy-preview` or `deploy-production` job
**Format**: `https://<hostname>.azurestaticapps.net`
**Behavior**:
- **Preview deployment**: `https://<swa-name>-<pr-number>-<region>.azurestaticapps.net` (e.g., `https://innoventity-dev-web-abc123.azurestaticapps.net`)
- **Production deployment**: `https://innoventity-dev-web.azurestaticapps.net`

**Availability**:
- Set by `Azure/static-web-apps-deploy@v1` action (output: `static_web_app_url`)
- Available immediately after deployment job completes
- Used by `validate` job for health check

**Guarantee**: URL is accessible within 5 minutes of output being set (CDN propagation time)

---

#### deployment_status

**Type**: `string` (enum)
**Values**: `success` | `failed` | `skipped`
**Behavior**:
- `success`: Deployment completed and validation passed
- `failed`: Deployment or validation failed
- `skipped`: Deploy job not executed (e.g., deploy-production skipped on feature branch)

**Usage**: Downstream workflows can check this output to trigger notifications or further actions

---

### Job Outputs (Available Within Workflow)

#### build.artifact_id

**Type**: `string`
**Description**: GitHub artifact ID for frontend build output
**Usage**: Downloaded by `deploy-preview` and `deploy-production` jobs

---

#### validate.health_check_result

**Type**: `string` (`pass` | `fail`)
**Description**: Health check result for deployed URL
**Usage**: Gates approval for production deployment (if staging validation fails, don't approve)

---

## Performance Contract (SLAs)

### Job Execution Times

| Job | Target Duration | Timeout | Notes |
|-----|----------------|---------|-------|
| `build` | <5 minutes | 10 minutes | Includes `npm ci` (cached) + `ng build` |
| `test` | <8 minutes | 15 minutes | Jest (28 tests) + Playwright (3 tests) with browser installs |
| `deploy-preview` | <3 minutes | 10 minutes | Artifact download + Azure upload |
| `deploy-production` | <5 minutes | 10 minutes | Artifact download + Azure upload + CDN propagation |
| `validate` | <30 seconds | 2 minutes | HTTP health check with retries |

**Total Workflow Duration**:
- **Feature branch** (no approval): 10-15 minutes (build → test → deploy-preview → validate)
- **Main branch** (with approval): 10 minutes + approval wait time (up to 24 hours) + 5 minutes (production deploy + validate)

**Guarantee**: Workflow completes within **15 minutes** (excluding approval wait time) or times out (NFR-001)

---

### Caching Strategy

**npm Dependencies**:
- Cache key: `${{ runner.os }}-node-${{ hashFiles('**/package-lock.json') }}`
- Cache hit: `npm ci` completes in ~30 seconds (vs ~2 minutes on miss)
- Cache invalidation: Automatic when `package-lock.json` changes

**Playwright Browsers**:
- Cache key: `${{ runner.os }}-playwright-${{ hashFiles('**/package-lock.json') }}`
- Cache hit: `npx playwright install --with-deps` skips browser downloads (~200MB saved)
- Cache invalidation: Automatic when Playwright version changes in `package.json`

**Build Artifacts** (not cached, uploaded/downloaded):
- Size: ~500kB-1MB (Angular prod build, compressed)
- Upload time: ~10 seconds
- Download time: ~5 seconds
- Retention: 90 days (GitHub default, configurable)

---

## Behavioral Contract

### Success Criteria

Workflow is **successful** if:
1. ✅ Build job completed successfully (Angular compiled, artifacts uploaded)
2. ✅ Test job completed successfully OR `continue-on-error: true` (Phase 1 warnings allowed)
3. ✅ Deploy job completed successfully (SWA upload succeeded, URL accessible)
4. ✅ Validate job passed (HTTP 200, Content-Type: text/html)
5. ✅ (Production only) Approval gate approved within 24 hours

---

### Failure Modes

| Failure Type | Detection | Recovery | Example Error |
|--------------|-----------|----------|---------------|
| **Build failure** | `build` job exits non-zero | Fix TypeScript/template errors, push new commit | `NG0301: Export not found!` |
| **Test failure** (Phase 2+) | `test` job exits non-zero | Fix failing tests or code, push new commit | `Expected 3 to equal 5` |
| **Deployment failure** | `deploy` job exits non-zero | Check Azure Portal logs, verify secret valid | `InvalidToken: Authentication failed` |
| **Quota exhaustion** | `deploy-preview` fails with quota error | Delete old previews in Azure Portal, close stale PRs | `QuotaExceeded: Preview limit reached (3/3)` |
| **Validation failure** | `validate` job exits non-zero | Check SWA logs, verify routing config, verify artifact | `HTTP 404: Not Found` |
| **Approval timeout** | `deploy-production` times out after 24 hours | Re-run workflow, ensure reviewers notified | `Error: Workflow run exceeded timeout` |
| **Approval rejection** | Reviewer clicks "Reject deployment" | Address concerns, push new commit, retry | `Deployment rejected by reviewer` |

**Guarantee**: All failures are logged with actionable error messages (no silent failures)

---

### Conditional Execution

#### Deploy-Preview Job (Feature Branches Only)

```yaml
if: github.ref != 'refs/heads/Main'
```

**Behavior**:
- Executes on feature branches (e.g., `002-frontend-cicd`, `feature/new-component`)
- Skips on `Main` branch (replaced by `deploy-production`)
- Creates preview environment with PR-specific URL

**Guarantees**:
- Preview URL posted as PR comment (via `Azure/static-web-apps-deploy@v1` action)
- Preview deleted automatically on PR closure (cleanup workflow)

---

#### Deploy-Production Job (Main Branch Only)

```yaml
if: github.ref == 'refs/heads/Main'
environment:
  name: production
  url: https://innoventity-dev-web.azurestaticapps.net
timeout-minutes: 1440  # 24 hours
```

**Behavior**:
- Executes ONLY on `Main` branch (secured via branch protection)
- Pauses at approval gate (GitHub Environment protection rule)
- Times out after 24 hours if no approval/rejection

**Guarantees**:
- Required reviewers receive email notification when gate triggered
- Approval/rejection logged in workflow run (audit trail)
- Production URL updated within 5 minutes of approval grant (NFR-002)

---

## Security Contract

### Secret Handling

**Guarantees**:
- Secrets NEVER logged in workflow outputs (GitHub automatically redacts)
- Secrets NEVER exposed via job outputs
- Secrets NEVER committed to repository (`.gitignore` for local `.secrets` file)
- Secrets accessible only to workflows in same repository (not forks)

**Validation**:
- GitHub automatically masks secrets in logs (shows `***` instead of value)
- If secret value appears in logs, workflow is **insecure** (contract violation)

---

### Branch Protection

**Production Deployments**:
- MUST execute on `Main` branch only (`if: github.ref == 'refs/heads/Main'`)
- `Main` branch MUST be protected (Settings → Branches → Branch protection rules):
  - Require pull request reviews before merging
  - Require status checks to pass (deploy workflow)
  - Do not allow force pushes
  - Do not allow deletions

**Guarantee**: No direct pushes to `Main` → all production deployments are reviewed via PR

---

### Approval Authorization

**Required Reviewers**:
- Configured in GitHub Environment settings (Settings → Environments → production → Required reviewers)
- Only specified users can approve production deployments
- Minimum reviewers: 1 (Phase 1), can configure up to 6

**Guarantee**: Unauthorized users cannot approve production deployments (GitHub enforces)

---

## Failure Recovery Contract

### Automatic Retries

**Transient Failures**:
- GitHub Actions automatically retries failed jobs up to 3 times (configurable)
- Retries apply to: Network timeouts, Azure service unavailability, runner failures
- Does NOT retry: Build errors, test failures, deployment token errors (user errors, not transient)

**Manual Retries**:
- User can re-run failed workflow via GitHub UI (Actions → failed run → Re-run jobs)
- Re-running preserves same commit SHA (consistency)

---

### Rollback Capability

**Preview Deployments**:
- No rollback needed (preview is temporary, delete and redeploy)
- Close PR and reopen to trigger fresh preview

**Production Deployments**:
- **Manual rollback**: Re-run workflow from previous commit SHA
  - Git: `git reset --hard <previous-commit>; git push --force origin Main`
  - Workflow: Automatically deploys previous version
- **Terraform rollback** (infrastructure): Run `terraform apply` from previous state (Phase 2+ enhancement)

**Guarantee**: Can rollback to any previous commit within 90 days (artifact retention period)

---

## Monitoring & Observability Contract

### Logs & Artifacts

**Workflow Logs**:
- Retained for 90 days (GitHub default, configurable up to 400 days)
- Includes: Job timestamps, console output, error messages, environment variables (non-sensitive)
- Accessible via: GitHub Actions UI → Workflow run → Job logs

**Build Artifacts**:
- `frontend-build` artifact: Angular compiled output (`dist/` folder), retained 90 days
- `test-results` artifact: Jest coverage reports + Playwright traces, retained 90 days
- Download via: GitHub Actions UI → Workflow run → Artifacts section

**Azure SWA Logs**:
- Deployment logs: Azure Portal → Static Web Apps → Deployments → View logs
- Application logs: Azure Monitor / Application Insights (if enabled)
- Retention: 30 days (Azure default)

---

### Metrics & Telemetry

**Available Metrics**:
- Workflow success rate: `successful runs / total runs`
- Workflow duration: p50, p95, p99 (from GitHub Actions API)
- Deployment frequency: `deployments / week`
- Approval wait time: `approval_granted_at - approval_requested_at` (median, p95)

**Monitoring Tools**:
- GitHub Actions UI: Workflow insights (Actions → Workflows → deploy-frontend → Analytics)
- GitHub CLI: `gh run list --workflow=deploy-frontend.yml --json status,conclusion,startedAt,completedAt`

**Guarantee**: Workflow execution data available via GitHub API for 90 days minimum (NFR-004)

---

## Version Compatibility Contract

### Node.js Version

**Supported**: Node.js 18.x LTS, 20.x LTS
**Tested**: 20.x (GitHub Actions `ubuntu-latest` runner default)
**Compatibility**:
- Angular 19 requires Node.js 18.13+ or 20.x (official requirement)
- Workflow pins to `20.x` for reproducibility
- Can upgrade to future LTS (22.x) by changing `node-version: '22.x'` in setup step

---

### Angular Version

**Current**: Angular 19.2.0
**CLI**: @angular/cli 19.2.23
**Compatibility**:
- Workflow assumes `ng build` command available
- Workflow assumes build output in `dist/innoventity.client/` (configurable via angular.json)
- Major version upgrades (Angular 20+) may require workflow changes (e.g., new build commands)

---

### Azure SWA Action Version

**Current**: `Azure/static-web-apps-deploy@v1`
**Stability**: v1 is stable (LTS), breaking changes require major version bump (v2)
**Pinning Strategy**: Use `@v1` tag (auto-updates to latest v1.x.y, no breaking changes)

---

## Contract Validation

### How to Verify This Contract

| Contract Element | Validation Method |
|------------------|-------------------|
| **Trigger paths** | Push to backend code → workflow should NOT trigger |
| **Secrets required** | Remove `AZURE_STATIC_WEB_APPS_API_TOKEN` → workflow should fail with clear error |
| **Build timeout** | Introduce infinite loop in build → workflow should timeout at 10 minutes |
| **Test failure** (Phase 2+) | Break a test → workflow should fail at test job |
| **Deployment failure** | Use invalid deployment token → deploy job should fail with `InvalidToken` |
| **Validation failure** | Deploy broken build (missing index.html) → validate job should fail with HTTP 404 |
| **Approval timeout** | Trigger production deploy, wait 24 hours → workflow should auto-fail |
| **Approval rejection** | Reject production approval → workflow should fail immediately |
| **Preview quota** | Create 4 PRs simultaneously → 4th deployment should fail with `QuotaExceeded` |
| **Caching** | Delete cache, run workflow → build should take ~2 min. Re-run → build should take ~30 sec |

---

## Contract Change Policy

### Breaking Changes

A change is **breaking** if it:
- Removes or renames a required secret
- Changes workflow name (breaks external references)
- Changes output format (breaks downstream consumers)
- Removes a trigger type (e.g., removes `workflow_dispatch`)
- Changes branch protection requirements

**Process for Breaking Changes**:
1. Document change in this contract
2. Increment major version (e.g., `v1 → v2`)
3. Communicate to all workflow consumers (team notification)
4. Provide migration guide (e.g., "Secret renamed: update GitHub settings")

---

### Non-Breaking Changes (Additive)

**Acceptable without notification**:
- Adding new optional inputs (with defaults)
- Adding new job outputs
- Adding new jobs (if existing behavior unchanged)
- Performance optimizations (caching, parallelization)
- Dependency updates (Node.js version, Angular CLI version)

---

## Related Contracts

- **Infrastructure Contract**: `infrastructure/modules/static-web-app/README.md` (Terraform module inputs/outputs)
- **API Contract**: `specs/001-platform-core/contracts/openapi.yaml` (Backend API contract that frontend consumes)
- **SWA Configuration Contract**: `src/Innoventity.Client/staticwebapp.config.json` (Azure SWA routing rules)

---

**Contract Version**: 1.0.0
**Last Updated**: April 12, 2026
**Next Review**: After first production deployment
