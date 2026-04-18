# Research: Frontend CI/CD Automation

**Feature**: Frontend CI/CD Automation
**Branch**: `002-frontend-cicd`
**Date**: April 12, 2026
**Purpose**: Resolve unknowns and establish best practices before implementation

---

## Research Tasks

### 1. GitHub Actions for Angular Deployment

**Question**: What are the best practices for Angular build/test/deploy in GitHub Actions?

**Findings**:

**Build Optimization**:

- Use `actions/setup-node@v4` with `cache: 'npm'` to cache `~/.npm` directory (reduces `npm ci` from ~2min to ~30s on cache hit)
- Pin Node.js version to LTS (18.x or 20.x) for reproducibility
- Use `npm ci` instead of `npm install` for lockfile-based deterministic installs
- Angular production build with `ng build --configuration production` generates optimized bundle in `dist/` folder
- Upload `dist/` folder as GitHub Actions artifact for reuse in deployment jobs (avoids rebuilding)

**Test Execution**:

- Run Jest tests with `npm test -- --ci --coverage` (CI mode disables watch, generates coverage reports)
- Playwright E2E tests require `npx playwright install --with-deps` to install browser binaries
- Use `continue-on-error: true` for Phase 1 test warnings (allows deployment despite failures), remove for Phase 2+ enforcement
- Upload test results as artifacts for debugging (JUnit XML, coverage reports, Playwright traces)

**Dependency Management**:

- Use `actions/cache@v4` with `~/.npm` path and `hashFiles('**/package-lock.json')` key for npm cache
- `npm audit` should run but not block (security vulnerabilities addressed separately, not deployment blockers)

**Workflow Triggers**:

- `push: paths: ['src/Innoventity.Client/**']` limits triggers to frontend changes only (avoids unnecessary runs on backend changes)
- `workflow_dispatch` enables manual deployments with environment parameter

**Decision**: Follow standard Node.js + Angular CI/CD pattern with npm caching, separate build/test/deploy jobs, and artifact reuse.

**Alternatives Considered**:

- **Monolithic single job** (build+test+deploy in one step) — Rejected: harder to debug failures, can't reuse build artifacts, slower feedback
- **Matrix strategy for multiple Node versions** — Deferred to Phase 2+: adds complexity, LTS single version sufficient for Phase 1

---

### 2. Azure Static Web Apps Deployment

**Question**: How should deployment to Azure SWA be structured for preview vs. production?

**Findings**:

**Official Action**: Use `Azure/static-web-apps-deploy@v1` (Microsoft-maintained)

- **Inputs**:
  - `azure_static_web_apps_api_token`: Deployment token from Terraform output (stored as GitHub secret)
  - `repo_token`: `${{ secrets.GITHUB_TOKEN }}` for PR comment posting
  - `action`: `'upload'` for deployment, `'close'` for preview cleanup
  - `app_location`: `'/'` (repository root, since dist/ is uploaded separately)
  - `app_artifact_location`: `'dist/innoventity.client'` (Angular build output path)
  - `skip_app_build`: `true` (build already done in build job, upload prebuilt artifacts only)
  - `production_branch`: `'Main'` (triggers production deployment, otherwise creates preview)
- **Outputs**:
  - `static_web_app_url`: Deployed URL (production or preview)

**Preview Environments**:

- SWA automatically creates preview environment for PRs when `production_branch` is set and current branch ≠ production
- Preview URL format: `https://<swa-default-hostname>-<pr-number>-<region>.azurestaticapps.net`
- Action posts preview URL as PR comment automatically
- Cleanup: Run action with `action: 'close'` on PR closure (or SWA auto-deletes after 7 days)

**Production Deployment**:

- Set `deployment_environment: 'production'` in workflow (NOT a slot swap—SWA uses single production environment)
- Use GitHub Environment protection rules for approval gate (Settings → Environments → production → Required reviewers)
- Approval timeout configured at environment level (default 30 days, spec requires 24 hours)

**Health Validation**:

