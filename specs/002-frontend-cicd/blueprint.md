# Blueprint: Frontend CI/CD Automation

**Branch**: `002-frontend-cicd` | **Date**: May 4, 2026
**Mode**: doc-only
**Total Tasks**: 35 remaining | **Files**: 1 new, 4 modified, 0 deleted

---

## Key Decisions

- `deploy-production` uses `github.event_name == 'push' && github.ref == 'refs/heads/Main'` rather than a bare ref check to prevent the job from triggering on `pull_request` events targeting Main (open/sync/reopen). Only the push event fired after a PR merge should start production deployment → T049
- `validate` uses `always()` combined with URL-selection logic so it works for both PR (preview) and Main-push (production) deployments. Since only one deploy job runs per workflow execution, the other job's output is an empty string, and the URL-selection `if` picks the correct one → T057, T058, T059, T060, T061
- `continue-on-error: true` appears on three separate steps in the `test` job (Jest, Playwright install, Playwright E2E). All three must be removed in T068 so test failures block deployment, satisfying FR-009 Phase 2 enforcement → T068
- T069 (npm caching) is pre-completed: `actions/setup-node@v6` with `cache: 'npm'` and `cache-dependency-path` is already present in both `build` and `test` jobs in the current `deploy-frontend.yml` → T069
- GitHub Environment `production` must be created before workflow references it (T044–T048 before T049). An `environment:` key referencing a non-existent GitHub Environment will fail at job dispatch, not at parse time → T044, T049
- The `deploy-frontend.yml` file is modified in three separate phases (T049–T051, T057–T061, T068). A final consolidated version is provided in the appendix at the end of Phase 8 → Appendix

---

## Implementation Order

```
T032 ─── (independent: act local test, Phase 4 straggler)
T043 ─── (independent: cleanup manual test, Phase 5 straggler)

T044 → T045 → T046 → T047 → T048   (sequential: GitHub Environment CLI/UI setup)
                                       ↓
                               T049 → T050 → T051  (workflow: add deploy-production job)
                                                   ↓
                                       T052 → T053 → T054 → T055 → T056  (manual tests)

T057 → T058 → T059 → T060 → T061   (workflow: add validate job, after T049–T051)
                                       ↓
                               T062 → T063 → T064  (manual tests)

T065 ─── (independent: new runbook file)
T066 ─── (independent: infrastructure/README.md update)
T067 ─── (independent: README.md badge)
T068 ─── (workflow: remove continue-on-error; independent of T049–T061)
T073 ─── (independent: copilot-instructions.md CI/CD patterns)
T070 → T071 → T072  (manual verification, after all file changes)
T074 ─── (final commit, last)
```

---

## Phase 4 Straggler: User Story 1 — Automated Deployment

### T032: Test workflow locally using act

**File**: (none — command task)

**Requirements**: FR-001, FR-003

**Dependencies**: T031 (workflow exists)

Run the `act` tool from the repository root to validate the `push` trigger path without pushing to GitHub. This satisfies Principle 5 — observe the workflow failure path before trusting the success path.

```bash
# Install act if not present
# https://github.com/nektos/act
brew install act      # macOS
choco install act-cli  # Windows

# Dry-run the push event through the build and test jobs
act push \
  -W .github/workflows/deploy-frontend.yml \
  --secret-file .secrets \
  --dry-run

# Full local run (requires Docker)
act push \
  -W .github/workflows/deploy-frontend.yml \
  --secret AZURE_STATIC_WEB_APPS_API_TOKEN=<token-from-terraform>

# Expected output:
# [Deploy Frontend/Build] ✅ ...
# [Deploy Frontend/Test]  ✅ ...
# [Deploy Frontend/Deploy Preview] ⏭️ skipped (push event, not pull_request)
```

**Verification**: `act` exits 0; build and test jobs complete; deploy-preview is correctly skipped for the push event type.

---

## Phase 5 Straggler: User Story 2 — PR Preview Environments

### T043: Test cleanup — close PR and verify preview deleted

**File**: (none — manual test task)

**Requirements**: FR-037, SC-009

**Dependencies**: T042 (cleanup job implemented), open PR with active preview

1. Navigate to https://github.com/khaledm/innoventity-prototype-redux/pulls and identify the active PR with a live preview.
2. Close (or merge) the PR.
3. Observe the `cleanup-preview` job run in GitHub Actions (Actions tab → Deploy Frontend → most recent run).
4. Verify the `Azure/static-web-apps-deploy@v1` `action: close` step completes successfully.
5. Attempt to access the former preview URL — the Azure SWA CDN should return 404 within seconds.

**Verification**: Preview URL returns 404 immediately after cleanup job completes. GitHub Actions bot posts no new preview URL comment on the closed PR.

---

## Phase 6: User Story 3 — Production Deployment with Approval Gate

### T044: Create GitHub Environment via GitHub CLI

**File**: (none — CLI task)

**Requirements**: FR-015, FR-040, FR-041

**Dependencies**: T031 (workflow file exists)

```bash
# Create the production environment (idempotent — safe to re-run)
gh api \
  repos/khaledm/innoventity-prototype-redux/environments/production \
  -X PUT \
  -f wait_timer=0

# Verify environment was created
gh api repos/khaledm/innoventity-prototype-redux/environments \
  | jq '.environments[].name'
# Expected: "production" appears in output
```

**Verification**: `gh api repos/khaledm/innoventity-prototype-redux/environments | jq` lists `"production"`.

---

### T045: Configure production environment protection rules via GitHub UI

**File**: (none — UI task)

**Requirements**: FR-015, FR-040, FR-041

**Dependencies**: T044

1. Navigate to: **GitHub → Repository → Settings → Environments → production**
2. Under **Deployment protection rules**, check **Required reviewers**.
3. Under **Deployment branches and tags**, select **Selected branches and tags**, click **Add deployment branch or tag rule**, set **Ref type** = Branch and **Name pattern** = `Main`.
4. Save the protection rules.

**Verification**: Navigate to Settings → Environments → production and confirm: required reviewers is ON, deployment branch is `Main`.

---

### T046: Add required reviewers to production environment

**File**: (none — UI task)

**Requirements**: FR-040

**Dependencies**: T045

1. In **Settings → Environments → production → Required reviewers**.
2. Search for your GitHub username (`khaledm`) and add as a required reviewer.
3. Save changes.

**Verification**: The reviewer appears in the Required reviewers list.

---

### T047: Set deployment branches to Main only

**File**: (none — already completed as part of T045)

**Requirements**: FR-047 (implied — restrict production deploys to Main)

**Dependencies**: T045

This was completed as part of T045 (Deployment branches pattern = `Main`). No additional action required.

**Verification**: Settings → Environments → production → Deployment branches shows pattern `Main`.

---

### T048: Configure approval timeout to 1440 minutes (24 hours)

**File**: (none — UI task)

**Requirements**: FR-041

**Dependencies**: T044

The wait timer is set in Settings → Environments → production:

1. Under **Deployment protection rules**, configure **Wait timer** to `1440` minutes.
2. Save.