- After deployment, use `curl` or `Invoke-WebRequest` to validate `<static_web_app_url>` returns HTTP 200
- Check `Content-Type: text/html` to ensure SPA served (not JSON error)
- For production, validate custom domain after deployment: `https://innoventity-dev-web.azurestaticapps.net`

**Decision**: Use official Azure action with prebuilt artifacts, rely on SWA's native preview environment feature (no custom infrastructure), implement GitHub Environment approval gates for production.

**Alternatives Considered**:

- **Custom deployment script** (Azure CLI `az staticwebapp upload`) — Rejected: reinvents wheel, official action handles PR comments and preview URLs automatically
- **Staging slot + swap** — Not applicable: SWA Free tier doesn't support staging slots (Standard tier feature), and deployment is direct-to-production after approval
- **separate preview/production workflows** — Rejected: adds duplication, single workflow with conditional logic is cleaner

---

### 3. Terraform azurerm_static_web_app Best Practices

**Question**: How to structure Terraform module for Azure Static Web Apps provisioning?

**Findings**:

**Resource Configuration**:

```hcl
resource "azurerm_static_web_app" "main" {
  name                = "innoventity-${var.environment}-web"
  resource_group_name = var.resource_group_name
  location            = var.location
  sku_tier            = var.sku_tier  # "Free" or "Standard"
  sku_size            = var.sku_tier  # Same as tier for SWA

  # No app_settings block in Free tier (Standard only)
  # No identity block needed (GitHub Actions uses deployment token, not managed identity)
}
```

**Key Outputs**:

- `default_host_name` (string): SWA default URL `<name>.azurestaticapps.net`
- `api_key` (string, sensitive): Deployment token for GitHub Actions authentication
- `id` (string): Resource ID for dependencies or diagnostic settings

**Integration Pattern**:

- Module should NOT create GitHub secret directly (Terraform has no azuredevops/github provider configured)
- Output `api_key` as sensitive value; manual step or separate workflow sets GitHub secret
- Integrate into `environments/dev/core/main.tf` alongside `app-service` and `monitoring` modules

**Tags**:

```hcl
tags = {
  Environment = var.environment
  ManagedBy   = "Terraform"
  Component   = "Frontend"
  Project     = "Innoventity"
}
```

**State Management**:

- Use existing `azurerm` backend (same as other modules): `storage_account_name = "innoventitydevtfstate"`, `container_name = "tfstate"`, `key = "dev-core.tfstate"`
- State locking via Azure Storage lease (automatic)

**Free Tier Limitations**:

- 1 production environment + 3 preview environments (quota limit)
- 100 GB bandwidth/month (sufficient for dev)
- No custom domains (Standard tier feature) — use default `.azurestaticapps.net` subdomain
- No app settings or environment variables (runtime config via Angular environment files)

**Decision**: Create `infrastructure/modules/static-web-app/` module with minimal configuration (name, location, SKU), output deployment token, integrate into dev/core environment. Manual GitHub secret creation as post-apply step (documented in quickstart).

**Alternatives Considered**:

- **Provision via Azure Portal** — Rejected: violates infrastructure-as-code principle, not reproducible
- **Use azuredevops provider for GitHub secret** — Deferred to Phase 2+: requires additional provider configuration and credentials, manual secret creation acceptable for Phase 1
- **Standard tier for advanced features** — Rejected: Free tier sufficient for dev environment, cost optimization

---

### 4. Test Validation Strategy (Principle 5 Requirement)

**Question**: How to ensure tests have epistemic validity before trusting CI/CD workflow?

**Findings**:

**GitHub Actions Workflow Tests**:

- **Local execution**: Use `act` (nektos/act) to run workflows locally before pushing
  - Install: `choco install act` (Windows) or `brew install act` (macOS) or `curl -s https://raw.githubusercontent.com/nektos/act/master/install.sh | sudo bash` (Linux)
  - Run: `act push -W .github/workflows/deploy-frontend.yml --secret-file .secrets` (simulates push event)
  - Limitations: May not perfectly replicate GitHub's hosted runner environment, but catches syntax errors and basic logic