Alternatively via CLI:

```bash
gh api \
  repos/khaledm/innoventity-prototype-redux/environments/production \
  -X PUT \
  -f wait_timer=1440 \
  --jq '.name + " wait_timer=" + (.wait_timer | tostring)'
# Expected: "production wait_timer=1440"
```

**Verification**: CLI or UI shows `wait_timer=1440`.

---

### T049: Define deploy-production job with conditional execution

**File**: `.github/workflows/deploy-frontend.yml` (modify)

**Requirements**: FR-014, FR-015, FR-016, FR-017

**Dependencies**: T044–T048 (production environment must exist)

**Before** (~line 192, after deploy-preview outputs block ends):

```yaml
          # production_branch tells SWA which branch maps to the production environment.
          # All other branches (including this one) are routed to preview environments.
          production_branch: Main

  # ─────────────────────────────────────────────────────────────────
  # 4. Cleanup Preview — delete SWA preview environment on PR close/merge
  #    Triggered by pull_request: [closed] to prevent quota exhaustion.
  #    Immediate deletion aligns with FR-037 (Q1 decision: delete on close).
  # ─────────────────────────────────────────────────────────────────
  cleanup-preview:
    name: Cleanup Preview
```

**After**:

```yaml
          # production_branch tells SWA which branch maps to the production environment.
          # All other branches (including this one) are routed to preview environments.
          production_branch: Main

  # ─────────────────────────────────────────────────────────────────
  # 4. Deploy Production — deploy directly to production environment
  #    Main branch push (including PR merge) only. Requires manual approval
  #    via GitHub Environment "production" protection rule (FR-015, FR-041).
  #    24-hour approval timeout — auto-fails if not approved (FR-041).
  #    Uses event_name == 'push' to avoid triggering on pull_request events
  #    targeting Main (open/sync/reopen emit pull_request, not push).
  # ─────────────────────────────────────────────────────────────────
  deploy-production:
    name: Deploy Production
    runs-on: ubuntu-latest
    needs: [build, test]
    if: github.event_name == 'push' && github.ref == 'refs/heads/Main'
    environment: production
    outputs:
      production-url: ${{ steps.deploy.outputs.static_web_app_url }}
    steps:
      - name: Checkout repository
        uses: actions/checkout@v6

      - name: Download build artifact
        uses: actions/download-artifact@v8
        with:
          name: frontend-build
          path: dist

      - name: Deploy to Azure Static Web Apps production
        id: deploy
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: upload
          app_location: dist
          skip_app_build: true
          output_location: ''
          production_branch: Main

  # ─────────────────────────────────────────────────────────────────
  # 5. Cleanup Preview — delete SWA preview environment on PR close/merge
  #    Triggered by pull_request: [closed] to prevent quota exhaustion.
  #    Immediate deletion aligns with FR-037 (Q1 decision: delete on close).
  # ─────────────────────────────────────────────────────────────────
  cleanup-preview:
    name: Cleanup Preview
```

**Verification**: Push a commit directly to `Main` (or merge a PR). Workflow pauses at the `deploy-production` job with a "Waiting for approval" banner. No deployment to production occurs until approved.

---

### T050: Add environment: production to deploy-production job configuration

**File**: `.github/workflows/deploy-frontend.yml` (modify)

**Requirements**: FR-015

**Dependencies**: T049

`environment: production` is included in the T049 code block above (line `environment: production` immediately after the `if:` condition). No additional file change is required.

**Verification**: The `deploy-production` job definition in the workflow file contains `environment: production`.

---

### T051: Implement deploy-production job steps

**File**: `.github/workflows/deploy-frontend.yml` (modify)

**Requirements**: FR-016, FR-017, FR-018

**Dependencies**: T049, T050

The three steps (checkout, download-artifact, deploy) are included in the T049 code block above. No additional file change is required beyond what T049 establishes.

**Verification**: After approval, the workflow runs all three steps: checkout, download-artifact, and `Azure/static-web-apps-deploy@v1 action: upload`. The job output `production-url` is populated with the live production URL.

---

### T052: Test approval workflow — merge feature branch to Main

**File**: (none — manual test task)

**Requirements**: SC-003

**Dependencies**: T049–T051, T044–T048

Push a change to `src/Innoventity.Client/` on the `002-frontend-cicd` branch and open a PR to `Main`, or push directly to `Main` if branch protections allow.

```bash
# Option A: Push direct to Main (if branch protection allows)
git checkout Main
echo "# test" >> src/Innoventity.Client/README.md
git add . && git commit -m "test: trigger production approval gate"
git push

# Option B: Merge existing PR
gh pr merge 6 --squash
```

**Verification**: GitHub Actions runs the `Deploy Frontend` workflow. The `deploy-production` job shows status "Waiting for approval" in the workflow visualization.

---

### T053: Verify workflow pauses at production approval gate

**File**: (none — observation task)

**Requirements**: FR-015, SC-003, AC-1 (US3)

**Dependencies**: T052

Navigate to: **GitHub → Actions → Deploy Frontend → most recent run**

Expected state: `build` ✅, `test` ✅, `deploy-preview` ⏭️ (skipped), `deploy-production` ⏸️ (awaiting approval). The job card shows a **Review deployments** button.

**Verification**: The `deploy-production` job card shows the approval banner with reviewer name and approve/reject buttons. No deployment has occurred to production yet.

---

### T054: Approve deployment via GitHub UI and verify production deployment completes

**File**: (none — manual approval task)

**Requirements**: FR-015, FR-017, SC-003, AC-3 (US3)

**Dependencies**: T053

1. Click **Review deployments** in the workflow run.
2. Check the `production` environment checkbox.
3. Click **Approve and deploy**.
4. Observe the `deploy-production` job run to completion (green ✅).

**Verification**: `deploy-production` job completes with exit 0. Output `production-url` is set to `https://innoventity-dev-web.azurestaticapps.net`.

---

### T055: Verify production URL serves new frontend version

**File**: (none — verification task)

**Requirements**: FR-018, SC-003, AC-4 (US3)

**Dependencies**: T054

```bash
# Health check
curl -s -o /dev/null -w "HTTP %{http_code}\n" \
  https://innoventity-dev-web.azurestaticapps.net
# Expected: HTTP 200

# Verify Angular app loads
curl -s https://innoventity-dev-web.azurestaticapps.net \
  | head -5
# Expected: <!DOCTYPE html> ... <title>InnoventityClient</title>
```

**Verification**: Production URL returns HTTP 200 with `Content-Type: text/html`. The Angular app is visible in a browser.

---

### T056: Test approval rejection — trigger workflow, reject approval, verify no deployment

**File**: (none — manual test task)

**Requirements**: FR-015, AC-5 (US3)

**Dependencies**: T044–T048, T049–T051

1. Trigger the `Deploy Frontend` workflow (push to Main or use workflow_dispatch if configured for production path).
2. When the `deploy-production` job shows the approval banner, click **Review deployments**.
3. Select **Reject** with reason "Testing rejection path".
4. Observe the workflow.

**Verification**: `deploy-production` job fails (red ✗) with message "Deployment was rejected". The production URL at `https://innoventity-dev-web.azurestaticapps.net` continues serving the previous version. Workflow overall status is failure.

---

## Phase 7: User Story 5 — Validation and Health Checks

### T057: Define validate job in workflow with dependencies on deploy jobs

**File**: `.github/workflows/deploy-frontend.yml` (modify)

**Requirements**: FR-003, SC-001, SC-007

**Dependencies**: T049–T051 (deploy-production job must exist to be listed in needs)

The `validate` job must be inserted after `deploy-production` and before `cleanup-preview`. The `needs: [deploy-preview, deploy-production]` with `always()` ensures the job runs after whichever deploy job succeeded, while skipping if both were skipped.

**Before** (~line after deploy-production job ends, before cleanup-preview comment):

```yaml
      - name: Deploy to Azure Static Web Apps production
        id: deploy
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: upload
          app_location: dist
          skip_app_build: true
          output_location: ''
          production_branch: Main

  # ─────────────────────────────────────────────────────────────────
  # 5. Cleanup Preview — delete SWA preview environment on PR close/merge
```

**After**:

```yaml
      - name: Deploy to Azure Static Web Apps production
        id: deploy
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: upload
          app_location: dist
          skip_app_build: true
          output_location: ''
          production_branch: Main

  # ─────────────────────────────────────────────────────────────────
  # 5. Validate — HTTP health check on the deployed URL (FR-018, SC-007).
  #    Runs after whichever deploy job succeeded (preview XOR production).
  #    always() + result checks allow the job to run even if the other
  #    deploy job was skipped (GitHub treats skipped as non-failure).
  #    3 retry attempts with 10-second delay handle CDN propagation lag.
  #    Fails workflow on non-200 or non-text/html response, preventing
  #    silent broken deployments from reaching the approval gate.
  # ─────────────────────────────────────────────────────────────────
  validate:
    name: Validate Deployment
    runs-on: ubuntu-latest
    needs: [deploy-preview, deploy-production]
    if: |
      always() &&
      (needs.deploy-preview.result == 'success' || needs.deploy-production.result == 'success')
    steps:
      - name: Determine deployment URL
        id: url
        run: |
          PROD_URL="${{ needs.deploy-production.outputs.production-url }}"
          PREV_URL="${{ needs.deploy-preview.outputs.preview-url }}"
          if [ -n "$PROD_URL" ]; then
            echo "url=$PROD_URL" >> $GITHUB_OUTPUT
            echo "Validating production URL: $PROD_URL"
          else
            echo "url=$PREV_URL" >> $GITHUB_OUTPUT
            echo "Validating preview URL: $PREV_URL"
          fi

      - name: Health check with retry (3 × 10s)
        run: |
          URL="${{ steps.url.outputs.url }}"
          SUCCESS=0
          for ATTEMPT in 1 2 3; do
            HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" --max-time 30 "$URL")
            CONTENT_TYPE=$(curl -s -o /dev/null -w "%{content_type}" --max-time 30 "$URL")
            echo "Attempt $ATTEMPT/3: HTTP $HTTP_STATUS | Content-Type: $CONTENT_TYPE | URL: $URL"
            if [ "$HTTP_STATUS" = "200" ] && echo "$CONTENT_TYPE" | grep -qi "text/html"; then
              echo "✅ Validation passed on attempt $ATTEMPT."
              SUCCESS=1
              break
            fi
            if [ $ATTEMPT -lt 3 ]; then
              echo "Retrying in 10 seconds..."
              sleep 10
            fi
          done
          if [ $SUCCESS -eq 0 ]; then
            echo "::error::Deployment validation failed after 3 attempts. URL: $URL returned HTTP $HTTP_STATUS (Content-Type: $CONTENT_TYPE). Expected: HTTP 200, text/html."
            exit 1
          fi

  # ─────────────────────────────────────────────────────────────────
  # 6. Cleanup Preview — delete SWA preview environment on PR close/merge
```

**Verification**: After a PR deploy, the `validate` job runs with `URL=<preview-url>`, makes an HTTP GET, and reports `HTTP 200` in the logs. The overall workflow succeeds.

---

### T058: Implement validation job HTTP GET request

**File**: `.github/workflows/deploy-frontend.yml` (modify)

**Requirements**: FR-058 (implied: send GET to deployment URL)

**Dependencies**: T057

Implemented within the T057 code block above. The `curl -s -o /dev/null -w "%{http_code}"` and `curl -s -o /dev/null -w "%{content_type}"` commands in the `Health check with retry` step constitute the HTTP GET implementation.

**Verification**: Workflow logs show `Attempt 1/3: HTTP 200 | Content-Type: text/html...` after a successful deployment.

---

### T059: Add validation checks — HTTP status 200 and Content-Type text/html

**File**: `.github/workflows/deploy-frontend.yml` (modify)

**Requirements**: FR-059 (implied: status must be 200; content-type must be text/html)

**Dependencies**: T058

Implemented within the T057 code block. The condition `[ "$HTTP_STATUS" = "200" ] && echo "$CONTENT_TYPE" | grep -qi "text/html"` enforces both checks. Both must pass on the same attempt for validation to succeed.

**Verification**: Artificially respond with a different content type (e.g., a broken route returning JSON) and confirm the validation job reports failure.

---

### T060: Add retry logic — 3 attempts with 10-second delay

**File**: `.github/workflows/deploy-frontend.yml` (modify)

**Requirements**: FR-060 (implied: retry logic for CDN propagation, 3 × 10s)

**Dependencies**: T059

Implemented within the T057 code block. The `for ATTEMPT in 1 2 3; do ... sleep 10 ... done` loop provides exactly 3 attempts with a 10-second delay between failures. If attempt 3 fails, the loop exits and `SUCCESS=0` triggers the error annotation.

**Verification**: Workflow logs show all three attempt lines on failure before the `::error::` annotation.

---

### T061: Configure validation job to fail workflow on check failure

**File**: `.github/workflows/deploy-frontend.yml` (modify)

**Requirements**: FR-061 (implied: workflow fails on validation failure)

**Dependencies**: T060

Implemented within the T057 code block. `exit 1` after the `::error::` annotation causes the step to fail, which fails the `validate` job, which fails the overall workflow. The `::error::` annotation also creates a visible error annotation in the GitHub Actions summary.

**Verification**: When the validation fails, the workflow run shows a red ✗ overall status, and the error annotation is visible in the Actions summary panel.

---

### T062: Test validation failure — deploy intentionally broken build

**File**: (none — manual test task)

**Requirements**: SC-007, Principle 5 (observe the test failing)

**Dependencies**: T057–T061

**Critical — Principle 5 compliance**: The validate job must be observed failing before it can be trusted.

```bash
# Temporarily rename index.html to break the SWA response
cd src/Innoventity.Client

# Modify staticwebapp.config.json to return 404 for all routes
# (temporary — revert immediately after test)
cat > staticwebapp.config.json << 'EOF'
{
  "routes": [
    {
      "route": "/*",
      "statusCode": 404
    }
  ]
}
EOF

git add staticwebapp.config.json
git commit -m "test: intentionally break validation for Principle 5 test"
git push
# Observe the validate job failing in GitHub Actions

# Immediately revert:
git revert HEAD --no-edit
git push
```