- **Observe failures**: Intentionally break workflow (e.g., invalid Node version, wrong command) and verify failure detection
- **Characterization testing**: After initial workflow is working, delete key steps (e.g., remove `npm ci`) and verify workflow fails as expected

**Infrastructure Tests (Terraform)**:

- **Pester tests** for Terraform outputs:
  - Test `static_web_app_default_hostname` output exists and matches pattern `*.azurestaticapps.net`
  - Test `static_web_app_api_key` output is non-empty and marked sensitive
  - Test SWA resource tags include required keys (Environment, ManagedBy, Component)
- **Terraform plan validation**: Run `terraform plan` and verify output shows SWA resource creation (observe before `apply`)
- **Post-deployment validation**: After first deployment, manually delete SWA resource in Azure Portal and run `terraform plan` — should detect drift

**Deployment Validation Tests**:

- **Staging health check**: After deploy-preview job, validate staging URL returns HTTP 200 and `Content-Type: text/html`
- **Production health check**: After production deployment, validate production URL serves new version (check build timestamp in HTML or API version endpoint)
- **Failure simulation**: Deploy intentionally broken build (e.g., missing `index.html`) and verify validation job fails before production approval

**Red-Green-Refactor Workflow**:

1. **Red**: Write workflow with intentional error (e.g., `npm cii` typo), push, observe failure
2. **Green**: Fix error (`npm ci`), push, observe success
3. **Refactor**: Optimize (add caching), verify still succeeds

**Decision**: Use `act` for local workflow testing, implement Pester tests for Terraform outputs, add health check validation jobs, follow red-green-refactor for initial workflow development. All tests must be observed failing before trusting.

**Alternatives Considered**:

- **Trust workflows without validation** — Rejected: violates Principle 5 (tests must prove epistemic value)
- **Only validate in production** — Rejected: too risky, validation must happen in CI before production
- **Skip Terraform tests** — Rejected: infrastructure failures are as critical as application failures

---

### 5. Approval Gate Implementation

**Question**: How to implement 24-hour manual approval gate for production deployments?

**Findings**:

**GitHub Environments**:

- Create Environment named `production` (Settings → Environments → New environment)
- Configure protection rules:
  - **Required reviewers**: Add repository owner and/or designated operators (max 6 reviewers)
  - **Wait timer**: 0 minutes (no automatic delay before approval gate, just pause for manual review)
  - **Deployment branches**: Limit to `Main` branch only (prevents production deployments from feature branches)
- **Approval timeout**: Not configurable at environment level — implemented via workflow `timeout-minutes` setting on production deployment job

**Workflow Configuration**:

```yaml
jobs:
  deploy-production:
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/Main'
    needs: [build, test, deploy-preview]
    environment:
      name: production
      url: https://innoventity-dev-web.azurestaticapps.net
    timeout-minutes: 1440  # 24 hours = 1440 minutes
    steps:
      - name: Deploy to production
        uses: Azure/static-web-apps-deploy@v1
        # ...
```

**Approval Flow**:

1. Workflow reaches `deploy-production` job
2. GitHub pauses execution and shows "Waiting for approval" in UI
3. Email sent to required reviewers
4. Reviewer visits Actions tab, reviews staging deployment, clicks "Approve deployment" or "Reject deployment"
5. If approved within 24 hours, job continues; if rejected or timeout, workflow fails

**Notifications**:

- Approval requests sent via GitHub email notifications (native, no Slack required for Phase 1)
- Requester notified when approved/rejected
- Workflow status visible in GitHub mobile app (reviewers can approve from mobile)

**Bypass for Emergencies**:

- Repository owner can force-approve (if configured as required reviewer)
- For emergency hotfixes, consider separate workflow with expedited approval (Phase 2+ enhancement)

**Decision**: Use GitHub Environments with production protection rules, set `timeout-minutes: 1440` (24 hours) on deployment job, rely on GitHub native email notifications.

**Alternatives Considered**:

- **Manual workflow_dispatch approval** (run workflow, wait, manually trigger second workflow) — Rejected: clunky UX, no audit trail
- **Slack approval bot** — Deferred to Phase 2+: adds complexity, GitHub native approval sufficient for Phase 1
- **Auto-approve after time delay** — Rejected: approval must be active decision, not passive timeout