**Verification**: The `validate` job shows red ✗ with the error annotation: `Deployment validation failed after 3 attempts. URL: ... returned HTTP 404`. Workflow overall status is failure.

---

### T063: Verify validation job fails and prevents production approval

**File**: (none — observation task)

**Requirements**: SC-007, AC-1 (US5)

**Dependencies**: T062

When the `validate` job fails on a PR workflow run, the `deploy-production` job is not triggered (it only runs on push to Main, not PR events). The validation failure is visible in the PR checks, and a reviewer cannot see the validation passing before approving merge.

For Main-branch runs: when the `validate` job fails, the workflow is marked failed. The failure notification is sent via GitHub native notifications (email/UI/mobile) to workflow watchers.

**Verification**: PR checks show `validate` as failed. Workflow summary shows overall failure with the error annotation in red.

---

### T064: Test validation success — deploy working build

**File**: (none — observation task)

**Requirements**: SC-007, AC-5 (US5)

**Dependencies**: T063

After reverting the broken `staticwebapp.config.json` (T062 revert step), push a new commit to the feature branch. Open or re-sync the PR.

**Verification**: The `validate` job shows green ✅ in GitHub Actions. Logs show: `Attempt 1/3: HTTP 200 | Content-Type: text/html... ✅ Validation passed on attempt 1.` Overall workflow succeeds.

---

## Phase 8: Polish & Documentation

### Pre-completed Tasks

| Task | File | Status |
|------|------|--------|
| T069: Add npm dependency caching optimization | `.github/workflows/deploy-frontend.yml` | Already complete — `actions/setup-node@v6` with `cache: 'npm'` and `cache-dependency-path` already present in `build` and `test` jobs |

---

### T065: Create deployment runbook

**File**: `specs/002-frontend-cicd/runbooks/deployment.md` (new)

**Requirements**: FR-003 (operational runbook for completeness)

**Dependencies**: T049–T064 (runbook documents the fully implemented workflows)

```markdown
# Deployment Runbook: Frontend CI/CD

**Feature**: 002-frontend-cicd
**Last Updated**: May 4, 2026
**Pipeline**: `.github/workflows/deploy-frontend.yml`
**Production URL**: https://innoventity-dev-web.azurestaticapps.net

---

## Normal Workflow Overview

```text
Feature branch push → build → test → [skipped: no deploy on push]
PR open/sync/reopen → build → test → deploy-preview → validate → cleanup-preview (on close)
Push to Main (merge) → build → test → [approval gate] → deploy-production → validate
```

---

## Standard Operating Procedures

### SOP-1: Deploy a Change to Production

1. Push changes to a feature branch under `src/Innoventity.Client/**`.
2. Open a Pull Request to `Main` — preview environment is created automatically.
3. Share the preview URL (posted in PR comments by the GitHub bot) with reviewers.
4. Get PR approved and merge to `Main`.
5. Navigate to **GitHub → Actions → Deploy Frontend** — the workflow runs automatically.
6. The `deploy-production` job pauses at the approval gate.
7. Navigate to the workflow run — click **Review deployments**.
8. Confirm the preview deployment looks correct, then click **Approve and deploy**.
9. The `deploy-production` job runs and `validate` confirms HTTP 200.
10. Production URL `https://innoventity-dev-web.azurestaticapps.net` now serves the new version.

**Timeout**: If approval is not granted within 24 hours (1440 minutes), the workflow auto-fails. Re-run the workflow from GitHub Actions to restart the approval clock.

---

### SOP-2: Manually Trigger a Frontend Deployment

```bash
# Trigger build+test only (no deploy — workflow_dispatch does not fire production deploy)
gh workflow run deploy-frontend.yml \
  --ref Main \
  -f environment=dev

# Monitor
gh run list --workflow deploy-frontend.yml --limit 5
gh run watch  # interactive tail
```

---

### SOP-3: Clean Up a Stale Preview Environment

When the SWA Free tier 3-preview limit is reached, all new `deploy-preview` jobs will fail with quota error. To free a slot:

```bash
# List all open PRs with active previews
gh pr list --state open

# Close the stale PR (this triggers the cleanup-preview job automatically)
gh pr close <PR_NUMBER>

# Verify the preview environment was deleted
# Check GitHub Actions for the cleanup-preview job on the closed PR
gh run list --workflow deploy-frontend.yml --limit 10
```

If the cleanup job did not run (e.g., the PR was force-deleted):

```bash
# Manually trigger cleanup via Azure CLI
az staticwebapp environment delete \
  --name innoventity-dev-web \
  --resource-group innoventity-dev-rg \
  --environment-name <pr-environment-name>
```

---

### SOP-4: Rotate the SWA Deployment Token

The deployment token (`AZURE_STATIC_WEB_APPS_API_TOKEN`) does not expire automatically. Rotate it if the repository is compromised or the token is leaked.

```bash
# Step 1: Retrieve new token from Terraform
cd infrastructure/environments/dev/core
terraform output -raw static_web_app_api_key

# Step 2: Update the GitHub secret
gh secret set AZURE_STATIC_WEB_APPS_API_TOKEN \
  --body "$(terraform output -raw static_web_app_api_key)"

# Step 3: Verify the secret is updated
gh secret list
# AZURE_STATIC_WEB_APPS_API_TOKEN should show a recent "Updated" timestamp
```

If the token was compromised, also rotate it in the Azure Portal:
1. Azure Portal → Static Web Apps → `innoventity-dev-web` → Manage deployment token → Reset token.
2. Repeat Step 1 and 2 above with the new token.

---

### SOP-5: Roll Back Production to Previous Version

Azure Static Web Apps does not support slot swapping or versioned rollback natively on the Free tier. To roll back:

**Option A — Revert via git (recommended)**:

```bash
# Find the commit to roll back to
git log --oneline src/Innoventity.Client/

# Revert the offending commit
git revert <commit-hash> --no-edit
git push origin Main
# Triggers new deployment → approval gate → new production deploy
```

**Option B — Re-deploy from a prior artifact**:

```bash
# Re-run a previous workflow that had a good build artifact
gh run list --workflow deploy-frontend.yml --status success --limit 10

# Re-run the chosen run ID (note: this re-triggers the approval gate)
gh run rerun <run-id>
```

---

## Troubleshooting

### Error: "Deployment quota exceeded" (FR-043)

**Symptom**: `deploy-preview` fails with SWA quota error.

**Cause**: SWA Free tier allows maximum 3 preview environments. A 4th PR triggered a deploy.

**Resolution**: Follow SOP-3. Close one stale PR to free a preview slot. Do NOT add `continue-on-error` to the deploy-preview job — quota exhaustion is a signal requiring action, not a warning to suppress.

---

### Error: "Validation failed after 3 attempts — HTTP 404"

**Symptom**: `validate` job fails after deployment.

**Cause**: SWA CDN has not propagated the new deployment, or `staticwebapp.config.json` routing is broken.

**Resolution**:
1. Check `src/Innoventity.Client/staticwebapp.config.json` — the navigation fallback must point to `index.html`.
2. Wait 2–5 minutes for CDN propagation and manually trigger a new workflow run.
3. If the issue persists, check the SWA deployment logs in the Azure Portal.

---

### Error: "Workflow paused — approval timeout exceeded"

**Symptom**: `deploy-production` fails with "Timed out waiting for a reviewer to approve the deployment to production".

**Cause**: No reviewer approved within 1440 minutes (24 hours).

**Resolution**: Re-run the workflow from GitHub Actions → Deploy Frontend → Re-run failed jobs. The 24-hour clock resets on re-run.

---

### Error: "Secret AZURE_STATIC_WEB_APPS_API_TOKEN not found or invalid"

**Symptom**: Both `deploy-preview` and `deploy-production` fail with authentication error.

**Cause**: The secret is expired, not set, or was deleted.

**Resolution**: Follow SOP-4 to retrieve and reset the token.

---

### Error: "deploy-production not triggered after merging PR"

**Symptom**: PR was merged to Main but `deploy-production` job was not triggered or was skipped.

**Cause**: Possible causes:
- Changes did not touch `src/Innoventity.Client/**` (path filter in `on.push.paths`).
- The merge did not generate a push event to Main (e.g., squash merge — verify branch protection settings).
- The workflow was triggered by a `pull_request` event, not a `push` event.

**Resolution**: Verify that `github.event_name == 'push'` in the workflow log. Manually trigger via `gh workflow run deploy-frontend.yml --ref Main`.

---

## Environment Reference

| Item | Value |
|------|-------|
| Production URL | `https://innoventity-dev-web.azurestaticapps.net` |
| Azure resource name | `innoventity-dev-web` |
| Azure resource group | `innoventity-dev-rg` |
| GitHub Environment | `production` |
| Approval timeout | 1440 minutes (24 hours) |
| Preview quota | 3 environments max |
| GitHub secret | `AZURE_STATIC_WEB_APPS_API_TOKEN` |
| Terraform output | `terraform output -raw static_web_app_api_key` (in `infrastructure/environments/dev/core`) |
| Deployment token rotation | Manual — SOP-4 |
| Log retention | 90 days (GitHub Actions artifacts) |

---

## Related Documents

- [spec.md](../spec.md) — Feature requirements
- [tasks.md](../tasks.md) — Implementation tasks
- [quickstart.md](../quickstart.md) — First-time setup guide
- [infrastructure/README.md](../../../infrastructure/README.md) — Terraform creation workflow
```

**Verification**: File exists at `specs/002-frontend-cicd/runbooks/deployment.md` and renders correctly in GitHub.

---

### T066: Update infrastructure/README.md with SWA module documentation

**File**: `infrastructure/README.md` (modify)

**Requirements**: FR-020, FR-025, FR-026

**Dependencies**: T006–T011

**Before** (Directory Structure section, modules block):

```markdown
```
infrastructure/
├── modules/
│   ├── app-service/       # Azure App Service Plan + Linux Web App + staging slot
│   ├── sql-database/      # Azure SQL Server + Database + firewall rules
│   └── monitoring/        # Log Analytics Workspace + Application Insights
├── environments/
```
```

**After**:

```markdown
```
infrastructure/
├── modules/
│   ├── app-service/       # Azure App Service Plan + Linux Web App + staging slot
│   ├── sql-database/      # Azure SQL Server + Database + firewall rules
│   ├── monitoring/        # Log Analytics Workspace + Application Insights
│   └── static-web-app/    # Azure Static Web Apps (Angular SPA, Free tier)
├── environments/
```
```

**Before** (Step 2 — Apply core layer, after `terraform apply tfplan` line):

```markdown
### Step 3 — Apply EF Core migrations
```

**After**:

```markdown
### Step 2.5 — Extract SWA deployment token

After applying the core layer, extract the Static Web Apps deployment token and store it as a GitHub secret:

```powershell
# Extract deployment token (sensitive — do not log or commit)
$SWA_TOKEN = terraform output -raw static_web_app_api_key

# Store as GitHub repository secret
gh secret set AZURE_STATIC_WEB_APPS_API_TOKEN --body "$SWA_TOKEN"

# Verify
gh secret list
# Expected: AZURE_STATIC_WEB_APPS_API_TOKEN  Updated ...
```

The `AZURE_STATIC_WEB_APPS_API_TOKEN` secret is required by `.github/workflows/deploy-frontend.yml`. Without it, all deploy-preview and deploy-production jobs will fail with authentication error.

---

### Step 3 — Apply EF Core migrations
```

**Verification**: `infrastructure/README.md` lists `static-web-app/` in the directory tree and includes Step 2.5 with token extraction instructions.

---

### T067: Update repository README with CI/CD pipeline status badge

**File**: `README.md` (modify)

**Requirements**: NFR-004 (visibility of deployment status)

**Dependencies**: T020 (deploy-frontend.yml exists and has runs)

**Before** (lines 1–3, the title and Phase 0 status line):

```markdown
# innoventity-prototype-redux

A .NET 8 Minimal API + Angular 19 SPA for the Innoventity platform — a B2B innovation marketplace connecting Idea Generators with Manufacturing, R&D, Sales/Marketing, and Investor partners.
```

**After**:

```markdown
# innoventity-prototype-redux

[![Deploy Frontend](https://github.com/khaledm/innoventity-prototype-redux/actions/workflows/deploy-frontend.yml/badge.svg)](https://github.com/khaledm/innoventity-prototype-redux/actions/workflows/deploy-frontend.yml)
[![Deploy API](https://github.com/khaledm/innoventity-prototype-redux/actions/workflows/deploy.yml/badge.svg)](https://github.com/khaledm/innoventity-prototype-redux/actions/workflows/deploy.yml)
[![Drift Detection](https://github.com/khaledm/innoventity-prototype-redux/actions/workflows/drift.yml/badge.svg)](https://github.com/khaledm/innoventity-prototype-redux/actions/workflows/drift.yml)

A .NET 8 Minimal API + Angular 19 SPA for the Innoventity platform — a B2B innovation marketplace connecting Idea Generators with Manufacturing, R&D, Sales/Marketing, and Investor partners.
```

**Verification**: README renders three green (or current-status) badge pills at the top of the file in GitHub.

---

### T068: Remove continue-on-error: true from test job

**File**: `.github/workflows/deploy-frontend.yml` (modify)

**Requirements**: FR-009 (Phase 2 enforcement — test failures block deployment)

**Dependencies**: T049–T061 (workflow is otherwise complete; this is the final enforcement change)

Apply the three changes below in **bottom-to-top order** (highest line first) to avoid shifting line references.

**Before** (Playwright E2E step, ~line 142):

```yaml
      - name: Run Playwright E2E tests
        working-directory: ${{ env.WORKING_DIR }}
        continue-on-error: true
        run: npm run e2e
```

**After**:

```yaml
      - name: Run Playwright E2E tests
        working-directory: ${{ env.WORKING_DIR }}
        run: npm run e2e
```

---

**Before** (Playwright install step, ~line 136):

```yaml
      - name: Install Playwright browsers
        working-directory: ${{ env.WORKING_DIR }}
        continue-on-error: true
        run: npx playwright install --with-deps chromium
```

**After**:

```yaml
      - name: Install Playwright browsers
        working-directory: ${{ env.WORKING_DIR }}
        run: npx playwright install --with-deps chromium
```

---

**Before** (Jest unit test step, ~line 127):

```yaml
      - name: Run Jest unit tests
        working-directory: ${{ env.WORKING_DIR }}
        # Phase 1: continue-on-error allows deployment even with test failures.
        # Remove continue-on-error in Phase 2 to enforce blocking (FR-009).
        continue-on-error: true
        run: npm run test:coverage -- --ci
```

**After**:

```yaml
      - name: Run Jest unit tests
        working-directory: ${{ env.WORKING_DIR }}
        run: npm run test:coverage -- --ci
```

**Verification**: All three `continue-on-error: true` lines are absent from the `test` job. A deliberate test failure now causes the `test` job to fail with exit code non-zero and blocks `deploy-preview` and `deploy-production`.

---

### T070: Validate quickstart.md steps end-to-end with fresh clone

**File**: (none — manual verification task)

**Requirements**: Principle 4 (spec drives implementation), SC-004

**Dependencies**: T065–T068

```bash
# Clone into a temp directory to simulate fresh start
git clone https://github.com/khaledm/innoventity-prototype-redux /tmp/innoventity-fresh
cd /tmp/innoventity-fresh

# Follow every step in specs/002-frontend-cicd/quickstart.md
# Note any steps that fail or are out of date
# Document any corrections needed back in quickstart.md
```

**Verification**: All steps in `quickstart.md` complete without error from a fresh clone. Document any corrections directly in `quickstart.md`.

---

### T071: Run constitution checklist validation (Principle 5 compliance)

**File**: (none — manual checklist task)

**Requirements**: Principle 5 (Tests Must Prove They Work)

**Dependencies**: T062–T064 (observe failure paths), T032 (act test), T043 (cleanup test)

Verify the following have been **observed failing** (not just passing):

| Check | Observed Failing? | Date |
|-------|-------------------|------|
| T032: `act` workflow fails when no artifact is present | | |
| T043: Preview URL returns 404 after cleanup-preview | | |
| T056: `deploy-production` fails when approval is rejected | | |
| T062: `validate` fails on HTTP 404 from broken deployment | | |

If any row is empty, execute the corresponding task before marking T071 complete.

**Verification**: All four rows have "Yes" and a date.

---

### T072: Verify all success criteria met (SC-001 through SC-010)

**File**: (none — verification checklist)

**Requirements**: All SCs in spec.md

**Dependencies**: All prior tasks

| Criterion | Evidence |
|-----------|----------|
| SC-001: Deploy to preview within 10 min | Measure from push to preview URL accessible in workflow logs |
| SC-002: PR preview URL within 5 min | Measure from PR open to bot comment in PR |
| SC-003: Production deploy within 5 min after approval | Measure from approval click to `validate` ✅ |
| SC-004: Zero manual `swa deploy` commands | Audit git history — no `swa deploy` command in any commit |
| SC-005: Terraform apply < 3 min, idempotent | `terraform apply` in `core/` twice: first < 3 min, second "No changes" |
| SC-006: Test results visible in workflow logs | Open any workflow run → Test job → logs show Jest/Playwright output |
| SC-007: Validation detects failures | T062 evidence — validation failed on broken build |
| SC-008: Frontend routes to backend API endpoints | Verify `environment.production.ts` has correct API URL, deployed app makes successful API calls |
| SC-009: Preview cleanup immediate on PR close | T043 evidence — preview URL 404 immediately after cleanup |
| SC-010: Error messages enable < 15 min resolution | Review error messages from T062 failure — are they actionable? |

**Verification**: All 10 rows have evidence. Document any gaps in `runbooks/deployment.md`.

---

### T073: Update .github/agents/copilot-instructions.md with CI/CD workflow patterns

**File**: `.github/agents/copilot-instructions.md` (modify)

**Requirements**: Principle 6 (AI tools must have context to assist correctly)

**Dependencies**: T049–T068 (workflow fully implemented)

**Before** (manual additions section):

```markdown
<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
```

**After**:

```markdown
<!-- MANUAL ADDITIONS START -->

## CI/CD Workflow Patterns (002-frontend-cicd)

### Frontend Pipeline Structure

Job chain (mutually exclusive paths):
- **PR path**: `build` → `test` → `deploy-preview` → `validate`
- **Main path**: `build` → `test` → `deploy-production` *(approval gate)* → `validate`
- **Independent**: `cleanup-preview` (triggers only on `pull_request: [closed]`)

### Required GitHub Secrets

| Secret | Source | Purpose |
|--------|--------|---------|
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | `terraform output -raw static_web_app_api_key` (core/) | Authenticates deploy actions to Azure SWA |
| `GITHUB_TOKEN` | Automatic (GitHub) | Allows GitHub bot to post preview URL PR comments |

### Required GitHub Environment

`production` environment must be configured:
- Required reviewer: `khaledm`
- Deployment branch: `Main` only
- Wait timer: 1440 minutes (24h approval timeout)

### SWA Free Tier Constraints

- Max 3 preview environments — `deploy-preview` FAILS HARD on quota exhaustion (FR-043)
- Do NOT add `continue-on-error` to deploy-preview — quota exhaustion requires human action
- Production environment maps to `Main` branch via `production_branch: Main` in the deploy action
- PR preview URL format: `https://innoventity-dev-web-<pr-number>.azurestaticapps.net`

### Conditional Execution Logic

- `deploy-preview`: `github.event_name == 'pull_request' && github.event.action != 'closed'`
- `deploy-production`: `github.event_name == 'push' && github.ref == 'refs/heads/Main'`
  - Uses `event_name == 'push'` (not just ref check) to exclude PR events targeting Main
- `validate`: `always() && (needs.deploy-preview.result == 'success' || needs.deploy-production.result == 'success')`
  - Uses `always()` so it runs even when one deploy job is skipped
- `cleanup-preview`: `github.event_name == 'pull_request' && github.event.action == 'closed'`
- `build` (and test): `github.event_name != 'pull_request' || github.event.action != 'closed'`
  - Skipped on PR close (cleanup path only)

### Test Enforcement

Phase 2 (current): `continue-on-error` removed — test failures block deployment (FR-009).
All three test steps (Jest, Playwright install, Playwright E2E) must pass for `deploy-*` jobs to run.

<!-- MANUAL ADDITIONS END -->
```

**Verification**: `.github/agents/copilot-instructions.md` renders the CI/CD section. AI agents referencing this file will understand the job conditional logic without re-reading the workflow YAML.

---

### T074: Final commit following Conventional Commits format

**File**: (none — git task)

**Requirements**: Principle 8 (Commit Messages Are Documentation)

**Dependencies**: All prior tasks complete

```bash
git add \
  .github/workflows/deploy-frontend.yml \
  .github/agents/copilot-instructions.md \
  infrastructure/README.md \
  README.md \
  specs/002-frontend-cicd/runbooks/deployment.md \
  specs/002-frontend-cicd/blueprint.md

git commit -m "feat(ci): complete frontend CI/CD automation

Implement remaining phases of 002-frontend-cicd:
- Add deploy-production job with GitHub Environment approval gate (US3)
- Add validate job with 3-retry HTTP health check (US5, FR-018)
- Remove continue-on-error from test job (Phase 2 enforcement, FR-009)
- Create deployment runbook with SOPs and troubleshooting guide (T065)
- Add CI/CD status badges to root README (T067)
- Document SWA module in infrastructure/README (T066)
- Update copilot-instructions.md with workflow patterns (T073)

Approval gate: GitHub Environment 'production', 1440-min timeout (FR-041).
Validation: curl with 3x10s retry, checks HTTP 200 + text/html (SC-007).
Test enforcement: Jest and Playwright now block deployment on failure.

Closes: T044-T074 (35 remaining tasks in 002-frontend-cicd)
Ref: specs/002-frontend-cicd/tasks.md"
```

**Verification**: `git log --oneline -1` shows the commit with the message above.

---

## Appendix: Final Consolidated deploy-frontend.yml

Complete file state after applying T049–T051, T057–T061, and T068. Replace entire file with this content.

```yaml
# .github/workflows/deploy-frontend.yml
#
# Frontend CI/CD pipeline — builds, tests, and deploys Angular 19 app to Azure Static Web Apps.
#
# Job chain: build → test → deploy-preview → validate         (PR path)
#             build → test → deploy-production → validate      (Main push path)
#             cleanup-preview                                   (PR closed — independent)
#
# Trigger:
#   push to any branch      — build/test validation when frontend code changes
#   pull_request            — preview deploy on open/sync/reopen; cleanup on close
#   workflow_dispatch       — manual build/test (bypasses path filter)
#
# Phase 2 behavior (continue-on-error removed per T068):
#   - Test failures BLOCK deployment (FR-009)
#   - deploy-preview runs on pull_request events only (SWA Free tier: PR previews only)
#   - deploy-production runs on push to Main with manual approval gate (FR-015)
#   - validate runs after whichever deploy succeeded (SC-007)
#
# Required secrets:
#   AZURE_STATIC_WEB_APPS_API_TOKEN  — from: terraform output -raw static_web_app_api_key
#
# Required GitHub Environment:
#   production — required reviewers + deployment branch Main + 1440-min timeout
#
# SWA Free tier limits (FR-043):
#   - 1 production environment
#   - 3 preview environments max; deploy-preview FAILS HARD when quota exhausted
#     (do NOT add continue-on-error — failure is the intended signal to clean up PRs)

name: Deploy Frontend

permissions:
  contents: read
  pull-requests: write   # Required for GitHub bot to post preview URL as PR comment

on:
  push:
    branches:
      - '**'
    paths:
      - 'src/Innoventity.Client/**'
      - '.github/workflows/deploy-frontend.yml'
  pull_request:
    types: [opened, synchronize, reopened, closed]
    branches:
      - Main
  workflow_dispatch:
    inputs:
      environment:
        description: 'Target environment (dev only in Phase 1)'
        required: false
        default: 'dev'
        type: choice
        options:
          - dev

env:
  NODE_VERSION: '20.x'
  WORKING_DIR: 'src/Innoventity.Client'

jobs:
  # ─────────────────────────────────────────────────────────────────
  # 1. Build — compile Angular app with production configuration
  # ─────────────────────────────────────────────────────────────────
  build:
    name: Build
    runs-on: ubuntu-latest
    # Skip build-test-deploy when PR is closed (only cleanup job needed)
    if: github.event_name != 'pull_request' || github.event.action != 'closed'
    steps:
      - name: Checkout repository
        uses: actions/checkout@v6

      - name: Setup Node.js ${{ env.NODE_VERSION }}
        uses: actions/setup-node@v6
        with:
          node-version: ${{ env.NODE_VERSION }}
          cache: 'npm'
          cache-dependency-path: ${{ env.WORKING_DIR }}/package-lock.json

      - name: Install dependencies
        working-directory: ${{ env.WORKING_DIR }}
        run: npm ci

      - name: Build Angular app (production)
        working-directory: ${{ env.WORKING_DIR }}
        run: npm run build -- --configuration production

      - name: Upload build artifact
        uses: actions/upload-artifact@v7
        with:
          name: frontend-build
          # Angular 17+ application builder outputs to dist/<name>/browser/
          path: ${{ env.WORKING_DIR }}/dist/innoventity.client/browser
          retention-days: 90
          if-no-files-found: error

  # ─────────────────────────────────────────────────────────────────
  # 2. Test — run unit and E2E tests (Phase 2: failures block deployment)
  # ─────────────────────────────────────────────────────────────────
  test:
    name: Test
    runs-on: ubuntu-latest
    needs: build
    steps:
      - name: Checkout repository
        uses: actions/checkout@v6

      - name: Setup Node.js ${{ env.NODE_VERSION }}
        uses: actions/setup-node@v6
        with:
          node-version: ${{ env.NODE_VERSION }}
          cache: 'npm'
          cache-dependency-path: ${{ env.WORKING_DIR }}/package-lock.json

      - name: Install dependencies
        working-directory: ${{ env.WORKING_DIR }}
        run: npm ci

      - name: Run Jest unit tests
        working-directory: ${{ env.WORKING_DIR }}
        run: npm run test:coverage -- --ci

      - name: Install Playwright browsers
        working-directory: ${{ env.WORKING_DIR }}
        run: npx playwright install --with-deps chromium

      - name: Run Playwright E2E tests
        working-directory: ${{ env.WORKING_DIR }}
        run: npm run e2e

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v7
        with:
          name: test-results-${{ github.run_id }}
          path: |
            ${{ env.WORKING_DIR }}/coverage/
            ${{ env.WORKING_DIR }}/test-results/
            ${{ env.WORKING_DIR }}/playwright-report/
          retention-days: 90
          if-no-files-found: ignore

  # ─────────────────────────────────────────────────────────────────
  # 3. Deploy Preview — deploy to SWA preview environment (PR events)
  #    Skipped on push and workflow_dispatch events.
  #    SWA Free tier: max 3 preview environments. Workflow FAILS HARD when
  #    quota is exhausted — close a stale PR to free a slot (FR-043).
  # ─────────────────────────────────────────────────────────────────
  deploy-preview:
    name: Deploy Preview
    runs-on: ubuntu-latest
    needs: [build, test]
    # Run on PRs only — SWA Free tier only supports PR-based preview environments.
    # Named branch environments (push/workflow_dispatch on non-Main) require Standard tier.
    if: github.event_name == 'pull_request' && github.event.action != 'closed'
    outputs:
      preview-url: ${{ steps.deploy.outputs.static_web_app_url }}
    steps:
      - name: Checkout repository
        uses: actions/checkout@v6

      - name: Download build artifact
        uses: actions/download-artifact@v8
        with:
          name: frontend-build
          path: dist

      - name: Deploy to Azure Static Web Apps preview
        id: deploy
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: upload
          # app_location is relative to repo root; artifact already built — skip SWA build
          app_location: dist
          skip_app_build: true
          output_location: ''
          # production_branch tells SWA which branch maps to the production environment.
          # All other branches (including this one) are routed to preview environments.
          production_branch: Main

  # ─────────────────────────────────────────────────────────────────
  # 4. Deploy Production — deploy directly to production environment
  #    Main branch push (including PR merge) only. Requires manual approval
  #    via GitHub Environment "production" protection rule (FR-015, FR-041).
  #    24-hour approval timeout — auto-fails if not approved (FR-041).
  #    Uses event_name == 'push' to avoid triggering on pull_request events
  #    targeting Main (open/sync/reopen emit pull_request, not push).
  # ─────────────────────────────────────────────────────────────────
  deploy-production:
    name: Deploy Production
    runs-on: ubuntu-latest
    needs: [build, test]
    if: github.event_name == 'push' && github.ref == 'refs/heads/Main'
    environment: production
    outputs:
      production-url: ${{ steps.deploy.outputs.static_web_app_url }}
    steps:
      - name: Checkout repository
        uses: actions/checkout@v6

      - name: Download build artifact
        uses: actions/download-artifact@v8
        with:
          name: frontend-build
          path: dist

      - name: Deploy to Azure Static Web Apps production
        id: deploy
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: upload
          app_location: dist
          skip_app_build: true
          output_location: ''
          production_branch: Main

  # ─────────────────────────────────────────────────────────────────
  # 5. Validate — HTTP health check on the deployed URL (FR-018, SC-007).
  #    Runs after whichever deploy job succeeded (preview XOR production).
  #    always() + result checks allow the job to run even if the other
  #    deploy job was skipped (GitHub treats skipped as non-failure).
  #    3 retry attempts with 10-second delay handle CDN propagation lag.
  #    Fails workflow on non-200 or non-text/html response.
  # ─────────────────────────────────────────────────────────────────
  validate:
    name: Validate Deployment
    runs-on: ubuntu-latest
    needs: [deploy-preview, deploy-production]
    if: |
      always() &&
      (needs.deploy-preview.result == 'success' || needs.deploy-production.result == 'success')
    steps:
      - name: Determine deployment URL
        id: url
        run: |
          PROD_URL="${{ needs.deploy-production.outputs.production-url }}"
          PREV_URL="${{ needs.deploy-preview.outputs.preview-url }}"
          if [ -n "$PROD_URL" ]; then
            echo "url=$PROD_URL" >> $GITHUB_OUTPUT
            echo "Validating production URL: $PROD_URL"
          else
            echo "url=$PREV_URL" >> $GITHUB_OUTPUT
            echo "Validating preview URL: $PREV_URL"
          fi

      - name: Health check with retry (3 × 10s)
        run: |
          URL="${{ steps.url.outputs.url }}"
          SUCCESS=0
          for ATTEMPT in 1 2 3; do
            HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" --max-time 30 "$URL")
            CONTENT_TYPE=$(curl -s -o /dev/null -w "%{content_type}" --max-time 30 "$URL")
            echo "Attempt $ATTEMPT/3: HTTP $HTTP_STATUS | Content-Type: $CONTENT_TYPE | URL: $URL"
            if [ "$HTTP_STATUS" = "200" ] && echo "$CONTENT_TYPE" | grep -qi "text/html"; then
              echo "✅ Validation passed on attempt $ATTEMPT."
              SUCCESS=1
              break
            fi
            if [ $ATTEMPT -lt 3 ]; then
              echo "Retrying in 10 seconds..."
              sleep 10
            fi
          done
          if [ $SUCCESS -eq 0 ]; then
            echo "::error::Deployment validation failed after 3 attempts. URL: $URL returned HTTP $HTTP_STATUS (Content-Type: $CONTENT_TYPE). Expected: HTTP 200, text/html."
            exit 1
          fi

  # ─────────────────────────────────────────────────────────────────
  # 6. Cleanup Preview — delete SWA preview environment on PR close/merge
  #    Triggered by pull_request: [closed] to prevent quota exhaustion.
  #    Immediate deletion aligns with FR-037 (Q1 decision: delete on close).
  # ─────────────────────────────────────────────────────────────────
  cleanup-preview:
    name: Cleanup Preview
    runs-on: ubuntu-latest
    if: github.event_name == 'pull_request' && github.event.action == 'closed'
    steps:
      - name: Checkout repository
        uses: actions/checkout@v6

      - name: Delete Azure Static Web Apps preview environment
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          action: close
```

---

## Checklist

- [ ] T032: Test workflow locally using act
- [ ] T043: Test cleanup — close PR and verify preview deleted
- [ ] T044: Create GitHub Environment via CLI
- [ ] T045: Configure production environment protection rules
- [ ] T046: Add required reviewers to production environment
- [ ] T047: Set deployment branches to Main only
- [ ] T048: Configure approval timeout to 1440 minutes
- [ ] T049: Define deploy-production job with conditional execution
- [ ] T050: Add environment: production to job (implemented in T049)
- [ ] T051: Implement deploy-production job steps (implemented in T049)
- [ ] T052: Test approval workflow — trigger merge to Main
- [ ] T053: Verify workflow pauses at approval gate
- [ ] T054: Approve deployment and verify production deploy completes
- [ ] T055: Verify production URL serves new version
- [ ] T056: Test approval rejection — verify no deployment
- [ ] T057: Define validate job with retry and URL-selection logic
- [ ] T058: Implement HTTP GET request (implemented in T057)
- [ ] T059: Add HTTP 200 and text/html checks (implemented in T057)
- [ ] T060: Add 3 × 10s retry logic (implemented in T057)
- [ ] T061: Configure fail-on-error via exit 1 (implemented in T057)
- [ ] T062: Test validation failure — intentionally broken build
- [ ] T063: Verify validate job fails and prevents production approval
- [ ] T064: Test validation success — working build
- [ ] T065: Create deployment runbook at specs/002-frontend-cicd/runbooks/deployment.md
- [ ] T066: Update infrastructure/README.md with SWA module and Step 2.5
- [ ] T067: Add CI/CD status badges to README.md
- [ ] T068: Remove continue-on-error from all three test job steps
- [X] T069: Add npm dependency caching — already complete (actions/setup-node cache: npm)
- [ ] T070: Validate quickstart.md steps end-to-end
- [ ] T071: Run constitution checklist validation (Principle 5)
- [ ] T072: Verify all success criteria SC-001 through SC-010
- [ ] T073: Update .github/agents/copilot-instructions.md with CI/CD patterns
- [ ] T074: Final commit (Conventional Commits format)