---

### 6. Preview Environment Cleanup Strategy

**Question**: How to prevent preview environment quota exhaustion (Free tier: 3 concurrent previews)?

**Findings**:

**Automatic Cleanup**:

- Azure SWA automatically deletes preview environments after **7 days of inactivity** (no new deployments to that preview)
- Preview environments deleted automatically when PR is **closed** (if workflow includes cleanup step)

**Manual Cleanup Workflow**:

```yaml
on:
  pull_request:
    types: [closed]

jobs:
  cleanup-preview:
    runs-on: ubuntu-latest
    steps:
      - uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          action: 'close'
```

**Quota Monitoring**:

- Azure Portal → Static Web Apps → Overview → "Environments" tab shows active preview environments
- If quota exhausted (3/3 previews), new PR deployments fail with quota error
- Solution: Manually delete stale previews via Azure Portal or close old PRs

**Best Practices**:

- Encourage developers to close/merge PRs promptly (don't leave 10 open PRs)
- Add PR cleanup workflow (above) to auto-delete on PR closure
- Document manual cleanup procedure in runbook: `az staticwebapp environment list --name <swa> --resource-group <rg>` then `az staticwebapp environment delete --name <env>`

**Decision**: Implement automatic cleanup on PR closure, document manual cleanup for emergencies, monitor quota usage in Azure Portal. Free tier quota (3 previews) is sufficient for small team if PRs are closed promptly.

**Alternatives Considered**:

- **Upgrade to Standard tier** (unlimited preview environments) — Rejected: unnecessary cost (~$9/month) for dev environment with 3-5 developers
- **No cleanup** (rely on 7-day auto-deletion) — Rejected: risk of quota exhaustion if many PRs open simultaneously
- **Scheduled cleanup** (delete all previews >48 hours old) — Rejected: too aggressive, reviewers need time to test

---

## Summary of Decisions

| Topic | Decision | Rationale |
|-------|----------|-----------|
| **GitHub Actions pattern** | Separate build/test/deploy jobs with artifact reuse | Standard CI/CD pattern, fast feedback, debuggable failures |
| **SWA deployment** | Use `Azure/static-web-apps-deploy@v1` with prebuilt artifacts | Official action, handles PR comments and preview URLs automatically |
| **Preview environments** | Use SWA native preview feature (no custom infrastructure) | Leverages platform capability, no additional infrastructure needed |
| **Production deployment** | GitHub Environment with approval gate, 24-hour timeout | Native GitHub feature, audit trail, email notifications built-in |
| **Terraform module** | Minimal module with sensitive API key output, manual GitHub secret creation | Infrastructure-as-code for reproducibility, manual secret acceptable for Phase 1 |
| **Test validation** | Use `act` locally, Pester for Terraform, health checks in workflow, red-green-refactor | Ensures epistemic validity (Principle 5), validates tests before trusting |
| **Preview cleanup** | Automatic on PR closure, manual fallback documented | Prevents quota exhaustion, balances automation with manual control |
| **Node.js version** | Pin to LTS 20.x (or 18.x) | Reproducibility, aligns with Angular 19 requirements |
| **Test enforcement** | Phase 1: `continue-on-error: true`, Phase 2+: blocking | Pragmatic for test stabilization, enforced once tests reliable |

---

## Next Steps (Phase 1 Design)

1. **Generate data-model.md**: Define key entities (Workflow, Environment, Deployment, Preview, etc.) with their states and relationships
2. **Define contracts/** (workflow-schema.md): Document GitHub Actions workflow inputs, outputs, expected behavior, error codes
3. **Generate quickstart.md**: Step-by-step guide for first-time setup (Terraform apply, GitHub secret creation, first deployment)
4. **Update agent context**: Add technologies (GitHub Actions, Azure SWA, Terraform azurerm_static_web_app) to `.aitk/agent-context.copilot.md`

---

**Research Complete**: April 12, 2026
**Next Command**: Continue to Phase 1 Design Artifacts
